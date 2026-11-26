using System;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     発動中に両プレイヤーの攻撃による吹っ飛ばしを強くする
    /// </summary>
    [Serializable]
    public sealed class KnockbackMultiplierAction : IGimmickAction
    {
        [SerializeField, Min(1f), Tooltip("相手と攻撃で弾ける落下物に対する吹っ飛ばし倍率")]
        private float _multiplier = 1.5f;

        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            return context.StageTargets != null && float.IsFinite(_multiplier) && _multiplier > 1f &&
                context.StageTargets.TryApplyPlayerEffect(context, player =>
                    player.TryGetComponent(out PlayerAttack attack) ? attack.ApplyKnockbackMultiplier(_multiplier) : null);
        }
    }
}
