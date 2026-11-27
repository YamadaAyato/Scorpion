using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// ステージの制限時間が終わったときの「TIME UP!!」コール。大きく傾いた状態から中央へドンと叩きつけるように出て、
/// 画面を揺らすように小刻みに震え、少し待ってから小さくなりながら消える。
/// TIME UP!! の画像（Image）はこのオブジェクトの子に置く。
/// Play On Finish Cue がオンなら、GameManager がステージの終わりに FINISH を出したときに自動で出す（試合のシーンに置くだけでよい）。
/// 消え終わったら OnFinished を呼ぶ。
/// </summary>
public class TimeUpCallEffect : MonoBehaviour
{
    /// <summary> コールが消え終わったとき。 </summary>
    public event Action OnFinished;

    [Tooltip("動かす TIME UP!! の画像")]
    [SerializeField] private RectTransform _image;

    [Tooltip("GameManager がステージの終わりに FINISH を出したときに、自動で出す")]
    [SerializeField] private bool _playOnFinishCue = true;

    private CanvasGroup _group;
    private Vector3 _baseLocalPosition;
    private Sequence _sequence;
    private GameManager _game;

    // 出始めの大きさと傾き（度）、着地で一瞬つぶれる大きさ、消えるときの大きさ
    private const float START_SCALE = 1.8f;
    private const float START_ANGLE = 10f;
    private static readonly Vector3 IMPACT_SCALE = new Vector3(1.08f, 0.92f, 1f);
    private const float EXIT_SCALE = 0.6f;

    // 震えの強さ（Canvas 上の単位）と細かさ
    private const float SHAKE_STRENGTH = 14f;
    private const int SHAKE_VIBRATO = 25;

    // 時間（秒）：叩きつける、つぶれから戻る、震える、止まる、消える
    private const float SLAM_DURATION = 0.16f;
    private const float RECOVER_DURATION = 0.18f;
    private const float SHAKE_DURATION = 0.3f;
    private const float HOLD_DURATION = 0.35f;
    private const float EXIT_DURATION = 0.25f;

    // 叩きつける時間のうち、透明から見えるようになるまでの割合
    private const float FADE_IN_RATE = 0.5f;

    // GameManager がステージの終わりに出す表示の文字
    private const string FINISH_CUE = "FINISH";

    /// <summary> コールを最初から出す。 </summary>
    public void Play()
    {
        if (_image == null) return;

        Stop();
        _image.localScale = Vector3.one * START_SCALE;
        _image.localRotation = Quaternion.Euler(0f, 0f, START_ANGLE);

        // 始まりの値は、動き出す前の状態に左右されないよう、数値で決めておく
        _sequence = DOTween.Sequence()
            .Append(DOVirtual.Float(START_SCALE, 1f, SLAM_DURATION, SetScale).SetEase(Ease.InQuad))
            .Join(DOVirtual.Float(START_ANGLE, 0f, SLAM_DURATION, SetAngle).SetEase(Ease.OutQuad))
            .Join(DOVirtual.Float(0f, 1f, SLAM_DURATION * FADE_IN_RATE, SetAlpha))
            .Append(_image.DOScale(IMPACT_SCALE, RECOVER_DURATION * 0.3f).SetEase(Ease.OutQuad))
            .Append(_image.DOScale(Vector3.one, RECOVER_DURATION * 0.7f).SetEase(Ease.OutBack))
            .Join(_image.DOShakePosition(SHAKE_DURATION, SHAKE_STRENGTH, SHAKE_VIBRATO))
            .AppendInterval(HOLD_DURATION)
            .Append(DOVirtual.Float(1f, EXIT_SCALE, EXIT_DURATION, SetScale).SetEase(Ease.InBack))
            .Join(DOVirtual.Float(1f, 0f, EXIT_DURATION, SetAlpha))
            .OnComplete(HandleFinished)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    /// <summary> 動きを止めて隠す。 </summary>
    public void Stop()
    {
        if (_sequence != null && _sequence.IsActive()) _sequence.Kill();
        _sequence = null;
        if (_image == null) return;

        _image.localPosition = _baseLocalPosition;
        _image.localScale = Vector3.one;
        _image.localRotation = Quaternion.identity;
        SetAlpha(0f);
    }

    private void Awake()
    {
        if (_image == null) return;

        _baseLocalPosition = _image.localPosition;
        _group = _image.GetComponent<CanvasGroup>();
        if (_group == null) _group = _image.gameObject.AddComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;
        _group.alpha = 0f;
    }

    // GameManager の Instance は Awake で決まるので、購読は Start で行う
    private void Start()
    {
        if (!_playOnFinishCue) return;

        _game = GameManager.Instance;
        if (_game != null) _game.OnStageCueChanged += HandleStageCueChanged;
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        if (_game != null) _game.OnStageCueChanged -= HandleStageCueChanged;
    }

    /// <summary> このコンポーネントを付けたときに、子の Image を自動で探して入れる。 </summary>
    private void Reset()
    {
        _image = transform.Find("Image") as RectTransform;
    }

    /// <summary> 表示が FINISH になったとき（ステージの制限時間が終わったとき）。TIME UP!! を出す。 </summary>
    private void HandleStageCueChanged(string cue)
    {
        if (cue == FINISH_CUE) Play();
    }

    /// <summary> 消え終わったとき。 </summary>
    private void HandleFinished()
    {
        OnFinished?.Invoke();
    }

    private void SetScale(float scale)
    {
        _image.localScale = Vector3.one * scale;
    }

    private void SetAngle(float angle)
    {
        _image.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SetAlpha(float alpha)
    {
        if (_group != null) _group.alpha = alpha;
    }
}
