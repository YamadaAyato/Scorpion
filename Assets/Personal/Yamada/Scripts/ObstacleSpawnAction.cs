using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     XY平面に物理挙動を持つ妨害物を飛来させる
    /// </summary>
    [Serializable]
    public sealed class ObstacleSpawnAction : IGimmickAction
    {
        [SerializeField, Tooltip("ルートに非キネマティックなRigidbodyを持つ妨害物Prefab")]
        private Rigidbody _prefab;
        [SerializeField, Tooltip("ステージの出現基準からのワールドXYオフセット")]
        private Vector2 _spawnOffset = new(0f, 6f);
        [SerializeField, Tooltip("ワールドXY方向の初速。単位はメートル毎秒")]
        private Vector2 _initialVelocity = new(0f, -8f);
        [SerializeField, Min(1), Tooltip("指定地点が複数ある場合に同時に落とす最大数")]
        private int _maxSpawnCount = 1;
        [SerializeField, Min(1f), Tooltip("落下物の最大速度（メートル毎秒）")]
        private float _maxSpeed = 18f;

        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            if (_prefab == null || _prefab.isKinematic)
            {
                Debug.LogWarning($"[{nameof(ObstacleSpawnAction)}] 非キネマティックなRigidbodyのPrefabを設定してください。");
                return false;
            }

            var points = new List<Transform>();
            if (context.StageTargets != null)
            {
                foreach (Transform point in context.StageTargets.FallPoints)
                {
                    if (point != null)
                    {
                        points.Add(point);
                    }
                }
            }

            if (points.Count == 0)
            {
                Spawn(context, context.SpawnOrigin);
                return true;
            }

            int count = Mathf.Clamp(_maxSpawnCount, 1, points.Count);
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, points.Count);
                Spawn(context, points[index].position);
                points.RemoveAt(index);
            }

            return true;
        }

        /// <summary>
        ///     指定地点から妨害物を飛ばし、効果終了時に回収する
        /// </summary>
        private void Spawn(GimmickContext context, Vector3 position)
        {
            Rigidbody obstacle = UnityEngine.Object.Instantiate(
                _prefab, position + (Vector3)_spawnOffset, _prefab.rotation);
            if (!obstacle.TryGetComponent(out ExplosiveObstacle _) && !obstacle.TryGetComponent(out AttackableObstacle _))
            {
                obstacle.gameObject.AddComponent<AttackableObstacle>();
            }

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
            obstacle.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            obstacle.maxLinearVelocity = Mathf.Min(obstacle.maxLinearVelocity, _maxSpeed);
            obstacle.linearVelocity = _initialVelocity;
        }
    }
}
