using System;

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
            return context.StageTargets != null && context.StageTargets.TryEnableWind(context);
        }
    }
}
