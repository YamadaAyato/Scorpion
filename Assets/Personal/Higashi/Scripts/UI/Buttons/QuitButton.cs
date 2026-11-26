using UnityEngine;

/// <summary> 押すとゲームを終了するボタン。エディタで再生中のときは再生を止める。 </summary>
public class QuitButton : ButtonBase
{
    protected override void Execute()
    {
#if UNITY_EDITOR
        // エディタでは Application.Quit が効かないので、再生を止めて動作を確認できるようにする
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
