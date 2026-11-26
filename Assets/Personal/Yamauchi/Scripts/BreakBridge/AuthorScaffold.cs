using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class AuthorScaffold : MonoBehaviour
{
    [Header("足場")]
    [SerializeField] private GameObject _object;

    [Header("足場生成のインターバル")]
    [SerializeField] private float _interval;

    private GameObject _currentObject;

    public GameObject CurrentObject => _currentObject;

    private void OnDisable()
    {
        if (_currentObject == null) return;

        if (_currentObject.TryGetComponent<Scaffold>(out var s))
            s.HandlerDestroy -= ReceiveDestroy;
    }

    private void Start() => MakeScaffold();

    private void MakeScaffold()
    {
        _currentObject = Instantiate(_object, transform.position, transform.rotation);

        if (_currentObject.TryGetComponent<Scaffold>(out var s))
            s.HandlerDestroy += ReceiveDestroy;
    }

    private void ReceiveDestroy() => TaskReciveDestroy().Forget();

    private async UniTask TaskReciveDestroy()
    {
        if (_currentObject.TryGetComponent<Scaffold>(out var s))
            s.HandlerDestroy -= ReceiveDestroy;

        await UniTask.Delay(TimeSpan.FromSeconds(_interval));

        MakeScaffold();
    }
}
