using System;
using System.Collections;
using System.Collections.Generic;
using Scorpion.Gimmicks;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
///     ステージの制限時間、シーン内の構成、復活位置とギミックを設定する
/// </summary>
[Serializable]
public struct StageDefinition
{
    /// <summary> 無効な値には既定の30秒を使う制限時間 </summary>
    public float DurationSeconds => float.IsFinite(_durationSeconds) && _durationSeconds > 0f
        ? _durationSeconds
        : TimeManager.DEFAULT_STAGE_DURATION_SECONDS;

    /// <summary> 画面に表示するステージ名を取得する </summary>
    public string StageName => _stageName;
    /// <summary> ステージ中に有効化するルートオブジェクトを取得する </summary>
    public GameObject[] StageRoots => _stageRoots;
    /// <summary> 1Pのステージ専用の復活地点を取得する </summary>
    public Transform PlayerOneSpawn => _playerOneSpawn;
    /// <summary> 2Pのステージ専用の復活地点を取得する </summary>
    public Transform PlayerTwoSpawn => _playerTwoSpawn;
    /// <summary> ステージのギミック設定を取得する </summary>
    public StageGimmickSettings GimmickSettings => _gimmickSettings;

    [FormerlySerializedAs("durationSeconds")]
    [SerializeField, Tooltip("制限時間（秒）。0の場合は30秒")]
    private float _durationSeconds;
    [FormerlySerializedAs("stageName")]
    [SerializeField, Tooltip("画面に表示するステージ名")]
    private string _stageName;
    [FormerlySerializedAs("stageRoots")]
    [SerializeField, Tooltip("このステージで有効化するオブジェクト群")]
    private GameObject[] _stageRoots;
    [FormerlySerializedAs("playerOneSpawn")]
    [SerializeField, Tooltip("1Pの復活地点。未指定の場合は試合共通の位置を使用する")]
    private Transform _playerOneSpawn;
    [FormerlySerializedAs("playerTwoSpawn")]
    [SerializeField, Tooltip("2Pの復活地点。未指定の場合は試合共通の位置を使用する")]
    private Transform _playerTwoSpawn;
    [FormerlySerializedAs("gimmickSettings")]
    [SerializeField, Tooltip("通常・ラッシュの抽選とラッシュ突入時のギミック設定")]
    private StageGimmickSettings _gimmickSettings;
}

/// <summary>
///     ステージ進行、落下後の復活、試合結果の通知を管理する
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary> ステージが始まったときに通知する </summary>
    public event Action<int, StageDefinition> OnStageStarted;
    /// <summary> ステージがラッシュに入ったときに通知する </summary>
    public event Action<int, StageDefinition> OnGimmickRushStarted;
    /// <summary> 勝者番号を通知する。引き分けは0 </summary>
    public event Action<int> OnMatchFinished;
    /// <summary> カウントダウンやステージ終了の表示が変わったときに通知する </summary>
    public event Action<string> OnStageCueChanged;

    /// <summary> 現在の試合管理を取得する </summary>
    public static GameManager Instance { get; private set; }
    /// <summary> 試合が進行中かどうかを取得する </summary>
    public bool IsMatchActive { get; private set; }
    /// <summary> 現在のステージがラッシュ中かどうかを取得する </summary>
    public bool IsGimmickRushActive => _timer != null && _timer.IsGimmickRushActive;
    /// <summary> 現在のステージ番号を0始まりで取得する </summary>
    public int CurrentStageIndex { get; private set; }
    /// <summary> 加点時に使う現在のギミック倍率を取得する </summary>
    public int ScoreMultiplier => _gimmickManager != null ? _gimmickManager.ScoreMultiplier : 1;

    [SerializeField, Tooltip("1P本体")]
    private GameObject _playerOneObject;
    [SerializeField, Tooltip("1Pの試合共通の復活地点")]
    private Transform _playerOneSpawn;
    [SerializeField, Tooltip("2P本体")]
    private GameObject _playerTwoObject;
    [SerializeField, Tooltip("2Pの試合共通の復活地点")]
    private Transform _playerTwoSpawn;
    [SerializeField, Tooltip("試合で使用するステージを進行順に並べる")]
    private List<StageDefinition> _stages = new();
    [SerializeField, Tooltip("落下から復活までの待ち時間（秒）")]
    private float _respawnDelaySeconds = 0.75f;
    [SerializeField, Tooltip("ステージ設定に沿ってギミックを抽選するコンポーネント")]
    private GimmickManager _gimmickManager;
    [SerializeField, Min(0), Tooltip("ステージ開始前のカウントダウン秒数")]
    private int _countdownSeconds = 3;
    [SerializeField, Min(0), Tooltip("FINISH を表示してから次へ進むまでの秒数")]
    private float _finishDisplaySeconds = 0.6f;

    private readonly Coroutine[] _respawnCoroutines = new Coroutine[2];
    private TimeManager _timer;
    private Coroutine _stageCueCoroutine;

    /// <summary>
    ///     指定されたプレイヤーが復活待ちかどうかを取得する
    /// </summary>
    public bool IsPlayerRespawning(int playerIndex)
    {
        return playerIndex is 1 or 2 && _respawnCoroutines[playerIndex - 1] != null;
    }

    /// <summary>
    ///     得点をリセットし、最初のステージから試合を始める
    /// </summary>
    public void StartMatch()
    {
        if (!isActiveAndEnabled || Instance != this)
        {
            return;
        }

        StopStage();
        AttachTimer(TimeManager.Instance);
        if (_timer == null || ScoreManager.Instance == null || _stages.Count == 0)
        {
            Debug.LogWarning($"[{nameof(GameManager)}] タイマー、スコア、進行するステージを設定してください。", this);
            return;
        }

        ScoreManager.Instance.ResetScore();
        IsMatchActive = true;
        ApplyStage(0);
    }

    /// <summary>
    ///     落下したプレイヤーを復活待ちにする
    /// </summary>
    public void StartRespawn(int playerIndex)
    {
        TryStartRespawn(playerIndex);
    }

    /// <summary>
    ///     有効なステージで、重複していない復活を予約する
    /// </summary>
    public bool TryStartRespawn(int playerIndex)
    {
        if (!IsMatchActive || _timer == null || !_timer.IsTimerRunning ||
            playerIndex is not (1 or 2) || IsPlayerRespawning(playerIndex))
        {
            return false;
        }

        _respawnCoroutines[playerIndex - 1] = StartCoroutine(RespawnAfterDelay(playerIndex));
        return true;
    }

    /// <summary>
    ///     この試合のプレイヤーの落下を得点処理に渡す
    /// </summary>
    /// <returns> この試合で管理するプレイヤーの場合はtrue </returns>
    public bool TryHandlePlayerFall(GameObject player)
    {
        if (player == _playerOneObject)
        {
            ScoreManager.Instance?.NotifyPlayerFell(1);
            return true;
        }

        if (player == _playerTwoObject)
        {
            ScoreManager.Instance?.NotifyPlayerFell(2);
            return true;
        }

        return false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (_gimmickManager == null)
        {
            TryGetComponent(out _gimmickManager);
        }
    }

    private void Start()
    {
        StartMatch();
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            StopStage();
            AttachTimer(null);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleTimeChanged(float remainingSeconds)
    {
        if (IsMatchActive && _gimmickManager != null)
        {
            _gimmickManager.UpdateStageTime(remainingSeconds);
        }
    }

    private void HandleGimmickRushStarted()
    {
        if (IsMatchActive)
        {
            OnGimmickRushStarted?.Invoke(CurrentStageIndex, _stages[CurrentStageIndex]);
        }
    }

    private void HandleStageTimeExpired()
    {
        if (!IsMatchActive)
        {
            return;
        }

        int nextStageIndex = CurrentStageIndex + 1;
        StopStage();
        OnStageCueChanged?.Invoke("FINISH");
        _stageCueCoroutine = StartCoroutine(AdvanceAfterFinish(nextStageIndex));
    }

    /// <summary>
    ///     タイマーの通知先を設定する
    /// </summary>
    private void AttachTimer(TimeManager timer)
    {
        if (_timer != null)
        {
            _timer.OnTimeChanged -= HandleTimeChanged;
            _timer.OnGimmickRushStarted -= HandleGimmickRushStarted;
            _timer.OnStageTimeExpired -= HandleStageTimeExpired;
        }

        _timer = timer;
        if (_timer != null)
        {
            _timer.OnTimeChanged += HandleTimeChanged;
            _timer.OnGimmickRushStarted += HandleGimmickRushStarted;
            _timer.OnStageTimeExpired += HandleStageTimeExpired;
        }
    }

    /// <summary>
    ///     足場を切り替え、プレイヤーを配置して次のステージを開始する
    /// </summary>
    private void ApplyStage(int stageIndex)
    {
        CurrentStageIndex = stageIndex;
        StageDefinition stage = _stages[stageIndex];
        for (int i = 0; i < _stages.Count; i++)
        {
            GameObject[] roots = _stages[i].StageRoots;
            if (roots == null)
            {
                continue;
            }

            foreach (GameObject root in roots)
            {
                if (root != null)
                {
                    root.SetActive(stage.StageRoots != null && Array.IndexOf(stage.StageRoots, root) >= 0);
                }
            }
        }

        RespawnPlayerImmediate(1);
        RespawnPlayerImmediate(2);
        OnStageStarted?.Invoke(stageIndex, stage);
        _stageCueCoroutine = StartCoroutine(CountDownToStageStart(stage));
    }

    /// <summary>
    ///     開始演出が終わってから操作、ギミック、残り時間の進行を開始する
    /// </summary>
    private IEnumerator CountDownToStageStart(StageDefinition stage)
    {
        SetPlayerInputEnabled(false);
        for (int remaining = _countdownSeconds; remaining > 0; remaining--)
        {
            OnStageCueChanged?.Invoke(remaining.ToString());
            yield return new WaitForSeconds(1f);
        }

        float duration = stage.DurationSeconds;
        if (_gimmickManager != null)
        {
            _gimmickManager.StartStage(stage.GimmickSettings, duration);
        }

        SetPlayerInputEnabled(true);
        _timer.StartStageTimer(duration);
        OnStageCueChanged?.Invoke("START");
        yield return new WaitForSeconds(0.5f);
        _stageCueCoroutine = null;
        OnStageCueChanged?.Invoke(string.Empty);
    }

    /// <summary>
    ///     終了演出を表示し、次のステージまたはリザルトへ進む
    /// </summary>
    private IEnumerator AdvanceAfterFinish(int nextStageIndex)
    {
        yield return new WaitForSeconds(_finishDisplaySeconds);
        _stageCueCoroutine = null;
        OnStageCueChanged?.Invoke(string.Empty);
        if (nextStageIndex < _stages.Count)
        {
            IsMatchActive = true;
            ApplyStage(nextStageIndex);
        }
        else
        {
            FinishMatch();
        }
    }

    /// <summary>
    ///     管理対象の2人に入力の可否を反映する
    /// </summary>
    private void SetPlayerInputEnabled(bool isEnabled)
    {
        if (_playerOneObject != null && _playerOneObject.TryGetComponent(out PlayerMove playerOne))
        {
            playerOne.SetInputEnabled(isEnabled);
        }

        if (_playerTwoObject != null && _playerTwoObject.TryGetComponent(out PlayerMove playerTwo))
        {
            playerTwo.SetInputEnabled(isEnabled);
        }
    }

    /// <summary>
    ///     復活待ち時間のあと、現在のステージの地点へ戻す
    /// </summary>
    private IEnumerator RespawnAfterDelay(int playerIndex)
    {
        GameObject player = playerIndex == 1 ? _playerOneObject : _playerTwoObject;
        if (player != null)
        {
            player.SetActive(false);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, _respawnDelaySeconds));

        _respawnCoroutines[playerIndex - 1] = null;
        if (IsMatchActive && _timer.IsTimerRunning)
        {
            RespawnPlayerImmediate(playerIndex);
        }
    }

    /// <summary>
    ///     プレイヤーを現在のステージの専用位置に配置し、移動速度をリセットする
    /// </summary>
    private void RespawnPlayerImmediate(int playerIndex)
    {
        GameObject player = playerIndex == 1 ? _playerOneObject : _playerTwoObject;
        if (player == null)
        {
            return;
        }

        StageDefinition stage = _stages[CurrentStageIndex];
        Transform spawn = playerIndex == 1 ? stage.PlayerOneSpawn : stage.PlayerTwoSpawn;
        if (spawn == null)
        {
            spawn = playerIndex == 1 ? _playerOneSpawn : _playerTwoSpawn;
        }

        player.SetActive(false);
        if (spawn != null)
        {
            player.transform.position = spawn.position;
        }

        player.SetActive(true);
        if (player.TryGetComponent(out Rigidbody body))
        {
            body.position = player.transform.position;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    ///     ステージのタイマー、ギミック、復活待ちを終了する
    /// </summary>
    private void StopStage()
    {
        IsMatchActive = false;
        if (_stageCueCoroutine != null)
        {
            StopCoroutine(_stageCueCoroutine);
            _stageCueCoroutine = null;
        }

        SetPlayerInputEnabled(false);
        if (_timer != null)
        {
            _timer.StopTimer();
        }

        if (_gimmickManager != null)
        {
            _gimmickManager.EndStage();
        }

        for (int i = 0; i < _respawnCoroutines.Length; i++)
        {
            if (_respawnCoroutines[i] != null)
            {
                StopCoroutine(_respawnCoroutines[i]);
                _respawnCoroutines[i] = null;
            }
        }
    }

    /// <summary>
    ///     両者の合計得点から勝敗を決めて通知する
    /// </summary>
    private void FinishMatch()
    {
        if (_playerOneObject != null)
        {
            _playerOneObject.SetActive(false);
        }

        if (_playerTwoObject != null)
        {
            _playerTwoObject.SetActive(false);
        }

        ScoreManager score = ScoreManager.Instance;
        int winner = score.PlayerOneScore == score.PlayerTwoScore ? 0
            : score.PlayerOneScore > score.PlayerTwoScore ? 1 : 2;
        OnMatchFinished?.Invoke(winner);
    }
}
