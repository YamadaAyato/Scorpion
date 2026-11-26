using UnityEngine;

/// <summary>
///     攻撃に触れた相手を吹き飛ばし、一定時間操作不能にする
/// </summary>
public class Hitbox : MonoBehaviour
{
    [SerializeField, Tooltip("相手を吹き飛ばす力")]
    private float _knockbackForce = 10f;
    [SerializeField, Tooltip("相手が操作できなくなる時間（秒）")]
    private float _stunDurationSeconds = 0.5f;

    private void OnTriggerEnter(Collider other)
    {
        Transform attacker = transform.root;
        if (other.gameObject == attacker.gameObject || !other.TryGetComponent(out Rigidbody targetBody))
        {
            return;
        }

        if (other.TryGetComponent(out PlayerMove targetMovement))
        {
            targetMovement.ApplyStun(_stunDurationSeconds);
        }

        // 地上でも押し合いから離れるよう、吹き飛ぶ方向に上向きの成分を加える
        Vector3 direction = (other.transform.position - attacker.position).normalized;
        Vector3 knockbackDirection = (direction + Vector3.up * 0.5f).normalized;
        targetBody.AddForce(knockbackDirection * _knockbackForce, ForceMode.Impulse);
        Debug.Log("攻撃ひっと");
    }
}
