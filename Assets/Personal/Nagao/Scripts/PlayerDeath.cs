using UnityEngine;

/// <summary>
///     落下を進行中の試合へ渡し、単体テスト時は既存の地点へ戻す。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerDeath : MonoBehaviour
{
    [SerializeField, Tooltip("試合管理を使用しない場合の復活地点。")]
    private Transform respawnPoint;

    private Rigidbody _rigidbody;

    /// <summary>
    ///     試合管理へ落下を通知する。登録外のプレイヤーは復活地点へ戻す。
    /// </summary>
    public void Death()
    {
        GameManager game = GameManager.Instance;
        if (game != null && game.TryHandlePlayerFall(gameObject))
        {
            return;
        }

        if (respawnPoint != null)
        {
            _rigidbody.position = respawnPoint.position;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }
}
