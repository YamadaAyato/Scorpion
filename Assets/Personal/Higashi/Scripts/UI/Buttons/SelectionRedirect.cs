using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// このオブジェクトが選ばれたら、次のフレームで別のオブジェクトを選び直す。
/// 見えないボタンに選択が移ってしまう場面（音量設定を閉じたときなど）で、キーボード・パッドの選択を見えるボタンに戻すために使う。
/// 選ばれた瞬間に選び直すと EventSystem がエラーを出すので、1フレーム待つ。
/// </summary>
public class SelectionRedirect : MonoBehaviour, ISelectHandler
{
    [Tooltip("代わりに選ぶオブジェクト")]
    [SerializeField] private GameObject _target;

    private bool _isRedirectRequested;

    private void Update()
    {
        if (!_isRedirectRequested) return;

        _isRedirectRequested = false;
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || _target == null || !_target.activeInHierarchy) return;
        if (eventSystem.currentSelectedGameObject == gameObject) eventSystem.SetSelectedGameObject(_target);
    }

    /// <summary> 選ばれたとき。次のフレームで選び直す。 </summary>
    public void OnSelect(BaseEventData eventData)
    {
        _isRedirectRequested = true;
    }
}
