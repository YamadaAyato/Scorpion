using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ボタンの演出の共通部分をまとめた基底クラス。継承したクラスを、ButtonBase（を継承したボタン）と同じオブジェクトに付ける。
/// ButtonBase のイベント（選択・押し込み・決定など）を受け取って、継承したクラスの Play〇〇 を呼ぶ。
/// 見た目の親（Visual）の押し込み・膨らみと、星粒・紙吹雪の作成と使い回しもここで行う。
/// </summary>
[DisallowMultipleComponent]
public abstract class ButtonEffectBase : MonoBehaviour
{
    [Header("共通の部品")]
    [Tooltip("押し込み・膨らみ・弾ける動きをさせる見た目の親。ボタン本体とは別のオブジェクトにする（本体は ButtonBase が拡大・縮小するため）")]
    [SerializeField] private RectTransform _visualRoot;

    [Tooltip("弾けて消えるときなどに透明度を変える CanvasGroup（Visual Root に付ける）")]
    [SerializeField] private CanvasGroup _visualGroup;

    [Tooltip("ボタンの板（色を変える）")]
    [SerializeField] private Graphic _frame;

    [Tooltip("ボタンの文字（色を変える）")]
    [SerializeField] private Graphic _label;

    [Header("星粒・紙吹雪")]
    [Tooltip("星粒・紙吹雪の色。この中からランダムに選ぶ")]
    [SerializeField] private Color[] _particleColors =
    {
        new Color32(0xFF, 0xD6, 0x6B, 0xFF),
        new Color32(0xFF, 0xFD, 0xF5, 0xFF),
        new Color32(0x8B, 0xE9, 0xFF, 0xFF),
        new Color32(0xFF, 0x9E, 0xC7, 0xFF),
    };

    [Header("時間")]
    [Tooltip("Time.timeScale を無視して動かすか。ポーズ中（timeScale = 0）でも動くよう、基本はオンのままにする")]
    [SerializeField] private bool _ignoreTimeScale = true;

    private ButtonBase _button;
    private Vector2 _visualBasePosition;
    private RectTransform _particleRoot;
    private readonly List<StarShapeGraphic> _particlePool = new List<StarShapeGraphic>();
    private bool _isHighlighted;
    private bool _isHolding;
    private int _holdEndedFrame = -1;
    private Tween _visualTween;
    private Tween _visualAlphaTween;

    protected virtual void Awake()
    {
        _button = GetComponent<ButtonBase>();
        if (_button == null)
        {
            Debug.LogWarning($"{name}: {GetType().Name} は ButtonBase を継承したボタンと同じオブジェクトに付けてください", this);
            enabled = false;
            return;
        }

        if (_visualRoot != null) _visualBasePosition = _visualRoot.anchoredPosition;
        CreateParticleRoot();
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

        _isHighlighted = false;
        _isHolding = false;
        ResetVisuals();

        // 表示された時点ですでに選ばれていたら、動きなしで選択の見た目にする
        if (_button.IsHighlighted)
        {
            _isHighlighted = true;
            PlayHighlight(false);
        }
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

    private void HandleHighlighted(ButtonBase button)
    {
        _isHighlighted = true;
        PlayHighlight(true);
    }

    private void HandleUnhighlighted(ButtonBase button)
    {
        _isHighlighted = false;
        PlayUnhighlight();
    }

    private void HandleHoldStarted(ButtonBase button)
    {
        _isHolding = true;
        PlayHoldStart();
    }

    private void HandleHoldEnded(ButtonBase button)
    {
        _isHolding = false;
        _holdEndedFrame = Time.frameCount;
        PlayHoldEnd();
    }

    private void HandlePressed(ButtonBase button)
    {
        // マウスで押した場合は、同じフレームで先に「離した」が届いている。キーで決定した場合は届いていない
        bool fromHold = _holdEndedFrame == Time.frameCount;
        _isHolding = false;
        PlayPress(fromHold);
    }

    private void HandleSubmitted(ButtonBase button)
    {
        if (!isActiveAndEnabled) return;
        PlayRestore();
    }

    /// <summary> 押されたボタン。 </summary>
    protected ButtonBase TargetButton => _button;

    /// <summary> 見た目の親。 </summary>
    protected RectTransform VisualRoot => _visualRoot;

    /// <summary> ボタンの板。 </summary>
    protected Graphic Frame => _frame;

    /// <summary> ボタンの文字。 </summary>
    protected Graphic Label => _label;

    /// <summary> 選択の見た目になっているか。 </summary>
    protected bool IsHighlighted => _isHighlighted;

    /// <summary> マウスで押し込んでいる途中か。 </summary>
    protected bool IsHolding => _isHolding;

    /// <summary> Time.timeScale を無視して動かすか。継承したクラスで作る Tween の SetUpdate に渡す。 </summary>
    protected bool IgnoreTimeScale => _ignoreTimeScale;

    /// <summary> 選ばれたとき。animate が false なら、動きなしですぐ選択の見た目にする。 </summary>
    protected abstract void PlayHighlight(bool animate);

    /// <summary> 選択が外れたとき。 </summary>
    protected abstract void PlayUnhighlight();

    /// <summary> マウスを押し込んだとき。 </summary>
    protected abstract void PlayHoldStart();

    /// <summary> 押し込んでいたマウスを離したとき（ボタンの上で離したときは、このあと PlayPress も呼ばれる）。 </summary>
    protected abstract void PlayHoldEnd();

    /// <summary> 決定されたとき。fromHold はマウスで押し込んでから離した場合に true、キーで決定した場合は false。 </summary>
    protected abstract void PlayPress(bool fromHold);

    /// <summary> 決定の処理が終わっても、ボタンが表示されたままだったとき（パネルを開くボタンなど）。 </summary>
    protected abstract void PlayRestore();

    /// <summary> 見た目をふだんの状態に戻す。上書きするときは base.ResetVisuals() も呼ぶ。 </summary>
    protected virtual void ResetVisuals()
    {
        if (_visualRoot != null)
        {
            _visualRoot.localScale = Vector3.one;
            _visualRoot.anchoredPosition = _visualBasePosition;
        }
        if (_visualGroup != null) _visualGroup.alpha = 1f;
        if (_frame != null) _frame.color = NormalFrameColor;
        if (_label != null) _label.color = NormalLabelColor;

        foreach (StarShapeGraphic particle in _particlePool)
        {
            if (particle == null) continue;
            DOTween.Kill(particle.rectTransform);
            particle.gameObject.SetActive(false);
        }
    }

    /// <summary> 動いている Tween をすべて止める。上書きするときは base.KillAllTweens() も呼ぶ。 </summary>
    protected virtual void KillAllTweens()
    {
        KillTween(_visualTween);
        KillTween(_visualAlphaTween);
    }

    /// <summary> ふだんの板の色。 </summary>
    protected abstract Color NormalFrameColor { get; }

    /// <summary> ふだんの文字の色。 </summary>
    protected abstract Color NormalLabelColor { get; }

    /// <summary> 見た目の親を、指定の大きさと位置（元の位置からのずれ）へ動かす。 </summary>
    protected void AnimateVisual(Vector3 scale, Vector2 offset, float duration, Ease ease, float overshoot = 1.70158f)
    {
        if (_visualRoot == null) return;

        Vector2 target = _visualBasePosition + offset;
        ReplaceVisualTween(DOTween.Sequence()
            .Append(_visualRoot.DOScale(scale, duration).SetEase(ease, overshoot))
            .Join(DOTween.To(() => _visualRoot.anchoredPosition, value => _visualRoot.anchoredPosition = value, target, duration).SetEase(ease, overshoot)));
    }

    /// <summary> 見た目の親を動かす Tween を差し替える（前の Tween は止める）。何段階かの動きを自分で組むときに使う。 </summary>
    protected void ReplaceVisualTween(Tween tween)
    {
        KillTween(_visualTween);
        _visualTween = tween?.SetUpdate(_ignoreTimeScale).SetLink(gameObject);
    }

    /// <summary> 見た目の親の透明度を変える。 </summary>
    protected void FadeVisual(float alpha, float duration, float delay = 0f)
    {
        if (_visualGroup == null) return;

        KillTween(_visualAlphaTween);
        _visualAlphaTween = DOTween.To(() => _visualGroup.alpha, value => _visualGroup.alpha = value, alpha, duration)
            .SetDelay(delay)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 見た目の親を、元の位置へ戻しながら指定の大きさにする Tween を作る（何段階かの動きを組むとき用）。 </summary>
    protected Tween CreateVisualTween(Vector3 scale, float duration, Ease ease, float overshoot = 1.70158f)
    {
        return DOTween.Sequence()
            .Append(_visualRoot.DOScale(scale, duration).SetEase(ease, overshoot))
            .Join(DOTween.To(() => _visualRoot.anchoredPosition, value => _visualRoot.anchoredPosition = value, _visualBasePosition, duration).SetEase(ease, overshoot));
    }

    /// <summary> Graphic の色を変える Tween を作る。duration と delay が 0 ならすぐ変えて null を返す。 </summary>
    protected Tween TweenColor(Graphic graphic, Color target, float duration, float delay = 0f)
    {
        if (graphic == null) return null;
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

    /// <summary>
    /// 星や丸（紙吹雪）を1つ飛ばす。ポンと大きくなって、回りながら移動し、後半で小さく・透明になって消える。
    /// 位置はボタンの中心が (0, 0)。pointCount と innerRatio で形を変える（innerRatio を 1 にすると丸）。
    /// color を省略すると、Particle Colors の中からランダムに選ぶ。
    /// </summary>
    protected void SpawnParticle(Vector2 start, Vector2 end, float size, int pointCount, float innerRatio, float duration, Ease moveEase, float spin, Color? color = null)
    {
        if (!isActiveAndEnabled) return;

        StarShapeGraphic particle = GetParticle();
        particle.PointCount = pointCount;
        particle.InnerRatio = innerRatio;
        particle.color = color ?? RandomParticleColor();

        RectTransform rect = particle.rectTransform;
        DOTween.Kill(rect);
        particle.gameObject.SetActive(true);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = start;
        rect.localScale = Vector3.zero;
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        DOTween.Sequence()
            .Append(rect.DOScale(Vector3.one, duration * 0.25f).SetEase(Ease.OutBack))
            .Join(DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, end, duration).SetEase(moveEase))
            .Join(rect.DOLocalRotate(new Vector3(0f, 0f, spin), duration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad))
            .Insert(duration * 0.55f, rect.DOScale(Vector3.zero, duration * 0.45f).SetEase(Ease.InQuad))
            .Insert(duration * 0.5f, DOTween.To(() => particle.color.a, value => SetAlpha(particle, value), 0f, duration * 0.5f))
            .OnComplete(() => particle.gameObject.SetActive(false))
            .SetTarget(rect)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(particle.gameObject);
    }

    /// <summary> 指定の位置から、星や丸を四方へ飛び散らせる。color を省略すると、1つずつランダムな色にする。 </summary>
    protected void SpawnBurst(Vector2 center, int count, Vector2 distanceRange, Vector2 sizeRange, int pointCount, float innerRatio, float duration, Color? color = null)
    {
        for (int i = 0; i < count; i++)
        {
            // 均等な角度に少しばらつきを足して、規則的に見えすぎないようにする
            float angle = (360f / Mathf.Max(1, count) * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            float distance = Random.Range(distanceRange.x, distanceRange.y);
            Vector2 end = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            float size = Random.Range(sizeRange.x, sizeRange.y);
            float spin = Random.Range(180f, 360f) * (Random.value < 0.5f ? -1f : 1f);
            SpawnParticle(center, end, size, pointCount, innerRatio, duration, Ease.OutCubic, spin, color);
        }
    }

    /// <summary> ワールド座標を、星粒の位置（ボタンの中心が (0, 0)）に直す。パーツの中心から星粒を出すときに使う。 </summary>
    protected Vector2 ToParticleSpace(Vector3 worldPosition)
    {
        return _particleRoot != null ? (Vector2)_particleRoot.InverseTransformPoint(worldPosition) : Vector2.zero;
    }

    /// <summary> まだ動いている Tween だけを止める（止まっているものを止めようとすると警告が出るため）。 </summary>
    protected static void KillTween(Tween tween)
    {
        if (tween != null && tween.IsActive()) tween.Kill();
    }

    /// <summary> 色はそのままで透明度だけを変える。 </summary>
    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    /// <summary> 星粒・紙吹雪の色をランダムに選ぶ。 </summary>
    private Color RandomParticleColor()
    {
        if (_particleColors == null || _particleColors.Length == 0) return Color.white;
        return _particleColors[Random.Range(0, _particleColors.Length)];
    }

    /// <summary> 使っていない星粒を取り出す。足りなければ新しく作る。 </summary>
    private StarShapeGraphic GetParticle()
    {
        foreach (StarShapeGraphic particle in _particlePool)
        {
            if (particle != null && !particle.gameObject.activeSelf) return particle;
        }

        var go = new GameObject("Particle", typeof(RectTransform));
        go.layer = gameObject.layer;
        go.transform.SetParent(_particleRoot, false);
        var created = go.AddComponent<StarShapeGraphic>();
        created.raycastTarget = false;
        go.SetActive(false);
        _particlePool.Add(created);
        return created;
    }

    /// <summary>
    /// 星粒を入れる親をボタンの子として作る。
    /// Visual の中に置くと、弾けて Visual が消えるときに星まで一緒に消えるので、Visual とは別にして一番手前に置く。
    /// </summary>
    private void CreateParticleRoot()
    {
        var go = new GameObject("Particles", typeof(RectTransform));
        go.layer = gameObject.layer;
        _particleRoot = (RectTransform)go.transform;
        _particleRoot.SetParent(transform, false);
        _particleRoot.anchorMin = Vector2.zero;
        _particleRoot.anchorMax = Vector2.one;
        _particleRoot.offsetMin = Vector2.zero;
        _particleRoot.offsetMax = Vector2.zero;
        _particleRoot.SetAsLastSibling();
    }
}
