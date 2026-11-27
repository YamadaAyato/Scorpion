using System.Linq;
using Scorpion.Audio;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 試合の表示の合図（GameManager の OnStageCueChanged）に合わせて、文字の代わりに画像の演出と効果音を出す。試合のシーンに置く。
/// ・カウントダウン（3・2・1）：数字の文字を隠し、READY を出して待つ。最初の数字でカウントダウンの効果音を鳴らす
/// ・START：文字を隠し、READY を飛び去らせて GO! を出す
/// ・FINISH：文字を隠し、TIME UP!! を出して、時間切れの効果音を鳴らす
/// ・それ以外の表示（空など）：文字を元に戻す
/// 文字は消さずに Text の表示だけを切り替えるので、GameManager・UIManager の中身は変えない。
/// </summary>
[DefaultExecutionOrder(-50)]
public class StageCueImages : MonoBehaviour
{
    [Tooltip("UIManager が合図を出している文字（MatchHud/StageCue）。画像を出している間だけ隠す")]
    [SerializeField] private Text _cueText;

    [Tooltip("READY → GO! を出す演出")]
    [SerializeField] private ReadyCallEffect _readyCall;

    [Tooltip("TIME UP!! を出す演出")]
    [SerializeField] private TimeUpCallEffect _timeUpCall;

    [Tooltip("カウントダウンが始まったときに鳴らす効果音の名前（AudioManager の SE List の Name）。空なら鳴らさない")]
    [SerializeField] private string _countdownSe = "se_count321";

    [Tooltip("時間切れ（TIME UP!!）のときに鳴らす効果音の名前。空なら鳴らさない")]
    [SerializeField] private string _timeUpSe = "se_timeup";

    private GameManager _game;
    private bool _isCountingDown;

    // GameManager が出す合図の文字
    private const string START_CUE = "START";
    private const string FINISH_CUE = "FINISH";

    // GameManager の Instance は Awake で決まるので、購読は Start で行う
    private void Start()
    {
        _game = GameManager.Instance;
        if (_game == null)
        {
            Debug.LogWarning($"{name}: GameManager が見つからないので、合図の画像を出せません", this);
            return;
        }
        _game.OnStageCueChanged += HandleStageCueChanged;
    }

    private void OnDestroy()
    {
        if (_game != null) _game.OnStageCueChanged -= HandleStageCueChanged;
    }

    /// <summary> 合図が変わったとき。合図に合わせて画像と効果音を出し、文字を隠すか戻す。 </summary>
    private void HandleStageCueChanged(string cue)
    {
        if (IsCountdown(cue))
        {
            SetTextVisible(false);
            if (_isCountingDown) return;

            _isCountingDown = true;
            if (_readyCall != null) _readyCall.PlayReady();
            PlaySe(_countdownSe);
            return;
        }

        _isCountingDown = false;
        if (cue == START_CUE)
        {
            SetTextVisible(false);
            if (_readyCall != null) _readyCall.PlayGo();
        }
        else if (cue == FINISH_CUE)
        {
            SetTextVisible(false);
            if (_timeUpCall != null) _timeUpCall.Play();
            PlaySe(_timeUpSe);
        }
        else
        {
            SetTextVisible(true);
        }
    }

    /// <summary> カウントダウンの数字（1文字以上の数字だけ）かどうか。 </summary>
    private static bool IsCountdown(string cue)
    {
        return !string.IsNullOrEmpty(cue) && cue.All(char.IsDigit);
    }

    /// <summary> 合図の文字を見せるか隠す（オブジェクトの表示は UIManager が切り替えるので、Text だけを切り替える）。 </summary>
    private void SetTextVisible(bool isVisible)
    {
        if (_cueText != null) _cueText.enabled = isVisible;
    }

    private static void PlaySe(string seName)
    {
        if (!string.IsNullOrEmpty(seName)) AudioManager.Instance?.PlaySE(seName);
    }
}
