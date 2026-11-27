using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [SerializeField] private string _sceneName;

    [SerializeField] private List<PlayerUIBase> _bases = new();

    private static Action OnAction;

    public static event Action HandlerAction
    {
        add { OnAction += value; }
        remove { OnAction -= value; }
    }

    private bool _active = true;

    private void Update()
    {
        for (int i = 0; i < _bases.Count; i++)
        {
            if (!_bases[i].IsGo)
                _active = false;

            if (i == _bases.Count - 1 && _active)
            {
                OnAction.Invoke();
                SceneManager.LoadScene(_sceneName);
            }
        }
    }
}
