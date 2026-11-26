using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     ステージごとの抽選間隔・候補・ラッシュ突入時の効果を定義する。
    /// </summary>
    [CreateAssetMenu(menuName = "Scorpion/Gimmicks/Stage Gimmick Settings")]
    public sealed class StageGimmickSettings : ScriptableObject
    {
        /// <summary> 通常時の抽選間隔を秒数で取得する。 </summary>
        public float NormalInterval => Mathf.Max(MIN_INTERVAL, _normalInterval);
        /// <summary> ラッシュ時の抽選間隔を秒数で取得する。 </summary>
        public float RushInterval => Mathf.Clamp(_rushInterval, MIN_INTERVAL, NormalInterval);

        internal IReadOnlyList<GimmickSpawnEntry> NormalEntries => Array.AsReadOnly(_normalEntries);
        internal IReadOnlyList<GimmickSpawnEntry> RushEntries => Array.AsReadOnly(_rushEntries);
        internal IReadOnlyList<GimmickDefinition> RushStartGimmicks => Array.AsReadOnly(_rushStartGimmicks);

        [SerializeField, Min(MIN_INTERVAL), Tooltip("通常時の抽選間隔（秒）。")]
        private float _normalInterval = 5f;
        [SerializeField, Min(MIN_INTERVAL), Tooltip("ラッシュ時の抽選間隔（秒）。通常時の間隔を上限とします。")]
        private float _rushInterval = 2f;
        [SerializeField, Tooltip("通常時の重み付き抽選テーブル。")]
        private GimmickSpawnEntry[] _normalEntries = Array.Empty<GimmickSpawnEntry>();
        [SerializeField, Tooltip("ラッシュ時の重み付き抽選テーブル。")]
        private GimmickSpawnEntry[] _rushEntries = Array.Empty<GimmickSpawnEntry>();
        [SerializeField, Tooltip("残り10秒になった時点で一度だけ発動するギミック。")]
        private GimmickDefinition[] _rushStartGimmicks = Array.Empty<GimmickDefinition>();

        private const float MIN_INTERVAL = 0.1f;
    }
}
