using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 押したときの処理を Inspector で設定するボタン（ポーズの「再開」、パネルの開閉など）。
/// 専用のクラスを作るほどではない処理に使う。Button の On Click() ではなく、こちらの On Execute に入れる。
/// </summary>
public class EventButton : ButtonBase
{
    [Tooltip("押したときに実行する処理。押した動きを見せる待ち時間のあとに実行される")]
    [SerializeField] private UnityEvent _onExecute = new UnityEvent();

    protected override void Execute()
    {
        _onExecute?.Invoke();
    }
}
