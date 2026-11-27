using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 別のボタン（uGUI の Button）を押したことにする。UnityEvent から Click を呼ぶ。
/// 見た目は自分のボタンにして、押したときの処理はもとからあるボタンのもの（音量設定を開くなど）をそのまま使いたいときに使う。
/// </summary>
public class ButtonClickRelay : MonoBehaviour
{
    [Tooltip("押したことにするボタン。非表示にしてあってもよい")]
    [SerializeField] private Button _target;

    /// <summary> 対象のボタンの On Click を呼ぶ。 </summary>
    public void Click()
    {
        if (_target == null)
        {
            Debug.LogWarning($"{name}: 押したことにするボタンが入っていません", this);
            return;
        }
        _target.onClick.Invoke();
    }
}
