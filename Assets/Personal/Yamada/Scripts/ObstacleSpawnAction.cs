using System;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     XY平面に物理挙動を持つ妨害物を飛来させる。
    /// </summary>
    [Serializable]
    public sealed class ObstacleSpawnAction : IGimmickAction
    {
        [SerializeField, Tooltip("ルートに非キネマティックなRigidbodyを持つ妨害物Prefab。")]
        private Rigidbody _prefab;
        [SerializeField, Tooltip("ステージの出現基準からのワールドXYオフセット。")]
        private Vector2 _spawnOffset = new(0f, 6f);
        [SerializeField, Tooltip("ワールドXY方向の初速。単位はメートル毎秒。")]
        private Vector2 _initialVelocity = new(0f, -8f);

        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            if (_prefab == null || _prefab.isKinematic)
            {
                Debug.LogWarning($"[{nameof(ObstacleSpawnAction)}] 非キネマティックなRigidbodyのPrefabを設定してください。");
                return false;
            }

            Rigidbody obstacle = UnityEngine.Object.Instantiate(
                _prefab, context.SpawnOrigin + (Vector3)_spawnOffset, _prefab.rotation);

            context.RegisterCleanup(() =>
            {
                if (obstacle != null)
                {
                    obstacle.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(obstacle.gameObject);
                }
            });

            // 3D物理の接触を使いながら、飛来物をプレイヤーの移動平面に留める。
            obstacle.constraints |= RigidbodyConstraints.FreezePositionZ;
            obstacle.linearVelocity = _initialVelocity;
            return true;
        }
    }
}
