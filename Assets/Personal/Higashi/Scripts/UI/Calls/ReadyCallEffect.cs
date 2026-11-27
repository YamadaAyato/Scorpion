using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 試合開始前の「READY?」コール。上からドスンと落ちてきて、着地で横に潰れてぽよんと戻り、
/// シーソーのように左右へ1回揺れてから、少し沈んで上へ飛び去りながら消える。
/// READY の画像（Image）はこのオブジェクトの子に置く。画像の Pivot を文字の下端にしておくと、着地の潰れと揺れが下端を支点にして起きる。
/// 終わったら OnFinished を呼ぶ（3・2・1 や START へつなぐ）。
/// </summary>
public class ReadyCallEffect : MonoBehaviour
{
    /// <summary> コールが消え終わったとき。 </summary>
    public event Action OnFinished;

    [Tooltip("動かす READY の画像")]
    [SerializeField] private RectTransform _image;

    [Tooltip("再生を始めたときに自動で出す。試合の流れから出すときはオフにして Play を呼ぶ")]
    [SerializeField] private bool _playOnStart = true;

    [Tooltip("消え終わってから、もう一度出すまでの時間（秒）。0 ならくり返さない（確認用）")]
    [SerializeField] private float _loopDelay;

    private CanvasGroup _group;
    private Vector2 _basePosition;
    private Sequence _sequence;
    private Tween _loopTween;

    // 落ちてくる高さ、潰れ方、揺れる角度、沈む深さ、飛び去る高さ（Canvas 上の単位・度）
    private const float DROP_HEIGHT = 250f;
    private static readonly Vector3 SQUASH_SCALE = new Vector3(1.18f, 0.82f, 1f);
    private const float SWAY_ANGLE = 3f;
    private const float SINK_DEPTH = 20f;
    private const float EXIT_HEIGHT = 320f;

    // 時間（秒）
    private const float FALL_DURATION = 0.22f;
    private const float SQUASH_DURATION = 0.06f;
    private const float RECOVER_DURATION = 0.3f;
    private const float SWAY_DURATION = 0.5f;
    private const float HOLD_DURATION = 0.5f;
    private const float SINK_DURATION = 0.12f;
    private const float EXIT_DURATION = 0.3f;

    // 落ちる時間のうち、透明から見えるようになるまでの割合
    private const float FADE_IN_RATE = 0.4f;

    /// <summary> コールを最初から出す。 </summary>
    public void Play()
    {
        if (_image == null) return;

        Stop();
        SetImage(_basePosition + new Vector2(0f, DROP_HEIGHT), Vector3.one, 0f, 0f);

        // Z の回転はプラスが反時計回り。揺れは 左 → 右 → 元 の順
        _sequence = DOTween.Sequence()
            .Append(Move(_basePosition, FALL_DURATION, Ease.InQuad))
            .Join(Fade(1f, FALL_DURATION * FADE_IN_RATE))
            .Append(_image.DOScale(SQUASH_SCALE, SQUASH_DURATION).SetEase(Ease.OutQuad))
            .Append(_image.DOScale(Vector3.one, RECOVER_DURATION).SetEase(Ease.OutBack))
            .Append(Rotate(SWAY_ANGLE, SWAY_DURATION * 0.25f, Ease.OutSine))
            .Append(Rotate(-SWAY_ANGLE, SWAY_DURATION * 0.5f, Ease.InOutSine))
            .Append(Rotate(0f, SWAY_DURATION * 0.25f, Ease.InSine))
            .AppendInterval(HOLD_DURATION)
            .Append(Move(_basePosition + new Vector2(0f, -SINK_DEPTH), SINK_DURATION, Ease.OutQuad))
            .Append(Move(_basePosition + new Vector2(0f, EXIT_HEIGHT), EXIT_DURATION, Ease.InQuad))
            .Join(Fade(0f, EXIT_DURATION))
            .OnComplete(HandleFinished)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    /// <summary> 動きを止めて隠す（くり返しの予約も止める）。 </summary>
    public void Stop()
    {
        if (_sequence != null && _sequence.IsActive()) _sequence.Kill();
        if (_loopTween != null && _loopTween.IsActive()) _loopTween.Kill();
        _sequence = null;
        _loopTween = null;
        if (_image != null) SetImage(_basePosition, Vector3.one, 0f, 0f);
    }

    private void Awake()
    {
        if (_image == null) return;

        _basePosition = _image.anchoredPosition;
        _group = _image.GetComponent<CanvasGroup>();
        if (_group == null) _group = _image.gameObject.AddComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;
        _group.alpha = 0f;
    }

    private void Start()
    {
        if (_playOnStart) Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    /// <summary> このコンポーネントを付けたときに、子の Image を自動で探して入れる。 </summary>
    private void Reset()
    {
        _image = transform.Find("Image") as RectTransform;
    }

    /// <summary> 消え終わったとき。知らせてから、くり返す設定なら次を予約する。 </summary>
    private void HandleFinished()
    {
        OnFinished?.Invoke();
        if (_loopDelay > 0f) _loopTween = DOVirtual.DelayedCall(_loopDelay, Play, true).SetLink(gameObject);
    }

    /// <summary> 画像の位置・大きさ・角度・濃さをすぐ変える。 </summary>
    private void SetImage(Vector2 position, Vector3 scale, float angle, float alpha)
    {
        _image.anchoredPosition = position;
        _image.localScale = scale;
        _image.localRotation = Quaternion.Euler(0f, 0f, angle);
        if (_group != null) _group.alpha = alpha;
    }

    private Tween Move(Vector2 target, float duration, Ease ease)
    {
        return DOTween.To(() => _image.anchoredPosition, value => _image.anchoredPosition = value, target, duration).SetEase(ease);
    }

    private Tween Rotate(float angle, float duration, Ease ease)
    {
        return _image.DOLocalRotate(new Vector3(0f, 0f, angle), duration).SetEase(ease);
    }

    private Tween Fade(float alpha, float duration)
    {
        return DOTween.To(() => _group.alpha, value => _group.alpha = value, alpha, duration).SetEase(Ease.Linear);
    }
}
