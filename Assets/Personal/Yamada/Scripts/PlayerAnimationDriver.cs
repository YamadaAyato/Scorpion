using UnityEngine;

namespace Scorpion.Player
{
    /// <summary>
    ///     プレイヤーの状態を Animator に反映し、星の見た目を相手側に向ける
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMove), typeof(Animator))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField, Tooltip("見た目の向きだけを変える、星モデルのルートボーン")]
        private Transform _facingBone;
        [SerializeField, Range(0f, 90f), Tooltip("正面から対戦相手側へ向ける角度")]
        private float _facingAngle = 45f;

        private PlayerMove _movement;
        private PlayerAttack _attack;
        private Animator _animator;
        private Quaternion _animationPose;
        private bool _hasAppliedFacing;
        private int _facingDirection;

        private static readonly int IS_MOVING = Animator.StringToHash("IsMoving");
        private static readonly int IS_AIRBORNE = Animator.StringToHash("IsAirborne");
        private static readonly int JUMP = Animator.StringToHash("Jump");
        private static readonly int ATTACK = Animator.StringToHash("Attack");
        private const float FACING_DEAD_ZONE = 0.05f;

        private void Awake()
        {
            _movement = GetComponent<PlayerMove>();
            _animator = GetComponent<Animator>();
            TryGetComponent(out _attack);
        }

        private void OnEnable()
        {
            _movement.OnJumpStarted += HandleJumpStarted;
            if (_attack != null)
            {
                _attack.OnAttackStarted += HandleAttackStarted;
            }
        }

        private void Update()
        {
            RestoreAnimationPose();
            _animator.SetBool(IS_MOVING, _movement.IsMoving);
            _animator.SetBool(IS_AIRBORNE, !_movement.IsGrounded);
        }

        private void LateUpdate()
        {
            if (_facingBone == null)
            {
                return;
            }

            GameManager game = GameManager.Instance;
            GameObject opponent = game != null ? game.GetOpponent(gameObject) : null;
            if (opponent != null && opponent.activeInHierarchy)
            {
                float distanceX = opponent.transform.position.x - transform.position.x;
                if (Mathf.Abs(distanceX) > FACING_DEAD_ZONE)
                {
                    _facingDirection = distanceX > 0f ? 1 : -1;
                }
            }

            if (_facingDirection != 0)
            {
                // Animator が作った姿勢に向きだけを重ね、物理ルートは回転させない
                _animationPose = _facingBone.localRotation;
                Quaternion facing = Quaternion.AngleAxis(-_facingDirection * _facingAngle, Vector3.up);
                _facingBone.localRotation = facing * _animationPose;
                _hasAppliedFacing = true;
            }
        }

        private void OnDisable()
        {
            RestoreAnimationPose();
            _movement.OnJumpStarted -= HandleJumpStarted;
            if (_attack != null)
            {
                _attack.OnAttackStarted -= HandleAttackStarted;
            }
        }

        /// <summary>
        ///     成立したジャンプを Animator に通知する
        /// </summary>
        private void HandleJumpStarted()
        {
            _animator.SetBool(IS_MOVING, false);
            _animator.SetBool(IS_AIRBORNE, true);
            _animator.SetTrigger(JUMP);
        }

        /// <summary>
        ///     クールタイムを通過した攻撃を Animator に通知する
        /// </summary>
        private void HandleAttackStarted()
        {
            _animator.SetTrigger(ATTACK);
        }

        /// <summary>
        ///     前フレームの向きを取り除き、Animator が評価する前の姿勢に戻す
        /// </summary>
        private void RestoreAnimationPose()
        {
            if (_hasAppliedFacing && _facingBone != null)
            {
                _facingBone.localRotation = _animationPose;
                _hasAppliedFacing = false;
            }
        }
    }
}
