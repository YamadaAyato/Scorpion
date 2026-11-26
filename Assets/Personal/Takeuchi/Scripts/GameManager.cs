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
    /// 外部へ状態変化を通知するイベント
    /// </summary>
    public event Action<int, StageDefinition> OnStageStarted; /// ステージ開始時
    public event Action<int, StageDefinition> OnGimmickRushStarted; /// 残り10秒ラッシュ突入時
    public event Action<int> OnMatchFinished; /// 全ステージ終了・勝敗確定時

    public bool IsMatchActive { get; private set; } /// 試合進行中かどうか
    public bool IsGimmickRushActive => TimeManager.Instance != null && TimeManager.Instance.IsGimmickRushActive;

    public int CurrentStageIndex
    {
        get => _currentStageIndex;
        private set
        {
            _currentStageIndex = Mathf.Max(0, value);
        }
    }

    [Header("プレイヤー")]

    [Header("1P本体・スポーン位置")]
    [Tooltip("1P本体")][SerializeField] private GameObject _playerOneObject; /// 1P本体
    [Tooltip("1Pスポーン位置")][SerializeField] private Transform _playerOneSpawn; /// 1Pスポーン位置

    [Header("2P本体・スポーン位置")]
    [Tooltip("2P本体")][SerializeField] private GameObject _playerTwoObject; /// 2P本体
    [Tooltip("2Pスポーン位置")][SerializeField] private Transform _playerTwoSpawn; /// 2Pスポーン位置

    [Header("ステージ")]
    [Tooltip("ステージ設定")][SerializeField] private List<StageDefinition> _stages = new List<StageDefinition>(); /// 全ステージ設定

    [Header("復活ルール設定")]
    [Tooltip("復活ルール")][SerializeField] private float _respawnDelaySeconds = 1.5f; /// 落下から復活までの待ち時間

    private int _currentStageIndex;
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
        ResetFlags();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore(); /// スコアリセット
        }

        IsMatchActive = true;
        _matchCoroutine = StartCoroutine(RunMatchSequence());
    }

    /// <summary>
    /// 落下したプレイヤーを一旦非表示にして、一定時間後に復活させる
    /// </summary>
    public void StartRespawn(int playerIndex)
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

    private void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnGimmickRushStarted += HandleGimmickRushStarted;
        }
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnGimmickRushStarted -= HandleGimmickRushStarted;
        }
    }

    private void Start()
    {
        /// Awake後に購読漏れがないよう保険として登録
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnGimmickRushStarted -= HandleGimmickRushStarted;
            TimeManager.Instance.OnGimmickRushStarted += HandleGimmickRushStarted;
        }

        StartMatch();
    }

    private void HandleGimmickRushStarted()
    {
        if (_stages.Count > 0 && CurrentStageIndex < _stages.Count)
        {
            OnGimmickRushStarted?.Invoke(CurrentStageIndex, _stages[CurrentStageIndex]);
        }
    }

    /// <summary>
    /// 全ステージを順番に進行するメインループ
    /// </summary>
    private IEnumerator RunMatchSequence()
    {
        for (int stageIndex = 0; stageIndex < _stages.Count; stageIndex++)
        {
            ApplyStage(stageIndex); /// ステージ切り替え＆タイマー開始

            while (IsMatchActive && TimeManager.Instance != null && TimeManager.Instance.IsTimerRunning)
            {
                yield return null;
            }

            if (!IsMatchActive)
            {
                yield break;
            }
            /// 制限時間到達 → 次のステージへ（スコアはそのまま引き継ぎ）
        }

        FinishMatch(); /// 全ステージ終了 → 合計スコアで勝敗判定
    }

    /// <summary>
    /// 試合開始時の復活状態・初期配置リセット
    /// </summary>
    private void ResetFlags()
    {
        _isPlayerOneRespawning = false;
        _isPlayerTwoRespawning = false;

        RespawnPlayerImmediate(playerIndex: 1);
        RespawnPlayerImmediate(playerIndex: 2);
    }

    /// <summary>
    /// 指定ステージのオブジェクト表示切替とタイマー開始
    /// </summary>
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

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StartStageTimer(
                currentStage.durationSeconds,
                currentStage.rushThresholdSeconds,
                currentStage.normalSpawnInterval,
                currentStage.rushSpawnInterval);
        }

        OnStageStarted?.Invoke(CurrentStageIndex, currentStage);
    }

    /// <summary>
    /// 復活待ち時間経過後にスポーン位置へ戻す
    /// </summary>
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

    /// <summary>
    /// スポーン位置へ移動して即座に有効化
    /// </summary>
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

    /// <summary>
    /// プレイヤーの表示/非表示を切り替え
    /// </summary>
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

    /// <summary>
    /// 進行中・復活待ちコルーチンをまとめて停止
    /// </summary>
    private void StopAllMatchCoroutines()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StopTimer();
        }

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

    /// <summary>
    /// 全ステージ終了時の勝敗判定（1: 1P勝利 / 2: 2P勝利 / 0: 引き分け）
    /// </summary>
    private void FinishMatch()
    {
        if (!IsMatchActive)
        {
            return;
        }

        IsMatchActive = false;
        StopAllMatchCoroutines();

        int winnerPlayerIndex = 0;

        if (ScoreManager.Instance != null)
        {
            if (ScoreManager.Instance.PlayerOneScore > ScoreManager.Instance.PlayerTwoScore)
            {
                winnerPlayerIndex = 1;
            }
            else if (ScoreManager.Instance.PlayerTwoScore > ScoreManager.Instance.PlayerOneScore)
            {
                winnerPlayerIndex = 2;
            }
        }

        OnMatchFinished?.Invoke(winnerPlayerIndex);
    }

    
}