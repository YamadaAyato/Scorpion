using System;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     ギミックの発動1回分の実行状態と解除処理を保持する
    /// </summary>
    public sealed class GimmickContext
    {
        internal GimmickContext(Vector3 spawnOrigin)
        {
            SpawnOrigin = spawnOrigin;
        }

        /// <summary> ステージ上の妨害物の出現基準位置を取得する </summary>
        public Vector3 SpawnOrigin { get; }

        internal int ScoreMultiplier { get; private set; } = 1;

        private Action _cleanup;

        /// <summary>
        ///     今回の発動が加点処理に適用する倍率を設定する
        /// </summary>
        public void SetScoreMultiplier(int multiplier)
        {
            ScoreMultiplier = Mathf.Max(1, multiplier);
        }

        /// <summary>
        ///     効果終了時に実行する解除処理を追加する
        /// </summary>
        public void RegisterCleanup(Action cleanup)
        {
            _cleanup += cleanup;
        }

        /// <summary>
        ///     今回の発動に紐づく効果を解除する
        /// </summary>
        internal void Clear()
        {
            ScoreMultiplier = 1;
            Action cleanup = _cleanup;
            _cleanup = null;
            if (cleanup == null)
            {
                return;
            }

            foreach (Action callback in cleanup.GetInvocationList())
            {
                try
                {
                    callback();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
    }
}
