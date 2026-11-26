using DG.Tweening;
using UnityEngine;

/// <summary>
/// ボタンに「もこもこ波」の演出を付ける（案1）。ButtonBase（を継承したボタン）と同じオブジェクトに付ける。
/// ・選んだ瞬間：右からクリームゴールドのもこもこ波がふわ〜っと押し寄せて塗り替わり、波のフチから丸っこい星が2〜3個ぽわんと浮かぶ
/// ・選択が外れたとき：波が右へ引いていく
/// ・マウスを押し込んでいる間：マシュマロのように、ぷにっと少し潰れて沈む
/// ・決定：ポヨン！と弾んで戻り、丸い星とドットの紙吹雪がパッと散る
/// 波は板の Mask の中に置いた WaveEdgeGraphic を横に動かして作る。
/// </summary>
public class MokoMokoWaveButtonEffect : ButtonEffectBase
{
    [Header("もこもこ波")]
    [Tooltip("右から押し寄せる波（WaveEdgeGraphic）。板の Mask の中に置き、Pivot を左端（X = 0）にする")]
    [SerializeField] private WaveEdgeGraphic _wave;

    [Header("色")]
    [Tooltip("ふだんの板の色")]
    [SerializeField] private Color _normalColor = new Color32(0x3D, 0x3B, 0x6E, 0xFF);

    [Tooltip("押し寄せる波の色（クリームゴールド）")]
    [SerializeField] private Color _waveColor = new Color32(0xFF, 0xE7, 0xA3, 0xFF);

    [Tooltip("ふだんの文字の色")]
    [SerializeField] private Color _normalLabelColor = new Color32(0xFF, 0xFD, 0xF5, 0xFF);

    [Tooltip("波に塗り替わったあとの文字の色")]
    [SerializeField] private Color _highlightLabelColor = new Color32(0x3D, 0x3B, 0x6E, 0xFF);

    [Header("押し寄せる・引く")]
    [Tooltip("波が押し寄せて塗り替わるまでの時間（秒）。少しゆっくりめにすると、ふわ〜っと見える")]
    [SerializeField] private float _waveInDuration = 0.5f;

    [Tooltip("波が引いていく時間（秒）")]
    [SerializeField] private float _waveOutDuration = 0.3f;

    [Tooltip("押し寄せる間に、もこもこが上下にずれる量（1 で山1つぶん）。0 なら動かない")]
    [SerializeField] private float _wavePhaseShift = 1f;

    [Header("波のフチから浮かぶ星")]
    [Tooltip("浮かぶ星の数（最小）")]
    [SerializeField] private int _edgeStarMin = 2;

    [Tooltip("浮かぶ星の数（最大）")]
    [SerializeField] private int _edgeStarMax = 3;

    [Tooltip("浮かぶ星の大きさ（最小・最大）")]
    [SerializeField] private Vector2 _edgeStarSizeRange = new Vector2(10f, 16f);

    [Tooltip("浮かび上がる高さ（Canvas 上の単位）")]
    [SerializeField] private float _edgeStarRise = 26f;

    [Tooltip("星が出てから消えるまでの時間（秒）")]
    [SerializeField] private float _edgeStarDuration = 0.7f;

    [Header("押し込み（マシュマロ）")]
    [Tooltip("押し込んでいる間の大きさ（X は横、Y は縦。元の何倍か）。横に少し広がって縦に潰れる")]
    [SerializeField] private Vector2 _squishScale = new Vector2(1.06f, 0.88f);

    [Tooltip("押し込んでいる間に沈む量（Canvas 上の単位）")]
    [SerializeField] private Vector2 _squishOffset = new Vector2(0f, -4f);

    [Tooltip("潰れるまでの時間（秒）")]
    [SerializeField] private float _squishDuration = 0.08f;

    [Header("決定（ポヨン＋紙吹雪）")]
    [Tooltip("ポヨンと弾んで戻るまでの時間（秒）")]
    [SerializeField] private float _bounceDuration = 0.4f;

    [Tooltip("弾む強さ。大きいほど行き過ぎてから戻る")]
    [SerializeField] private float _bounceOvershoot = 3f;

    [Tooltip("散る丸い星の数")]
    [SerializeField] private int _burstStarCount = 5;

    [Tooltip("散るドットの数")]
    [SerializeField] private int _burstDotCount = 8;

    [Tooltip("散る距離（最小・最大。Canvas 上の単位）")]
    [SerializeField] private Vector2 _burstDistanceRange = new Vector2(60f, 120f);

    [Tooltip("丸い星の大きさ（最小・最大）")]
    [SerializeField] private Vector2 _burstStarSizeRange = new Vector2(12f, 20f);

    [Tooltip("ドットの大きさ（最小・最大）")]
    [SerializeField] private Vector2 _burstDotSizeRange = new Vector2(5f, 9f);

    [Tooltip("散ってから消えるまでの時間（秒）")]
    [SerializeField] private float _burstDuration = 0.5f;

    private Tween _waveTween;
    private Tween _labelTween;

    // 丸っこい星の形（へこみを浅くする）と、ドット（角を多くして丸にする）
    private const int ROUND_STAR_POINTS = 5;
    private const float ROUND_STAR_INNER_RATIO = 0.7f;
    private const int DOT_POINTS = 8;
    private const float DOT_INNER_RATIO = 1f;

    // 波が板の外へ完全に出るように足す余白
    private const float WAVE_MARGIN = 8f;

    protected override void Awake()
    {
        base.Awake();
        if (_wave != null) _wave.color = _waveColor;
    }

    protected override Color NormalFrameColor => _normalColor;

    protected override Color NormalLabelColor => _normalLabelColor;

    /// <summary> 右から波を押し寄せさせ、フチから星を浮かべる。 </summary>
    protected override void PlayHighlight(bool animate)
    {
        if (_wave == null || Frame == null) return;

        FitWaveSize();
        KillTween(_waveTween);
        KillTween(_labelTween);

        float startX = _wave.rectTransform.anchoredPosition.x;
        float targetX = CoveredX();

        if (!animate)
        {
            SetWaveX(targetX);
            _labelTween = TweenColor(Label, _highlightLabelColor, 0f);
            return;
        }

        _waveTween = DOTween.Sequence()
            .Append(DOTween.To(() => _wave.rectTransform.anchoredPosition.x, SetWaveX, targetX, _waveInDuration).SetEase(Ease.OutSine))
            .Join(DOTween.To(() => _wave.Phase, value => _wave.Phase = value, _wave.Phase + _wavePhaseShift, _waveInDuration).SetEase(Ease.OutSine))
            .SetUpdate(IgnoreTimeScale)
            .SetLink(gameObject);

        // 波がボタンの真ん中あたりを過ぎるころに、文字の色を変える
        _labelTween = TweenColor(Label, _highlightLabelColor, _waveInDuration * 0.4f, _waveInDuration * 0.35f);

        int starCount = Random.Range(_edgeStarMin, Mathf.Max(_edgeStarMin, _edgeStarMax) + 1);
        for (int i = 0; i < starCount; i++) ScheduleEdgeStar(startX, targetX);
    }

    /// <summary> 波を右へ引かせて、ふだんの見た目に戻す。 </summary>
    protected override void PlayUnhighlight()
    {
        if (_wave == null || Frame == null) return;

        KillTween(_waveTween);
        KillTween(_labelTween);
        _waveTween = DOTween.To(() => _wave.rectTransform.anchoredPosition.x, SetWaveX, HiddenX(), _waveOutDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(IgnoreTimeScale)
            .SetLink(gameObject);
        _labelTween = TweenColor(Label, _normalLabelColor, _waveOutDuration * 0.5f);
    }

    /// <summary> マシュマロのように、ぷにっと潰れて沈む。 </summary>
    protected override void PlayHoldStart()
    {
        AnimateVisual(new Vector3(_squishScale.x, _squishScale.y, 1f), _squishOffset, _squishDuration, Ease.OutQuad);
    }

    /// <summary> ポヨンと弾んで元に戻る。 </summary>
    protected override void PlayHoldEnd()
    {
        AnimateVisual(Vector3.one, Vector2.zero, _bounceDuration, Ease.OutBack, _bounceOvershoot);
    }

    /// <summary> ポヨンと弾ませて、丸い星とドットの紙吹雪を散らす。キーで決定したときは、先に一瞬潰してから弾ませる。 </summary>
    protected override void PlayPress(bool fromHold)
    {
        if (!fromHold && VisualRoot != null)
        {
            Vector3 squish = new Vector3(_squishScale.x, _squishScale.y, 1f);
            ReplaceVisualTween(DOTween.Sequence()
                .Append(VisualRoot.DOScale(squish, _squishDuration).SetEase(Ease.OutQuad))
                .Append(CreateVisualTween(Vector3.one, _bounceDuration, Ease.OutBack, _bounceOvershoot)));
        }

        SpawnBurst(Vector2.zero, _burstStarCount, _burstDistanceRange, _burstStarSizeRange, ROUND_STAR_POINTS, ROUND_STAR_INNER_RATIO, _burstDuration);
        SpawnBurst(Vector2.zero, _burstDotCount, _burstDistanceRange, _burstDotSizeRange, DOT_POINTS, DOT_INNER_RATIO, _burstDuration);
    }

    /// <summary> この演出は決定しても消えないので、元に戻す動きは無い。 </summary>
    protected override void PlayRestore()
    {
    }

    protected override void ResetVisuals()
    {
        base.ResetVisuals();
        if (_wave == null || Frame == null) return;

        FitWaveSize();
        SetWaveX(HiddenX());
    }

    protected override void KillAllTweens()
    {
        base.KillAllTweens();
        KillTween(_waveTween);
        KillTween(_labelTween);
    }

    /// <summary> 波が押し寄せる途中のどこかで、そのときのフチの位置から丸っこい星を1つ浮かべる予約をする。 </summary>
    private void ScheduleEdgeStar(float startX, float targetX)
    {
        Rect frame = Frame.rectTransform.rect;
        float progress = Random.Range(0.15f, 0.7f);

        // 波は OutSine で動くので、時間の進み具合を同じ形で位置に直して、そのときのフチの位置を求める
        float edgeX = Mathf.Lerp(startX, targetX, Mathf.Sin(progress * Mathf.PI * 0.5f)) + _wave.BumpRadius * 0.5f;
        float x = Mathf.Clamp(edgeX, -frame.width * 0.5f, frame.width * 0.5f);
        float y = Random.Range(-frame.height * 0.3f, frame.height * 0.3f);

        var start = new Vector2(x, y);
        var end = start + new Vector2(Random.Range(-6f, 6f), _edgeStarRise);
        float size = Random.Range(_edgeStarSizeRange.x, _edgeStarSizeRange.y);
        float spin = Random.Range(-60f, 60f);

        DOVirtual.DelayedCall(_waveInDuration * progress,
                () => SpawnParticle(start, end, size, ROUND_STAR_POINTS, ROUND_STAR_INNER_RATIO, _edgeStarDuration, Ease.OutSine, spin),
                IgnoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 波が板の右の外にいるときの位置（波の左端の X）。 </summary>
    private float HiddenX()
    {
        return Frame.rectTransform.rect.width * 0.5f + WAVE_MARGIN;
    }

    /// <summary> 波が板を全部覆ったときの位置（もこもこのフチまで板の左の外に出る）。 </summary>
    private float CoveredX()
    {
        return -Frame.rectTransform.rect.width * 0.5f - _wave.BumpRadius * 2f - WAVE_MARGIN;
    }

    /// <summary> 波の大きさを、板を全部覆えるように合わせる。 </summary>
    private void FitWaveSize()
    {
        Rect frame = Frame.rectTransform.rect;
        RectTransform waveRect = _wave.rectTransform;
        waveRect.pivot = new Vector2(0f, 0.5f);
        waveRect.sizeDelta = new Vector2(frame.width + _wave.BumpRadius * 2f + WAVE_MARGIN * 2f, frame.height + WAVE_MARGIN * 2f);
    }

    /// <summary> 波の横位置を変える。 </summary>
    private void SetWaveX(float x)
    {
        _wave.rectTransform.anchoredPosition = new Vector2(x, 0f);
    }
}
