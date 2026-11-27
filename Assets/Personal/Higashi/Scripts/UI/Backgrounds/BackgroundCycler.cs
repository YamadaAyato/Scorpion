using DG.Tweening;
using Scorpion.Audio;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背景の絵を、用意した数枚の中で順番に切り替える。背景の Image に付ける。
/// ・WhenHidden：星のトランジションが画面を覆いきった瞬間（画面が隠れている間）に、パッと次の絵にする。シーンをまたいでも、次のシーンは次の絵から始まる
/// ・EveryInterval：Interval 秒ごとに、ゆっくり重ねて（クロスフェードで）次の絵にする
/// ・Both：上の両方
/// 今どの絵かはシーンをまたいで覚えておく（同じゲームの間だけ）。再生中に Mode を変えても、そのまま切り替わる。
/// シーンが始まったときに、このシーンの BGM も AudioManager で流す。
/// </summary>
[RequireComponent(typeof(Image))]
public class BackgroundCycler : MonoBehaviour
{
    /// <summary> いつ背景を変えるか。 </summary>
    public enum CycleMode
    {
        /// <summary> 星が画面を覆いきったとき。 </summary>
        WhenHidden,

        /// <summary> 一定時間ごと。 </summary>
        EveryInterval,

        /// <summary> 両方。 </summary>
        Both,
    }

    [Tooltip("順番に使う背景の絵")]
    [SerializeField] private Sprite[] _backgrounds;

    [Tooltip("いつ背景を変えるか。WhenHidden：星が画面を覆いきったとき / EveryInterval：一定時間ごと / Both：両方")]
    [SerializeField] private CycleMode _mode = CycleMode.WhenHidden;

    [Tooltip("EveryInterval のとき、何秒ごとに変えるか")]
    [SerializeField] private float _interval = 10f;

    [Tooltip("シーンが始まったときに流す BGM の名前（AudioManager の BGM List の Name）。空なら流さない")]
    [SerializeField] private string _sceneBgm = "music_title";

    private Image _image;
    private Image _fadeImage;
    private float _timer;
    private Tween _fadeTween;

    // 今の絵の番号（シーンをまたいで覚えておく）
    private static int s_currentIndex;

    // EveryInterval で重ねて切り替える時間（秒）
    private const float CROSSFADE_DURATION = 1.5f;

    private void Awake()
    {
        _image = GetComponent<Image>();
        CreateFadeImage();
        ShowCurrent();
    }

    // AudioManager の Instance は Awake で決まるので、BGM は Start で流す
    private void Start()
    {
        if (!string.IsNullOrEmpty(_sceneBgm)) AudioManager.Instance?.PlayBGM(_sceneBgm);
    }

    private void OnEnable()
    {
        StarTransitionOverlay.OnAnyCovered += HandleTransitionCovered;
    }

    private void OnDisable()
    {
        StarTransitionOverlay.OnAnyCovered -= HandleTransitionCovered;
        if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill(true);
    }

    private void Update()
    {
        if (_mode == CycleMode.WhenHidden || _backgrounds == null || _backgrounds.Length < 2) return;

        _timer += Time.unscaledDeltaTime;
        if (_timer < _interval) return;

        _timer = 0f;
        CrossfadeToNext();
    }

    /// <summary> 星が画面を覆いきったとき。隠れている間に、パッと次の絵にする。 </summary>
    private void HandleTransitionCovered()
    {
        if (_mode == CycleMode.EveryInterval || _backgrounds == null || _backgrounds.Length < 2) return;

        if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill(true);
        Advance();
        ShowCurrent();
        _timer = 0f;
    }

    /// <summary> 次の絵を上に重ねてだんだん濃くし、濃くなりきったら下の絵と入れ替える。 </summary>
    private void CrossfadeToNext()
    {
        if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill(true);
        Advance();
        _fadeImage.sprite = _backgrounds[s_currentIndex];
        SetFadeAlpha(0f);
        _fadeImage.enabled = true;
        _fadeTween = DOTween.To(() => _fadeImage.color.a, SetFadeAlpha, 1f, CROSSFADE_DURATION)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(ShowCurrent);
    }

    /// <summary> 次の番号に進める（最後まで来たら最初に戻る）。 </summary>
    private void Advance()
    {
        s_currentIndex = (s_currentIndex + 1) % _backgrounds.Length;
    }

    /// <summary> 今の番号の絵を、下の Image にすぐ出す（重ねていた絵は隠す）。 </summary>
    private void ShowCurrent()
    {
        if (_backgrounds == null || _backgrounds.Length == 0) return;

        s_currentIndex %= _backgrounds.Length;
        _image.sprite = _backgrounds[s_currentIndex];
        _image.color = Color.white;
        if (_fadeImage != null) _fadeImage.enabled = false;
    }

    /// <summary> クロスフェード用に、同じ大きさの Image を子に作る（クリックは受けない）。 </summary>
    private void CreateFadeImage()
    {
        var go = new GameObject("CrossfadeImage", typeof(RectTransform));
        go.layer = gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _fadeImage = go.AddComponent<Image>();
        _fadeImage.raycastTarget = false;
        _fadeImage.preserveAspect = _image.preserveAspect;
        _fadeImage.enabled = false;
    }

    private void SetFadeAlpha(float alpha)
    {
        _fadeImage.color = new Color(1f, 1f, 1f, alpha);
    }
}
