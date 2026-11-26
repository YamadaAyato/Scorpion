using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
   public static ScoreManager Instance { get; private set; }

    [Tooltip("落下スコア")][SerializeField] private int _pointPerFall = 1; /// 落下1回あたりの加点（基本1pt）

    private int _playerOneScore; /// ステージを跨いでも引き継ぐ合計スコア
    private int _playerTwoScore; /// ステージを跨いでも引き継ぐ合計スコア
    public event Action<int, int> OnScoreChanged; /// 1P/2Pのスコア変化時
    public int PlayerOneScore
    {
        get => _playerOneScore;
        private set
        {
            _playerOneScore = Mathf.Max(0, value);
            
            OnScoreChanged?.Invoke(1, _playerOneScore); /// 1Pスコア更新を通知
        }
    }

    public int PlayerTwoScore
    {
        get => _playerTwoScore;
        private set
        {
            _playerTwoScore = Mathf.Max(0, value);
            
            OnScoreChanged?.Invoke(2, _playerTwoScore); /// 2Pスコア更新を通知
        }
    }

    /// <summary>
    /// どちらかが落下したときに呼ぶ（落下した側ではなく相手側に+1pt＆復活予約）
    /// </summary>
    /// <param name="fallenPlayerIndex"></param>
    public void NotifyPlayerFell(int fallenPlayerIndex)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsMatchActive)
        {
            return;
        }

        /// すでに復活待ち中のプレイヤーは再落下判定しない
        if (GameManager.Instance.IsPlayerRespawning(fallenPlayerIndex))
        {
            return;
        }

        if (fallenPlayerIndex == 1)
        {
            PlayerTwoScore += _pointPerFall; /// 1P落下 → 2Pに加点
            GameManager.Instance.StartRespawn(playerIndex: 1);
        }
        else if (fallenPlayerIndex == 2)
        {
            PlayerOneScore += _pointPerFall; /// 2P落下 → 1Pに加点
            GameManager.Instance.StartRespawn(playerIndex: 2);
        }
    }

    /// <summary>
    /// 試合開始時のスコアリセット
    /// </summary>
    public void ResetScore()
    {
        PlayerOneScore = 0;
        PlayerTwoScore = 0;
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
}
