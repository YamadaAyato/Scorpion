using System.Collections.Generic;
using Scorpion.Gimmicks;
using UnityEngine;

/// <summary>
///     落下を進行中の試合へ渡し、単体テスト時は既存の地点へ戻す
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerDeath : MonoBehaviour
{
    /// <summary> リスポーン直後の無敵が有効かどうかを取得する </summary>
    public bool IsRespawnProtected { get; private set; }

    [SerializeField, Tooltip("試合管理を使用しない場合の復活地点")]
    private Transform _respawnPoint;
    [SerializeField, Min(0f), Tooltip("リスポーン後の無敵時間（秒）")]
    private float _respawnProtectionSeconds = 2f;
    [SerializeField, Min(0.01f), Tooltip("無敵中の点滅の切り替え間隔（秒）")]
    private float _blinkIntervalSeconds = 0.12f;

    private static readonly HashSet<PlayerDeath> _protectedPlayers = new();
    private readonly List<(Collider Player, Collider Obstacle)> _ignoredCollisions = new();
    private Collider[] _bodyColliders;
    private Renderer[] _blinkRenderers;
    private Rigidbody _rigidbody;
    private float _protectionEndsAt;

    /// <summary>
    ///     試合管理へ落下を通知する。登録外のプレイヤーは復活地点へ戻す
    /// </summary>
    public void Death()
    {
        GameManager game = GameManager.Instance;
        if (game != null && game.TryHandlePlayerFall(gameObject))
        {
            return;
        }

        if (_respawnPoint != null)
        {
            _rigidbody.position = _respawnPoint.position;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            StartRespawnProtection();
        }
    }

    /// <summary>
    ///     復活直後から指定秒数だけ無敵にし、すでに存在する妨害物との衝突を無視する
    /// </summary>
    public void StartRespawnProtection()
    {
        StopRespawnProtection();
        if (!isActiveAndEnabled || _respawnProtectionSeconds <= 0f)
        {
            return;
        }

        IsRespawnProtected = true;
        _protectionEndsAt = Time.time + _respawnProtectionSeconds;
        _protectedPlayers.Add(this);
        foreach (AttackableObstacle obstacle in FindObjectsByType<AttackableObstacle>(FindObjectsSortMode.None))
        {
            IgnoreObstacle(obstacle.GetComponent<Rigidbody>());
        }

        foreach (ExplosiveObstacle obstacle in FindObjectsByType<ExplosiveObstacle>(FindObjectsSortMode.None))
        {
            IgnoreObstacle(obstacle.GetComponent<Rigidbody>());
        }
    }

    /// <summary>
    ///     無敵中に出現した妨害物にも衝突無視を適用する
    /// </summary>
    internal static void RegisterObstacle(Rigidbody obstacle)
    {
        foreach (PlayerDeath player in _protectedPlayers)
        {
            player.IgnoreObstacle(obstacle);
        }
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _bodyColliders = GetComponentsInChildren<Collider>();
        _blinkRenderers = System.Array.FindAll(GetComponentsInChildren<Renderer>(true),
            renderer => renderer is MeshRenderer or SkinnedMeshRenderer or SpriteRenderer && !renderer.forceRenderingOff);
    }

    private void Update()
    {
        if (!IsRespawnProtected)
        {
            return;
        }

        // 接地の有無と切り離し、復活からの経過時間で点滅と無敵を終える
        float remaining = _protectionEndsAt - Time.time;
        if (remaining <= 0f)
        {
            StopRespawnProtection();
            return;
        }

        float elapsed = _respawnProtectionSeconds - remaining;
        bool hidden = Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, _blinkIntervalSeconds)) % 2 != 0;
        foreach (Renderer renderer in _blinkRenderers)
        {
            if (renderer != null)
            {
                renderer.forceRenderingOff = hidden;
            }
        }
    }

    private void OnDisable()
    {
        StopRespawnProtection();
    }

    /// <summary>
    ///     妨害物とプレイヤー本体のコライダーの組だけを衝突対象から除く
    /// </summary>
    private void IgnoreObstacle(Rigidbody obstacle)
    {
        foreach (Collider other in obstacle.GetComponentsInChildren<Collider>())
        {
            if (!other.enabled || other.isTrigger || other.attachedRigidbody != obstacle)
            {
                continue;
            }

            foreach (Collider body in _bodyColliders)
            {
                if (body != null && body.enabled && !body.isTrigger && body.attachedRigidbody == _rigidbody &&
                    !Physics.GetIgnoreCollision(body, other))
                {
                    // 今回無視した組だけを記録し、元から無視されていた組には触れない
                    Physics.IgnoreCollision(body, other);
                    _ignoredCollisions.Add((body, other));
                }
            }
        }
    }

    /// <summary>
    ///     点滅を終え、無敵によって無視していた衝突を復元する
    /// </summary>
    private void StopRespawnProtection()
    {
        if (!IsRespawnProtected)
        {
            return;
        }

        IsRespawnProtected = false;
        _protectedPlayers.Remove(this);
        foreach ((Collider player, Collider obstacle) in _ignoredCollisions)
        {
            if (player != null && obstacle != null)
            {
                Physics.IgnoreCollision(player, obstacle, false);
            }
        }

        _ignoredCollisions.Clear();
        foreach (Renderer renderer in _blinkRenderers)
        {
            if (renderer != null)
            {
                renderer.forceRenderingOff = false;
            }
        }
    }
}
