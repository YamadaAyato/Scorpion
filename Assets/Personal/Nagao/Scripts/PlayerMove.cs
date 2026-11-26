
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

    [SerializeField] GameObject target;
    private PlayerAttack playerAttack;
    private Rigidbody rb;
    private bool isGrounded;
    private bool _isJumpButtonPressed;
    public bool isStunned;

    private float _chargeJumpTime;

    private Coroutine stunRoutine;

    void Start()
    {
        playerAttack = GetComponent<PlayerAttack>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (isStunned) return;


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
        //攻撃
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            if(target != null&&playerAttack!=null)
            {
                playerAttack.Attack(target);
            }
        }
    }

    void FixedUpdate()
    {
        if (isStunned) return;
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
    // 攻撃を受けたときに呼び出す（duration秒間、操作不能にする）
    public void ApplyStun(float duration)
    {
        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
        }
        stunRoutine = StartCoroutine(StunCoroutine(duration));
    }

    System.Collections.IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        _isJumpButtonPressed = false; // ジャンプチャージ中だった場合も解除
        yield return new WaitForSeconds(duration);
        isStunned = false;
        stunRoutine = null;
    }
}
