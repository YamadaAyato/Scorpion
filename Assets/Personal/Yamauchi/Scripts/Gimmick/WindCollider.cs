using UnityEngine;

public class WindCollider : MonoBehaviour
{
    [Header("風の強さ")]
    [SerializeField] private float _windForce;

    private int _direction;

    private void Start()
    {
        _direction = Random.Range(0, 2) == 0 ? -1 : 1;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(Vector3.right * (_windForce * _direction));
        }
    }
}
