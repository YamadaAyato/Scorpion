using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     得点時に獲得ポイントを表示し、上へ移動しながらフェードアウト
/// </summary>
public class ScorePopupUI : MonoBehaviour
{
    [SerializeField, Tooltip("表示を担当するプレイヤー番号")]
    private int myPlayerIndex;
    [SerializeField, Tooltip("フェードを始めるまでの表示時間（秒）")]
    private float showDuration = 1f;
    [SerializeField, Tooltip("フェードアウトにかかる時間（秒）")]
    private float fadeDuration = 0.5f;
    [SerializeField, Tooltip("表示を上に動かす距離")]
    private float moveDistance = 30f;

    private RectTransform _rectTransform;
    private Vector2 _startPosition;
    private Text _pointText;
    private ScoreManager _scoreManager;

    private void Awake()
    {
        _pointText = GetComponent<Text>();
        _rectTransform = _pointText.rectTransform;
        _startPosition = _rectTransform.anchoredPosition;
    }

    private void Start()
    {
        _scoreManager = ScoreManager.Instance;
        if (_scoreManager != null)
        {
            _scoreManager.OnPointGained += HandlePointGained;
        }
    }

    private void OnDestroy()
    {
        if (_scoreManager != null)
        {
            _scoreManager.OnPointGained -= HandlePointGained;
        }
    }

    /// <summary>
    ///     担当プレイヤーに加算された得点と、適用された倍率を表示
    /// </summary>
    private void HandlePointGained(int playerIndex, int points, int multiplier)
    {
        if (playerIndex != myPlayerIndex)
        {
            return;
        }

        _pointText.text = multiplier > 1 ? $"+{points} Point (×{multiplier})" : $"+{points} Point";

        // 連続で得点したときも表示位置と透明度を初期状態からやり直す
        _pointText.DOKill();
        _rectTransform.DOKill();
        Color color = _pointText.color;
        color.a = 1f;
        _pointText.color = color;
        _rectTransform.anchoredPosition = _startPosition;

        _rectTransform.DOAnchorPosY(_startPosition.y + moveDistance, showDuration + fadeDuration)
            .SetLink(gameObject);
        _pointText.DOFade(0f, fadeDuration)
            .SetDelay(showDuration)
            .SetLink(gameObject);
    }
}
