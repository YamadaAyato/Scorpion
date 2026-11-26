
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float airMoveSpeed = 2f;

    [Header("ジャンプ設定")]
    [SerializeField] float jumpPower = 5f;
    [SerializeField] float chargeJumpPower = 8f;
    [SerializeField] float maxChargeTime = 0.3f;

    private Rigidbody rb;
    private bool isGrounded;
    private bool _isJumpButtonPressed;

    private float _chargeJumpTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // ジャンプ開始
        if (Keyboard.current.wKey.wasPressedThisFrame && isGrounded)
        {
            _chargeJumpTime = 0f;
            _isJumpButtonPressed = true;

            Jump(jumpPower);
            isGrounded = false;
        }

        // Wキーを離したらチャージ終了
        if (Keyboard.current.wKey.wasReleasedThisFrame)
        {
            _isJumpButtonPressed = false;
        }
    }

    void FixedUpdate()
    {
        Move();
        ChargeJump();
    }

    // 左右移動
    void Move()
    {
        if (Keyboard.current == null) return;

        float moveX = 0f;

        if (Keyboard.current.aKey.isPressed)
        {
            moveX = -1f;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            moveX = 1f;
        }

        // 地上と空中で移動速度を変更
        float currentSpeed = isGrounded ? moveSpeed : airMoveSpeed;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveX * currentSpeed;

        rb.linearVelocity = velocity;
    }

    // ジャンプ開始
    void Jump(float power)
    {
        rb.AddForce(Vector3.up * power, ForceMode.Impulse);
    }

    // 長押しジャンプ
    void ChargeJump()
    {
        if (!_isJumpButtonPressed) return;

        // 最大チャージ時間に達したら終了
        if (_chargeJumpTime >= maxChargeTime)
        {
            _isJumpButtonPressed = false;
            return;
        }

        // 上昇中だけ追加のジャンプ力を与える
        if (rb.linearVelocity.y <= 0)
        {
            _isJumpButtonPressed = false;
            return;
        }

        // チャージ時間を加算
        _chargeJumpTime += Time.fixedDeltaTime;

        // 追加ジャンプ力
        rb.AddForce(Vector3.up * chargeJumpPower, ForceMode.Acceleration);
    }

    // 着地判定
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            _isJumpButtonPressed = false;
        }
    }
}
