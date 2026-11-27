using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    [SerializeField] private string _sceneName;

    [SerializeField] private List<PlayerUIBase> _bases = new();

    [SerializeField] private Image _fadeImage;

    private static Action OnAction;

    public static event Action HandlerAction
    {
        add { OnAction += value; }
        remove { OnAction -= value; }
    }

    private bool _active = true;

    private void Start() => _fadeImage.DOFade(0f, 1f);

    private void Update()
    {
        for (int i = 0; i < _bases.Count; i++)
        {
            if (!_bases[i].IsGo)
                _active = false;

            if (i == _bases.Count - 1 && _active)
            {
                OnAction.Invoke();
                ChangeFade().Forget();
            }
        }
    }

    private async UniTask ChangeFade()
    {
        await _fadeImage.DOFade(1f, 1f).AsyncWaitForCompletion();
        SceneManager.LoadScene(_sceneName);
    }
}
