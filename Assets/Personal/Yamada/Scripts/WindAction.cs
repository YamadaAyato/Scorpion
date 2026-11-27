using System;
using Scorpion.Audio;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     ステージに設定された風を発動する
    /// </summary>
    [Serializable]
    public sealed class WindAction : IGimmickAction
    {
        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            if (context.StageTargets == null || !context.StageTargets.TryEnableWind(context))
            {
                return false;
            }

            AudioManager.Instance?.PlaySE("se_wind");
            return true;
        }
    }
}
