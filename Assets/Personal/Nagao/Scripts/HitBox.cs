using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] float damagePower = 10f; // 吹き飛ばしの強さ
    [SerializeField] float stunDuration = 0.5f; // 相手の操作不能時間（秒）

    void OnTriggerEnter(Collider other)
    {
        // 自分自身（プレイヤー）に当たった場合は無視する
        if (other.gameObject == transform.root.gameObject) return;

        // 相手がRigidbodyを持っているか確認
        Rigidbody targetRb = other.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            // 相手のPlayerMoveを取得して操作不能にする
            PlayerMove targetMove = other.GetComponent<PlayerMove>();
            if (targetMove != null)
            {
                targetMove.ApplyStun(stunDuration);
            }

            // 相手から自分への方向を計算して吹き飛ばす
            Vector3 direction = (other.transform.position - transform.root.position).normalized;
            
            // 少し斜め上に吹き飛ばすとスマブラっぽくなります
            Vector3 forceDirection = (direction + Vector3.up * 0.5f).normalized;

            targetRb.AddForce(forceDirection * damagePower, ForceMode.Impulse);
            Debug.Log("攻撃ひっと");
        }
    }
}