using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct StageDefinition
{
    [Tooltip("このステージの制限時間（秒）")]
    public float durationSeconds; /// ステージの長さ(30秒）
    [Tooltip("残り何秒になったらギミック大量発生（ラッシュ）に入るか")]
    public float rushThresholdSeconds; /// ラッシュ突入の残り秒数(10秒)
    [Tooltip("ステージごとの表示名")]
    public string stageName; /// UI表示用のステージ名
    [Tooltip("このステージで有効化するオブジェクト群")]
    public GameObject[] stageRoots; /// ステージごとの床・背景・ギミック親など
    [Tooltip("このステージ通常時のギミックスポーン間隔（秒）")]
    public float normalSpawnInterval; /// 通常時の発生間隔
    [Tooltip("残り10秒ラッシュ時のギミックスポーン間隔（秒）")]
    public float rushSpawnInterval; /// ラッシュ時の発生間隔
}

public class GameManager : MonoBehaviour
{
    /// <summary>
    /// どこからでも GameManager.Instance で呼べるシングルトン
    /// </summary>
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// 外部（UI・演出・ギミック制御など）へ状態変化を通知するイベント
    /// </summary>
    public event Action<int, StageDefinition> OnStageStarted; /// ステージ開始時
    public event Action<int, StageDefinition> OnGimmickRushStarted; /// 残り10秒ラッシュ突入時
    public event Action<int, int> OnScoreChanged; /// 1P/2Pのスコア変化時
    public event Action<int> OnMatchFinished; /// 全ステージ終了・勝敗確定時

    public bool IsMatchActive { get; private set; } /// 試合進行中かどうか
    public bool IsGimmickRushActive { get; private set; } /// ギミックラッシュ中かどうか

    public int CurrentStageIndex
    {
        get => _currentStageIndex;
        private set
        {
            _currentStageIndex = Mathf.Max(0, value);
            UpdateStageUi(); /// ステージ番号が変わったら表示も更新
        }
    }

    public int PlayerOneScore
    {
        get => _playerOneScore;
        private set
        {
            _playerOneScore = Mathf.Max(0, value);
            UpdateScoreUi();
            OnScoreChanged?.Invoke(1, _playerOneScore); /// 1Pスコア更新を通知
        }
    }

    public int PlayerTwoScore
    {
        get => _playerTwoScore;
        private set
        {
            _playerTwoScore = Mathf.Max(0, value);
            UpdateScoreUi();
            OnScoreChanged?.Invoke(2, _playerTwoScore); /// 2Pスコア更新を通知
        }
    }

    [Header("プレイヤー・UI参照")]

    [Header("1p本体・スポーン位置・スコア表示")]
    [Tooltip("1ｐ本体")][SerializeField] private GameObject _playerOneObject; /// 1P本体
    [Tooltip("1ｐスポーン位置")][SerializeField] private Transform _playerOneSpawn; /// 1Pスポーン位置
    [Tooltip("1ｐスコア表示")][SerializeField] private Text _playerOneScoreText; /// 1Pスコア表示

    [Header("2p本体・スポーン位置・スコア表示")]
    [Tooltip("2ｐ本体")][SerializeField] private GameObject _playerTwoObject; /// 2P本体
    [Tooltip("2ｐスポーン位置")][SerializeField] private Transform _playerTwoSpawn; /// 2Pスポーン位置
    [Tooltip("2ｐスコア表示")][SerializeField] private Text _playerTwoScoreText; /// 2Pスコア表示

    [Header("ステージ・残り時間・ラッシュ告知表示")]
    [Tooltip("ステージ表示")][SerializeField] private Text _stageText; /// 現在のステージ表示
    [Tooltip("残り時間")][SerializeField] private Text _timerText; /// 残り時間表示
    [Tooltip("ラッシュ告知")][SerializeField] private Text _rushNoticeText; /// ギミックラッシュ告知表示

    [Header("ステージ・復活ルール設定")]
    [Tooltip("復活ルール")] [SerializeField] private float _respawnDelaySeconds = 1.5f; /// 落下から復活までの待ち時間
    [Tooltip("落下スコア")] [SerializeField] private int _pointPerFall = 1; /// 落下1回あたりの加点（基本1pt）
    [Tooltip("ステージ設定")] [SerializeField] private List<StageDefinition> _stages = new List<StageDefinition>(); /// 全ステージ設定

    private int _currentStageIndex;
    private int _playerOneScore; /// ステージを跨いでも引き継ぐ合計スコア
    private int _playerTwoScore; /// ステージを跨いでも引き継ぐ合計スコア
    private float _stageRemainingSeconds; /// 現ステージの残り秒数
    private float _gimmickSpawnTimer; /// ギミック生成間隔カウント用
    private bool _isPlayerOneRespawning; /// 1P復活待ち中フラグ
    private bool _isPlayerTwoRespawning; /// 2P復活待ち中フラグ
    private Coroutine _matchCoroutine; /// 全体進行コルーチン
    private Coroutine _playerOneRespawnCoroutine; /// 1P復活コルーチン
    private Coroutine _playerTwoRespawnCoroutine; /// 2P復活コルーチン

    public bool IsPlayerRespawning(int playerIndex)
    {
        if (playerIndex == 1)
        {
            return _isPlayerOneRespawning;
        }

        if (playerIndex == 2)
        {
            return _isPlayerTwoRespawning;
        }

        return false;
    }

    /// <summary>
    /// 試合の開始・リスタート処理
    /// </summary>
    public void StartMatch()
    {
        StopAllMatchCoroutines();
        ResetScoresAndFlags();
        ApplyStage(0); /// 1ステージ目から開始

        IsMatchActive = true;
        _matchCoroutine = StartCoroutine(RunMatchSequence());
    }

    /// <summary>
    /// どちらかが落下したときに呼ぶ（落下した側ではなく相手側に+1pt＆復活予約）
    /// </summary>
    /// <param name="fallenPlayerIndex"></param>
    public void NotifyPlayerFell(int fallenPlayerIndex)
    {
        if (!IsMatchActive)
        {
            return;
        }

        /// すでに復活待ち中のプレイヤーは再落下判定しない
        if (IsPlayerRespawning(fallenPlayerIndex))
        {
            return;
        }

        if (fallenPlayerIndex == 1)
        {
            PlayerTwoScore += _pointPerFall; /// 1P落下 → 2Pに加点
            StartRespawn(playerIndex: 1);
        }
        else if (fallenPlayerIndex == 2)
        {
            PlayerOneScore += _pointPerFall; /// 2P落下 → 1Pに加点
            StartRespawn(playerIndex: 2);
        }
    }

    /// <summary>
    /// 両方が落下したときに呼ぶ
    /// </summary>
    public void NotifySimultaneousFall()
    {
        if (!IsMatchActive)
        {
            return;
        }

        PlayerOneScore += _pointPerFall;
        PlayerTwoScore += _pointPerFall;

        StartRespawn(playerIndex: 1);
        StartRespawn(playerIndex: 2);
    }

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

    private void Start()
    {
        StartMatch();
    }

    /// <summary>
    /// 全ステージを順番に進行するメインループ
    /// </summary>
    /// <returns></returns>
    private IEnumerator RunMatchSequence()
    {
        for (int stageIndex = 0; stageIndex < _stages.Count; stageIndex++)
        {
            ApplyStage(stageIndex); /// ステージ切り替え＆タイマー初期化

            while (_stageRemainingSeconds > 0f)
            {
                if (!IsMatchActive)
                {
                    yield break;
                }

                _stageRemainingSeconds = Mathf.Max(0f, _stageRemainingSeconds - Time.deltaTime);
                UpdateTimerUi();
                EvaluateGimmickRush(_stages[stageIndex]); /// 残り10秒チェック
                UpdateGimmickSpawner(_stages[stageIndex]); /// ギミック発生間隔管理

                yield return null;
            }
            /// 制限時間到達 → 次のステージへ（スコアはそのまま引き継ぎ）
        }

        FinishMatch(); /// 全ステージ終了 → 合計スコアで勝敗判定
    }

    /// <summary>
    /// 試合開始時のスコア・復活状態・初期配置リセット
    /// </summary>
    private void ResetScoresAndFlags()
    {
        PlayerOneScore = 0;
        PlayerTwoScore = 0;
        _isPlayerOneRespawning = false;
        _isPlayerTwoRespawning = false;

        RespawnPlayerImmediate(playerIndex: 1);
        RespawnPlayerImmediate(playerIndex: 2);
    }

    /// <summary>
    /// 指定ステージのオブジェクト表示切替とタイマー・ラッシュ状態の初期化
    /// </summary>
    /// <param name="stageIndex"></param>
    private void ApplyStage(int stageIndex)
    {
        if (_stages.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(stageIndex, 0, _stages.Count - 1);
        StageDefinition currentStage = _stages[clampedIndex];

        /// 対象ステージだけ有効化し、それ以外のステージは非表示にする
        for (int i = 0; i < _stages.Count; i++)
        {
            GameObject[] stageRoots = _stages[i].stageRoots;
            bool isTargetStage = i == clampedIndex;

            if (stageRoots == null)
            {
                continue;
            }

            for (int j = 0; j < stageRoots.Length; j++)
            {
                if (stageRoots[j] != null)
                {
                    stageRoots[j].SetActive(isTargetStage);
                }
            }
        }

        CurrentStageIndex = clampedIndex;
        _stageRemainingSeconds = Mathf.Max(1f, currentStage.durationSeconds);
        _gimmickSpawnTimer = 0f;
        IsGimmickRushActive = false;

        if (_rushNoticeText != null)
        {
            _rushNoticeText.gameObject.SetActive(false);
        }

        UpdateTimerUi();
        OnStageStarted?.Invoke(CurrentStageIndex, currentStage);
    }

    /// <summary>
    /// 残り時間がしきい値（10秒）以下になったらギミックラッシュ開始
    /// </summary>
    /// <param name="stage"></param>
    private void EvaluateGimmickRush(StageDefinition stage)
    {
        float threshold = stage.rushThresholdSeconds > 0f ? stage.rushThresholdSeconds : 10f;

        if (!IsGimmickRushActive && _stageRemainingSeconds <= threshold)
        {
            IsGimmickRushActive = true;

            if (_rushNoticeText != null)
            {
                _rushNoticeText.gameObject.SetActive(true);
                _rushNoticeText.text = "GIMMICK RUSH!!";
            }

            OnGimmickRushStarted?.Invoke(CurrentStageIndex, stage);
        }
    }

    /// <summary>
    /// 通常時／ラッシュ時の間隔に合わせてギミック生成タイミングを進める
    /// </summary>
    /// <param name="stage"></param>
    private void UpdateGimmickSpawner(StageDefinition stage)
    {
        float interval = IsGimmickRushActive
            ? Mathf.Max(0.1f, stage.rushSpawnInterval)
            : Mathf.Max(0.2f, stage.normalSpawnInterval);

        _gimmickSpawnTimer += Time.deltaTime;

        if (_gimmickSpawnTimer >= interval)
        {
            _gimmickSpawnTimer = 0f;
            /// TODO: ここにギミック生成処理（またはギミックスポナー呼び出し）を入れる
        }
    }

    /// <summary>
    /// 落下したプレイヤーを一旦非表示にして、一定時間後に復活させる
    /// </summary>
    /// <param name="playerIndex"></param>
    private void StartRespawn(int playerIndex)
    {
        if (playerIndex == 1)
        {
            SetPlayerActive(playerIndex: 1, isActive: false);
            _isPlayerOneRespawning = true;

            if (_playerOneRespawnCoroutine != null)
            {
                StopCoroutine(_playerOneRespawnCoroutine);
            }

            _playerOneRespawnCoroutine = StartCoroutine(RespawnAfterDelay(playerIndex: 1));
        }
        else if (playerIndex == 2)
        {
            SetPlayerActive(playerIndex: 2, isActive: false);
            _isPlayerTwoRespawning = true;

            if (_playerTwoRespawnCoroutine != null)
            {
                StopCoroutine(_playerTwoRespawnCoroutine);
            }

            _playerTwoRespawnCoroutine = StartCoroutine(RespawnAfterDelay(playerIndex: 2));
        }
    }

    /// 復活待ち時間経過後にスポーン位置へ戻す
    private IEnumerator RespawnAfterDelay(int playerIndex)
    {
        yield return new WaitForSeconds(_respawnDelaySeconds);

        if (!IsMatchActive)
        {
            yield break;
        }

        RespawnPlayerImmediate(playerIndex);

        if (playerIndex == 1)
        {
            _isPlayerOneRespawning = false;
            _playerOneRespawnCoroutine = null;
        }
        else if (playerIndex == 2)
        {
            _isPlayerTwoRespawning = false;
            _playerTwoRespawnCoroutine = null;
        }
    }

    /// スポーン位置へ移動して即座に有効化
    private void RespawnPlayerImmediate(int playerIndex)
    {
        if (playerIndex == 1 && _playerOneObject != null && _playerOneSpawn != null)
        {
            _playerOneObject.transform.position = _playerOneSpawn.position;
            _playerOneObject.SetActive(true);
        }
        else if (playerIndex == 2 && _playerTwoObject != null && _playerTwoSpawn != null)
        {
            _playerTwoObject.transform.position = _playerTwoSpawn.position;
            _playerTwoObject.SetActive(true);
        }
    }

    /// プレイヤーの表示/非表示を切り替え
    private void SetPlayerActive(int playerIndex, bool isActive)
    {
        if (playerIndex == 1 && _playerOneObject != null)
        {
            _playerOneObject.SetActive(isActive);
        }
        else if (playerIndex == 2 && _playerTwoObject != null)
        {
            _playerTwoObject.SetActive(isActive);
        }
    }

    /// 進行中・復活待ちコルーチンをまとめて停止
    private void StopAllMatchCoroutines()
    {
        if (_matchCoroutine != null)
        {
            StopCoroutine(_matchCoroutine);
            _matchCoroutine = null;
        }

        if (_playerOneRespawnCoroutine != null)
        {
            StopCoroutine(_playerOneRespawnCoroutine);
            _playerOneRespawnCoroutine = null;
        }

        if (_playerTwoRespawnCoroutine != null)
        {
            StopCoroutine(_playerTwoRespawnCoroutine);
            _playerTwoRespawnCoroutine = null;
        }
    }

    /// 全ステージ終了時の勝敗判定（1: 1P勝利 / 2: 2P勝利 / 0: 引き分け）
    private void FinishMatch()
    {
        if (!IsMatchActive)
        {
            return;
        }

        IsMatchActive = false;
        StopAllMatchCoroutines();

        int winnerPlayerIndex = 0;

        if (_playerOneScore > _playerTwoScore)
        {
            winnerPlayerIndex = 1;
        }
        else if (_playerTwoScore > _playerOneScore)
        {
            winnerPlayerIndex = 2;
        }

        OnMatchFinished?.Invoke(winnerPlayerIndex);
    }

    /// スコアUI更新
    private void UpdateScoreUi()
    {
        if (_playerOneScoreText != null)
        {
            _playerOneScoreText.text = $"1P: {_playerOneScore}";
        }

        if (_playerTwoScoreText != null)
        {
            _playerTwoScoreText.text = $"2P: {_playerTwoScore}";
        }
    }

    /// 残り時間UI更新（切り上げ表示）
    private void UpdateTimerUi()
    {
        if (_timerText != null)
        {
            _timerText.text = Mathf.CeilToInt(_stageRemainingSeconds).ToString();
        }
    }

    /// ステージ名UI更新
    private void UpdateStageUi()
    {
        if (_stageText == null || _stages.Count == 0)
        {
            return;
        }

        string title = string.IsNullOrEmpty(_stages[CurrentStageIndex].stageName)
            ? $"STAGE {CurrentStageIndex + 1}"
            : _stages[CurrentStageIndex].stageName;

        _stageText.text = title;
    }
}