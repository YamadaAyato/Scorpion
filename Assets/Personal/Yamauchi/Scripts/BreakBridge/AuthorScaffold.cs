using System.Collections;
using UnityEngine;

/// <summary>
///     橋の足場を生成し、崩れたあとに作り直す
/// </summary>
public class AuthorScaffold : MonoBehaviour
{
    /// <summary> 現在生成されている足場を取得する </summary>
    public GameObject CurrentObject => _currentObject;

    [SerializeField, Tooltip("この位置に生成する足場Prefab")]
    private GameObject _object;
    [SerializeField, Tooltip("足場が崩れてから作り直すまでの時間（秒）")]
    private float _interval;
    [SerializeField, Tooltip("プレイヤーが乗っても崩れない復活用の足場にする")]
    private bool _isPermanent;

    private GameObject _currentObject;
    private Coroutine _respawn;

    private void OnEnable()
    {
        if (_currentObject == null && _object != null)
        {
            MakeScaffold();
        }
        else if (_currentObject != null && _currentObject.TryGetComponent(out Scaffold scaffold))
        {
            scaffold.HandlerDestroy += HandleScaffoldDestroyed;
        }
    }

    private void OnDisable()
    {
        if (_respawn != null)
        {
            StopCoroutine(_respawn);
            _respawn = null;
        }

        if (_currentObject != null && _currentObject.TryGetComponent(out Scaffold scaffold))
        {
            scaffold.HandlerDestroy -= HandleScaffoldDestroyed;
        }
    }

    /// <summary>
    ///     崩れた足場を切り離し、復旧を予約する
    /// </summary>
    private void HandleScaffoldDestroyed()
    {
        if (_currentObject != null && _currentObject.TryGetComponent(out Scaffold scaffold))
        {
            scaffold.HandlerDestroy -= HandleScaffoldDestroyed;
        }

        _currentObject = null;
        _respawn = StartCoroutine(Respawn());
    }

    /// <summary>
    ///     ステージPrefabの配下に足場を生成する
    /// </summary>
    private void MakeScaffold()
    {
        _currentObject = Instantiate(_object, transform.position, transform.rotation, transform);
        _currentObject.tag = "Ground";
        if (_currentObject.TryGetComponent(out Scaffold scaffold))
        {
            if (_isPermanent)
            {
                scaffold.KeepIntact();
            }

            scaffold.HandlerDestroy += HandleScaffoldDestroyed;
        }
    }

    /// <summary>
    ///     復旧時間が経過してから足場を再生成する
    /// </summary>
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(_interval);
        _respawn = null;
        MakeScaffold();
    }
}
