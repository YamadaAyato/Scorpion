using System;
using System.Collections;
using System.Collections.Generic;
using Scorpion.Gimmicks;
using Scorpion.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     ステージPrefab、制限時間とギミックを設定する
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
    /// <summary> ステージ切り替え時に生成するPrefabを取得する </summary>
    public StageLayout Prefab => _prefab;
    /// <summary> ステージのギミック設定を取得する </summary>
    public StageGimmickSettings GimmickSettings => _gimmickSettings;

    [SerializeField, Tooltip("制限時間（秒）。0の場合は30秒")]
    private float _durationSeconds;
    [SerializeField, Tooltip("画面に表示するステージ名")]
    private string _stageName;
    [SerializeField, Tooltip("開始地点と復活地点を設定したステージPrefab")]
    private StageLayout _prefab;
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

    /// <summary> UIが追従する1P本体を取得する </summary>
    internal GameObject PlayerOneObject => _playerOneObject;
    /// <summary> UIが追従する2P本体を取得する </summary>
    internal GameObject PlayerTwoObject => _playerTwoObject;
    /// <summary> HUDが発動通知を購読するギミック管理を取得する </summary>
    internal GimmickManager Gimmicks => _gimmickManager;

    [SerializeField, Tooltip("1P本体")]
    private GameObject _playerOneObject;
    [SerializeField, Tooltip("2P本体")]
    private GameObject _playerTwoObject;
    [SerializeField, Tooltip("1Pの勝利2D画像")]
    private Image __playerOneWinImage;
    [SerializeField, Tooltip("1Pの敗北2D画像")]
    private Image __playerOneLoseImage;

    [SerializeField, Tooltip("2Pの勝利2D画像")]
    private Image __playerTwoWinImage;
    [SerializeField, Tooltip("2Pの敗北2D画像")]
    private Image __playerTwoLoseImage;
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
    private StageLayout _stageInstance;
    private MatchBackground _matchBackground;

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
        PrepareStage(0);
        BeginStage();
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
        __playerOneLoseImage.enabled=false;
        __playerOneWinImage.enabled=false;
        __playerTwoLoseImage.enabled=false;
        __playerTwoWinImage.enabled=false;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            StopStage();
            ClearStageInstance();
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
    ///     ステージPrefabを入れ替え、プレイヤーを開始地点に配置する
    /// </summary>
    private void PrepareStage(int stageIndex)
    {
        ClearStageInstance();
        CurrentStageIndex = stageIndex;
        StageDefinition stage = _stages[stageIndex];
        if (stage.Prefab != null)
        {
            _stageInstance = Instantiate(stage.Prefab);
            if (_stageInstance.TryGetComponent(out StageGimmickTargets targets))
            {
                PlayerMove playerOne = _playerOneObject != null ? _playerOneObject.GetComponent<PlayerMove>() : null;
                PlayerMove playerTwo = _playerTwoObject != null ? _playerTwoObject.GetComponent<PlayerMove>() : null;
                targets.SetPlayers(playerOne, playerTwo);
            }
        }

        if (_matchBackground == null)
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                _matchBackground = camera.GetComponentInChildren<MatchBackground>();
            }
        }

        if (_matchBackground != null)
        {
            _matchBackground.SetTexture(_stageInstance != null ? _stageInstance.BackgroundTexture : null);
        }

        PlacePlayer(1, isRespawn: false);
        PlacePlayer(2, isRespawn: false);
        SetPlayersStageIntroFrozen(true);
    }

    /// <summary>
    ///     準備済みのステージの開始を通知し、カウントダウンを始める
    /// </summary>
    private void BeginStage()
    {
        StageDefinition stage = _stages[CurrentStageIndex];
        OnStageStarted?.Invoke(CurrentStageIndex, stage);
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

        // カウントだけでなく START の表示中も入力を止め、表示が消えてから試合を進める
        OnStageCueChanged?.Invoke("START");
        yield return new WaitForSeconds(0.5f);
        _stageCueCoroutine = null;
        OnStageCueChanged?.Invoke(string.Empty);

        float duration = stage.DurationSeconds;
        if (_gimmickManager != null)
        {
            StageGimmickTargets targets = _stageInstance != null
                ? _stageInstance.GetComponent<StageGimmickTargets>()
                : null;
            _gimmickManager.StartStage(stage.GimmickSettings, duration, targets);
        }

        SetPlayersStageIntroFrozen(false);
        SetPlayerInputEnabled(true);
        _timer.StartStageTimer(duration);
    }

    /// <summary>
    ///     終了演出を表示し、次のステージまたはリザルトへ進む
    /// </summary>
    private IEnumerator AdvanceAfterFinish(int nextStageIndex)
    {
        if (nextStageIndex < _stages.Count)
        {
            // FINISH が画面を覆う間に入れ替え、表示が消えてからカウントダウンに進む
            float halfDuration = _finishDisplaySeconds * 0.5f;
            yield return new WaitForSeconds(halfDuration);
            PrepareStage(nextStageIndex);
            yield return new WaitForSeconds(halfDuration);
        }
        else
        {
            yield return new WaitForSeconds(_finishDisplaySeconds);
        }

        _stageCueCoroutine = null;
        OnStageCueChanged?.Invoke(string.Empty);
        if (nextStageIndex < _stages.Count)
        {
            IsMatchActive = true;
            BeginStage();
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
    ///     落下しても復活できない開始演出中だけ、両プレイヤーを開始地点に留める
    /// </summary>
    private void SetPlayersStageIntroFrozen(bool frozen)
    {
        if (_playerOneObject != null && _playerOneObject.TryGetComponent(out PlayerMove playerOne))
        {
            playerOne.SetStageIntroFrozen(frozen);
        }

        if (_playerTwoObject != null && _playerTwoObject.TryGetComponent(out PlayerMove playerTwo))
        {
            playerTwo.SetStageIntroFrozen(frozen);
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

        // 復活待ちを解除し、待機中にラウンドが終わっていたらプレイヤーを出さない
        _respawnCoroutines[playerIndex - 1] = null;
        if (IsMatchActive && _timer.IsTimerRunning)
        {
            PlacePlayer(playerIndex, isRespawn: true);
        }
    }

    /// <summary>
    ///     プレイヤーを開始地点または復活地点に配置し、移動速度をリセットする
    /// </summary>
    private void PlacePlayer(int playerIndex, bool isRespawn)
    {
        GameObject player = playerIndex == 1 ? _playerOneObject : _playerTwoObject;
        if (player == null)
        {
            return;
        }

        Transform spawn = _stageInstance != null ? _stageInstance.GetSpawnPoint(playerIndex, isRespawn) : null;

        player.SetActive(false);
        if (spawn != null)
        {
            player.transform.position = spawn.position;
        }

        player.SetActive(true);
        if (player.TryGetComponent(out Rigidbody body))
        {
            PlayerDeath.ResetBodyForSpawn(body, player.transform.position);
        }

        if (isRespawn && player.TryGetComponent(out PlayerDeath playerDeath))
        {
            playerDeath.StartRespawnProtection();
        }
    }

    /// <summary>
    ///     現在のステージPrefabを無効化してから破棄する
    /// </summary>
    private void ClearStageInstance()
    {
        if (_stageInstance != null)
        {
            _stageInstance.gameObject.SetActive(false);
            Destroy(_stageInstance.gameObject);
            _stageInstance = null;
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
        SetPlayersStageIntroFrozen(false);
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
        if (winner == 1)
        {
            __playerOneWinImage.enabled=true;
            __playerTwoLoseImage.enabled=true;
        }
        else
        {
            __playerTwoWinImage.enabled = true;
            __playerOneLoseImage.enabled = true;
        }
    }
}
