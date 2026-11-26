using System;
using UnityEngine;

/// <summary>
///     ステージの残り時間とラッシュへの移行を管理する
/// </summary>
[DefaultExecutionOrder(100)]
public class TimeManager : MonoBehaviour
{
    /// <summary> 残り秒数が設定・更新されたときに通知する </summary>
    public event Action<float> OnTimeChanged;
    /// <summary> 制限時間が0になったときに通知する </summary>
    public event Action OnStageTimeExpired;
    /// <summary> 残り10秒に入ったときに通知する </summary>
    public event Action OnGimmickRushStarted;

    /// <summary> 現在のタイマーを取得する </summary>
    public static TimeManager Instance { get; private set; }
    /// <summary> カウントダウン中かどうかを取得する </summary>
    public bool IsTimerRunning { get; private set; }
    /// <summary> ラッシュ中かどうかを取得する </summary>
    public bool IsGimmickRushActive { get; private set; }
    /// <summary> ステージの残り秒数を取得する </summary>
    public float RemainingSeconds { get; private set; }

    /// <summary> 仕様で定められたラッシュ開始時の残り秒数 </summary>
    public const float RUSH_REMAINING_SECONDS = 10f;

    internal const float DEFAULT_STAGE_DURATION_SECONDS = 30f;

    /// <summary>
    ///     指定秒数でステージのカウントダウンを始める
    /// </summary>
    public void StartStageTimer(float durationSeconds)
    {
        RemainingSeconds = float.IsFinite(durationSeconds) && durationSeconds > 0f
            ? durationSeconds
            : DEFAULT_STAGE_DURATION_SECONDS;
        IsGimmickRushActive = false;
        IsTimerRunning = true;
        PublishTime();
    }

    /// <summary>
    ///     カウントダウンとラッシュ状態を終了する
    /// </summary>
    public void StopTimer()
    {
        IsTimerRunning = false;
        IsGimmickRushActive = false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!IsTimerRunning)
        {
            return;
        }

        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
        if (RemainingSeconds <= 0f)
        {
            StopTimer();
            PublishTime();
            OnStageTimeExpired?.Invoke();
            return;
        }

        PublishTime();
    }

    /// <summary>
    ///     残り時間を反映し、一度だけラッシュ開始を通知する
    /// </summary>
    private void PublishTime()
    {
        bool startsRush = IsTimerRunning && !IsGimmickRushActive && RemainingSeconds <= RUSH_REMAINING_SECONDS;
        if (startsRush)
        {
            IsGimmickRushActive = true;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTimerText(RemainingSeconds);
        }

        OnTimeChanged?.Invoke(RemainingSeconds);
        if (startsRush && IsGimmickRushActive)
        {
            OnGimmickRushStarted?.Invoke();
        }
    }
}
