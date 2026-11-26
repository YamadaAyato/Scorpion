using System;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     発動中に両プレイヤーのジャンプ力を変える
    /// </summary>
    [Serializable]
    public sealed class JumpPowerMultiplierAction : IGimmickAction
    {
        [SerializeField, Min(0.01f), Tooltip("発動中のジャンプ力倍率。1で通常、1未満で低下")]
        private float _multiplier = 1.25f;

        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            return context.StageTargets != null && float.IsFinite(_multiplier) && _multiplier > 0f &&
                context.StageTargets.TryApplyPlayerEffect(context, player => player.ApplyJumpPowerMultiplier(_multiplier));
        }
    }
}
