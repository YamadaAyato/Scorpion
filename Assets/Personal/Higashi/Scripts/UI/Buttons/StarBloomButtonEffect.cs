using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// ボタンに「星ひろがり」の演出を付ける（案2）。ButtonBase（を継承したボタン）と同じオブジェクトに付ける。
/// ・選んだ瞬間：真ん中から丸っこい星がポワン！と行き過ぎてから戻る勢いで広がり、シアンからゴールドへ塗り替わる。四隅の小さな星がキラキラ上下に揺れる
/// ・マウスを押し込んでいる間：広がっていた星がギュッと縮み、ボタンが沈み込む（タメ）
/// ・決定：ボタンがふわっと膨らんで、ポンッ！と星になって弾ける
/// 広がる星は、板の Mask の中に置いた StarShapeGraphic の大きさを変えて作る。
/// </summary>
public class StarBloomButtonEffect : ButtonEffectBase
{
    [Header("星ひろがり")]
    [Tooltip("真ん中から広がる丸っこい星（StarShapeGraphic）。板の Mask の中の真ん中に置く")]
    [SerializeField] private StarShapeGraphic _bloomStar;

    [Tooltip("四隅でキラキラ揺れる小さな星。板の外（Visual の中）の四隅に置く")]
    [SerializeField] private RectTransform[] _cornerStars;

    [Header("色")]
    [Tooltip("ふだんの板の色")]
    [SerializeField] private Color _normalColor = new Color32(0x8B, 0xE9, 0xFF, 0xFF);

    [Tooltip("広がる星の色")]
    [SerializeField] private Color _bloomColor = new Color32(0xFF, 0xD6, 0x6B, 0xFF);

    [Tooltip("ふだんの文字の色")]
    [SerializeField] private Color _normalLabelColor = new Color32(0x3D, 0x3B, 0x6E, 0xFF);

    [Tooltip("星が広がったあとの文字の色")]
    [SerializeField] private Color _highlightLabelColor = new Color32(0x3D, 0x3B, 0x6E, 0xFF);

    [Header("広がる・縮む")]
    [Tooltip("星が広がるまでの時間（秒）")]
    [SerializeField] private float _bloomDuration = 0.35f;

    [Tooltip("広がるときに行き過ぎてから戻る強さ")]
    [SerializeField] private float _bloomOvershoot = 1.6f;

    [Tooltip("広がりながら回る角度（度）")]
    [SerializeField] private float _bloomSpin = 25f;

    [Tooltip("選択が外れたときに星が縮んで消えるまでの時間（秒）")]
    [SerializeField] private float _shrinkDuration = 0.2f;

    [Header("四隅の星")]
    [Tooltip("四隅の星が出てくるまでの時間（秒）")]
    [SerializeField] private float _cornerAppearDuration = 0.25f;

    [Tooltip("四隅の星が上下に揺れる幅（Canvas 上の単位）")]
    [SerializeField] private float _cornerBobHeight = 4f;

    [Tooltip("四隅の星が上下に揺れる周期の半分（秒）")]
    [SerializeField] private float _cornerBobDuration = 0.7f;

    [Tooltip("キラキラ瞬くときに一番小さくなる大きさ（元の何倍か）")]
    [SerializeField] private float _cornerTwinkleScale = 0.7f;

    [Header("押し込み")]
    [Tooltip("押し込んでいる間、広がった星をどこまで縮めるか（広がりきった大きさに対する割合）")]
    [SerializeField, Range(0f, 1f)] private float _holdBloomRate = 0.55f;

    [Tooltip("押し込んでいる間のボタンの大きさ（元の何倍か）")]
    [SerializeField] private float _holdScale = 0.94f;

    [Tooltip("押し込んでいる間に沈む量（Canvas 上の単位）")]
    [SerializeField] private Vector2 _holdOffset = new Vector2(0f, -3f);

    [Tooltip("縮む・沈むまでの時間（秒）")]
    [SerializeField] private float _holdDuration = 0.1f;

    [Header("決定（ふわっと膨らんでポンッ）")]
    [Tooltip("膨らんだときの大きさ（元の何倍か）")]
    [SerializeField] private float _puffScale = 1.12f;

    [Tooltip("膨らむまでの時間（秒）")]
    [SerializeField] private float _puffDuration = 0.1f;

    [Tooltip("弾けて消えるまでの時間（秒）")]
    [SerializeField] private float _vanishDuration = 0.12f;

    [Tooltip("弾けて飛び散る星の数")]
    [SerializeField] private int _burstCount = 8;

    [Tooltip("星が飛び散る距離（最小・最大。Canvas 上の単位）")]
    [SerializeField] private Vector2 _burstDistanceRange = new Vector2(70f, 130f);

    [Tooltip("飛び散る星の大きさ（最小・最大）")]
    [SerializeField] private Vector2 _burstSizeRange = new Vector2(16f, 26f);

    [Tooltip("星が飛び散ってから消えるまでの時間（秒）")]
    [SerializeField] private float _burstDuration = 0.45f;

    [Tooltip("決定のあとも画面が切り替わらなかったとき、元に戻し始めるまでの待ち時間（秒）")]
    [SerializeField] private float _restoreDelay = 0.1f;

    [Tooltip("元に戻るまでの時間（秒）")]
    [SerializeField] private float _restoreDuration = 0.3f;

    private Vector2[] _cornerBasePositions;
    private readonly List<Tween> _cornerTweens = new List<Tween>();
    private Tween _bloomTween;
    private Tween _labelTween;

    // 飛び散る星の形（少しぷっくりした星）
    private const int BURST_STAR_POINTS = 5;
    private const float BURST_STAR_INNER_RATIO = 0.55f;

    // 広がった星が板の角まで確実に覆うよう、少し大きめにする割合
    private const float BLOOM_COVER_MARGIN = 1.05f;

    protected override void Awake()
    {
        base.Awake();
        if (_bloomStar != null) _bloomStar.color = _bloomColor;

        int count = _cornerStars != null ? _cornerStars.Length : 0;
        _cornerBasePositions = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            if (_cornerStars[i] != null) _cornerBasePositions[i] = _cornerStars[i].anchoredPosition;
        }
    }

    protected override Color NormalFrameColor => _normalColor;

    protected override Color NormalLabelColor => _normalLabelColor;

    /// <summary> 真ん中から星をポワン！と広げて塗り替え、四隅の星を揺らし始める。 </summary>
    protected override void PlayHighlight(bool animate)
    {
        KillTween(_labelTween);
        _labelTween = TweenColor(Label, _highlightLabelColor, animate ? _bloomDuration * 0.5f : 0f);

        if (_bloomStar != null)
        {
            KillTween(_bloomTween);
            RectTransform bloom = _bloomStar.rectTransform;
            float cover = BloomCoverScale();
            if (animate)
            {
                bloom.localRotation = Quaternion.Euler(0f, 0f, -_bloomSpin);
                _bloomTween = DOTween.Sequence()
                    .Append(bloom.DOScale(Vector3.one * cover, _bloomDuration).SetEase(Ease.OutBack, _bloomOvershoot))
                    .Join(bloom.DOLocalRotate(Vector3.zero, _bloomDuration).SetEase(Ease.OutBack))
                    .SetUpdate(IgnoreTimeScale)
                    .SetLink(gameObject);
            }
            else
            {
                bloom.localScale = Vector3.one * cover;
                bloom.localRotation = Quaternion.identity;
            }
        }

        ShowCornerStars(animate);
    }

    /// <summary> 星を縮めて消し、四隅の星を隠す。 </summary>
    protected override void PlayUnhighlight()
    {
        KillTween(_labelTween);
        _labelTween = TweenColor(Label, _normalLabelColor, _shrinkDuration);
        TweenBloom(0f, _shrinkDuration, Ease.InBack);
        HideCornerStars();
    }

    /// <summary> 星をギュッと縮め、ボタンを沈ませる（タメ）。 </summary>
    protected override void PlayHoldStart()
    {
        TweenBloom(BloomCoverScale() * _holdBloomRate, _holdDuration, Ease.OutQuad);
        AnimateVisual(Vector3.one * _holdScale, _holdOffset, _holdDuration, Ease.OutQuad);
    }

    /// <summary> 星とボタンをポヨンと元に戻す（ボタンの上で離したときは、このあと決定の演出に切り替わる）。 </summary>
    protected override void PlayHoldEnd()
    {
        TweenBloom(BloomCoverScale(), _restoreDuration, Ease.OutBack);
        AnimateVisual(Vector3.one, Vector2.zero, _restoreDuration, Ease.OutBack);
    }

    /// <summary> ふわっと膨らんでから、ポンッ！と星になって弾ける。 </summary>
    protected override void PlayPress(bool fromHold)
    {
        if (VisualRoot == null) return;

        TweenBloom(BloomCoverScale(), _puffDuration, Ease.OutQuad);
        HideCornerStars();

        ReplaceVisualTween(DOTween.Sequence()
            .Append(CreateVisualTween(Vector3.one * _puffScale, _puffDuration, Ease.OutQuad))
            .AppendCallback(() => SpawnBurst(Vector2.zero, _burstCount, _burstDistanceRange, _burstSizeRange, BURST_STAR_POINTS, BURST_STAR_INNER_RATIO, _burstDuration))
            .Append(VisualRoot.DOScale(Vector3.zero, _vanishDuration).SetEase(Ease.InBack)));
        FadeVisual(0f, _vanishDuration, _puffDuration);
    }

    /// <summary> 弾けて消えたボタンを、ポヨンと元に戻す。 </summary>
    protected override void PlayRestore()
    {
        if (VisualRoot == null) return;

        ReplaceVisualTween(DOTween.Sequence()
            .AppendInterval(_restoreDelay)
            .Append(CreateVisualTween(Vector3.one, _restoreDuration, Ease.OutBack))
            .AppendCallback(ApplyCurrentStateAfterRestore));
        FadeVisual(1f, _restoreDuration * 0.5f, _restoreDelay);
    }

    protected override void ResetVisuals()
    {
        base.ResetVisuals();

        if (_bloomStar != null)
        {
            _bloomStar.rectTransform.localScale = Vector3.zero;
            _bloomStar.rectTransform.localRotation = Quaternion.identity;
        }

        for (int i = 0; i < CornerCount; i++)
        {
            if (_cornerStars[i] == null) continue;
            _cornerStars[i].localScale = Vector3.zero;
            _cornerStars[i].anchoredPosition = _cornerBasePositions[i];
        }
    }

    protected override void KillAllTweens()
    {
        base.KillAllTweens();
        KillTween(_bloomTween);
        KillTween(_labelTween);
        KillCornerTweens();
    }

    /// <summary> 元に戻ったあと、そのときの選択状態に合わせて四隅の星を出す。 </summary>
    private void ApplyCurrentStateAfterRestore()
    {
        if (TargetButton.IsHighlighted) ShowCornerStars(true);
    }

    /// <summary> 広がる星の大きさを変える。 </summary>
    private void TweenBloom(float scale, float duration, Ease ease)
    {
        if (_bloomStar == null) return;

        KillTween(_bloomTween);
        _bloomTween = _bloomStar.rectTransform.DOScale(Vector3.one * scale, duration)
            .SetEase(ease)
            .SetUpdate(IgnoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 四隅の星を出し、上下の揺れとキラキラした瞬きを始める。 </summary>
    private void ShowCornerStars(bool animate)
    {
        KillCornerTweens();

        for (int i = 0; i < CornerCount; i++)
        {
            RectTransform star = _cornerStars[i];
            if (star == null) continue;

            Vector2 basePosition = _cornerBasePositions[i];
            star.anchoredPosition = basePosition;

            // 星ごとに周期を少しずつずらして、そろって動いて見えないようにする
            float bobDuration = _cornerBobDuration + i * 0.08f;
            float twinkleDuration = 0.5f + i * 0.1f;

            _cornerTweens.Add(DOTween.To(() => star.anchoredPosition.y, y => star.anchoredPosition = new Vector2(basePosition.x, y), basePosition.y + _cornerBobHeight, bobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(IgnoreTimeScale)
                .SetLink(gameObject));

            Tween twinkle = star.DOScale(Vector3.one * _cornerTwinkleScale, twinkleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(IgnoreTimeScale)
                .SetLink(gameObject);

            if (animate)
            {
                // ポンと出てきてから瞬き始める
                star.localScale = Vector3.zero;
                twinkle.Pause();
                _cornerTweens.Add(star.DOScale(Vector3.one, _cornerAppearDuration)
                    .SetDelay(i * 0.05f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => twinkle.Play())
                    .SetUpdate(IgnoreTimeScale)
                    .SetLink(gameObject));
            }
            else
            {
                star.localScale = Vector3.one;
            }
            _cornerTweens.Add(twinkle);
        }
    }

    /// <summary> 四隅の星を縮めて隠す。 </summary>
    private void HideCornerStars()
    {
        KillCornerTweens();

        for (int i = 0; i < CornerCount; i++)
        {
            RectTransform star = _cornerStars[i];
            if (star == null) continue;

            _cornerTweens.Add(star.DOScale(Vector3.zero, _shrinkDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(IgnoreTimeScale)
                .SetLink(gameObject));
        }
    }

    /// <summary> 四隅の星の Tween をすべて止める。 </summary>
    private void KillCornerTweens()
    {
        foreach (Tween tween in _cornerTweens) KillTween(tween);
        _cornerTweens.Clear();
    }

    /// <summary> 広がった星が板の角まで覆う大きさ（星のへこんだ部分が、板の中心から角までの距離に届く倍率）。 </summary>
    private float BloomCoverScale()
    {
        if (_bloomStar == null || Frame == null) return 1f;

        Rect frame = Frame.rectTransform.rect;
        float halfDiagonal = Mathf.Sqrt(frame.width * frame.width + frame.height * frame.height) * 0.5f;
        Rect star = _bloomStar.rectTransform.rect;
        float innerRadius = Mathf.Min(star.width, star.height) * 0.5f * _bloomStar.InnerRatio;
        return innerRadius > 0f ? halfDiagonal / innerRadius * BLOOM_COVER_MARGIN : 1f;
    }

    /// <summary> 四隅の星の数。 </summary>
    private int CornerCount => _cornerStars != null ? _cornerStars.Length : 0;
}
