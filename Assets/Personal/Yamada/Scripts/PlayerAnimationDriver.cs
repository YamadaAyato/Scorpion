using UnityEngine;

namespace Scorpion.Player
{
    /// <summary>
    ///     プレイヤーの移動・接地状態と実際に開始したアクションを Animator に反映する
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMove), typeof(Animator))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private PlayerMove _movement;
        private PlayerAttack _attack;
        private Animator _animator;

        private static readonly int IS_MOVING = Animator.StringToHash("IsMoving");
        private static readonly int IS_AIRBORNE = Animator.StringToHash("IsAirborne");
        private static readonly int JUMP = Animator.StringToHash("Jump");
        private static readonly int ATTACK = Animator.StringToHash("Attack");

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
            _animator.SetBool(IS_MOVING, _movement.IsMoving);
            _animator.SetBool(IS_AIRBORNE, !_movement.IsGrounded);
        }

        private void OnDisable()
        {
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
    }
}
