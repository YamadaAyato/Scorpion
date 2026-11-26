using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
///     接地状態に応じてプレイヤーを移動させ、ジャンプと攻撃の入力を処理する
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    /// <summary> 接地状態からジャンプが成立したときに通知する </summary>
    public event Action OnJumpStarted;

    /// <summary> 足元が地面に接しているかを取得する </summary>
    public bool IsGrounded => _groundContacts.Count > 0;
    /// <summary> 接地中に横方向へ移動しているかを取得する </summary>
    public bool IsMoving => IsGrounded && _isInputEnabled && !_isStunned && Mathf.Abs(_rigidbody.linearVelocity.x) > 0.05f;

    /// <summary> 被弾硬直中かどうかを取得する </summary>
    public bool IsStunned => _isStunned;

    [Header("移動設定")]
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField, Tooltip("地上での横移動速度")]
    private float _moveSpeed = 5f;
    [FormerlySerializedAs("airMoveSpeed")]
    [SerializeField, Tooltip("空中での横移動速度")]
    private float _airMoveSpeed = 2f;

    [Header("ジャンプ設定")]
    [FormerlySerializedAs("jumpPower")]
    [SerializeField, Tooltip("踏み切り時に加える力")]
    private float _jumpPower = 5f;
    [FormerlySerializedAs("chargeJumpPower")]
    [SerializeField, Tooltip("ジャンプキーを長押ししている間の上向き加速度")]
    private float _chargeJumpPower = 8f;
    [FormerlySerializedAs("maxChargeTime")]
    [SerializeField, Tooltip("長押しで上向きに加速できる最長時間（秒）")]
    private float _maxChargeTimeSeconds = 0.3f;

    [SerializeField, Tooltip("オンの場合は矢印キーまたはテンキーで移動・ジャンプ・攻撃する")]
    private bool _useArrowKeys;
    [FormerlySerializedAs("isStunned")]
    [SerializeField, HideInInspector]
    private bool _isStunned;

    private readonly HashSet<Collider> _groundContacts = new();
    private PlayerAttack _playerAttack;
    private Rigidbody _rigidbody;
    private bool _isInputEnabled = true;
    private bool _jumpRequested;
    private bool _isJumpButtonPressed;
    private float _chargeJumpTime;
    private Coroutine _stunRoutine;

    /// <summary>
    ///     試合進行に合わせて入力の受け付けを切り替える
    /// </summary>
    public void SetInputEnabled(bool isEnabled)
    {
        _isInputEnabled = isEnabled;
        if (!isEnabled)
        {
            _jumpRequested = false;
            _isJumpButtonPressed = false;
        }
    }

    /// <summary>
    ///     指定時間、移動と入力を止める
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (_stunRoutine != null)
        {
            StopCoroutine(_stunRoutine);
        }

        _stunRoutine = StartCoroutine(StunCoroutine(duration));
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        TryGetComponent(out _playerAttack);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (!_isInputEnabled || keyboard == null || _isStunned)
        {
            return;
        }

        bool jumpPressed = _useArrowKeys
            ? keyboard.upArrowKey.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame
            : keyboard.wKey.wasPressedThisFrame;
        if (jumpPressed && IsGrounded)
        {
            _jumpRequested = true;
            _isJumpButtonPressed = true;
        }

        bool jumpHeld = _useArrowKeys
            ? keyboard.upArrowKey.isPressed || keyboard.numpad8Key.isPressed
            : keyboard.wKey.isPressed;
        if (!jumpHeld)
        {
            _isJumpButtonPressed = false;
        }

        bool attackPressed = _useArrowKeys
            ? keyboard.downArrowKey.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame
            : keyboard.sKey.wasPressedThisFrame;
        if (_playerAttack != null && attackPressed)
        {
            _playerAttack.Attack();
        }
    }

    private void FixedUpdate()
    {
        if (!_isInputEnabled || _isStunned)
        {
            return;
        }

        bool canJump = _jumpRequested && IsGrounded;
        _jumpRequested = false;
        if (canJump)
        {
            _chargeJumpTime = 0f;
            _groundContacts.Clear();
            _rigidbody.AddForce(Vector3.up * _jumpPower, ForceMode.Impulse);
            OnJumpStarted?.Invoke();
        }
        else
        {
            ChargeJump();
        }

        Move();
    }

    private void OnDisable()
    {
        _groundContacts.Clear();
        _jumpRequested = false;
        _isJumpButtonPressed = false;
        if (_stunRoutine != null)
        {
            StopCoroutine(_stunRoutine);
            _stunRoutine = null;
            _isStunned = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        UpdateGroundContact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        UpdateGroundContact(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        _groundContacts.Remove(collision.collider);
    }

    /// <summary>
    ///     入力に応じ、地上と空中それぞれの横移動速度を適用する
    /// </summary>
    private void Move()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool leftHeld = _useArrowKeys
            ? keyboard.leftArrowKey.isPressed || keyboard.numpad4Key.isPressed
            : keyboard.aKey.isPressed;
        bool rightHeld = _useArrowKeys
            ? keyboard.rightArrowKey.isPressed || keyboard.numpad6Key.isPressed
            : keyboard.dKey.isPressed;
        float moveX = leftHeld ? -1f : rightHeld ? 1f : 0f;
        float currentSpeed = IsGrounded ? _moveSpeed : _airMoveSpeed;
        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.x = moveX * currentSpeed;
        _rigidbody.linearVelocity = velocity;
    }

    /// <summary>
    ///     長押し中の上昇に限って上向き加速度を追加する
    /// </summary>
    private void ChargeJump()
    {
        if (!_isJumpButtonPressed)
        {
            return;
        }

        if (_chargeJumpTime >= _maxChargeTimeSeconds || _rigidbody.linearVelocity.y <= 0f)
        {
            _isJumpButtonPressed = false;
            return;
        }

        _chargeJumpTime += Time.fixedDeltaTime;
        _rigidbody.AddForce(Vector3.up * _chargeJumpPower, ForceMode.Acceleration);
    }

    /// <summary>
    ///     足元を支える地面との接触を記録する
    /// </summary>
    private void UpdateGroundContact(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
        {
            return;
        }

        // 踏み切った直後の残留接触と、足場の側面への衝突を接地扱いにしない。
        if (IsGrounded || _rigidbody.linearVelocity.y <= 0.1f)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    _groundContacts.Add(collision.collider);
                    _isJumpButtonPressed = false;
                    return;
                }
            }
        }

        _groundContacts.Remove(collision.collider);
    }

    /// <summary>
    ///     指定した被弾硬直時間が経過してから操作を再開する
    /// </summary>
    private IEnumerator StunCoroutine(float duration)
    {
        _isStunned = true;
        _jumpRequested = false;
        _isJumpButtonPressed = false;
        yield return new WaitForSeconds(duration);
        _isStunned = false;
        _stunRoutine = null;
    }
}
