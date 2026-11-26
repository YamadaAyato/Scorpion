using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 「泡シュワ塗り替え＆星くるりん」の演出（デザイナーの UI パーツ：カプセル型のボタン・右上の泡・左の丸い星 を動かす）。
/// ButtonBase（を継承したボタン）と同じオブジェクトに付ける。
/// ・ふだん：星は左にいて、右上の泡は出ていない
/// ・選んだ瞬間：星がくるくる転がりながら右へ進んで消える。星が泡の位置を通り過ぎると、そこに泡がぷくっと出てきて脈打つ。
///   泡が出そろったら、右から泡の波が押し寄せて色を塗り替える。選んでいる間は泡粒がぽわぽわ昇り続ける
/// ・選択が外れたとき：泡がしぼんで消え、波が引き、星が左の元の位置にポンと戻る
/// ・マウスを押し込んでいる間：ボタンがマシュマロのようにぷにっと潰れ、泡がキュッと内側に縮む（タメ）
/// ・決定：泡がパチンッと弾けてしぶきが飛ぶ。泡は少しして戻ってくる
/// 星と泡は、それぞれ Pivot をパーツの中心にして、CanvasGroup を付けておく。
/// </summary>
public class BubbleSodaButtonEffect : ButtonEffectBase
{
    [Header("パーツ")]
    [Tooltip("右から押し寄せて色を塗り替える泡の波（WaveEdgeGraphic を使うと、もこもこが転がる）。Wave Area の中に置き、Pivot を左端（X = 0）にする")]
    [SerializeField] private RectTransform _wave;

    [Tooltip("波が動く範囲（ボタンの板と同じ大きさの枠）。板の Mask の中に置く")]
    [SerializeField] private RectTransform _waveArea;

    [Tooltip("丸い星。Pivot を星の中心にして、CanvasGroup を付けておく")]
    [SerializeField] private RectTransform _star;

    [Tooltip("右上の泡（大・小など）。星が通る順（左から）に並べ、それぞれ Pivot を泡の中心にして、CanvasGroup を付けておく")]
    [SerializeField] private RectTransform[] _bubbles;

    [Header("色")]
    [Tooltip("ふだんの板の色（ButtonBorder の紺）")]
    [SerializeField] private Color _normalColor = new Color32(0x02, 0x28, 0x73, 0xFF);

    [Tooltip("泡の波の色（PopButton の黄）")]
    [SerializeField] private Color _waveColor = new Color32(0xFF, 0xBF, 0x36, 0xFF);

    [Tooltip("ふだんの文字の色")]
    [SerializeField] private Color _normalLabelColor = new Color32(0xFF, 0xFD, 0xF5, 0xFF);

    [Tooltip("塗り替わったあとの文字の色")]
    [SerializeField] private Color _highlightLabelColor = new Color32(0x02, 0x28, 0x73, 0xFF);

    [Tooltip("泡粒・しぶきの色。この中からランダムに選ぶ")]
    [SerializeField] private Color[] _bubbleParticleColors =
    {
        new Color32(0x9B, 0xE7, 0xFF, 0xFF),
        new Color32(0xFF, 0xFF, 0xFF, 0xFF),
    };

    [Header("星が右へ転がって消える")]
    [Tooltip("星が右へ進む距離（Canvas 上の単位）。ボタンの右端を越えるくらいにする")]
    [SerializeField] private float _starTravelDistance = 280f;

    [Tooltip("星が右へ進んで消えるまでの時間（秒）")]
    [SerializeField] private float _starTravelDuration = 0.45f;

    [Tooltip("進むときに、ぴょんと弧を描く高さ（Canvas 上の単位）")]
    [SerializeField] private float _starHopHeight = 14f;

    [Tooltip("進む間に回る角度（度）。マイナスで右回り（転がるように見える）")]
    [SerializeField] private float _starSpinAngle = -360f;

    [Tooltip("どこから小さく・透明になって消え始めるか（進む時間に対する割合）")]
    [SerializeField, Range(0f, 1f)] private float _starVanishStartRate = 0.7f;

    [Tooltip("選択が外れたとき、星が左の元の位置に戻ってくるまでの待ち時間（秒）")]
    [SerializeField] private float _starReturnDelay = 0.1f;

    [Tooltip("星がポンと戻ってくるまでの時間（秒）")]
    [SerializeField] private float _starReturnDuration = 0.3f;

    [Header("泡がぷくっと出る・脈打つ")]
    [Tooltip("泡がぷくっと出てくるまでの時間（秒）")]
    [SerializeField] private float _bubblePuffDuration = 0.3f;

    [Tooltip("泡が出てくるときに、行き過ぎてから戻る強さ。大きいほどぷくっと弾む")]
    [SerializeField] private float _bubblePuffOvershoot = 3f;

    [Tooltip("選んでいる間、泡が膨らむ大きさ（元の何倍か）")]
    [SerializeField] private float _bubblePulseScale = 1.08f;

    [Tooltip("泡が膨らむ（または戻る）までの時間（秒）")]
    [SerializeField] private float _bubblePulseDuration = 0.45f;

    [Tooltip("選択が外れたとき、泡がしぼんで消えるまでの時間（秒）")]
    [SerializeField] private float _bubbleDeflateDuration = 0.2f;

    [Header("泡の塗り替え")]
    [Tooltip("泡の波が押し寄せて塗り替わるまでの時間（秒）")]
    [SerializeField] private float _waveInDuration = 0.45f;

    [Tooltip("泡の波が引いていく時間（秒）")]
    [SerializeField] private float _waveOutDuration = 0.3f;

    [Tooltip("押し寄せる間に、波のフチのもこもこが上下にずれる量（1 で山1つぶん）")]
    [SerializeField] private float _wavePhaseShift = 1f;

    [Header("昇る泡粒")]
    [Tooltip("次の泡粒が出るまでの時間（最短・最長。秒）")]
    [SerializeField] private Vector2 _riseIntervalRange = new Vector2(0.25f, 0.5f);

    [Tooltip("泡粒の大きさ（最小・最大）")]
    [SerializeField] private Vector2 _riseSizeRange = new Vector2(6f, 12f);

    [Tooltip("泡粒が昇る高さ（最小・最大。Canvas 上の単位）")]
    [SerializeField] private Vector2 _riseHeightRange = new Vector2(40f, 70f);

    [Tooltip("泡粒が出てから消えるまでの時間（最短・最長。秒）")]
    [SerializeField] private Vector2 _riseDurationRange = new Vector2(0.9f, 1.3f);

    [Tooltip("泡粒が出る位置のばらつき（泡の中心からの距離。Canvas 上の単位）")]
    [SerializeField] private float _riseSpread = 14f;

    [Header("押し込み（タメ）")]
    [Tooltip("押し込んでいる間のボタンの大きさ（X は横、Y は縦。元の何倍か）。横に少し広がって縦に潰れる")]
    [SerializeField] private Vector2 _squishScale = new Vector2(1.06f, 0.9f);

    [Tooltip("押し込んでいる間に沈む量（Canvas 上の単位）")]
    [SerializeField] private Vector2 _squishOffset = new Vector2(0f, -4f);

    [Tooltip("潰れるまでの時間（秒）")]
    [SerializeField] private float _squishDuration = 0.08f;

    [Tooltip("押し込んでいる間の泡の大きさ（元の何倍か）")]
    [SerializeField] private float _holdBubbleScale = 0.82f;

    [Tooltip("押し込んでいる間に、泡をボタンの内側（左下）へ寄せる量（Canvas 上の単位）")]
    [SerializeField] private float _holdBubblePull = 6f;

    [Header("決定")]
    [Tooltip("ポヨンと弾んで戻るまでの時間（秒）")]
    [SerializeField] private float _bounceDuration = 0.4f;

    [Tooltip("弾む強さ。大きいほど行き過ぎてから戻る")]
    [SerializeField] private float _bounceOvershoot = 3f;

    [Tooltip("泡が弾けるときに膨らむ大きさ（元の何倍か）")]
    [SerializeField] private float _bubblePopScale = 1.4f;

    [Tooltip("泡が弾けて消えるまでの時間（秒）")]
    [SerializeField] private float _bubblePopDuration = 0.12f;

    [Tooltip("泡1つから飛ぶしぶきの数")]
    [SerializeField] private int _dropletCount = 6;

    [Tooltip("しぶきが飛ぶ距離（最小・最大。Canvas 上の単位）")]
    [SerializeField] private Vector2 _dropletDistanceRange = new Vector2(25f, 55f);

    [Tooltip("しぶきの大きさ（最小・最大）")]
    [SerializeField] private Vector2 _dropletSizeRange = new Vector2(4f, 8f);

    [Tooltip("しぶきが飛んでから消えるまでの時間（秒）")]
    [SerializeField] private float _dropletDuration = 0.4f;

    [Tooltip("弾けた泡が、またぷくっと出てくるまでの待ち時間（秒）")]
    [SerializeField] private float _bubbleReturnDelay = 0.45f;

    private Vector2 _starBasePosition;
    private CanvasGroup _starGroup;
    private Vector2[] _bubbleBasePositions;
    private CanvasGroup[] _bubbleGroups;
    private Tween[] _bubbleTweens;
    private bool[] _isBubbleShown;
    private WaveEdgeGraphic _waveEdge;
    private Tween _waveTween;
    private Tween _labelTween;
    private Tween _starTween;
    private Tween _riseTween;
    private bool _isWaveCovered;

    // 泡粒・しぶきの形（角を多くして丸にする）
    private const int DOT_POINTS = 10;
    private const float DOT_INNER_RATIO = 1f;

    // 波が板の外へ完全に出るように足す余白
    private const float WAVE_MARGIN = 8f;

    protected override void Awake()
    {
        base.Awake();

        if (_wave != null)
        {
            _waveEdge = _wave.GetComponent<WaveEdgeGraphic>();
            var waveGraphic = _wave.GetComponent<Graphic>();
            if (waveGraphic != null) waveGraphic.color = _waveColor;
        }

        if (_star != null)
        {
            _starBasePosition = _star.anchoredPosition;
            _starGroup = _star.GetComponent<CanvasGroup>();
        }

        int count = BubbleCount;
        _bubbleBasePositions = new Vector2[count];
        _bubbleGroups = new CanvasGroup[count];
        _bubbleTweens = new Tween[count];
        _isBubbleShown = new bool[count];
        for (int i = 0; i < count; i++)
        {
            if (_bubbles[i] == null) continue;
            _bubbleBasePositions[i] = _bubbles[i].anchoredPosition;
            _bubbleGroups[i] = _bubbles[i].GetComponent<CanvasGroup>();
        }
    }

    /// <summary> 星が右へ進み終わったとき。通り過ぎ判定から漏れた泡があれば、ここで出す。 </summary>
    private void HandleStarTravelFinished()
    {
        HideStar();
        for (int i = 0; i < BubbleCount; i++) PuffBubble(i);
    }

    /// <summary> 弾けた泡の待ち時間が終わったとき。まだ選ばれていれば、またぷくっと出す。 </summary>
    private void HandleBubblePopFinished(int index)
    {
        _bubbles[index].localScale = Vector3.zero;
        _bubbles[index].anchoredPosition = _bubbleBasePositions[index];
        if (!IsHighlighted) return;

        PuffBubble(index);
        if (AllBubblesShown) ScheduleRise();
    }

    protected override Color NormalFrameColor => _normalColor;

    protected override Color NormalLabelColor => _normalLabelColor;

    /// <summary> 星を右へ転がして消し、通り過ぎた泡をぷくっと出す。泡が出そろったら波で塗り替える。 </summary>
    protected override void PlayHighlight(bool animate)
    {
        KillTween(_starTween);

        if (!animate)
        {
            // 動きなしのときは、星が通り過ぎて泡が出そろい、塗り替わったあとの見た目にする
            HideStar();
            for (int i = 0; i < BubbleCount; i++) ShowBubbleImmediately(i);
            MoveWave(true, false);
            KillTween(_labelTween);
            _labelTween = TweenColor(Label, _highlightLabelColor, 0f);
            ScheduleRise();
            return;
        }

        if (_star == null)
        {
            for (int i = 0; i < BubbleCount; i++) PuffBubble(i);
            return;
        }

        PlaceStarAtHome(1f);
        _starTween = DOTween.To(() => 0f, ApplyStarTravel, 1f, _starTravelDuration)
            .SetEase(Ease.Linear)
            .OnComplete(HandleStarTravelFinished)
            .SetUpdate(IgnoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 泡をしぼませて消し、波を引かせ、星を左の元の位置にポンと戻す。 </summary>
    protected override void PlayUnhighlight()
    {
        KillTween(_starTween);
        KillTween(_riseTween);

        MoveWave(false, true);
        KillTween(_labelTween);
        _labelTween = TweenColor(Label, _normalLabelColor, _waveOutDuration * 0.5f);

        for (int i = 0; i < BubbleCount; i++) DeflateBubble(i);

        if (_star != null)
        {
            PlaceStarAtHome(0f);
            _starTween = _star.DOScale(Vector3.one, _starReturnDuration)
                .SetDelay(_starReturnDelay)
                .SetEase(Ease.OutBack)
                .SetUpdate(IgnoreTimeScale)
                .SetLink(gameObject);
        }
    }

    /// <summary> ボタンをぷにっと潰し、出ている泡をキュッと内側に縮める（タメ）。 </summary>
    protected override void PlayHoldStart()
    {
        AnimateVisual(new Vector3(_squishScale.x, _squishScale.y, 1f), _squishOffset, _squishDuration, Ease.OutQuad);
        KillTween(_riseTween);

        for (int i = 0; i < BubbleCount; i++)
        {
            if (_bubbles[i] == null || !_isBubbleShown[i]) continue;

            Vector2 target = _bubbleBasePositions[i] + new Vector2(-_holdBubblePull, -_holdBubblePull * 0.5f);
            SetBubbleTween(i, DOTween.Sequence()
                .Append(_bubbles[i].DOScale(Vector3.one * _holdBubbleScale, _squishDuration).SetEase(Ease.OutQuad))
                .Join(TweenAnchored(_bubbles[i], target, _squishDuration).SetEase(Ease.OutQuad)));
        }
    }

    /// <summary> ボタンと泡をポヨンと元に戻す（ボタンの上で離したときは、このあと決定の演出に切り替わる）。 </summary>
    protected override void PlayHoldEnd()
    {
        AnimateVisual(Vector3.one, Vector2.zero, _bounceDuration, Ease.OutBack, _bounceOvershoot);

        for (int i = 0; i < BubbleCount; i++)
        {
            if (_bubbles[i] == null || !_isBubbleShown[i]) continue;

            int index = i;
            SetBubbleTween(i, DOTween.Sequence()
                .Append(_bubbles[i].DOScale(Vector3.one, _bounceDuration).SetEase(Ease.OutBack, _bounceOvershoot))
                .Join(TweenAnchored(_bubbles[i], _bubbleBasePositions[i], _bounceDuration).SetEase(Ease.OutBack))
                .OnComplete(() => StartBubblePulseIfHighlighted(index)));
        }
        ScheduleRise();
    }

    /// <summary> 出ている泡をパチンッと弾けさせてしぶきを飛ばす。キーで決定したときは、先に一瞬潰してから弾ませる。 </summary>
    protected override void PlayPress(bool fromHold)
    {
        if (!fromHold && VisualRoot != null)
        {
            Vector3 squish = new Vector3(_squishScale.x, _squishScale.y, 1f);
            ReplaceVisualTween(DOTween.Sequence()
                .Append(VisualRoot.DOScale(squish, _squishDuration).SetEase(Ease.OutQuad))
                .Append(CreateVisualTween(Vector3.one, _bounceDuration, Ease.OutBack, _bounceOvershoot)));
        }

        KillTween(_riseTween);
        for (int i = 0; i < BubbleCount; i++)
        {
            if (_isBubbleShown[i]) PopBubble(i);
        }
    }

    /// <summary> 泡は弾けたあと自分で戻ってくるので、ここでは何もしない。 </summary>
    protected override void PlayRestore()
    {
    }

    protected override void ResetVisuals()
    {
        base.ResetVisuals();

        _isWaveCovered = false;
        MoveWave(false, false);
        PlaceStarAtHome(1f);

        for (int i = 0; i < BubbleCount; i++)
        {
            if (_bubbles[i] == null) continue;
            _isBubbleShown[i] = false;
            _bubbles[i].anchoredPosition = _bubbleBasePositions[i];
            _bubbles[i].localScale = Vector3.zero;
            SetBubbleAlpha(i, 0f);
        }
    }

    protected override void KillAllTweens()
    {
        base.KillAllTweens();
        KillTween(_waveTween);
        KillTween(_labelTween);
        KillTween(_starTween);
        KillTween(_riseTween);
        for (int i = 0; i < BubbleCount; i++) KillTween(_bubbleTweens[i]);
    }

    /// <summary>
    /// 星を、右へ進む途中の位置（progress：0〜1）に置く。ぴょんと弧を描きながら転がり、終わりぎわに小さく・透明になって消える。
    /// 星が泡の位置を通り過ぎたら、その泡をぷくっと出す。
    /// </summary>
    private void ApplyStarTravel(float progress)
    {
        float x = DOVirtual.EasedValue(0f, _starTravelDistance, progress, Ease.InOutSine);
        float y = Mathf.Sin(progress * Mathf.PI) * _starHopHeight;
        _star.anchoredPosition = _starBasePosition + new Vector2(x, y);
        _star.localRotation = Quaternion.Euler(0f, 0f, DOVirtual.EasedValue(0f, _starSpinAngle, progress, Ease.OutQuad));

        float vanish = _starVanishStartRate >= 1f ? 0f : Mathf.Clamp01((progress - _starVanishStartRate) / (1f - _starVanishStartRate));
        _star.localScale = Vector3.one * (1f - vanish * vanish);
        if (_starGroup != null) _starGroup.alpha = 1f - vanish;

        // 泡の中心より右に来たら「通り過ぎた」とみなす（画面上の位置で比べる）
        for (int i = 0; i < BubbleCount; i++)
        {
            if (_bubbles[i] != null && !_isBubbleShown[i] && _star.position.x >= _bubbles[i].position.x) PuffBubble(i);
        }
    }

    /// <summary> 星を左の元の位置に置く（scale で大きさを指定。0 にしておけば、そこからポンと出せる）。 </summary>
    private void PlaceStarAtHome(float scale)
    {
        if (_star == null) return;

        _star.anchoredPosition = _starBasePosition;
        _star.localRotation = Quaternion.identity;
        _star.localScale = Vector3.one * scale;
        if (_starGroup != null) _starGroup.alpha = 1f;
    }

    /// <summary> 星を見えなくする（右へ進み終わった状態）。 </summary>
    private void HideStar()
    {
        if (_star == null) return;

        _star.anchoredPosition = _starBasePosition + new Vector2(_starTravelDistance, 0f);
        _star.localScale = Vector3.zero;
        if (_starGroup != null) _starGroup.alpha = 0f;
    }

    /// <summary> 泡をぷくっと出して、出終わったら脈打たせる。全部の泡が出そろったら、波で塗り替えて泡粒を出し始める。 </summary>
    private void PuffBubble(int index)
    {
        RectTransform bubble = _bubbles[index];
        if (bubble == null || _isBubbleShown[index]) return;

        _isBubbleShown[index] = true;
        bubble.anchoredPosition = _bubbleBasePositions[index];
        bubble.localScale = Vector3.zero;

        Sequence sequence = DOTween.Sequence()
            .Append(bubble.DOScale(Vector3.one, _bubblePuffDuration).SetEase(Ease.OutBack, _bubblePuffOvershoot))
            .OnComplete(() => StartBubblePulseIfHighlighted(index));
        CanvasGroup group = _bubbleGroups[index];
        if (group != null) sequence.Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, _bubblePuffDuration * 0.4f));
        SetBubbleTween(index, sequence);

        if (AllBubblesShown && IsHighlighted) CoverWithWave();
    }

    /// <summary> 泡を動きなしで出して、脈打たせる。 </summary>
    private void ShowBubbleImmediately(int index)
    {
        if (_bubbles[index] == null) return;

        _isBubbleShown[index] = true;
        _bubbles[index].anchoredPosition = _bubbleBasePositions[index];
        _bubbles[index].localScale = Vector3.one;
        SetBubbleAlpha(index, 1f);
        StartBubblePulseIfHighlighted(index);
    }

    /// <summary> 泡をしぼませて消す。 </summary>
    private void DeflateBubble(int index)
    {
        RectTransform bubble = _bubbles[index];
        if (bubble == null) return;

        _isBubbleShown[index] = false;
        Sequence sequence = DOTween.Sequence()
            .Append(bubble.DOScale(Vector3.zero, _bubbleDeflateDuration).SetEase(Ease.InBack))
            .Join(TweenAnchored(bubble, _bubbleBasePositions[index], _bubbleDeflateDuration));
        CanvasGroup group = _bubbleGroups[index];
        if (group != null) sequence.Join(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, _bubbleDeflateDuration));
        SetBubbleTween(index, sequence);
    }

    /// <summary> 泡をパチンッと弾けさせてしぶきを飛ばし、少ししてから（まだ選ばれていれば）またぷくっと出す。 </summary>
    private void PopBubble(int index)
    {
        RectTransform bubble = _bubbles[index];
        if (bubble == null) return;

        _isBubbleShown[index] = false;
        SpawnBurst(ToParticleSpace(bubble.position), _dropletCount, _dropletDistanceRange, _dropletSizeRange, DOT_POINTS, DOT_INNER_RATIO, _dropletDuration, RandomBubbleParticleColor());

        // 膨らむのと同時に透明にしてから、少し待って戻す
        Sequence sequence = DOTween.Sequence()
            .Append(bubble.DOScale(Vector3.one * _bubblePopScale, _bubblePopDuration).SetEase(Ease.OutQuad));
        CanvasGroup group = _bubbleGroups[index];
        if (group != null) sequence.Join(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, _bubblePopDuration));
        sequence.AppendInterval(_bubbleReturnDelay).OnComplete(() => HandleBubblePopFinished(index));
        SetBubbleTween(index, sequence);
    }

    /// <summary> 選ばれていて、押し込み中でなければ、泡を脈打たせ続ける。 </summary>
    private void StartBubblePulseIfHighlighted(int index)
    {
        if (!IsHighlighted || IsHolding || !_isBubbleShown[index] || _bubbles[index] == null) return;

        SetBubbleTween(index, _bubbles[index].DOScale(Vector3.one * _bubblePulseScale, _bubblePulseDuration)
            .SetDelay(index * 0.15f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo));
    }

    /// <summary> 右から泡の波を押し寄せさせて塗り替え、泡粒を出し始める（1回の選択で1回だけ）。 </summary>
    private void CoverWithWave()
    {
        if (_isWaveCovered) return;

        MoveWave(true, true);
        KillTween(_labelTween);
        _labelTween = TweenColor(Label, _highlightLabelColor, _waveInDuration * 0.4f, _waveInDuration * 0.35f);
        ScheduleRise();
    }

    /// <summary> 泡の波を押し寄せさせる（cover が true）か、引かせる（false）。animate が false ならすぐ動かす。 </summary>
    private void MoveWave(bool cover, bool animate)
    {
        _isWaveCovered = cover;
        if (_wave == null || _waveArea == null) return;

        FitWaveSize();
        KillTween(_waveTween);
        float target = cover ? CoveredX() : HiddenX();

        if (!animate)
        {
            SetWaveX(target);
            return;
        }

        float duration = cover ? _waveInDuration : _waveOutDuration;
        Sequence sequence = DOTween.Sequence()
            .Append(DOTween.To(() => _wave.anchoredPosition.x, SetWaveX, target, duration).SetEase(cover ? Ease.OutSine : Ease.InOutSine));

        // 押し寄せるときだけ、フチのもこもこを転がして「ぷくぷく」広がって見せる
        if (cover && _waveEdge != null)
        {
            sequence.Join(DOTween.To(() => _waveEdge.Phase, value => _waveEdge.Phase = value, _waveEdge.Phase + _wavePhaseShift, duration).SetEase(Ease.OutSine));
        }

        _waveTween = sequence.SetUpdate(IgnoreTimeScale).SetLink(gameObject);
    }

    /// <summary> 選んでいる間、泡粒を1つずつ出し続ける予約をする（泡が出そろってから）。 </summary>
    private void ScheduleRise()
    {
        KillTween(_riseTween);
        if (!IsHighlighted || IsHolding || !AllBubblesShown || BubbleCount == 0) return;

        float interval = Random.Range(_riseIntervalRange.x, Mathf.Max(_riseIntervalRange.x, _riseIntervalRange.y));
        _riseTween = DOVirtual.DelayedCall(interval, EmitRisingBubble, IgnoreTimeScale).SetLink(gameObject);
    }

    /// <summary> 右上の泡のどれかのまわりから、泡粒を1つぽわんと昇らせて、次を予約する。 </summary>
    private void EmitRisingBubble()
    {
        if (!IsHighlighted || IsHolding || !AllBubblesShown) return;

        RectTransform bubble = _bubbles[Random.Range(0, BubbleCount)];
        if (bubble != null)
        {
            Vector2 center = ToParticleSpace(bubble.position);
            Vector2 start = center + new Vector2(Random.Range(-_riseSpread, _riseSpread), Random.Range(-_riseSpread, _riseSpread) * 0.5f);
            Vector2 end = start + new Vector2(Random.Range(-10f, 10f), Random.Range(_riseHeightRange.x, _riseHeightRange.y));
            float size = Random.Range(_riseSizeRange.x, _riseSizeRange.y);
            float duration = Random.Range(_riseDurationRange.x, _riseDurationRange.y);
            SpawnParticle(start, end, size, DOT_POINTS, DOT_INNER_RATIO, duration, Ease.OutSine, 0f, RandomBubbleParticleColor());
        }
        ScheduleRise();
    }

    /// <summary> 泡の Tween を差し替える（その泡の前の Tween は止める）。 </summary>
    private void SetBubbleTween(int index, Tween tween)
    {
        KillTween(_bubbleTweens[index]);
        _bubbleTweens[index] = tween.SetUpdate(IgnoreTimeScale).SetLink(gameObject);
    }

    /// <summary> 泡の透明度を変える（CanvasGroup が無ければ何もしない）。 </summary>
    private void SetBubbleAlpha(int index, float alpha)
    {
        if (_bubbleGroups[index] != null) _bubbleGroups[index].alpha = alpha;
    }

    /// <summary> 泡粒・しぶきの色をランダムに選ぶ。 </summary>
    private Color RandomBubbleParticleColor()
    {
        if (_bubbleParticleColors == null || _bubbleParticleColors.Length == 0) return Color.white;
        return _bubbleParticleColors[Random.Range(0, _bubbleParticleColors.Length)];
    }

    /// <summary> 波が板の右の外にいるときの位置（波の左端の X）。 </summary>
    private float HiddenX()
    {
        return _waveArea.rect.width * 0.5f + WAVE_MARGIN;
    }

    /// <summary> 波が板を全部覆ったときの位置（もこもこのフチまで板の左の外に出る）。 </summary>
    private float CoveredX()
    {
        return -_waveArea.rect.width * 0.5f - WaveBumpRadius * 2f - WAVE_MARGIN;
    }

    /// <summary> 波の大きさを、板を全部覆えるように合わせる。 </summary>
    private void FitWaveSize()
    {
        Rect area = _waveArea.rect;
        _wave.pivot = new Vector2(0f, 0.5f);
        _wave.sizeDelta = new Vector2(area.width + WaveBumpRadius * 2f + WAVE_MARGIN * 2f, area.height + WAVE_MARGIN * 2f);
    }

    /// <summary> 波の横位置を変える。 </summary>
    private void SetWaveX(float x)
    {
        _wave.anchoredPosition = new Vector2(x, 0f);
    }

    /// <summary> RectTransform の位置を動かす Tween を作る。 </summary>
    private static Tween TweenAnchored(RectTransform target, Vector2 position, float duration)
    {
        return DOTween.To(() => target.anchoredPosition, value => target.anchoredPosition = value, position, duration);
    }

    /// <summary> 右上の泡が全部出ているか。 </summary>
    private bool AllBubblesShown
    {
        get
        {
            for (int i = 0; i < BubbleCount; i++)
            {
                if (_bubbles[i] != null && !_isBubbleShown[i]) return false;
            }
            return true;
        }
    }

    /// <summary> 波のフチのもこもこの半径（WaveEdgeGraphic でなければ 0）。 </summary>
    private float WaveBumpRadius => _waveEdge != null ? _waveEdge.BumpRadius : 0f;

    /// <summary> 右上の泡の数。 </summary>
    private int BubbleCount => _bubbles != null ? _bubbles.Length : 0;
}
