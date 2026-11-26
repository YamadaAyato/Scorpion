using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;

public class Scaffold : MonoBehaviour
{
    [Header("耐久値")]
    [SerializeField] private int _durability;

    private bool _canRide = true;

    private Material[] _materials;

    private Action OnDestroy;

    public event Action HandlerDestroy
    {
        add => OnDestroy += value;
        remove => OnDestroy -= value;
    }

    private void Start() => _materials = GetComponent<Renderer>().materials;

    public void ZeroDurability() => DestroyScaffold().Forget();

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (_durability == 0 && _canRide)
            {
                DestroyScaffold().Forget();
                return;
            }

            _durability--;
        }
    }

    private async UniTask DestroyScaffold()
    {
        if (_canRide)
        {
            _canRide = false;
            OnDestroy.Invoke();

            Sequence sequence = DOTween.Sequence();

            sequence.Join(transform.DOMoveY(transform.position.y - 1f, 2f));

            await sequence.AsyncWaitForCompletion();

            Destroy(gameObject);
        }
    }
}
