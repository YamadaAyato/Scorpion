using System.Collections.Generic;
using DG.Tweening;
using Scorpion.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 星と泡のボタン（StarBubbleButton）の演出。ButtonBase（を継承したボタン）と同じオブジェクトに付ける。
/// ・選んだとき：星が回りながら右へ転がり、星が通ったところからカプセルがピンクに塗られる。泡が一列にゆらゆら上り、花びらが舞う
/// ・選択が外れたとき：泡が消え、星が反対向きに転がって元の位置へ戻る（塗りも星について消える）
/// ・押したとき：泡・星粒・花びらがはじけ、星がそのまま大きくなって画面を覆う（StarTransitionOverlay）。
///   On Transition Covered に何か入れると、覆いきったときにそれを呼んで、シーンを切り替えずにそのまま元の画面を見せる（オプションのパネルの切り替えなど）
/// 星と泡は、Pivot をそれぞれの中心にした入れ物（Art/Star、Art/BubbleBig、Art/BubbleSmall）ごと動かす。
/// </summary>
public class StarBubbleButtonEffect : ButtonEffectBase
{
    [Tooltip("転がす星の入れ物。Pivot が星の中心になっているもの（Art/Star）")]
    [SerializeField] private RectTransform _star;

    [Tooltip("カプセルの塗り（Art/BarFill）。これを複製して、星が通ったところだけ別の色で塗る")]
    [SerializeField] private Image _barFill;

    [Tooltip("泡の見本になる入れ物。Pivot が泡の中心になっているもの（Art/BubbleBig、Art/BubbleSmall）。ふだんは隠しておく")]
    [SerializeField] private RectTransform[] _bubbles;

    [Tooltip("選んだときに星が右へ進む距離（Canvas 上の単位）。ボタンの右端に星が来るくらいにする")]
    [SerializeField] private float _rollDistance = 390f;

    [Tooltip("星が通ったあとの塗りの色")]
    [SerializeField] private Color _trailColor = new Color32(0xFF, 0x9E, 0xC7, 0xFF);

    [Tooltip("星が画面を覆いきったときにすること（オプションのパネルの切り替えなど）。何か入れると、シーンを切り替えずにそのまま元の画面を見せる。シーンを切り替えるボタンでは空にしておく")]
    [SerializeField] private UnityEvent _onTransitionCovered = new UnityEvent();

    [Tooltip("選んだとき（カーソルを合わせたとき）に鳴らす効果音の名前（AudioManager の SE List の Name）。空なら鳴らさない")]
    [SerializeField] private string _highlightSe = "se_botton_01";

    [Tooltip("押したときに鳴らす効果音の名前。空なら鳴らさない")]
    [SerializeField] private string _pressSe = "se_botton_02";

    private Vector2 _starBasePosition;
    private Vector2 _starBaseCenter;
    private float _starAngle;
    private readonly List<RectTransform> _bubblePool = new List<RectTransform>();
    private readonly List<Vector2> _bubbleBasePositions = new List<Vector2>();
    private readonly List<Vector2> _bubbleBaseCenters = new List<Vector2>();
    private readonly List<Tween> _bubbleTweens = new List<Tween>();
    private int _nextBubble;
    private Color _baseFrameColor = Color.white;
    private Color _baseLabelColor = Color.white;
    private Tween _starTween;
    private Tween _emitTween;
    private Tween _returnTween;
    private bool _isStarHidden;
    private StarTransitionOverlay _transition;
    private RectTransform _trailMask;

    // 選んだとき：1回転しながら進む時間（秒）。外れたとき：戻る時間（秒）
    private const float ROLL_ANGLE = 360f;
    private const float ROLL_DURATION = 0.6f;
    private const float RETURN_DURATION = 0.35f;

    // 塗り：星がふだんの位置にいるとみなすずれ（これ以下なら塗らない）と、残りも塗り始める進み具合（右へ進む距離に対する割合）
    private const float TRAIL_REST_THRESHOLD = 0.5f;
    private const float TRAIL_FULL_START = 0.85f;

    // 泡：使う数、出る位置（進み終わった星の中心からのずれ）、大きさ、間隔（秒）、上る高さと時間、揺れ
    private const int BUBBLE_COUNT = 3;
    private static readonly Vector2 BUBBLE_OFFSET = new Vector2(20f, 60f);
    private static readonly Vector2 BUBBLE_SCALE_RANGE = new Vector2(0.45f, 0.8f);
    private const float BUBBLE_FIRST_DELAY = 0.3f;
    private static readonly Vector2 BUBBLE_INTERVAL_RANGE = new Vector2(0.9f, 1.1f);
    private const float BUBBLE_APPEAR_DURATION = 0.25f;
    private const float BUBBLE_RISE_HEIGHT = 90f;
    private const float BUBBLE_RISE_DURATION = 2f;
    private const float BUBBLE_SWAY_WIDTH = 8f;
    private const float BUBBLE_SWAY_CYCLES = 1.5f;
    private const float BUBBLE_POP_SCALE = 1.25f;
    private const float BUBBLE_POP_DURATION = 0.1f;
    private const float BUBBLE_HIDE_DURATION = 0.15f;

    // 花びら：数、大きさ、時間（秒）、回る角度、選んだときの流れ方、押したときの広がり方
    private const int HIGHLIGHT_PETAL_COUNT = 10;
    private const int PRESS_PETAL_COUNT = 18;
    private static readonly Color[] PETAL_COLORS =
    {
        new Color32(0xFF, 0xB7, 0xD5, 0xFF),
        new Color32(0xFF, 0x9E, 0xC7, 0xFF),
        new Color32(0xFF, 0xE4, 0xF0, 0xFF),
        Color.white,
    };
    private static readonly Vector2 PETAL_SIZE_RANGE = new Vector2(10f, 18f);
    private static readonly Vector2 PETAL_DURATION_RANGE = new Vector2(0.8f, 1.2f);
    private static readonly Vector2 PETAL_SPIN_RANGE = new Vector2(180f, 540f);
    private static readonly Vector2 HIGHLIGHT_PETAL_DRIFT = new Vector2(80f, 90f);
    private static readonly Vector2 PRESS_PETAL_DISTANCE_RANGE = new Vector2(90f, 200f);
    private const float PRESS_PETAL_FALL_RATE = 0.4f;

    // 押したときにはじける泡と星粒：数、飛ぶ距離、時間（秒）、大きさ、色・形
    private const int BURST_BUBBLE_COUNT = 10;
    private const int BURST_STAR_COUNT = 6;
    private static readonly Vector2 BURST_DISTANCE_RANGE = new Vector2(60f, 130f);
    private const float BURST_DURATION = 0.5f;
    private static readonly Vector2 BURST_BUBBLE_SIZE_RANGE = new Vector2(12f, 24f);
    private static readonly Vector2 BURST_STAR_SIZE_RANGE = new Vector2(16f, 26f);
    private static readonly Color BURST_BUBBLE_COLOR = new Color32(0xE8, 0xFB, 0xFF, 0xFF);

    // 星で画面を覆う：覆いきる時間・消える時間と回る角度。シーンを切り替えるボタンの Submit Delay は COVER_DURATION より長くする
    private const float COVER_DURATION = 0.5f;
    private const float REVEAL_DURATION = 0.45f;
    private const float COVER_SPIN = 180f;

    // 星の絵のへこんだ角までの半径。画面を覆う大きさの計算に使う。星の絵を描き直したら測り直す
    private const float STAR_INNER_RADIUS_RATE = 215f / 2480f;

    // 覆ったあとボタンが残っていたとき、星が元の場所に戻ってくるまでの待ち時間・時間と、戻る前の向きのずれ
    private const float STAR_RETURN_DELAY = 0.15f;
    private const float STAR_RETURN_DURATION = 0.3f;
    private const float STAR_RETURN_SPIN = 90f;

    // StarShapeGraphic の形：丸、泡、丸い花びら、星粒、小さな花
    private const int CIRCLE_POINTS = 5;
    private const int ROUND_PETAL_POINTS = 6;
    private const float CIRCLE_INNER_RATIO = 1f;
    private const int STAR_POINTS = 5;
    private const float STAR_INNER_RATIO = 0.5f;
    private const int FLOWER_POINTS = 5;
    private const float FLOWER_INNER_RATIO = 0.72f;

    // 付けたときに自動で探す場所
    private const string STAR_PATH = "Art/Star";
    private const string BAR_FILL_PATH = "Art/BarFill";
    private static readonly string[] BUBBLE_PATHS = { "Art/BubbleBig", "Art/BubbleSmall" };

    protected override void Awake()
    {
        base.Awake();
        if (_star != null)
        {
            _starBasePosition = _star.anchoredPosition;
            _starBaseCenter = _star.localPosition;
        }
        BuildBubblePool();
        CreateTrailFill();

        if (Frame != null) _baseFrameColor = Frame.color;
        if (Label != null) _baseLabelColor = Label.color;
    }

    /// <summary> このコンポーネントを付けたときに、星・塗り・泡の入れ物を自動で探して入れる。 </summary>
    private void Reset()
    {
        _star = transform.Find(STAR_PATH) as RectTransform;
        Transform barFill = transform.Find(BAR_FILL_PATH);
        _barFill = barFill != null ? barFill.GetComponent<Image>() : null;

        var found = new List<RectTransform>();
        foreach (string path in BUBBLE_PATHS)
        {
            if (transform.Find(path) is RectTransform bubble) found.Add(bubble);
        }
        _bubbles = found.ToArray();
    }

    /// <summary> 星が画面を覆いきったとき。選択を外してから、入れておいたことをする。 </summary>
    private void HandleTransitionCovered()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject == gameObject) eventSystem.SetSelectedGameObject(null);
        _onTransitionCovered.Invoke();
    }

    protected override Color NormalFrameColor => _baseFrameColor;

    protected override Color NormalLabelColor => _baseLabelColor;

    /// <summary> 星を右へ転がし、泡を出し始め、花びらを舞わせる。animate が false なら、星は進んだ状態にするだけ。 </summary>
    protected override void PlayHighlight(bool animate)
    {
        if (_star == null) return;

        // 覆ったあと星が隠れている途中なら、元の場所に戻すだけにする
        if (_isStarHidden)
        {
            ReturnStar();
            return;
        }
        StopStarReturn();

        // Z の回転はマイナスが時計回り
        Vector2 target = RolledPosition();
        if (animate)
        {
            MoveStar(target, -ROLL_ANGLE, ROLL_DURATION);
            SpawnHighlightPetals();
            PlaySe(_highlightSe);
        }
        else
        {
            KillTween(_starTween);
            SetStar(target, -ROLL_ANGLE);
        }
        StartBubbles(animate ? BUBBLE_FIRST_DELAY : 0f);
    }

    /// <summary> 泡を消し、星を反対向きに転がして元の位置へ戻す。 </summary>
    protected override void PlayUnhighlight()
    {
        HideBubbles();
        if (_star == null) return;

        if (_isStarHidden)
        {
            ReturnStar();
            return;
        }
        StopStarReturn();
        MoveStar(_starBasePosition, 0f, RETURN_DURATION);
    }

    protected override void PlayHoldStart()
    {
    }

    protected override void PlayHoldEnd()
    {
    }

    /// <summary> 泡・星粒・花びらをはじけさせ、星の複製で画面を覆う。元の星は隠しておく。 </summary>
    protected override void PlayPress(bool fromHold)
    {
        if (_star == null) return;

        // 星がまだ転がっている途中なら、転がり終わった位置からはじけさせる
        KillTween(_returnTween);
        if (IsHighlighted)
        {
            KillTween(_starTween);
            SetStar(RolledPosition(), -ROLL_ANGLE);
        }

        Vector2 center = ToParticleSpace(_star.position);
        SpawnBurst(center, BURST_BUBBLE_COUNT, BURST_DISTANCE_RANGE, BURST_BUBBLE_SIZE_RANGE, CIRCLE_POINTS, CIRCLE_INNER_RATIO, BURST_DURATION, BURST_BUBBLE_COLOR);
        SpawnBurst(center, BURST_STAR_COUNT, BURST_DISTANCE_RANGE, BURST_STAR_SIZE_RANGE, STAR_POINTS, STAR_INNER_RATIO, BURST_DURATION);
        SpawnPressPetals(center);
        PlaySe(_pressSe);
        HideBubbles();

        bool hasCoveredAction = _onTransitionCovered.GetPersistentEventCount() > 0;
        float innerRadius = _star.rect.width * STAR_INNER_RADIUS_RATE;
        _transition = StarTransitionOverlay.Play(_star, innerRadius, COVER_DURATION, REVEAL_DURATION, COVER_SPIN,
            hasCoveredAction ? HandleTransitionCovered : (System.Action)null);

        _star.localScale = Vector3.zero;
        _isStarHidden = true;
    }

    /// <summary> 決定のあともボタンが残っていたとき。覆いをどけて、星を元の場所に戻す。 </summary>
    protected override void PlayRestore()
    {
        if (_transition != null)
        {
            _transition.Reveal();
            _transition = null;
        }
        if (_isStarHidden) ReturnStar();
    }

    /// <summary> 星を元の位置・向き・大きさに戻し、泡を隠す。 </summary>
    protected override void ResetVisuals()
    {
        base.ResetVisuals();
        _isStarHidden = false;
        if (_star != null)
        {
            SetStar(_starBasePosition, 0f);
            _star.localScale = Vector3.one;
        }

        for (int i = 0; i < _bubblePool.Count; i++)
        {
            if (_bubblePool[i] == null) continue;
            _bubblePool[i].anchoredPosition = _bubbleBasePositions[i];
            _bubblePool[i].localScale = Vector3.zero;
        }
    }

    /// <summary> 星と泡の動きも止める。 </summary>
    protected override void KillAllTweens()
    {
        base.KillAllTweens();
        KillTween(_starTween);
        KillTween(_returnTween);
        KillBubbleTweens();
    }

    /// <summary> 泡をそろえる。見本をそのまま使い、足りない分は見本を交互に複製して見本のすぐ後ろに置く。ふだんの位置と中心の位置も覚える。 </summary>
    private void BuildBubblePool()
    {
        var templates = new List<RectTransform>();
        if (_bubbles != null)
        {
            foreach (RectTransform bubble in _bubbles)
            {
                if (bubble != null) templates.Add(bubble);
            }
        }
        if (templates.Count == 0) return;

        int count = Mathf.Max(BUBBLE_COUNT, templates.Count);
        for (int i = 0; i < count; i++)
        {
            RectTransform template = templates[i % templates.Count];
            RectTransform bubble = template;
            if (i >= templates.Count)
            {
                bubble = Instantiate(template, template.parent);
                bubble.name = $"{template.name}_{i}";
                bubble.SetSiblingIndex(template.GetSiblingIndex() + 1);
            }

            _bubblePool.Add(bubble);
            _bubbleBasePositions.Add(template.anchoredPosition);
            _bubbleBaseCenters.Add(template.localPosition);
            _bubbleTweens.Add(null);
        }
    }

    /// <summary>
    /// 星が通ったあとの塗りを作る。Art の左端に横幅だけが変わるマスクを置き、その中にカプセルの塗りの複製を固定して入れる。
    /// マスクの横幅を星の中心までにすると、星が通ったところだけ見える。マスクは塗りのすぐ上に置く。
    /// </summary>
    private void CreateTrailFill()
    {
        if (_barFill == null) return;

        RectTransform fillRect = _barFill.rectTransform;
        var parent = (RectTransform)fillRect.parent;

        var maskObject = new GameObject("BarTrail", typeof(RectTransform), typeof(RectMask2D));
        maskObject.layer = _barFill.gameObject.layer;
        _trailMask = (RectTransform)maskObject.transform;
        _trailMask.SetParent(parent, false);
        _trailMask.SetSiblingIndex(fillRect.GetSiblingIndex() + 1);
        _trailMask.anchorMin = Vector2.zero;
        _trailMask.anchorMax = new Vector2(0f, 1f);
        _trailMask.pivot = new Vector2(0f, 0.5f);
        _trailMask.anchoredPosition = Vector2.zero;
        _trailMask.sizeDelta = Vector2.zero;

        Image trail = Instantiate(_barFill, _trailMask);
        trail.name = "Fill";
        trail.color = _trailColor;
        trail.raycastTarget = false;
        RectTransform trailRect = trail.rectTransform;
        trailRect.anchorMin = new Vector2(0f, 0.5f);
        trailRect.anchorMax = new Vector2(0f, 0.5f);
        trailRect.pivot = new Vector2(0f, 0.5f);
        trailRect.sizeDelta = parent.rect.size;
        trailRect.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 塗りの横幅を、Art の左端から星の中心までにする。星が右端の近くまで進んだら残りもなめらかに塗り、着いたときにはカプセル全体を塗る。
    /// 星がふだんの位置にいるときは塗らない。
    /// </summary>
    private void UpdateTrailFill()
    {
        if (_trailMask == null || _star == null) return;

        float width = 0f;
        float travel = _star.anchoredPosition.x - _starBasePosition.x;
        if (Mathf.Abs(travel) > TRAIL_REST_THRESHOLD)
        {
            var parent = (RectTransform)_trailMask.parent;
            float toStar = Mathf.Max(0f, _star.localPosition.x - parent.rect.xMin);
            float progress = Mathf.Approximately(_rollDistance, 0f) ? 1f : Mathf.Clamp01(travel / _rollDistance);
            float fullRate = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(TRAIL_FULL_START, 1f, progress));
            width = Mathf.Lerp(toStar, Mathf.Max(toStar, parent.rect.width), fullRate);
        }
        _trailMask.sizeDelta = new Vector2(width, 0f);
    }

    /// <summary> 右へ進み終わった星の位置。 </summary>
    private Vector2 RolledPosition()
    {
        return _starBasePosition + new Vector2(_rollDistance, 0f);
    }

    /// <summary> 星を、今の位置・角度から指定の位置・角度へ動かす（前の動きは止める）。 </summary>
    private void MoveStar(Vector2 targetPosition, float targetAngle, float duration)
    {
        KillTween(_starTween);
        _starTween = DOTween.Sequence()
            .Append(DOTween.To(() => _star.anchoredPosition, value => _star.anchoredPosition = value, targetPosition, duration).SetEase(Ease.OutCubic))
            .Join(DOTween.To(() => _starAngle, SetStarAngle, targetAngle, duration).SetEase(Ease.OutCubic))
            .OnUpdate(UpdateTrailFill)
            .SetUpdate(IgnoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 星の位置と角度をすぐ変える。 </summary>
    private void SetStar(Vector2 position, float angle)
    {
        _star.anchoredPosition = position;
        SetStarAngle(angle);
        UpdateTrailFill();
    }

    /// <summary> 星の角度を変える。localRotation から読み直すと 360 度を超えた分がわからなくなるので、角度は自分で覚えておく。 </summary>
    private void SetStarAngle(float angle)
    {
        _starAngle = angle;
        _star.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary> 隠れていた星を、元の場所に、少し回りながらポンと戻す。選ばれていれば泡も出し直す。 </summary>
    private void ReturnStar()
    {
        _isStarHidden = false;
        KillTween(_starTween);
        KillTween(_returnTween);

        bool highlighted = IsHighlighted;
        float homeAngle = highlighted ? -ROLL_ANGLE : 0f;
        SetStar(highlighted ? RolledPosition() : _starBasePosition, homeAngle + STAR_RETURN_SPIN);
        _star.localScale = Vector3.zero;
        _returnTween = DOTween.Sequence()
            .AppendInterval(STAR_RETURN_DELAY)
            .Append(_star.DOScale(Vector3.one, STAR_RETURN_DURATION).SetEase(Ease.OutBack))
            .Join(DOTween.To(() => _starAngle, SetStarAngle, homeAngle, STAR_RETURN_DURATION).SetEase(Ease.OutCubic))
            .SetUpdate(IgnoreTimeScale)
            .SetLink(gameObject);

        if (highlighted) StartBubbles(STAR_RETURN_DELAY + STAR_RETURN_DURATION);
    }

    /// <summary> 星が戻ってくる途中の動きを止めて、大きさを元に戻す。 </summary>
    private void StopStarReturn()
    {
        KillTween(_returnTween);
        _star.localScale = Vector3.one;
    }

    /// <summary> 選んだときの花吹雪：ボタンの上のあちこちから花びらが出て、左右に流れながら落ちる。 </summary>
    private void SpawnHighlightPetals()
    {
        Rect rect = ((RectTransform)transform).rect;
        for (int i = 0; i < HIGHLIGHT_PETAL_COUNT; i++)
        {
            var start = new Vector2(Random.Range(rect.xMin, rect.xMax), Random.Range(0f, rect.yMax));
            var drift = new Vector2(Random.Range(-HIGHLIGHT_PETAL_DRIFT.x, HIGHLIGHT_PETAL_DRIFT.x), -Random.Range(HIGHLIGHT_PETAL_DRIFT.y * 0.5f, HIGHLIGHT_PETAL_DRIFT.y));
            SpawnPetal(start, start + drift);
        }
    }

    /// <summary> 押したときの花吹雪：center から花びらが四方へ広がり、少し下へ落ちながら消える。 </summary>
    private void SpawnPressPetals(Vector2 center)
    {
        for (int i = 0; i < PRESS_PETAL_COUNT; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(PRESS_PETAL_DISTANCE_RANGE.x, PRESS_PETAL_DISTANCE_RANGE.y);
            var end = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) - PRESS_PETAL_FALL_RATE) * distance;
            SpawnPetal(center, end);
        }
    }

    /// <summary> 花びらを1枚舞わせる。形・大きさ・色・回り方・時間は毎回ランダムに選ぶ。 </summary>
    private void SpawnPetal(Vector2 start, Vector2 end)
    {
        bool isFlower = Random.value < 0.5f;
        float size = Random.Range(PETAL_SIZE_RANGE.x, PETAL_SIZE_RANGE.y);
        float duration = Random.Range(PETAL_DURATION_RANGE.x, PETAL_DURATION_RANGE.y);
        float spin = Random.Range(PETAL_SPIN_RANGE.x, PETAL_SPIN_RANGE.y) * (Random.value < 0.5f ? -1f : 1f);
        Color color = PETAL_COLORS[Random.Range(0, PETAL_COLORS.Length)];
        SpawnParticle(start, end, size,
            isFlower ? FLOWER_POINTS : ROUND_PETAL_POINTS,
            isFlower ? FLOWER_INNER_RATIO : CIRCLE_INNER_RATIO,
            duration, Ease.OutQuad, spin, color);
    }

    /// <summary> 泡を隠してから、delay 秒後に一列のポコポコを始める。 </summary>
    private void StartBubbles(float delay)
    {
        KillBubbleTweens();
        if (_bubblePool.Count == 0) return;

        foreach (RectTransform bubble in _bubblePool)
        {
            if (bubble != null) bubble.localScale = Vector3.zero;
        }
        _nextBubble = 0;
        _emitTween = DOVirtual.DelayedCall(delay, EmitBubble, IgnoreTimeScale).SetLink(gameObject);
    }

    /// <summary> 泡を1つ出して、少し待ってから次の泡を出す。泡は順番に使い回す。 </summary>
    private void EmitBubble()
    {
        if (!IsHighlighted || _bubblePool.Count == 0) return;

        PlayBubble(_nextBubble);
        _nextBubble = (_nextBubble + 1) % _bubblePool.Count;

        float interval = Random.Range(BUBBLE_INTERVAL_RANGE.x, BUBBLE_INTERVAL_RANGE.y);
        _emitTween = DOVirtual.DelayedCall(interval, EmitBubble, IgnoreTimeScale).SetLink(gameObject);
    }

    /// <summary> 泡を1回出す。進み終わった星の後ろでポコッと膨らみ、ゆらゆら真上へ上って、膨らんで弾ける。 </summary>
    private void PlayBubble(int index)
    {
        RectTransform bubble = _bubblePool[index];
        if (bubble == null) return;

        KillTween(_bubbleTweens[index]);
        float size = Random.Range(BUBBLE_SCALE_RANGE.x, BUBBLE_SCALE_RANGE.y);
        float sway = BUBBLE_SWAY_WIDTH * (Random.value < 0.5f ? -1f : 1f);
        // 泡の中心が、進み終わった星の中心＋ずれ に来る位置
        Vector2 start = _bubbleBasePositions[index] + (_starBaseCenter + new Vector2(_rollDistance, 0f) + BUBBLE_OFFSET - _bubbleBaseCenters[index]);
        float progress = 0f;

        bubble.anchoredPosition = start;
        bubble.localScale = Vector3.zero;
        _bubbleTweens[index] = DOTween.Sequence()
            .Append(bubble.DOScale(Vector3.one * size, BUBBLE_APPEAR_DURATION).SetEase(Ease.OutBack))
            .Insert(0f, DOTween.To(() => progress, value =>
            {
                progress = value;
                float swayX = Mathf.Sin(value * BUBBLE_SWAY_CYCLES * Mathf.PI * 2f) * sway;
                bubble.anchoredPosition = start + new Vector2(swayX, BUBBLE_RISE_HEIGHT * value);
            }, 1f, BUBBLE_RISE_DURATION).SetEase(Ease.Linear))
            .Append(bubble.DOScale(Vector3.one * (size * BUBBLE_POP_SCALE), BUBBLE_POP_DURATION * 0.5f).SetEase(Ease.OutQuad))
            .Append(bubble.DOScale(Vector3.zero, BUBBLE_POP_DURATION * 0.5f).SetEase(Ease.InQuad))
            .SetUpdate(IgnoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 泡を出すのをやめて、出ている泡をその場でしぼませて消す。 </summary>
    private void HideBubbles()
    {
        KillBubbleTweens();
        for (int i = 0; i < _bubblePool.Count; i++)
        {
            if (_bubblePool[i] == null) continue;
            _bubbleTweens[i] = _bubblePool[i].DOScale(Vector3.zero, BUBBLE_HIDE_DURATION)
                .SetEase(Ease.InBack)
                .SetUpdate(IgnoreTimeScale)
                .SetLink(gameObject);
        }
    }

    /// <summary> 泡の動きと、次の泡を出す予約をすべて止める。 </summary>
    private void KillBubbleTweens()
    {
        KillTween(_emitTween);
        _emitTween = null;
        for (int i = 0; i < _bubbleTweens.Count; i++)
        {
            KillTween(_bubbleTweens[i]);
            _bubbleTweens[i] = null;
        }
    }

    /// <summary> 名前が入っていれば、その効果音を鳴らす（AudioManager がなければ何もしない）。 </summary>
    private static void PlaySe(string seName)
    {
        if (!string.IsNullOrEmpty(seName)) AudioManager.Instance?.PlaySE(seName);
    }
}
