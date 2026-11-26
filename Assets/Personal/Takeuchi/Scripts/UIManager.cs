using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     試合中のスコア・ステージ・残り時間・ラッシュ告知・リザルト画面のUI表示を管理するクラス
/// </summary>
[DefaultExecutionOrder(-100)]
public class UIManager : MonoBehaviour
{
    /// <summary>
    ///     どこからでもアクセスできる唯一のインスタンス
    /// </summary>
    public static UIManager Instance { get; private set; }

    [Header("スコアUI参照")]
    [SerializeField, Tooltip("1Pスコア表示")]
    private Text _playerOneScoreText;
    [SerializeField, Tooltip("2Pスコア表示")]
    private Text _playerTwoScoreText;

    [Header("進行・タイマーUI参照")]
    [SerializeField, Tooltip("ステージ表示")]
    private Text _stageText;
    [SerializeField, Tooltip("残り時間")]
    private Text _timerText;
    [SerializeField, Tooltip("ラッシュ告知")]
    private Text _rushNoticeText;
    [SerializeField, Tooltip("カウントダウン、START、FINISH を表示する")]
    private Text _stageCueText;

    [Header("リザルトUI参照")]
    [SerializeField, Tooltip("勝敗結果表示")]
    private Text _resultText;
    [SerializeField, Tooltip("リザルトパネル")]
    private GameObject _resultPanel;

    [SerializeField, Tooltip("ラッシュ中に使うタイマーの色")]
    private Color _rushTimerColor = new Color(1f, 0.35f, 0.25f);

    private ScoreManager _scoreManager;
    private GameManager _gameManager;
    private Color _normalTimerColor;

    /// <summary>
    ///     残り時間表示を更新する（小数点以下は切り上げ表示）
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
    ///     シングルトンインスタンスの初期化と重複破棄を行う
    /// </summary>
    private void Awake()
    {
        ///     重複インスタンスがあれば破棄して1つだけ残す
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (_timerText != null)
        {
            _normalTimerColor = _timerText.color;
        }
    }

    /// <summary>
    ///     有効化時に各マネージャーのイベントを購読する
    /// </summary>
    private void OnEnable()
    {
        SubscribeEvents();
    }

    /// <summary>
    ///     開始時にイベント購読の確定とUIの初期表示を行う
    /// </summary>
    private void Start()
    {
        ///     Awake後に各マネージャーのInstanceが揃ってから購読を確実にする
        SubscribeEvents();
        InitializeUIState();
    }

    /// <summary>
    ///     無効化時に登録したイベントの購読を解除する
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    ///     スコア変更イベントを受け取り、対象プレイヤーのスコア表示を更新する
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
    ///     ステージ開始イベントを受け取り、ステージ名の表示とラッシュ告知の非表示化を行う
    /// </summary>
    /// <param name="stageIndex">開始したステージのインデックス（0始まり）</param>
    /// <param name="stage">開始したステージの設定データ</param>
    private void HandleStageStarted(int stageIndex, StageDefinition stage)
    {
        if (_stageText != null)
        {
            _stageText.text = string.IsNullOrEmpty(stage.StageName)
                ? $"STAGE {stageIndex + 1}"
                : $"STAGE {stageIndex + 1}  {stage.StageName}";
        }

        if (_resultPanel != null)
        {
            _resultPanel.SetActive(false);
        }

        if (_timerText != null)
        {
            _timerText.color = _normalTimerColor;
        }

        UpdateTimerText(stage.DurationSeconds);

        if (_rushNoticeText != null)
        {
            _rushNoticeText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    ///     ギミックラッシュ開始イベントを受け取り、ラッシュ告知UIを表示する
    /// </summary>
    /// <param name="stageIndex">ラッシュが発生したステージのインデックス</param>
    /// <param name="stage">現在のステージ設定データ</param>
    private void HandleGimmickRushStarted(int stageIndex, StageDefinition stage)
    {
        if (_timerText != null)
        {
            _timerText.color = _rushTimerColor;
        }

        if (_rushNoticeText != null)
        {
            _rushNoticeText.gameObject.SetActive(true);
            _rushNoticeText.text = "GIMMICK RUSH!!";
        }
    }

    /// <summary>
    ///     カウントダウンとステージ終了の表示を切り替える
    /// </summary>
    private void HandleStageCueChanged(string cue)
    {
        if (_stageCueText != null)
        {
            _stageCueText.text = cue;
            _stageCueText.gameObject.SetActive(!string.IsNullOrEmpty(cue));
        }

        if (cue == "FINISH" && _rushNoticeText != null)
        {
            _rushNoticeText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    ///     試合終了イベントを受け取り、勝敗結果を表示してリザルトパネルを開く
    /// </summary>
    /// <param name="winnerPlayerIndex">勝者番号（1: 1P勝利, 2: 2P勝利, 0: 引き分け）</param>
    private void HandleMatchFinished(int winnerPlayerIndex)
    {
        if (_rushNoticeText != null)
        {
            _rushNoticeText.gameObject.SetActive(false);
        }

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

        if (_scoreManager != null)
        {
            _resultText.text += $"\n1P  {_scoreManager.PlayerOneScore}  -  {_scoreManager.PlayerTwoScore}  2P";
        }
    }

    /// <summary>
    ///     ScoreManager および GameManager のイベントを購読する
    /// </summary>
    private void SubscribeEvents()
    {
        if (_scoreManager == null && ScoreManager.Instance != null)
        {
            _scoreManager = ScoreManager.Instance;
            _scoreManager.OnScoreChanged += HandleScoreChanged;
        }

        if (_gameManager == null && GameManager.Instance != null)
        {
            _gameManager = GameManager.Instance;
            _gameManager.OnStageStarted += HandleStageStarted;
            _gameManager.OnGimmickRushStarted += HandleGimmickRushStarted;
            _gameManager.OnMatchFinished += HandleMatchFinished;
            _gameManager.OnStageCueChanged += HandleStageCueChanged;
        }
    }

    /// <summary>
    ///     購読中のイベントを解除し、再購読できる状態に戻す
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (_scoreManager != null)
        {
            _scoreManager.OnScoreChanged -= HandleScoreChanged;
            _scoreManager = null;
        }

        if (_gameManager != null)
        {
            _gameManager.OnStageStarted -= HandleStageStarted;
            _gameManager.OnGimmickRushStarted -= HandleGimmickRushStarted;
            _gameManager.OnMatchFinished -= HandleMatchFinished;
            _gameManager.OnStageCueChanged -= HandleStageCueChanged;
            _gameManager = null;
        }
    }

    /// <summary>
    ///     試合開始直後のUI初期状態（スコア・タイマー・告知・リザルトパネル）を整える
    /// </summary>
    private void InitializeUIState()
    {
        HandleScoreChanged(1, ScoreManager.Instance != null ? ScoreManager.Instance.PlayerOneScore : 0);
        HandleScoreChanged(2, ScoreManager.Instance != null ? ScoreManager.Instance.PlayerTwoScore : 0);
        UpdateTimerText(TimeManager.Instance != null ? TimeManager.Instance.RemainingSeconds : 0f);
        HandleStageCueChanged(string.Empty);

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
