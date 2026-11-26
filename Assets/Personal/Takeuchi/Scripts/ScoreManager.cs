using System;
using UnityEngine;

/// <summary>
///     落下したプレイヤーの相手に加点し、全ステージの合計を保持する。
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary> 得点が変わったプレイヤー番号と更新後の得点を通知する。 </summary>
    public event Action<int, int> OnScoreChanged;

    /// <summary> 現在の得点管理を取得する。 </summary>
    public static ScoreManager Instance { get; private set; }
    /// <summary> 1Pの合計得点を取得する。 </summary>
    public int PlayerOneScore { get; private set; }
    /// <summary> 2Pの合計得点を取得する。 </summary>
    public int PlayerTwoScore { get; private set; }

    [SerializeField, Min(1), Tooltip("落下1回の基本点。")]
    private int _pointPerFall = 1;

    /// <summary>
    ///     落下で復活予約が成立した場合、相手に現在の倍率で加点する。
    /// </summary>
    public void NotifyPlayerFell(int fallenPlayerIndex)
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            return;
        }

        int points = Mathf.Max(1, _pointPerFall) * game.ScoreMultiplier;
        if (!game.TryStartRespawn(fallenPlayerIndex))
        {
            return;
        }

        if (fallenPlayerIndex == 1)
        {
            PlayerTwoScore += points;
            OnScoreChanged?.Invoke(2, PlayerTwoScore);
        }
        else
        {
            PlayerOneScore += points;
            OnScoreChanged?.Invoke(1, PlayerOneScore);
        }
    }

    /// <summary>
    ///     新しい試合のために両者の得点を初期化する。
    /// </summary>
    public void ResetScore()
    {
        PlayerOneScore = 0;
        PlayerTwoScore = 0;
        OnScoreChanged?.Invoke(1, PlayerOneScore);
        OnScoreChanged?.Invoke(2, PlayerTwoScore);
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
}
