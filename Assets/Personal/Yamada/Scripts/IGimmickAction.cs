namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     発動ごとのコンテキストに効果を適用する
    /// </summary>
    public interface IGimmickAction
    {
        /// <summary>
        ///     効果の適用を試みる。実行状態と解除処理はコンテキストに保持する
        /// </summary>
        /// <returns> 効果を適用できた場合はtrue </returns>
        bool TryActivate(GimmickContext context);
    }
}
