using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [SerializeField] Transform respawnPoint;

    private Rigidbody _rb;
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Death()
    {
        // 復活地点に移動
        _rb.position = respawnPoint.position;

        // 速度をリセット
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }
}
