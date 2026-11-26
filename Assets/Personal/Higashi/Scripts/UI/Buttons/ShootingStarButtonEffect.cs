using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ボタンに「流れ星」の演出を付ける（ポップで柔らかい方向）。ButtonBase（を継承したボタン）と同じオブジェクトに付ける。
/// ・選んだ瞬間：背面の枠がポヨンと跳ねてズレ、太めのやわらかい光の帯が右上から左下へ走り抜けて星粒が2〜3個こぼれ、色が選択色に変わる
/// ・選んでいる間：2〜3秒おきに、控えめな光の筋が流れる（たまに星粒が1つこぼれる）
/// ・マウスを押し込んでいる間：流れ星を止めて色を一段濃くし、少しへこむ（ぷにっ）
/// ・決定：光の帯が走り、一瞬ぷっくり膨らんでから、ポンッと星になって弾ける
/// 部品（枠・流れ星など）は Inspector で設定する。星粒は実行中に自動で作る。
/// </summary>
[DisallowMultipleComponent]
public class ShootingStarButtonEffect : MonoBehaviour
{
    [Header("見た目の部品")]
    [Tooltip("膨らんだり弾けたりする見た目の親。ボタン本体とは別のオブジェクトにする（本体は ButtonBase が拡大・縮小するため）")]
    [SerializeField] private RectTransform _visualRoot;

    [Tooltip("弾けて消えるときに透明度を変える CanvasGroup（Visual Root に付ける）")]
    [SerializeField] private CanvasGroup _visualGroup;

    [Tooltip("色を塗り替える本体の板。流れ星はこの板の形で切り抜かれる（Mask を付けておく）")]
    [SerializeField] private Graphic _frame;

    [Tooltip("選んだときにズレて見える背面の枠")]
    [SerializeField] private RectTransform _backFrame;

    [Tooltip("ボタンの文字。選んでいる間は色を変える")]
    [SerializeField] private Graphic _label;

    [Tooltip("流れ星を斜めに傾けておく親。Z の回転が流れ星の角度になる（15〜30°がおすすめ）")]
    [SerializeField] private RectTransform _streakRoot;

    [Tooltip("流れ星本体。芯と尾をまとめて横に動かす")]
    [SerializeField] private RectTransform _streak;

    [Tooltip("流れ星の先頭の光の帯。ふちがぼやけた画像は自動で作る")]
    [SerializeField] private Image _streakCore;

    [Tooltip("流れ星の後ろに引く尾。後ろに行くほど透明になる画像は自動で作る")]
    [SerializeField] private Image _streakTail;

    [Header("色")]
    [Tooltip("ふだんの板の色。真っ黒に近い色は硬く見えるので、彩度の残った紺にする")]
    [SerializeField] private Color _normalColor = new Color32(0x3D, 0x3B, 0x6E, 0xFF);

    [Tooltip("選んでいるときの板の色")]
    [SerializeField] private Color _highlightColor = new Color32(0xFF, 0xD6, 0x6B, 0xFF);

    [Tooltip("マウスを押し込んでいる間の板の色。選んでいるときより一段濃くする")]
    [SerializeField] private Color _holdColor = new Color32(0xF5, 0xB8, 0x41, 0xFF);

    [Tooltip("ふだんの文字の色。真っ白よりクリーム寄りにすると優しく見える")]
    [SerializeField] private Color _normalLabelColor = new Color32(0xFF, 0xFD, 0xF5, 0xFF);

    [Tooltip("選んでいるときの文字の色")]
    [SerializeField] private Color _highlightLabelColor = new Color32(0x3D, 0x3B, 0x6E, 0xFF);

    [Tooltip("光の帯の色（クリーム寄りの白）")]
    [SerializeField] private Color _coreColor = new Color32(0xFF, 0xFD, 0xF5, 0xFF);

    [Tooltip("流れ星の尾の色")]
    [SerializeField] private Color _tailColor = new Color32(0x8B, 0xE9, 0xFF, 0xFF);

    [Header("背面の枠")]
    [Tooltip("選んだときに背面の枠をずらす量（Canvas 上の単位）")]
    [SerializeField] private Vector2 _backFrameOffset = new Vector2(10f, -10f);

    [Tooltip("背面の枠がずれる時間（秒）")]
    [SerializeField] private float _backFrameDuration = 0.25f;

    [Tooltip("背面の枠が行き過ぎてから戻る強さ。大きいほどポヨンと跳ねる")]
    [SerializeField] private float _backFrameOvershoot = 2.5f;

    [Header("選んだ瞬間の流れ星")]
    [Tooltip("走り抜ける時間（秒）")]
    [SerializeField] private float _highlightStreakDuration = 0.4f;

    [Tooltip("光の帯の太さ。細いレーザーではなく、少し太めにすると柔らかく見える")]
    [SerializeField] private float _highlightCoreWidth = 16f;

    [Tooltip("尾の長さ")]
    [SerializeField] private float _highlightTailLength = 160f;

    [Tooltip("こぼれる星粒の数（最小）")]
    [SerializeField] private int _highlightSparkleMin = 2;

    [Tooltip("こぼれる星粒の数（最大）")]
    [SerializeField] private int _highlightSparkleMax = 3;

    [Header("選んでいる間の瞬き")]
    [Tooltip("次の光の筋が流れるまでの最短の時間（秒）")]
    [SerializeField] private float _idleIntervalMin = 2f;

    [Tooltip("次の光の筋が流れるまでの最長の時間（秒）")]
    [SerializeField] private float _idleIntervalMax = 3f;

    [Tooltip("光の筋が流れる時間（秒）。選んだ瞬間より遅くして控えめにする")]
    [SerializeField] private float _idleStreakDuration = 0.7f;

    [Tooltip("光の筋の太さ")]
    [SerializeField] private float _idleCoreWidth = 8f;

    [Tooltip("光の筋の尾の長さ")]
    [SerializeField] private float _idleTailLength = 90f;

    [Tooltip("光の筋の濃さ（0〜1）")]
    [SerializeField, Range(0f, 1f)] private float _idleAlpha = 0.4f;

    [Tooltip("光の筋が流れるときに、星粒が1つこぼれる確率（0〜1）")]
    [SerializeField, Range(0f, 1f)] private float _idleSparkleChance = 0.5f;

    [Header("星粒")]
    [Tooltip("星粒・弾ける星の色。この中からランダムに選ぶ")]
    [SerializeField] private Color[] _sparkleColors =
    {
        new Color32(0xFF, 0xD6, 0x6B, 0xFF),
        new Color32(0xFF, 0xFD, 0xF5, 0xFF),
        new Color32(0x8B, 0xE9, 0xFF, 0xFF),
        new Color32(0xFF, 0x9E, 0xC7, 0xFF),
    };

    [Tooltip("こぼれる星粒の大きさ（最小・最大）")]
    [SerializeField] private Vector2 _sparkleSizeRange = new Vector2(10f, 18f);

    [Tooltip("星粒が落ちる距離（Canvas 上の単位）")]
    [SerializeField] private float _sparkleFallDistance = 30f;

    [Tooltip("星粒が出てから消えるまでの時間（秒）")]
    [SerializeField] private float _sparkleDuration = 0.65f;

    [Header("押し込み")]
    [Tooltip("マウスを押し込んでいる間の大きさ（元の何倍か）。少しへこませて、ぷにっとした手触りにする")]
    [SerializeField] private float _holdScale = 0.94f;

    [Tooltip("へこむまでの時間（秒）")]
    [SerializeField] private float _holdDuration = 0.08f;

    [Header("決定（ポンッと弾ける）")]
    [Tooltip("光の帯が走り抜ける時間（秒）")]
    [SerializeField] private float _submitStreakDuration = 0.12f;

    [Tooltip("光の帯の太さ")]
    [SerializeField] private float _submitCoreWidth = 28f;

    [Tooltip("光の帯の尾の長さ")]
    [SerializeField] private float _submitTailLength = 200f;

    [Tooltip("ぷっくり膨らんだときの大きさ（元の何倍か）")]
    [SerializeField] private float _popScale = 1.15f;

    [Tooltip("膨らむまでの時間（秒）")]
    [SerializeField] private float _popDuration = 0.08f;

    [Tooltip("弾けて消えるまでの時間（秒）")]
    [SerializeField] private float _vanishDuration = 0.1f;

    [Tooltip("弾けて飛び散る星の数")]
    [SerializeField] private int _burstCount = 8;

    [Tooltip("星が飛び散る距離（最小・最大。Canvas 上の単位）")]
    [SerializeField] private Vector2 _burstDistanceRange = new Vector2(70f, 130f);

    [Tooltip("飛び散る星の大きさ（最小・最大）")]
    [SerializeField] private Vector2 _burstSizeRange = new Vector2(16f, 28f);

    [Tooltip("星が飛び散ってから消えるまでの時間（秒）")]
    [SerializeField] private float _burstDuration = 0.45f;

    [Tooltip("決定のあとも画面が切り替わらなかったとき、元に戻し始めるまでの待ち時間（秒）")]
    [SerializeField] private float _restoreDelay = 0.1f;

    [Tooltip("元に戻るまでの時間（秒）")]
    [SerializeField] private float _restoreDuration = 0.3f;

    [Header("時間")]
    [Tooltip("Time.timeScale を無視して動かすか。ポーズ中（timeScale = 0）でも動くよう、基本はオンのままにする")]
    [SerializeField] private bool _ignoreTimeScale = true;

    private ButtonBase _button;
    private Vector2 _backFrameBasePosition;
    private RectTransform _sparkleRoot;
    private readonly List<StarShapeGraphic> _sparklePool = new List<StarShapeGraphic>();
    private bool _isHighlighted;
    private bool _isHolding;
    private Tween _frameColorTween;
    private Tween _labelColorTween;
    private Tween _backFrameTween;
    private Tween _holdTween;
    private Sequence _streakSequence;
    private Tween _idleTween;
    private Sequence _popSequence;

    // 光の帯（ふちがぼやけた画像）と尾（後ろほど透明な画像）。全部のボタンで1つを使い回す
    private static Sprite _coreSprite;
    private static Sprite _tailSprite;

    // 光の帯・尾の画像の横幅（ピクセル）
    private const int GRADIENT_TEXTURE_WIDTH = 64;

    // 流れ星がボタンの外から入って外へ抜けるよう、通る道のりに足す余白
    private const float STREAK_MARGIN = 20f;

    // 星粒をこぼす範囲（板の幅・高さに対する割合）
    private const float SPARKLE_AREA_WIDTH_RATE = 0.45f;
    private const float SPARKLE_AREA_HEIGHT_RATE = 0.25f;

    private void Awake()
    {
        _button = GetComponent<ButtonBase>();
        if (_button == null)
        {
            Debug.LogWarning($"{name}: {nameof(ShootingStarButtonEffect)} は ButtonBase を継承したボタンと同じオブジェクトに付けてください", this);
            enabled = false;
            return;
        }

        if (_backFrame != null) _backFrameBasePosition = _backFrame.anchoredPosition;
        if (_streakCore != null) _streakCore.sprite = GetCoreSprite();
        if (_streakTail != null) _streakTail.sprite = GetTailSprite();
        CreateSparkleRoot();
    }

    private void OnEnable()
    {
        if (_button == null) return;

        _button.OnHighlighted += HandleHighlighted;
        _button.OnUnhighlighted += HandleUnhighlighted;
        _button.OnHoldStarted += HandleHoldStarted;
        _button.OnHoldEnded += HandleHoldEnded;
        _button.OnPressed += HandlePressed;
        _button.OnSubmitted += HandleSubmitted;

        ResetVisuals();

        // 表示された時点ですでに選ばれていたら、流れ星なしで選択の見た目にする
        if (_button.IsHighlighted) ApplyHighlight(false);
    }

    private void OnDisable()
    {
        if (_button == null) return;

        _button.OnHighlighted -= HandleHighlighted;
        _button.OnUnhighlighted -= HandleUnhighlighted;
        _button.OnHoldStarted -= HandleHoldStarted;
        _button.OnHoldEnded -= HandleHoldEnded;
        _button.OnPressed -= HandlePressed;
        _button.OnSubmitted -= HandleSubmitted;

        KillAllTweens();
        ResetVisuals();
    }

    /// <summary> 選ばれたら、背面の枠をずらし、流れ星を走らせて選択色に塗り替える。 </summary>
    private void HandleHighlighted(ButtonBase button)
    {
        ApplyHighlight(true);
    }

    /// <summary> 選択が外れたら、ふだんの見た目に戻す。 </summary>
    private void HandleUnhighlighted(ButtonBase button)
    {
        _isHighlighted = false;
        KillTween(_idleTween);
        TweenFrameColor(_normalColor, 0.15f, 0f);
        TweenLabelColor(_normalLabelColor, 0.15f, 0f);
        MoveBackFrame(_backFrameBasePosition);
    }

    /// <summary> マウスを押し込んだら、流れ星を止めて色を一段濃くし、少しへこませる（タメ）。 </summary>
    private void HandleHoldStarted(ButtonBase button)
    {
        _isHolding = true;
        KillTween(_idleTween);
        KillTween(_streakSequence);
        HideStreak();
        TweenFrameColor(_holdColor, 0.06f, 0f);
        TweenVisualScale(_holdScale, _holdDuration, Ease.OutQuad);
    }

    /// <summary> 離したら、ポヨンと元の大きさに戻して選択中の見た目にする（ボタンの上で離したときは、このあと決定の演出に切り替わる）。 </summary>
    private void HandleHoldEnded(ButtonBase button)
    {
        _isHolding = false;
        TweenVisualScale(1f, _restoreDuration, Ease.OutBack);
        if (!_isHighlighted) return;

        TweenFrameColor(_highlightColor, 0.1f, 0f);
        ScheduleIdleStreak();
    }

    /// <summary> 決定されたら、光の帯を走らせ、ぷっくり膨らんでからポンッと星になって弾けさせる。 </summary>
    private void HandlePressed(ButtonBase button)
    {
        _isHolding = false;
        KillTween(_idleTween);
        KillTween(_holdTween);
        TweenFrameColor(_coreColor, _submitStreakDuration * 0.5f, 0f);
        PlayStreak(_submitStreakDuration, _submitCoreWidth, _submitTailLength, _coreColor, 1f, Ease.OutQuad, 0);
        PlayPop();
    }

    /// <summary> 決定の処理が終わってもボタンが表示されたままなら（パネルを開くボタンなど）、ポヨンと元の見た目に戻す。 </summary>
    private void HandleSubmitted(ButtonBase button)
    {
        if (!isActiveAndEnabled || _visualRoot == null) return;

        KillTween(_popSequence);
        _popSequence = DOTween.Sequence()
            .AppendInterval(_restoreDelay)
            .Append(_visualRoot.DOScale(Vector3.one, _restoreDuration).SetEase(Ease.OutBack))
            .Join(TweenVisualAlpha(1f, _restoreDuration * 0.5f))
            .OnComplete(ApplyCurrentStateAfterRestore)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 選択の見た目にする。withStreak が true なら、流れ星を走らせながら塗り替える。 </summary>
    private void ApplyHighlight(bool withStreak)
    {
        _isHighlighted = true;
        MoveBackFrame(_backFrameBasePosition + _backFrameOffset);

        if (withStreak && !_isHolding)
        {
            // 流れ星が板の上を通り過ぎるのに合わせて色が変わるよう、少し遅らせて塗り替える
            int sparkleCount = Random.Range(_highlightSparkleMin, Mathf.Max(_highlightSparkleMin, _highlightSparkleMax) + 1);
            PlayStreak(_highlightStreakDuration, _highlightCoreWidth, _highlightTailLength, _tailColor, 1f, Ease.OutCubic, sparkleCount);
            TweenFrameColor(_highlightColor, _highlightStreakDuration * 0.6f, _highlightStreakDuration * 0.15f);
            TweenLabelColor(_highlightLabelColor, _highlightStreakDuration * 0.6f, _highlightStreakDuration * 0.15f);
        }
        else
        {
            TweenFrameColor(_highlightColor, 0f, 0f);
            TweenLabelColor(_highlightLabelColor, 0f, 0f);
        }

        ScheduleIdleStreak();
    }

    /// <summary> 元に戻ったあと、そのときの選択状態に合った見た目にする。 </summary>
    private void ApplyCurrentStateAfterRestore()
    {
        if (_button.IsHighlighted)
        {
            _isHighlighted = true;
            TweenFrameColor(_highlightColor, 0.1f, 0f);
            TweenLabelColor(_highlightLabelColor, 0.1f, 0f);
            ScheduleIdleStreak();
        }
        else
        {
            HandleUnhighlighted(_button);
        }
    }

    /// <summary> 選んでいる間、2〜3秒おきに控えめな光の筋を流す予約をする。 </summary>
    private void ScheduleIdleStreak()
    {
        KillTween(_idleTween);
        if (!_isHighlighted || _isHolding) return;

        float interval = Random.Range(_idleIntervalMin, Mathf.Max(_idleIntervalMin, _idleIntervalMax));
        _idleTween = DOVirtual.DelayedCall(interval, PlayIdleStreak, _ignoreTimeScale).SetLink(gameObject);
    }

    /// <summary> 控えめな光の筋を1回流して、次を予約する。 </summary>
    private void PlayIdleStreak()
    {
        if (!_isHighlighted || _isHolding) return;

        // 別の流れ星が走っている途中なら、今回は流さずに次を待つ
        if (_streakSequence == null || !_streakSequence.IsActive())
        {
            int sparkleCount = Random.value < _idleSparkleChance ? 1 : 0;
            PlayStreak(_idleStreakDuration, _idleCoreWidth, _idleTailLength, _tailColor, _idleAlpha, Ease.OutSine, sparkleCount);
        }
        ScheduleIdleStreak();
    }

    /// <summary>
    /// 流れ星を1回走らせる。先頭の光の帯と後ろの尾を、板の右上の外から左下の外へ動かす。
    /// 初速を速くしてスッと抜けるよう Ease を指定し、終わりぎわに尾から先に消す。
    /// sparkleCount 個の星粒を、流れ星が通るタイミングに合わせてこぼす。
    /// </summary>
    private void PlayStreak(float duration, float coreWidth, float tailLength, Color tailColor, float alpha, Ease ease, int sparkleCount)
    {
        if (_frame == null || _streak == null || _streakCore == null || _streakTail == null) return;

        KillTween(_streakSequence);

        float halfLength = StreakTravelHalfLength();
        float height = StreakHeight();

        var coreRect = _streakCore.rectTransform;
        coreRect.sizeDelta = new Vector2(coreWidth, height);
        coreRect.anchoredPosition = Vector2.zero;

        // 尾は光の帯の中心（動く向きの反対側＝右）から伸ばす
        var tailRect = _streakTail.rectTransform;
        tailRect.pivot = new Vector2(0f, 0.5f);
        tailRect.sizeDelta = new Vector2(tailLength, height);
        tailRect.anchoredPosition = Vector2.zero;

        SetAlpha(_streakCore, _coreColor, alpha);
        SetAlpha(_streakTail, tailColor, alpha);

        _streak.gameObject.SetActive(true);
        float startX = halfLength + coreWidth;
        float endX = -(halfLength + coreWidth);
        _streak.anchoredPosition = new Vector2(startX, 0f);

        _streakSequence = DOTween.Sequence()
            .Append(DOTween.To(() => _streak.anchoredPosition.x, x => _streak.anchoredPosition = new Vector2(x, 0f), endX, duration).SetEase(ease))
            .Insert(duration * 0.45f, TweenAlpha(_streakTail, 0f, duration * 0.55f))
            .Insert(duration * 0.65f, TweenAlpha(_streakCore, 0f, duration * 0.35f))
            .OnComplete(HideStreak)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);

        for (int i = 0; i < sparkleCount; i++) ScheduleFallingSparkle(duration);
    }

    /// <summary> 流れ星が板の上のどこかを通るタイミングで、そこから星粒を1つこぼす予約をする。 </summary>
    private void ScheduleFallingSparkle(float streakDuration)
    {
        Rect rect = _frame.rectTransform.rect;
        float progress = Random.Range(0.2f, 0.75f);

        // 流れ星は初速が速く右から左へ進むので、時間の進み具合を EaseOut 相当で横位置に直す
        float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
        float x = Mathf.Lerp(rect.width * SPARKLE_AREA_WIDTH_RATE, -rect.width * SPARKLE_AREA_WIDTH_RATE, easedProgress);
        float y = Random.Range(-rect.height * SPARKLE_AREA_HEIGHT_RATE, rect.height * SPARKLE_AREA_HEIGHT_RATE);

        DOVirtual.DelayedCall(streakDuration * progress, () => SpawnFallingSparkle(new Vector2(x, y)), _ignoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 星粒を1つ、ポロッとこぼす（ポンと出て、くるっと回りながら少し落ちて消える）。 </summary>
    private void SpawnFallingSparkle(Vector2 position)
    {
        if (!isActiveAndEnabled) return;

        StarShapeGraphic sparkle = GetSparkle();
        float size = Random.Range(_sparkleSizeRange.x, _sparkleSizeRange.y);
        Vector2 endPosition = position + new Vector2(Random.Range(-10f, 10f), -_sparkleFallDistance);
        float spin = Random.Range(90f, 200f) * (Random.value < 0.5f ? -1f : 1f);
        PlaySparkle(sparkle, position, endPosition, size, spin, _sparkleDuration, Ease.OutQuad);
    }

    /// <summary> ぷっくり膨らんでから、ポンッと弾けて消える。弾けた瞬間に星を飛び散らせる。 </summary>
    private void PlayPop()
    {
        if (_visualRoot == null) return;

        KillTween(_popSequence);
        _popSequence = DOTween.Sequence()
            .AppendInterval(_submitStreakDuration * 0.3f)
            .Append(_visualRoot.DOScale(Vector3.one * _popScale, _popDuration).SetEase(Ease.OutQuad))
            .AppendCallback(SpawnBurst)
            .Append(_visualRoot.DOScale(Vector3.zero, _vanishDuration).SetEase(Ease.InBack))
            .Join(TweenVisualAlpha(0f, _vanishDuration))
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> ボタンの中心から、星を四方へ飛び散らせる。 </summary>
    private void SpawnBurst()
    {
        for (int i = 0; i < _burstCount; i++)
        {
            // 均等な角度に少しばらつきを足して、規則的に見えすぎないようにする
            float angle = (360f / Mathf.Max(1, _burstCount) * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            float distance = Random.Range(_burstDistanceRange.x, _burstDistanceRange.y);
            Vector2 endPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            float size = Random.Range(_burstSizeRange.x, _burstSizeRange.y);
            float spin = Random.Range(180f, 360f) * (Random.value < 0.5f ? -1f : 1f);
            PlaySparkle(GetSparkle(), Vector2.zero, endPosition, size, spin, _burstDuration, Ease.OutCubic);
        }
    }

    /// <summary> 星を1つ動かす：ポンと大きくなって、回りながら移動し、後半で小さく・透明になって消える。 </summary>
    private void PlaySparkle(StarShapeGraphic sparkle, Vector2 startPosition, Vector2 endPosition, float size, float spin, float duration, Ease moveEase)
    {
        RectTransform rect = sparkle.rectTransform;
        DOTween.Kill(rect);

        sparkle.gameObject.SetActive(true);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = startPosition;
        rect.localScale = Vector3.zero;
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        sparkle.color = RandomSparkleColor();

        DOTween.Sequence()
            .Append(rect.DOScale(Vector3.one, duration * 0.25f).SetEase(Ease.OutBack))
            .Join(DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, endPosition, duration).SetEase(moveEase))
            .Join(rect.DOLocalRotate(new Vector3(0f, 0f, spin), duration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad))
            .Insert(duration * 0.55f, rect.DOScale(Vector3.zero, duration * 0.45f).SetEase(Ease.InQuad))
            .Insert(duration * 0.5f, TweenAlpha(sparkle, 0f, duration * 0.5f))
            .OnComplete(() => sparkle.gameObject.SetActive(false))
            .SetTarget(rect)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(sparkle.gameObject);
    }

    /// <summary> 背面の枠を指定の位置へ、少し行き過ぎてから戻るように動かす（ポヨン）。 </summary>
    private void MoveBackFrame(Vector2 target)
    {
        if (_backFrame == null) return;

        KillTween(_backFrameTween);
        _backFrameTween = DOTween.To(() => _backFrame.anchoredPosition, value => _backFrame.anchoredPosition = value, target, _backFrameDuration)
            .SetEase(Ease.OutBack, _backFrameOvershoot)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 見た目の親の大きさを変える（押し込み・戻り用）。 </summary>
    private void TweenVisualScale(float scale, float duration, Ease ease)
    {
        if (_visualRoot == null) return;

        KillTween(_holdTween);
        _holdTween = _visualRoot.DOScale(Vector3.one * scale, duration)
            .SetEase(ease)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 板の色を変える。duration が 0 ならすぐ変える。 </summary>
    private void TweenFrameColor(Color target, float duration, float delay)
    {
        if (_frame == null) return;

        KillTween(_frameColorTween);
        _frameColorTween = TweenGraphicColor(_frame, target, duration, delay);
    }

    /// <summary> 文字の色を変える。duration が 0 ならすぐ変える。 </summary>
    private void TweenLabelColor(Color target, float duration, float delay)
    {
        if (_label == null) return;

        KillTween(_labelColorTween);
        _labelColorTween = TweenGraphicColor(_label, target, duration, delay);
    }

    /// <summary> Graphic の色を変える Tween を作る。duration が 0 ならすぐ変えて null を返す。 </summary>
    private Tween TweenGraphicColor(Graphic graphic, Color target, float duration, float delay)
    {
        if (duration <= 0f && delay <= 0f)
        {
            graphic.color = target;
            return null;
        }

        return DOTween.To(() => graphic.color, value => graphic.color = value, target, Mathf.Max(duration, 0.001f))
            .SetDelay(delay)
            .SetEase(Ease.OutQuad)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 見た目の親の透明度を変える Tween を作る（CanvasGroup が無ければ何もしない Tween を返す）。 </summary>
    private Tween TweenVisualAlpha(float target, float duration)
    {
        if (_visualGroup == null) return DOTween.To(() => 0f, _ => { }, 0f, duration);

        return DOTween.To(() => _visualGroup.alpha, value => _visualGroup.alpha = value, target, duration);
    }

    /// <summary> 画像の透明度だけを変える Tween を作る。 </summary>
    private static Tween TweenAlpha(Graphic graphic, float target, float duration)
    {
        return DOTween.To(() => graphic.color.a, value => SetAlpha(graphic, graphic.color, value), target, duration);
    }

    /// <summary> 色はそのままで透明度だけを指定の値にする。 </summary>
    private static void SetAlpha(Graphic graphic, Color baseColor, float alpha)
    {
        baseColor.a = alpha;
        graphic.color = baseColor;
    }

    /// <summary> 星粒の色をランダムに選ぶ。 </summary>
    private Color RandomSparkleColor()
    {
        if (_sparkleColors == null || _sparkleColors.Length == 0) return _coreColor;
        return _sparkleColors[Random.Range(0, _sparkleColors.Length)];
    }

    /// <summary> 使っていない星粒を取り出す。足りなければ新しく作る。 </summary>
    private StarShapeGraphic GetSparkle()
    {
        foreach (StarShapeGraphic sparkle in _sparklePool)
        {
            if (sparkle != null && !sparkle.gameObject.activeSelf) return sparkle;
        }

        var go = new GameObject("Sparkle", typeof(RectTransform));
        go.layer = gameObject.layer;
        go.transform.SetParent(_sparkleRoot, false);
        var created = go.AddComponent<StarShapeGraphic>();
        created.raycastTarget = false;
        go.SetActive(false);
        _sparklePool.Add(created);
        return created;
    }

    /// <summary>
    /// 星粒を入れる親をボタンの子として作る。
    /// Visual の中に置くと、弾けて Visual が消えるときに星まで一緒に消えるので、Visual とは別にして一番手前に置く。
    /// </summary>
    private void CreateSparkleRoot()
    {
        var go = new GameObject("Sparkles", typeof(RectTransform));
        go.layer = gameObject.layer;
        _sparkleRoot = (RectTransform)go.transform;
        _sparkleRoot.SetParent(transform, false);
        _sparkleRoot.anchorMin = Vector2.zero;
        _sparkleRoot.anchorMax = Vector2.one;
        _sparkleRoot.offsetMin = Vector2.zero;
        _sparkleRoot.offsetMax = Vector2.zero;
        _sparkleRoot.SetAsLastSibling();
    }

    /// <summary> 流れ星を隠す。 </summary>
    private void HideStreak()
    {
        if (_streak != null) _streak.gameObject.SetActive(false);
    }

    /// <summary> 流れ星の角度で見たとき、板の中心から端までの長さ（ここより外から入り、外へ抜ける）。 </summary>
    private float StreakTravelHalfLength()
    {
        Rect rect = _frame.rectTransform.rect;
        float angle = StreakAngleRadian();
        return Mathf.Abs(rect.width * 0.5f * Mathf.Cos(angle)) + Mathf.Abs(rect.height * 0.5f * Mathf.Sin(angle)) + STREAK_MARGIN;
    }

    /// <summary> 傾いた流れ星が、板を上から下まで覆うのに必要な高さ。 </summary>
    private float StreakHeight()
    {
        Rect rect = _frame.rectTransform.rect;
        float angle = StreakAngleRadian();
        return (Mathf.Abs(rect.width * 0.5f * Mathf.Sin(angle)) + Mathf.Abs(rect.height * 0.5f * Mathf.Cos(angle))) * 2f + STREAK_MARGIN;
    }

    /// <summary> 流れ星の傾き（ラジアン）。 </summary>
    private float StreakAngleRadian()
    {
        float degree = _streakRoot != null ? _streakRoot.localEulerAngles.z : 0f;
        return degree * Mathf.Deg2Rad;
    }

    /// <summary> 見た目をすべて、ふだんの状態に戻す。 </summary>
    private void ResetVisuals()
    {
        _isHighlighted = false;
        _isHolding = false;
        if (_frame != null) _frame.color = _normalColor;
        if (_label != null) _label.color = _normalLabelColor;
        if (_backFrame != null) _backFrame.anchoredPosition = _backFrameBasePosition;
        if (_visualRoot != null) _visualRoot.localScale = Vector3.one;
        if (_visualGroup != null) _visualGroup.alpha = 1f;
        HideStreak();

        foreach (StarShapeGraphic sparkle in _sparklePool)
        {
            if (sparkle == null) continue;
            DOTween.Kill(sparkle.rectTransform);
            sparkle.gameObject.SetActive(false);
        }
    }

    /// <summary> このコンポーネントが動かしている Tween をすべて止める。 </summary>
    private void KillAllTweens()
    {
        KillTween(_frameColorTween);
        KillTween(_labelColorTween);
        KillTween(_backFrameTween);
        KillTween(_holdTween);
        KillTween(_streakSequence);
        KillTween(_idleTween);
        KillTween(_popSequence);
    }

    /// <summary> 光の帯に使う、真ん中が濃く両端がぼやけた画像を作る（1回だけ作って使い回す）。 </summary>
    private static Sprite GetCoreSprite()
    {
        if (_coreSprite != null) return _coreSprite;

        // 両端が 0、真ん中が 1 になる山なりの形（sin）で、ふちをやわらかくする
        _coreSprite = CreateGradientSprite(rate => Mathf.Sin(rate * Mathf.PI), new Vector2(0.5f, 0.5f));
        return _coreSprite;
    }

    /// <summary> 尾に使う、左が濃く右へ行くほど透明になる画像を作る（1回だけ作って使い回す）。 </summary>
    private static Sprite GetTailSprite()
    {
        if (_tailSprite != null) return _tailSprite;

        // 光の帯の近くは濃く、離れるほど急に薄くなるよう 2 乗で減らす
        _tailSprite = CreateGradientSprite(rate => (1f - rate) * (1f - rate), new Vector2(0f, 0.5f));
        return _tailSprite;
    }

    /// <summary> 横方向に透明度が変わる白い画像を作る。alphaByRate には、左端 0〜右端 1 の位置を渡すと透明度を返す処理を渡す。 </summary>
    private static Sprite CreateGradientSprite(System.Func<float, float> alphaByRate, Vector2 pivot)
    {
        var texture = new Texture2D(GRADIENT_TEXTURE_WIDTH, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave,
        };

        for (int x = 0; x < GRADIENT_TEXTURE_WIDTH; x++)
        {
            float rate = x / (float)(GRADIENT_TEXTURE_WIDTH - 1);
            texture.SetPixel(x, 0, new Color(1f, 1f, 1f, Mathf.Clamp01(alphaByRate(rate))));
        }
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, GRADIENT_TEXTURE_WIDTH, 1f), pivot, 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    /// <summary> まだ動いている Tween だけを止める（止まっているものを止めようとすると警告が出るため）。 </summary>
    private static void KillTween(Tween tween)
    {
        if (tween != null && tween.IsActive()) tween.Kill();
    }
}
