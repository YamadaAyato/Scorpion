using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 試合中のスコア・ステージ・残り時間・ラッシュ告知・リザルト画面のUI表示を管理するクラス。
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>
    /// どこからでもアクセスできる唯一のインスタンス。
    /// </summary>
    public static UIManager Instance { get; private set; }

    [Header("スコアUI参照")]
    [Tooltip("1Pスコア表示")][SerializeField] private Text _playerOneScoreText; /// 1Pスコア表示
    [Tooltip("2Pスコア表示")][SerializeField] private Text _playerTwoScoreText; /// 2Pスコア表示

    [Header("進行・タイマーUI参照")]
    [Tooltip("ステージ表示")][SerializeField] private Text _stageText; /// 現在のステージ表示
    [Tooltip("残り時間")][SerializeField] private Text _timerText; /// 残り時間表示
    [Tooltip("ラッシュ告知")][SerializeField] private Text _rushNoticeText; /// ギミックラッシュ告知表示

    [Header("リザルトUI参照")]
    [Tooltip("勝敗結果表示")][SerializeField] private Text _resultText; /// 勝敗結果表示
    [Tooltip("リザルトパネル")][SerializeField] private GameObject _resultPanel; /// 終了時に表示するパネル

    private bool _isSubscribed; /// イベント二重購読防止フラグ

    /// <summary>
    /// 残り時間表示を更新する（小数点以下は切り上げ表示）。
    /// </summary>
    /// <param name="remainingSeconds">現在の残り時間（秒）</param>
    public void UpdateTimerText(float remainingSeconds)
    {
        if (_timerText != null)
        {
            _timerText.text = Mathf.CeilToInt(remainingSeconds).ToString();
        }
    }

    /// <summary>
    /// シングルトンインスタンスの初期化と重複破棄を行う。
    /// </summary>
    private void Awake()
    {
        /// 重複インスタンスがあれば破棄して1つだけ残す
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 有効化時に各マネージャーのイベントを購読する。
    /// </summary>
    private void OnEnable()
    {
        SubscribeEvents();
    }

    /// <summary>
    /// 開始時にイベント購読の確定とUIの初期表示を行う。
    /// </summary>
    private void Start()
    {
        /// Awake後に各マネージャーのInstanceが揃ってから購読を確実にする
        SubscribeEvents();
        InitializeUIState();
    }

    /// <summary>
    /// 無効化時に登録したイベントの購読を解除する。
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    /// <summary>
    /// スコア変更イベントを受け取り、対象プレイヤーのスコア表示を更新する。
    /// </summary>
    /// <param name="playerIndex">スコアが変わったプレイヤー番号（1: 1P, 2: 2P）</param>
    /// <param name="newScore">更新後のスコア値</param>
    private void HandleScoreChanged(int playerIndex, int newScore)
    {
        if (playerIndex == 1 && _playerOneScoreText != null)
        {
            _playerOneScoreText.text = $"1P: {newScore}";
        }
        else if (playerIndex == 2 && _playerTwoScoreText != null)
        {
            _playerTwoScoreText.text = $"2P: {newScore}";
        }
    }

    /// <summary>
    /// ステージ開始イベントを受け取り、ステージ名の表示とラッシュ告知の非表示化を行う。
    /// </summary>
    /// <param name="stageIndex">開始したステージのインデックス（0始まり）</param>
    /// <param name="stage">開始したステージの設定データ</param>
    private void HandleStageStarted(int stageIndex, StageDefinition stage)
    {
        if (_stageText != null)
        {
            _stageText.text = string.IsNullOrEmpty(stage.stageName)
                ? $"STAGE {stageIndex + 1}"
                : stage.stageName;
        }

        if (_rushNoticeText != null)
        {
            _rushNoticeText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ギミックラッシュ開始イベントを受け取り、ラッシュ告知UIを表示する。
    /// </summary>
    /// <param name="stageIndex">ラッシュが発生したステージのインデックス</param>
    /// <param name="stage">現在のステージ設定データ</param>
    private void HandleGimmickRushStarted(int stageIndex, StageDefinition stage)
    {
        if (_rushNoticeText != null)
        {
            _rushNoticeText.gameObject.SetActive(true);
            _rushNoticeText.text = "GIMMICK RUSH!!";
        }
    }

    /// <summary>
    /// 試合終了イベントを受け取り、勝敗結果を表示してリザルトパネルを開く。
    /// </summary>
    /// <param name="winnerPlayerIndex">勝者番号（1: 1P勝利, 2: 2P勝利, 0: 引き分け）</param>
    private void HandleMatchFinished(int winnerPlayerIndex)
    {
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(true);
        }

        if (_resultText == null)
        {
            return;
        }

        if (winnerPlayerIndex == 1)
        {
            _resultText.text = "1P WIN!";
        }
        else if (winnerPlayerIndex == 2)
        {
            _resultText.text = "2P WIN!";
        }
        else
        {
            _resultText.text = "DRAW";
        }
    }

    /// <summary>
    /// ScoreManager および GameManager のイベントを購読する。
    /// </summary>
    private void SubscribeEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStageStarted += HandleStageStarted;
            GameManager.Instance.OnGimmickRushStarted += HandleGimmickRushStarted;
            GameManager.Instance.OnMatchFinished += HandleMatchFinished;
        }

        if (ScoreManager.Instance != null && GameManager.Instance != null)
        {
            _isSubscribed = true;
        }
    }

    /// <summary>
    /// 購読中のイベントを解除し、再購読できる状態に戻す。
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStageStarted -= HandleStageStarted;
            GameManager.Instance.OnGimmickRushStarted -= HandleGimmickRushStarted;
            GameManager.Instance.OnMatchFinished -= HandleMatchFinished;
        }

        _isSubscribed = false;
    }

    /// <summary>
    /// 試合開始直後のUI初期状態（スコア・タイマー・告知・リザルトパネル）を整える。
    /// </summary>
    private void InitializeUIState()
    {
        HandleScoreChanged(1, ScoreManager.Instance != null ? ScoreManager.Instance.PlayerOneScore : 0);
        HandleScoreChanged(2, ScoreManager.Instance != null ? ScoreManager.Instance.PlayerTwoScore : 0);
        UpdateTimerText(TimeManager.Instance != null ? TimeManager.Instance.RemainingSeconds : 0f);

        if (_rushNoticeText != null)
        {
            _rushNoticeText.gameObject.SetActive(false);
        }

        if (_resultPanel != null)
        {
            _resultPanel.SetActive(false);
        }
    }
}
