using System;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     歯車を一時的に加速または逆回転させる
    /// </summary>
    [Serializable]
    public sealed class GearRotationAction : IGimmickAction
    {
        [SerializeField, Min(1f), Tooltip("歯車の回転速度にかける倍率")]
        private float _speedMultiplier = 1f;
        [SerializeField, Tooltip("発動中に歯車を逆回転させる")]
        private bool _reverse;

        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            return context.StageTargets != null &&
                context.StageTargets.TryModifyGear(context, Mathf.Max(1f, _speedMultiplier), _reverse);
        }
    }
}
