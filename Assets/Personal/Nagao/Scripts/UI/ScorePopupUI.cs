using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class ScorePopupUI : MonoBehaviour
{
    [Header("プレイヤー設定")]
    [SerializeField] int myPlayerIndex;


    [Header("アニメーション")]
    [SerializeField] float showDuration = 1f;
    [SerializeField] float fadeDuration = 0.5f;
    [SerializeField] float moveDistance = 30f;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Text pointText;

    private void Awake()
    {
        pointText = GetComponent<Text>();
        rectTransform = pointText.GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    private void Start()
    {
        if (ScoreManager.Instance == null) return;

        ScoreManager.Instance.OnPointGained += ShowPoint;
    }
    private void OnDestroy()
    {
        if (ScoreManager.Instance == null) return;

        ScoreManager.Instance.OnPointGained -= ShowPoint;
    }

    // 得点したプレイヤーと獲得ポイントを受け取る
    void ShowPoint(int playerIndex, int points)
    {
        if (playerIndex != myPlayerIndex) return;

        pointText.text = $"+{points} Point";

        // 前回のアニメーションを停止
        pointText.DOKill();
        rectTransform.DOKill();

        // 初期状態に戻す
        Color color=pointText.color;
        color.a = 1f;
        pointText.color=color;
        rectTransform.anchoredPosition = startPosition;

        // 上に移動
        rectTransform.DOAnchorPosY(
            startPosition.y + moveDistance,
            showDuration + fadeDuration
        );

        // フェードアウト
        pointText.DOFade(0f, fadeDuration)
            .SetDelay(showDuration);
        Debug.Log(points);
    }
}