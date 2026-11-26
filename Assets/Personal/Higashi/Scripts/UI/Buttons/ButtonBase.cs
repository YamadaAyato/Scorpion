using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ボタンの共通処理をまとめた基底クラス。
/// 選択されたとき（カーソルが乗った・キーで選ばれた）の拡大、押したときに弾む動き、押せないときの震え、連打防止をここで行う。
/// 押されたときの処理は、継承したクラスの Execute に書く。動きを変えたいときは PlayHighlight / PlayPress / PlayDenied を上書きする。
/// 押した処理はこのクラスが呼ぶので、Button の On Click() には何も入れない（入れると待ち時間や連打防止が効かずに実行される）。
/// </summary>
[RequireComponent(typeof(Button))]
public abstract class ButtonBase : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, ISubmitHandler
{
    /// <summary> 選択された瞬間（カーソルが乗った・キーで選ばれた）。選択音を鳴らすのに使う。 </summary>
    public event Action<ButtonBase> OnHighlighted;

    /// <summary> 選択が外れた瞬間。 </summary>
    public event Action<ButtonBase> OnUnhighlighted;

    /// <summary> マウスのボタンを押し込んだ瞬間（まだ離していない）。押している間の見た目に使う。キーで決定したときは呼ばれない。 </summary>
    public event Action<ButtonBase> OnHoldStarted;

    /// <summary> 押し込んでいたマウスのボタンを離した瞬間。ボタンの外で離したときも呼ばれる（そのときは OnPressed は呼ばれない）。 </summary>
    public event Action<ButtonBase> OnHoldEnded;

    /// <summary> 押された瞬間。決定音を鳴らすのに使う。 </summary>
    public event Action<ButtonBase> OnPressed;

    /// <summary> 押せない状態で押されたとき。ブブッという音を鳴らすのに使う。 </summary>
    public event Action<ButtonBase> OnDenied;

    /// <summary> 押してから待ち時間が過ぎて、Execute を実行したあと。 </summary>
    public event Action<ButtonBase> OnSubmitted;

    /// <summary> 押せるかどうか。false にすると選択の見た目が外れ、押すと震える。 </summary>
    public bool Interactable
    {
        get => CachedButton.interactable;
        set
        {
            CachedButton.interactable = value;
            UpdateHighlight();
        }
    }

    /// <summary> 押せない状態になっているか（押してから実行するまでの間、または実行後も押せないままにしている間）。 </summary>
    public bool IsLocked => _isLocked;

    /// <summary> 選択の見た目になっているか（押せる状態で、カーソルが乗っているかキーで選択中）。 </summary>
    public bool IsHighlighted => (_isPointerOver || _isSelected) && CachedButton.IsInteractable();

    [Header("選択")]
    [Tooltip("カーソルが乗ったとき・キーで選んだときの大きさ（元の何倍か）")]
    [SerializeField] private float _highlightScale = 1.08f;

    [Tooltip("選択の大きさに変わるまでの時間（秒）")]
    [SerializeField] private float _highlightDuration = 0.15f;

    [Header("押したとき")]
    [Tooltip("押した瞬間につぶれる大きさ（X は横、Y は縦。元の何倍か）")]
    [SerializeField] private Vector2 _pressSquash = new Vector2(1.1f, 0.85f);

    [Tooltip("つぶれるまでの時間（秒）")]
    [SerializeField] private float _pressDuration = 0.06f;

    [Tooltip("つぶれた状態から弾んで戻るまでの時間（秒）")]
    [SerializeField] private float _releaseDuration = 0.2f;

    [Tooltip("押してから Execute を実行するまでの待ち時間（秒）。すぐ画面が切り替わると押した動きが見えないため")]
    [SerializeField] private float _submitDelay = 0.2f;

    [Tooltip("実行したあとも押せないままにするか。押してから実行するまでの待ち時間の間は、この設定に関係なく押せない（連打で2回実行されるのを防ぐ）。オブジェクトが表示し直されると押せる状態に戻る")]
    [SerializeField] private bool _stayLockedAfterSubmit;

    [Header("押せないとき")]
    [Tooltip("押せない状態で押されたときに横に震える幅（Canvas 上の単位）")]
    [SerializeField] private float _deniedShakeStrength = 12f;

    [Tooltip("震える時間（秒）")]
    [SerializeField] private float _deniedShakeDuration = 0.3f;

    [Header("時間")]
    [Tooltip("Time.timeScale を無視して動かすか。ポーズ中（timeScale = 0）でもボタンが動くよう、基本はオンのままにする")]
    [SerializeField] private bool _ignoreTimeScale = true;

    private Button _button;
    private Vector3 _baseScale;
    private bool _isPointerOver;
    private bool _isSelected;
    private bool _isLocked;
    private bool _isHolding;
    private bool _wasHighlighted;
    private Tween _scaleTween;
    private Tween _shakeTween;
    private Tween _submitTween;

    // 押せないときに震える回数（大きいほど小刻みに震える）
    private const int DENIED_SHAKE_VIBRATO = 20;

    /// <summary> カーソルが乗ったら選択の見た目にする。 </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
        UpdateHighlight();
    }

    /// <summary> カーソルが離れたら元の見た目に戻す（キーで選択中なら選択の見た目のまま）。 </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        UpdateHighlight();
    }

    /// <summary> キーやパッドで選ばれたら選択の見た目にする。 </summary>
    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        UpdateHighlight();
    }

    /// <summary> 選択が外れたら元の見た目に戻す（カーソルが乗っていれば選択の見た目のまま）。 </summary>
    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        UpdateHighlight();
    }

    /// <summary> マウスのボタンを押し込んだら、押している間の見た目のためにイベントを出す。 </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!CachedButton.IsInteractable() || _isLocked) return;

        _isHolding = true;
        OnHoldStarted?.Invoke(this);
    }

    /// <summary> 押し込んでいたマウスのボタンを離したらイベントを出す（このあとボタンの上ならクリックとして押される）。 </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !_isHolding) return;

        _isHolding = false;
        OnHoldEnded?.Invoke(this);
    }

    /// <summary> クリックされたら押す。 </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 右クリック・中クリックでは押さない
        if (eventData.button != PointerEventData.InputButton.Left) return;

        TryPress();
    }

    /// <summary> 決定キーが押されたら押す。 </summary>
    public void OnSubmit(BaseEventData eventData)
    {
        TryPress();
    }

    protected virtual void Awake()
    {
        _button = GetComponent<Button>();
        _baseScale = transform.localScale;
    }

    protected virtual void OnEnable()
    {
        // 画面を開き直したときに、前に押した状態が残らないようにする
        _isLocked = false;
        _isHolding = false;
        _isPointerOver = false;
        _isSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        _wasHighlighted = IsHighlighted;
        transform.localScale = CurrentTargetScale();
    }

    protected virtual void OnDisable()
    {
        // 震えは途中で止めると位置がずれたままになるので、最後まで進めて元の位置に戻す
        KillTween(_shakeTween, true);
        KillTween(_scaleTween);
        KillTween(_submitTween);
        transform.localScale = _baseScale;
    }

    /// <summary> 押されたときの処理。継承したクラスで書く。 </summary>
    protected abstract void Execute();

    /// <summary> 選択の見た目に切り替える動き。isHighlighted が true なら大きく、false なら元の大きさに戻す。 </summary>
    protected virtual void PlayHighlight(bool isHighlighted)
    {
        KillTween(_scaleTween);
        _scaleTween = transform.DOScale(isHighlighted ? HighlightScale() : _baseScale, _highlightDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 押した瞬間の動き。縦につぶれてから弾んで戻る。 </summary>
    protected virtual void PlayPress()
    {
        KillTween(_scaleTween);
        Vector3 squashScale = Vector3.Scale(_baseScale, new Vector3(_pressSquash.x, _pressSquash.y, 1f));

        _scaleTween = DOTween.Sequence()
            .Append(transform.DOScale(squashScale, _pressDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(CurrentTargetScale(), _releaseDuration).SetEase(Ease.OutBack))
            .SetUpdate(_ignoreTimeScale)
            .SetLink(gameObject);
    }

    /// <summary> 押せない状態で押されたときの動き。横に震える。 </summary>
    protected virtual void PlayDenied()
    {
        // 前の震えが残っていると位置がずれたまま始まるので、先に元の位置に戻す
        KillTween(_shakeTween, true);

        Vector3 startPosition = transform.localPosition;
        _shakeTween = transform.DOShakePosition(_deniedShakeDuration, new Vector3(_deniedShakeStrength, 0f, 0f), DENIED_SHAKE_VIBRATO, 0f)
            .SetUpdate(_ignoreTimeScale)
            .OnComplete(() => transform.localPosition = startPosition)
            .SetLink(gameObject);
    }

    /// <summary> 実行後も押せないままにしたボタンを、もう一度押せるようにする。 </summary>
    protected void Unlock()
    {
        _isLocked = false;
    }

    /// <summary> Awake より前に Interactable が呼ばれても動くよう、Button を必要になった時点で取得する。 </summary>
    private Button CachedButton => _button != null ? _button : (_button = GetComponent<Button>());

    /// <summary> 押す処理。押せない・連打防止中なら押さない。 </summary>
    private void TryPress()
    {
        if (!CachedButton.IsInteractable())
        {
            PlayDenied();
            OnDenied?.Invoke(this);
            return;
        }
        if (_isLocked) return;

        // 実行するまでの待ち時間の間に、もう一度押されないようにする
        _isLocked = true;
        PlayPress();
        OnPressed?.Invoke(this);

        KillTween(_submitTween);
        if (_submitDelay <= 0f)
        {
            CompleteSubmit();
            return;
        }
        _submitTween = DOVirtual.DelayedCall(_submitDelay, CompleteSubmit, _ignoreTimeScale).SetLink(gameObject);
    }

    /// <summary> 待ち時間が過ぎたら、継承したクラスの処理を実行する。 </summary>
    private void CompleteSubmit()
    {
        Execute();
        if (!_stayLockedAfterSubmit) _isLocked = false;
        OnSubmitted?.Invoke(this);
    }

    /// <summary> 選択の見た目が変わったときだけ、動きとイベントを出す。 </summary>
    private void UpdateHighlight()
    {
        bool isHighlighted = IsHighlighted;
        if (isHighlighted == _wasHighlighted) return;

        _wasHighlighted = isHighlighted;
        PlayHighlight(isHighlighted);
        if (isHighlighted) OnHighlighted?.Invoke(this);
        else OnUnhighlighted?.Invoke(this);
    }

    /// <summary> 選択されているときの大きさ。 </summary>
    private Vector3 HighlightScale()
    {
        return _baseScale * _highlightScale;
    }

    /// <summary> 今の状態で目指す大きさ。 </summary>
    private Vector3 CurrentTargetScale()
    {
        return IsHighlighted ? HighlightScale() : _baseScale;
    }

    /// <summary> まだ動いている Tween だけを止める（止まっているものを止めようとすると警告が出るため）。complete が true なら最後まで進めてから止める。 </summary>
    private static void KillTween(Tween tween, bool complete = false)
    {
        if (tween != null && tween.IsActive()) tween.Kill(complete);
    }
}
