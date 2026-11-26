using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     攻撃で弾かれた落下物の勢いを、衝突したプレイヤーへ伝える
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AttackableObstacle : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f), Tooltip("衝突直前の速度をプレイヤーに伝える割合")]
        private float _impactTransfer = 0.85f;
        [SerializeField, Min(0f), Tooltip("落下物に当たったプレイヤーの操作不能時間")]
        private float _stunSeconds = 0.35f;

        private Rigidbody _body;
        private Rigidbody _launcher;
        private Vector3 _incomingVelocity;
        private bool _canTransferImpact;

        private const float MIN_IMPACT_SPEED = 2f;
        private const float MAX_LIFT_SPEED = 5f;

        /// <summary>
        ///     プレイヤーの攻撃で落下物を弾き、次の衝突に備える
        /// </summary>
        public void Launch(Rigidbody launcher, Vector3 velocity)
        {
            _launcher = launcher;
            _incomingVelocity = velocity;
            _canTransferImpact = true;
            _body.linearVelocity = velocity;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_canTransferImpact)
            {
                _incomingVelocity = _body.linearVelocity;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Rigidbody target = collision.rigidbody;
            if (!_canTransferImpact || target == null || target == _launcher ||
                !target.TryGetComponent(out PlayerMove player) ||
                _incomingVelocity.sqrMagnitude < MIN_IMPACT_SPEED * MIN_IMPACT_SPEED ||
                Vector3.Dot(_incomingVelocity, target.worldCenterOfMass - _body.worldCenterOfMass) <= 0f)
            {
                return;
            }

            _canTransferImpact = false;
            player.ApplyStun(_stunSeconds);

            // 物理衝突だけで十分に加速している方向はそのまま使い、足場へ押し込まないよう上へ逃がす
            Vector3 velocity = target.linearVelocity;
            float horizontal = _incomingVelocity.x * _impactTransfer;
            if (horizontal > 0f)
            {
                velocity.x = Mathf.Max(velocity.x, horizontal);
            }
            else if (horizontal < 0f)
            {
                velocity.x = Mathf.Min(velocity.x, horizontal);
            }

            float lift = Mathf.Min(_incomingVelocity.magnitude * _impactTransfer * 0.5f, MAX_LIFT_SPEED);
            velocity.y = Mathf.Max(velocity.y, lift);
            target.linearVelocity = velocity;
        }
    }
}
