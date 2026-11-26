using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    public event Action OnStageTimeExpired; /// 現ステージの制限時間が0になった時
    public event Action OnGimmickRushStarted; /// 残り10秒ラッシュ突入時
    public event Action OnGimmickSpawnTick; /// ギミックスポーン間隔に達した時

    public bool IsTimerRunning { get; private set; } /// カウントダウン中かどうか
    public bool IsGimmickRushActive { get; private set; } /// ギミックラッシュ中かどうか
    public float RemainingSeconds => _stageRemainingSeconds; /// 残り時間（秒）

    private float _stageRemainingSeconds; /// 現ステージの残り秒数
    private float _rushThresholdSeconds = 10f; /// ラッシュ突入の残り秒数
    private float _normalSpawnInterval = 1.5f; /// 通常時のギミック発生間隔
    private float _rushSpawnInterval = 0.4f; /// ラッシュ時のギミック発生間隔
    private float _gimmickSpawnTimer; /// ギミック生成間隔カウント用

    /// <summary>
    /// ステージ開始時に制限時間とラッシュ・スポーン設定を初期化する
    /// </summary>
    public void StartStageTimer(float durationSeconds, float rushThresholdSeconds, float normalSpawnInterval, float rushSpawnInterval)
    {
        _stageRemainingSeconds = Mathf.Max(1f, durationSeconds);
        _rushThresholdSeconds = rushThresholdSeconds > 0f ? rushThresholdSeconds : 10f;
        _normalSpawnInterval = Mathf.Max(0.2f, normalSpawnInterval);
        _rushSpawnInterval = Mathf.Max(0.1f, rushSpawnInterval);
        _gimmickSpawnTimer = 0f;

        IsGimmickRushActive = false;
        IsTimerRunning = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTimerText(_stageRemainingSeconds);
        }
    }

    /// <summary>
    /// タイマーを一時停止または完全停止する
    /// </summary>
    public void StopTimer()
    {
        IsTimerRunning = false;
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

    private void Update()
    {
        if (!IsTimerRunning)
        {
            return;
        }

        if (GameManager.Instance != null && !GameManager.Instance.IsMatchActive)
        {
            IsTimerRunning = false;
            return;
        }

        _stageRemainingSeconds = Mathf.Max(0f, _stageRemainingSeconds - Time.deltaTime);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTimerText(_stageRemainingSeconds);
        }

        EvaluateGimmickRush();
        UpdateGimmickSpawner();

        if (_stageRemainingSeconds <= 0f)
        {
            IsTimerRunning = false;
            OnStageTimeExpired?.Invoke();
        }
    }

    /// <summary>
    /// 残り時間がしきい値（10秒）以下になったらギミックラッシュ開始
    /// </summary>
    private void EvaluateGimmickRush()
    {
        if (!IsGimmickRushActive && _stageRemainingSeconds <= _rushThresholdSeconds)
        {
            IsGimmickRushActive = true;
            OnGimmickRushStarted?.Invoke();
        }
    }

    /// <summary>
    /// 通常時／ラッシュ時の間隔に合わせてギミック生成タイミングを進める
    /// </summary>
    private void UpdateGimmickSpawner()
    {
        float interval = IsGimmickRushActive ? _rushSpawnInterval : _normalSpawnInterval;
        _gimmickSpawnTimer += Time.deltaTime;

        if (_gimmickSpawnTimer >= interval)
        {
            _gimmickSpawnTimer = 0f;
            OnGimmickSpawnTick?.Invoke();
        }
    }
}
