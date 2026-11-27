using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 試合開始前の「READY?」→「GO!」コール。
/// ・READY：上からドスンと落ちてきて、着地で横に潰れてぽよんと戻り、シーソーのように左右へ1回揺れてから、少し沈んで上へ飛び去りながら消える
/// ・GO：READY が飛び去るのと入れ替わりに、傾きながら中央へポンと大きく出て、少し待ってから広がりながら消える
/// READY と GO の画像（Image）はこのオブジェクトの子に置く。READY の画像の Pivot を文字の下端にしておくと、着地の潰れと揺れが下端を支点にして起きる。
/// GO が出きったときに OnGo（ここで操作を始める）、全部消え終わったら OnFinished を呼ぶ。GO の画像を入れなければ READY だけ出す。
/// Play は READY → GO を続けて出す。試合のカウントダウンに合わせるときは、PlayReady で READY を出したまま待ち、合図が来たら PlayGo で GO へ進める。
/// </summary>
public class ReadyCallEffect : MonoBehaviour
{
    /// <summary> GO が出きったとき（ここで操作を始める）。 </summary>
    public event Action OnGo;

    /// <summary> コールが消え終わったとき。 </summary>
    public event Action OnFinished;

    [Tooltip("動かす READY の画像")]
    [SerializeField] private RectTransform _image;

    [Tooltip("READY のあとに出す GO の画像。空なら READY だけ出す")]
    [SerializeField] private RectTransform _goImage;

    [Tooltip("再生を始めたときに自動で出す。試合の流れから出すときはオフにして Play を呼ぶ")]
    [SerializeField] private bool _playOnStart = true;

    [Tooltip("消え終わってから、もう一度出すまでの時間（秒）。0 ならくり返さない（確認用）")]
    [SerializeField] private float _loopDelay;

    private CanvasGroup _group;
    private CanvasGroup _goGroup;
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

    // GO：出始めの傾き（度）、出たときの大きさ（行き過ぎ）、消えるときの大きさ
    private const float GO_START_ANGLE = -10f;
    private const float GO_POP_SCALE = 1.2f;
    private const float GO_EXIT_SCALE = 1.4f;

    // GO の時間（秒）。READY が飛び去り始めてから GO が出始めるまで、ポンと出る、元の大きさに戻る、止まる、消える
    private const float GO_START_DELAY = 0.15f;
    private const float GO_POP_DURATION = 0.2f;
    private const float GO_SETTLE_DURATION = 0.12f;
    private const float GO_HOLD_DURATION = 0.4f;
    private const float GO_EXIT_DURATION = 0.25f;

    /// <summary> コールを最初から出す。 </summary>
    public void Play()
    {
        if (_image == null) return;

        Stop();
        SetImage(_basePosition + new Vector2(0f, DROP_HEIGHT), Vector3.one, 0f, 0f);

        _sequence = CreateReadyEnter()
            .AppendInterval(HOLD_DURATION)
            .Append(CreateReadyExit());
        if (_goImage != null) _sequence.Insert(_sequence.Duration(false) - EXIT_DURATION + GO_START_DELAY, CreateGoSequence());
        _sequence
            .OnComplete(HandleFinished)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    /// <summary> READY を出して、PlayGo が呼ばれるまでそのまま止めておく（試合のカウントダウンの間）。 </summary>
    public void PlayReady()
    {
        if (_image == null) return;

        Stop();
        SetImage(_basePosition + new Vector2(0f, DROP_HEIGHT), Vector3.one, 0f, 0f);
        _sequence = CreateReadyEnter()
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    /// <summary>
    /// READY を飛び去らせて GO を出す。READY がまだ揺れている途中でも、今の状態から飛び去る。READY が出ていなければ GO だけ出す。
    /// </summary>
    public void PlayGo()
    {
        if (_image == null) return;

        bool isReadyShown = _group != null && _group.alpha > 0f;
        if (_sequence != null && _sequence.IsActive()) _sequence.Kill();
        if (_loopTween != null && _loopTween.IsActive()) _loopTween.Kill();

        _sequence = DOTween.Sequence();
        if (isReadyShown)
        {
            // 揺れや潰れの途中で止めたときのために、角度と大きさも元へ戻しながら飛び去る
            _sequence
                .Append(CreateReadyExit())
                .Join(Rotate(0f, SINK_DURATION, Ease.OutQuad))
                .Join(_image.DOScale(Vector3.one, SINK_DURATION).SetEase(Ease.OutQuad));
        }
        if (_goImage != null)
        {
            float goStart = isReadyShown ? SINK_DURATION + GO_START_DELAY : 0f;
            _sequence.Insert(goStart, CreateGoSequence());
        }
        _sequence
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
        if (_goImage != null) SetGo(Vector3.one, 0f, 0f);
    }

    private void Awake()
    {
        if (_goImage != null)
        {
            _goGroup = GetOrAddCanvasGroup(_goImage);
            _goGroup.alpha = 0f;
        }
        if (_image == null) return;

        _basePosition = _image.anchoredPosition;
        _group = GetOrAddCanvasGroup(_image);
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

    /// <summary> このコンポーネントを付けたときに、子の Image と Go を自動で探して入れる。 </summary>
    private void Reset()
    {
        _image = transform.Find("Image") as RectTransform;
        _goImage = transform.Find("Go") as RectTransform;
    }

    /// <summary> 消え終わったとき。知らせてから、くり返す設定なら次を予約する。 </summary>
    private void HandleFinished()
    {
        OnFinished?.Invoke();
        if (_loopDelay > 0f) _loopTween = DOVirtual.DelayedCall(_loopDelay, Play, true).SetLink(gameObject);
    }

    /// <summary> GO が出きったとき。 </summary>
    private void HandleGo()
    {
        OnGo?.Invoke();
    }

    /// <summary> READY が出るところ：落ちる → 潰れる → ぽよんと戻る → 左右に1回揺れる。Z の回転はプラスが反時計回りで、揺れは 左 → 右 → 元 の順。 </summary>
    private Sequence CreateReadyEnter()
    {
        return DOTween.Sequence()
            .Append(Move(_basePosition, FALL_DURATION, Ease.InQuad))
            .Join(Fade(1f, FALL_DURATION * FADE_IN_RATE))
            .Append(_image.DOScale(SQUASH_SCALE, SQUASH_DURATION).SetEase(Ease.OutQuad))
            .Append(_image.DOScale(Vector3.one, RECOVER_DURATION).SetEase(Ease.OutBack))
            .Append(Rotate(SWAY_ANGLE, SWAY_DURATION * 0.25f, Ease.OutSine))
            .Append(Rotate(-SWAY_ANGLE, SWAY_DURATION * 0.5f, Ease.InOutSine))
            .Append(Rotate(0f, SWAY_DURATION * 0.25f, Ease.InSine));
    }

    /// <summary> READY が消えるところ：少し沈む → 上へ飛び去りながら消える。 </summary>
    private Sequence CreateReadyExit()
    {
        return DOTween.Sequence()
            .Append(Move(_basePosition + new Vector2(0f, -SINK_DEPTH), SINK_DURATION, Ease.OutQuad))
            .Append(Move(_basePosition + new Vector2(0f, EXIT_HEIGHT), EXIT_DURATION, Ease.InQuad))
            .Join(Fade(0f, EXIT_DURATION));
    }

    /// <summary> GO：傾いた小さい状態からポンと大きく出る → 元の大きさに戻る（ここで OnGo）→ 止まる → 広がりながら消える。 </summary>
    private Sequence CreateGoSequence()
    {
        // 始まりの値は、動き出す前の状態に左右されないよう、すべて数値で決めておく
        return DOTween.Sequence()
            .AppendCallback(() => SetGo(Vector3.zero, GO_START_ANGLE, 1f))
            .Append(DOVirtual.Float(0f, GO_POP_SCALE, GO_POP_DURATION, SetGoScale).SetEase(Ease.OutBack))
            .Join(DOVirtual.Float(GO_START_ANGLE, 0f, GO_POP_DURATION, SetGoAngle).SetEase(Ease.OutBack))
            .Append(DOVirtual.Float(GO_POP_SCALE, 1f, GO_SETTLE_DURATION, SetGoScale).SetEase(Ease.OutQuad))
            .AppendCallback(HandleGo)
            .AppendInterval(GO_HOLD_DURATION)
            .Append(DOVirtual.Float(1f, GO_EXIT_SCALE, GO_EXIT_DURATION, SetGoScale).SetEase(Ease.OutQuad))
            .Join(DOVirtual.Float(1f, 0f, GO_EXIT_DURATION, value => _goGroup.alpha = value).SetEase(Ease.Linear));
    }

    /// <summary> 画像の位置・大きさ・角度・濃さをすぐ変える。 </summary>
    private void SetImage(Vector2 position, Vector3 scale, float angle, float alpha)
    {
        _image.anchoredPosition = position;
        _image.localScale = scale;
        _image.localRotation = Quaternion.Euler(0f, 0f, angle);
        if (_group != null) _group.alpha = alpha;
    }

    /// <summary> GO の大きさ・角度・濃さをすぐ変える。 </summary>
    private void SetGo(Vector3 scale, float angle, float alpha)
    {
        _goImage.localScale = scale;
        _goImage.localRotation = Quaternion.Euler(0f, 0f, angle);
        if (_goGroup != null) _goGroup.alpha = alpha;
    }

    private void SetGoScale(float scale)
    {
        _goImage.localScale = Vector3.one * scale;
    }

    private void SetGoAngle(float angle)
    {
        _goImage.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary> 画像に CanvasGroup を付けて（なければ足して）、クリックを受けないようにする。 </summary>
    private static CanvasGroup GetOrAddCanvasGroup(Component target)
    {
        var group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
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
