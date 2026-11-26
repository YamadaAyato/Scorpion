using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 星そのものがパネルになる（オプションなど）。
/// ・開く：押したボタンの星の複製を、ボタンの星と同じ位置・大きさから、回しながら画面の真ん中へ大きくしてパネルにし、中身をポンと出す。うしろのメニューは薄くして押せなくする
///   メニューを画面の端に寄せて置いても、パネルと中身は画面の真ん中に出る（Center On Screen がオンのとき）
/// ・閉じる：中身を消してから、星を回しながら元のボタンの星の位置・大きさへ小さく戻し、メニューを元に戻す
/// 星の複製は開くたびに作り、閉じ終わったら消す。中身は星より手前に描く。
/// </summary>
public class StarPanel : MonoBehaviour
{
    /// <summary> 開いているか（開いている途中・閉じている途中も含む）。 </summary>
    public bool IsOpen => _panelStar != null;

    [Header("部品")]
    [Tooltip("パネルの中身（見出し・ボタンなど）。CanvasGroup で出したり消したりする。閉じている間は見えず、押せない")]
    [SerializeField] private CanvasGroup _content;

    [Tooltip("開いている間、うしろで薄くして押せなくするメニュー")]
    [SerializeField] private CanvasGroup _menu;

    [Header("大きさ・位置")]
    [Tooltip("パネルになったときの星の高さ（Canvas 上の単位）")]
    [SerializeField] private float _panelHeight = 1050f;

    [Tooltip("パネルを画面の真ん中に出すか。オフにすると、このオブジェクトの真ん中に出す")]
    [SerializeField] private bool _centerOnScreen = true;

    [Tooltip("パネルになったときの星の中心の位置。画面の真ん中（Center On Screen がオフなら、このオブジェクトの真ん中）からのずれ（Canvas 上の単位）")]
    [SerializeField] private Vector2 _panelCenter = new Vector2(0f, -10f);

    [Tooltip("星の絵の高さ（星のパーツのキャンバスの高さに対する割合）。パネルの大きさの計算に使う。星の絵を描き直したら測り直す")]
    [SerializeField] private float _starHeightRate = 724f / 3508f;

    [Header("動き")]
    [Tooltip("星が大きくなってパネルになるまでの時間（秒）")]
    [SerializeField] private float _openDuration = 0.5f;

    [Tooltip("星が小さくなってボタンの星に戻るまでの時間（秒）")]
    [SerializeField] private float _closeDuration = 0.4f;

    [Tooltip("開くとき・閉じるときに回る角度（度）。開くときは時計回り、閉じるときは反時計回り")]
    [SerializeField] private float _spin = 360f;

    [Tooltip("中身がポンと出る・消えるまでの時間（秒）")]
    [SerializeField] private float _contentDuration = 0.2f;

    [Tooltip("開いている間のうしろのメニューの濃さ（0 で見えない）")]
    [SerializeField, Range(0f, 1f)] private float _menuAlphaWhileOpen = 0f;

    [Header("時間")]
    [Tooltip("Time.timeScale を無視して動かすか。ポーズ中でも開けるよう、基本はオンのままにする")]
    [SerializeField] private bool _ignoreTimeScale = true;

    private RectTransform _panelStar;
    private RectTransform _sourceStar;
    private Action _onClosed;
    private float _panelAngle;
    private Sequence _sequence;

    // 中身が出てくるときの最初の大きさ（元の何倍か）
    private const float CONTENT_START_SCALE = 0.8f;

    /// <summary>
    /// 開く。sourceStar は押したボタンの星（Pivot が星の中心のもの）。そこから星の複製を大きくしてパネルにする。
    /// onClosed は閉じ終わったときに呼ぶ（ボタンの星を元に戻すのに使う）。すでに開いていれば何もしない。
    /// </summary>
    public void Open(RectTransform sourceStar, Action onClosed)
    {
        if (IsOpen || sourceStar == null) return;

        _sourceStar = sourceStar;
        _onClosed = onClosed;
        _panelStar = CreatePanelStar(sourceStar);

        Vector2 startPosition = _panelStar.anchoredPosition;
        Vector2 targetPosition = PanelTargetPosition();
        float startScale = _panelStar.localScale.x;
        float targetScale = _panelHeight / Mathf.Max(sourceStar.rect.height * _starHeightRate, 1f);

        // 中身もパネルの星の中心に合わせる（中身の位置は、星の中心からのずれで置いてある）
        ((RectTransform)_content.transform).anchoredPosition = targetPosition;
        _panelAngle = NormalizeAngle(_panelStar.localEulerAngles.z);
        float targetAngle = -_spin;

        SetContentVisible(false);
        _content.transform.localScale = Vector3.one * CONTENT_START_SCALE;

        KillSequence();
        _sequence = DOTween.Sequence()
            .Append(DOTween.To(() => startPosition, value => _panelStar.anchoredPosition = value, targetPosition, _openDuration).SetEase(Ease.OutCubic))
            .Join(DOTween.To(() => startScale, value => _panelStar.localScale = Vector3.one * value, targetScale, _openDuration).SetEase(Ease.OutBack))
            .Join(DOTween.To(() => _panelAngle, SetPanelAngle, targetAngle, _openDuration).SetEase(Ease.OutCubic))
            .Join(FadeMenu(_menuAlphaWhileOpen, _openDuration * 0.5f))
            .Append(FadeGroup(_content, 1f, _contentDuration))
            .Join(_content.transform.DOScale(Vector3.one, _contentDuration).SetEase(Ease.OutBack))
            .OnComplete(() => SetContentInteractable(true))
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);

        if (_menu != null)
        {
            _menu.interactable = false;
            _menu.blocksRaycasts = false;
        }
    }

    /// <summary> 閉じる。中身を消してから、星を元のボタンの星の位置・大きさ・向きへ戻す。開いていなければ何もしない。 </summary>
    public void Close()
    {
        if (!IsOpen) return;

        // ボタンの星は、パネルを開いている間に元の位置へ戻っていることがあるので、閉じる時点の位置へ戻す
        Vector2 targetPosition = _panelStar.anchoredPosition;
        float targetScale = _panelStar.localScale.x;
        if (_sourceStar != null)
        {
            var parent = (RectTransform)transform;
            targetPosition = (Vector2)parent.InverseTransformPoint(_sourceStar.position) - parent.rect.center;
            targetScale = _sourceStar.lossyScale.x / Mathf.Max(parent.lossyScale.x, 0.0001f);
        }
        Vector2 startPosition = _panelStar.anchoredPosition;
        float startScale = _panelStar.localScale.x;

        SetContentInteractable(false);
        KillSequence();
        _sequence = DOTween.Sequence()
            .Append(FadeGroup(_content, 0f, _contentDuration))
            .Join(_content.transform.DOScale(Vector3.one * CONTENT_START_SCALE, _contentDuration).SetEase(Ease.InBack))
            .Append(DOTween.To(() => startPosition, value => _panelStar.anchoredPosition = value, targetPosition, _closeDuration).SetEase(Ease.InOutCubic))
            .Join(DOTween.To(() => startScale, value => _panelStar.localScale = Vector3.one * value, targetScale, _closeDuration).SetEase(Ease.InCubic))
            .Join(DOTween.To(() => _panelAngle, SetPanelAngle, _panelAngle + _spin, _closeDuration).SetEase(Ease.InOutCubic))
            .Join(FadeMenu(1f, _closeDuration))
            .OnComplete(FinishClose)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);
    }

    private void Awake()
    {
        SetContentVisible(false);
    }

    private void OnDisable()
    {
        // 途中で消えたときは、閉じ終わった状態にしておく
        if (IsOpen) FinishClose();
    }

    /// <summary>
    /// ボタンの星の複製を作り、このオブジェクトの一番奥（中身より奥）に置く。ボタンの星と同じ画面上の位置・大きさ・向きから始める。
    /// </summary>
    private RectTransform CreatePanelStar(RectTransform sourceStar)
    {
        var parent = (RectTransform)transform;
        RectTransform star = Instantiate(sourceStar, parent);
        star.name = "PanelStar";
        star.SetAsFirstSibling();
        star.anchorMin = new Vector2(0.5f, 0.5f);
        star.anchorMax = new Vector2(0.5f, 0.5f);
        star.pivot = sourceStar.pivot;
        star.sizeDelta = sourceStar.rect.size;
        star.localScale = Vector3.one * (sourceStar.lossyScale.x / Mathf.Max(parent.lossyScale.x, 0.0001f));
        star.localRotation = Quaternion.Inverse(parent.rotation) * sourceStar.rotation;
        star.anchoredPosition = (Vector2)parent.InverseTransformPoint(sourceStar.position) - parent.rect.center;
        return star;
    }

    /// <summary>
    /// パネルになったときの星の中心の位置（このオブジェクトの中心から見た位置）。
    /// Center On Screen がオンなら、画面（一番上の Canvas）の真ん中にずれを足した位置にする。
    /// </summary>
    private Vector2 PanelTargetPosition()
    {
        if (!_centerOnScreen) return _panelCenter;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return _panelCenter;

        var parent = (RectTransform)transform;
        var canvasRect = (RectTransform)canvas.rootCanvas.transform;
        Vector3 screenCenter = canvasRect.TransformPoint(canvasRect.rect.center);
        return (Vector2)parent.InverseTransformPoint(screenCenter) - parent.rect.center + _panelCenter;
    }

    /// <summary> 閉じ終わったとき。星の複製を消し、メニューを押せるように戻して、閉じたことを知らせる。 </summary>
    private void FinishClose()
    {
        KillSequence();
        if (_panelStar != null) Destroy(_panelStar.gameObject);
        _panelStar = null;
        _sourceStar = null;
        SetContentVisible(false);

        if (_menu != null)
        {
            _menu.alpha = 1f;
            _menu.interactable = true;
            _menu.blocksRaycasts = true;
        }

        Action onClosed = _onClosed;
        _onClosed = null;
        onClosed?.Invoke();
    }

    /// <summary> うしろのメニューの濃さを変える Tween を作る。メニューがなければ何もしない Tween を返す。 </summary>
    private Tween FadeMenu(float alpha, float duration)
    {
        if (_menu == null) return DOTween.Sequence().AppendInterval(duration);
        return FadeGroup(_menu, alpha, duration);
    }

    /// <summary> CanvasGroup の濃さを変える Tween を作る（DOTween の追加モジュールがなくても動くよう、DOTween.To で作る）。 </summary>
    private static Tween FadeGroup(CanvasGroup group, float alpha, float duration)
    {
        return DOTween.To(() => group.alpha, value => group.alpha = value, alpha, duration);
    }

    /// <summary> 中身を見せるか。見せないときは押せなくもする。 </summary>
    private void SetContentVisible(bool visible)
    {
        if (_content == null) return;
        _content.alpha = visible ? 1f : 0f;
        SetContentInteractable(visible);
    }

    /// <summary> 中身を押せるか。 </summary>
    private void SetContentInteractable(bool interactable)
    {
        if (_content == null) return;
        _content.interactable = interactable;
        _content.blocksRaycasts = interactable;
    }

    /// <summary> パネルの星の角度を変える（360 度を超えた分も覚えておく）。 </summary>
    private void SetPanelAngle(float angle)
    {
        _panelAngle = angle;
        if (_panelStar != null) _panelStar.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary> 動いている途中の動きを止める。 </summary>
    private void KillSequence()
    {
        if (_sequence != null && _sequence.IsActive()) _sequence.Kill();
        _sequence = null;
    }

    /// <summary> 角度を -180〜180 度に直す。 </summary>
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}
