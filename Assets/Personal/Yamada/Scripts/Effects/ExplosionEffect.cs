using System.Reflection;
using Scorpion.Gimmicks;
using UnityEngine;

namespace Scorpion.Effects
{
    /// <summary>
    ///     爆発する落下物（ExplosiveObstacle）が爆発した瞬間に、ポップな爆発エフェクトを出す
    /// </summary>
    /// <remarks>
    ///     ExplosiveObstacle は変更せず、爆発時に必ず行われる「当たり判定の無効化」を見て爆発を検知する。
    ///     エフェクトの大きさは ExplosiveObstacle の爆風半径に自動で合わせる。
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ExplosiveObstacle), typeof(Rigidbody))]
    public sealed class ExplosionEffect : MonoBehaviour
    {
        [Header("マテリアル")]
        [SerializeField, Tooltip("火の玉・雲・破片・火の粉に使うマテリアル（Tex_Boom の 2×2 アトラス）")]
        private Material _boomMaterial;
        [SerializeField, Tooltip("衝撃線に使うマテリアル")]
        private Material _lineMaterial;

        [Header("既存のリング")]
        [SerializeField, Tooltip("ExplosiveObstacle が出す細いオレンジのリングを隠し、このエフェクトに置き換えるか")]
        private bool _hideOriginalRing = true;

        [SerializeField]
        private ExplosionEffectSettings _settings = new();

        private ExplosiveObstacle _obstacle;
        private Rigidbody _body;
        private bool _hasPlayed;
        private LineRenderer _originalRing;

        private static bool s_isQuitting;
        private static readonly FieldInfo RADIUS_FIELD =
            typeof(ExplosiveObstacle).GetField("_radius", BindingFlags.Instance | BindingFlags.NonPublic);

        private const float FALLBACK_RADIUS = 3f;
        private const string ORIGINAL_RING_NAME = "Explosion Ring";

        /// <summary>
        ///     爆風の当たり判定の半径。ExplosiveObstacle の設定値を読む
        /// </summary>
        public float BlastRadius =>
            RADIUS_FIELD != null && _obstacle != null && RADIUS_FIELD.GetValue(_obstacle) is float radius
                ? radius
                : FALLBACK_RADIUS;

        /// <summary>
        ///     今の位置でエフェクトだけを再生する（爆風は起きない）
        /// </summary>
        [ContextMenu("テスト再生（エフェクトだけ）")]
        public void PlayPreview()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{nameof(ExplosionEffect)}: テスト再生はプレイ中に行ってください", this);
                return;
            }

            Awake();
            Vector3 center = _body.worldCenterOfMass;
            Spawn(center);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_isQuitting = false;
            Application.quitting -= HandleQuitting;
            Application.quitting += HandleQuitting;
        }

        private static void HandleQuitting()
        {
            s_isQuitting = true;
        }

        private void Awake()
        {
            _obstacle = GetComponent<ExplosiveObstacle>();
            _body = GetComponent<Rigidbody>();
        }

        private void LateUpdate()
        {
            TryPlay();
            if (_hasPlayed && _hideOriginalRing)
            {
                HideOriginalRing();
            }
        }

        private void OnDestroy()
        {
            // 爆発と同じフレームに破棄された場合（リングの材質が未設定のとき）も取りこぼさない
            if (!s_isQuitting && gameObject.scene.isLoaded)
            {
                TryPlay();
            }
        }

        /// <summary>
        ///     ExplosiveObstacle は爆発した瞬間に当たり判定を切るので、それを爆発の合図にする
        /// </summary>
        private void TryPlay()
        {
            if (_hasPlayed || _body == null || _body.detectCollisions)
            {
                return;
            }

            _hasPlayed = true;
            Spawn(_body.worldCenterOfMass);
        }

        private void Spawn(Vector3 center)
        {
            float radius = BlastRadius * _settings.RadiusScale;
            bool fanUp = _settings.FanUpOnGround && IsOnGround(center);
            ExplosionBurst.Spawn(center, radius, fanUp, _settings, _boomMaterial, _lineMaterial);
        }

        /// <summary>
        ///     爆心のすぐ下に Ground タグの足場があるか
        /// </summary>
        private bool IsOnGround(Vector3 center)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(center, Vector3.down, _settings.GroundCheckDistance, ~0,
                         QueryTriggerInteraction.Ignore))
            {
                if (hit.rigidbody != _body && hit.collider.CompareTag("Ground"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     ExplosiveObstacle が子に作るリングを見えなくする
        /// </summary>
        private void HideOriginalRing()
        {
            if (_originalRing == null)
            {
                Transform ring = transform.Find(ORIGINAL_RING_NAME);
                if (ring == null || !ring.TryGetComponent(out _originalRing))
                {
                    return;
                }
            }

            _originalRing.enabled = false;
        }
    }
}
