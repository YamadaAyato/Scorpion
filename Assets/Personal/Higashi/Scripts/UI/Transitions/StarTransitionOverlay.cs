using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 星を使った画面の切り替え（トランジション）。Play で作る。
/// ・覆う：ボタンの星の複製を一番手前の Canvas に置き、回しながら画面を覆いきるまで大きくする
/// ・見せる：覆ったあと、シーンが読み込まれるか Reveal を呼ばれたら、回しながら画面の真ん中へ小さくして消し、新しい画面を見せる
/// ・同じシーンの中でパネルを切り替えるとき：覆いきった瞬間に onCovered を呼び（ここでパネルを切り替える）、そのまま見せる
/// 覆いきったときは、どの切り替えでも OnAnyCovered を呼ぶ（画面が隠れている間に背景を変えるときなどに使う）。
/// シーンをまたいで残るよう DontDestroyOnLoad にし、消えたら自分を削除する。切り替えの間は、画面全体でクリックを受け付けない。
/// 星の位置は画面のピクセルで扱うので、元の星は Screen Space - Overlay の Canvas に置かれている必要がある。
/// </summary>
public class StarTransitionOverlay : MonoBehaviour
{
    /// <summary> どれかの切り替えで、星が画面を覆いきったとき（画面が完全に隠れている間）。 </summary>
    public static event Action OnAnyCovered;

    private RectTransform _star;
    private Image _blocker;
    private float _revealDuration;
    private float _revealSpin;
    private bool _isCovered;
    private bool _isRevealRequested;
    private bool _isRevealing;
    private Tween _tween;
    private Tween _autoRevealTween;
    private Action _onCovered;

    // 一番手前に描くための Canvas の並び順
    private const int SORTING_ORDER = 1000;

    // 画面の角まで確実に覆うよう、計算した大きさより少し大きくする割合
    private const float COVER_MARGIN = 1.15f;

    // 覆いきってから見せ始めるまでの間（秒）
    private const float HOLD_DURATION = 0.1f;

    // 覆ったまま、シーンの読み込みも Reveal も来なかったときに、自動で見せ始めるまでの時間（秒）。ずっと画面が隠れたままになるのを防ぐ
    private const float AUTO_REVEAL_DELAY = 3f;

    private const string OBJECT_NAME = "StarTransition";

    /// <summary>
    /// 星のトランジションを始める。
    /// sourceStar は元の星（Pivot が星の中心のもの）。innerRadius は星のへこんだ角までの半径（sourceStar の中の単位）で、画面を覆うのに必要な大きさの計算に使う。
    /// spin は覆うあいだ・見せるあいだに、それぞれ時計回りに回る角度（度）。
    /// onCovered を渡すと、覆いきった瞬間に呼んでから、シーンの読み込みを待たずにそのまま見せる（同じシーンの中でパネルを切り替えるとき用）。
    /// </summary>
    public static StarTransitionOverlay Play(RectTransform sourceStar, float innerRadius, float coverDuration, float revealDuration, float spin, Action onCovered = null)
    {
        var go = new GameObject(OBJECT_NAME, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SORTING_ORDER;

        var overlay = go.AddComponent<StarTransitionOverlay>();
        overlay._onCovered = onCovered;
        overlay.Begin(sourceStar, innerRadius, coverDuration, revealDuration, spin);
        return overlay;
    }

    /// <summary> 新しい画面を見せる。まだ覆いきっていなければ、覆いきってから見せる。何回呼んでもよい。 </summary>
    public void Reveal()
    {
        _isRevealRequested = true;
        if (_isCovered) StartReveal();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary> シーンが読み込まれたら、新しい画面を見せる。 </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Reveal();
    }

    /// <summary>
    /// 覆いきったとき。onCovered があれば呼んでから見せる。
    /// なければ、見せる合図が先に来ていれば見せ始め、来ていなければしばらく待ってから自動で見せる。
    /// </summary>
    private void HandleCovered()
    {
        _isCovered = true;
        OnAnyCovered?.Invoke();
        if (_onCovered != null)
        {
            _onCovered.Invoke();
            _onCovered = null;
            _isRevealRequested = true;
        }
        if (_isRevealRequested)
        {
            StartReveal();
            return;
        }
        _autoRevealTween = DOVirtual.DelayedCall(AUTO_REVEAL_DELAY, Reveal, true).SetLink(gameObject);
    }

    /// <summary>
    /// 星の複製とクリック止めの板を作り、覆う動きを始める。
    /// 複製は、元の星と同じ画面上の位置・大きさ・向きから始める（画面の真ん中を基準にした位置に直して置く）。
    /// </summary>
    private void Begin(RectTransform sourceStar, float innerRadius, float coverDuration, float revealDuration, float spin)
    {
        _revealDuration = revealDuration;
        _revealSpin = spin;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        // 切り替えの間はクリックを受け付けないよう、画面全体に透明な板を置く
        var blockerObject = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
        var blockerRect = (RectTransform)blockerObject.transform;
        blockerRect.SetParent(transform, false);
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        _blocker = blockerObject.GetComponent<Image>();
        _blocker.color = Color.clear;

        _star = Instantiate(sourceStar, transform);
        _star.name = "Star";
        _star.anchorMin = new Vector2(0.5f, 0.5f);
        _star.anchorMax = new Vector2(0.5f, 0.5f);
        _star.pivot = sourceStar.pivot;
        _star.sizeDelta = sourceStar.rect.size;
        _star.localRotation = sourceStar.rotation;
        _star.localScale = sourceStar.lossyScale;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 screenPosition = sourceStar.position;
        _star.anchoredPosition = screenPosition - screenSize * 0.5f;

        // 星のへこんだ角が、星から一番遠い画面の角より外に出る大きさまで広げる
        float farthest = 0f;
        foreach (Vector2 corner in new[] { Vector2.zero, new Vector2(screenSize.x, 0f), new Vector2(0f, screenSize.y), screenSize })
        {
            farthest = Mathf.Max(farthest, Vector2.Distance(screenPosition, corner));
        }
        float startScale = sourceStar.lossyScale.x;
        float innerOnScreen = Mathf.Max(innerRadius * startScale, 1f);
        float targetScale = startScale * farthest / innerOnScreen * COVER_MARGIN;

        _tween = DOTween.Sequence()
            .Append(_star.DOScale(Vector3.one * targetScale, coverDuration).SetEase(Ease.InCubic))
            .Join(_star.DOLocalRotate(new Vector3(0f, 0f, -spin), coverDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InCubic))
            .OnComplete(HandleCovered)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    /// <summary> 少し間を置いてから、星を回しながら画面の真ん中へ小さくして消し、消えたら自分を削除する。 </summary>
    private void StartReveal()
    {
        if (_isRevealing) return;
        _isRevealing = true;
        if (_autoRevealTween != null && _autoRevealTween.IsActive()) _autoRevealTween.Kill();

        // 見せ始めたら、新しい画面のボタンを押せるようにする
        _blocker.raycastTarget = false;

        _tween = DOTween.Sequence()
            .AppendInterval(HOLD_DURATION)
            .Append(_star.DOScale(Vector3.zero, _revealDuration).SetEase(Ease.InCubic))
            .Join(DOTween.To(() => _star.anchoredPosition, value => _star.anchoredPosition = value, Vector2.zero, _revealDuration).SetEase(Ease.OutCubic))
            .Join(_star.DOLocalRotate(new Vector3(0f, 0f, -_revealSpin), _revealDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InCubic))
            .OnComplete(() => Destroy(gameObject))
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
