using GameFoundation.Runtime.Attributers;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> 押すと指定したシーンに切り替えるボタン（タイトルの「スタート」、リザルトの「タイトルへ」など）。 </summary>
public class SceneLoadButton : ButtonBase
{
    [Tooltip("切り替え先のシーン。Build Settings に登録されているシーンから選ぶ")]
    [SceneNameSelector]
    [SerializeField] private string _sceneName;

    [Tooltip("切り替える前に Time.timeScale を 1 に戻すか。ポーズ中（timeScale = 0）のまま切り替えると、次のシーンも止まったままになるため")]
    [SerializeField] private bool _resetTimeScale = true;

    protected override void Execute()
    {
        if (string.IsNullOrEmpty(_sceneName))
        {
            Debug.LogWarning($"{name}: 切り替え先のシーンが設定されていません", this);
            return;
        }

        if (_resetTimeScale) Time.timeScale = 1f;
        SceneManager.LoadScene(_sceneName);
    }
}
