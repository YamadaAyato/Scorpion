using System;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     発動中に両プレイヤーの獲得ポイント倍率を引き上げる。
    /// </summary>
    [Serializable]
    public sealed class ScoreMultiplierAction : IGimmickAction
    {
        [SerializeField, Min(2), Tooltip("ギミック発動中の獲得ポイント倍率。")]
        private int _multiplier = 2;

        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            context.SetScoreMultiplier(Mathf.Max(2, _multiplier));
            return true;
        }
    }
}
