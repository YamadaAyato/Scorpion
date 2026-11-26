using System;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     抽選候補のギミックと当選の重みを保持する
    /// </summary>
    [Serializable]
    public sealed class GimmickSpawnEntry
    {
        /// <summary> 抽選対象のギミックを取得する </summary>
        public GimmickDefinition Definition => _definition;
        /// <summary> 抽選に使用する相対的な重みを取得する </summary>
        public int Weight => Mathf.Max(0, _weight);

        internal bool CanDraw => Weight > 0 && Definition != null && Definition.HasAction;

        [SerializeField, Tooltip("抽選対象のギミック")]
        private GimmickDefinition _definition;
        [SerializeField, Min(0), Tooltip("当選の重み。0の場合は抽選対象外です")]
        private int _weight = 1;

    }
}
