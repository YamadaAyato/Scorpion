using System;
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
        [SerializeField, Min(0f), Tooltip("斜めに飛来するとき、出現位置を横にずらす距離")]
        private float _diagonalSpawnOffset;
        [SerializeField, Min(0f), Tooltip("斜めに飛来するときに加える横方向の初速")]
        private float _diagonalSpeed;
        [SerializeField, Min(1), Tooltip("一度に出現させる最大数")]
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

            Vector3 left = context.SpawnOrigin;
            Vector3 right = context.SpawnOrigin;
            bool hasPoint = false;
            if (context.StageTargets != null)
            {
                foreach (Transform point in context.StageTargets.FallPoints)
                {
                    if (point == null)
                    {
                        continue;
                    }

                    Vector3 position = point.position;
                    if (!hasPoint || position.x < left.x)
                    {
                        left = position;
                    }

                    if (!hasPoint || position.x > right.x)
                    {
                        right = position;
                    }

                    hasPoint = true;
                }
            }

            // 両端の設定点を出現範囲に使い、複数個の場合は範囲を分けて重なりを抑える
            int count = left.x < right.x ? Mathf.Max(1, _maxSpawnCount) : 1;
            int firstDirection = _diagonalSpeed > 0f ? UnityEngine.Random.Range(0, 3) : 1;
            Camera camera = Camera.main;
            for (int i = 0; i < count; i++)
            {
                float fraction = (i + UnityEngine.Random.value) / count;
                Vector3 position = Vector3.Lerp(left, right, fraction);
                int direction = _diagonalSpeed > 0f ? (firstDirection + i) % 3 - 1 : 0;
                Spawn(context, position, direction, camera);
            }

            return true;
        }

        /// <summary>
        ///     指定地点から妨害物を飛ばし、効果終了時に回収する
        /// </summary>
        private void Spawn(GimmickContext context, Vector3 position, int direction, Camera camera)
        {
            // 斜めの場合は進行方向の反対側から出し、足場の範囲へ向かわせる
            position.x -= direction * _diagonalSpawnOffset;
            position += (Vector3)_spawnOffset;
            Vector2 velocity = _initialVelocity + Vector2.right * (direction * _diagonalSpeed);
            position = GetOffscreenSpawnPosition(camera, position, velocity);
            Rigidbody obstacle = UnityEngine.Object.Instantiate(
                _prefab, position, _prefab.transform.rotation);
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
            obstacle.linearVelocity = velocity;
        }

        /// <summary>
        ///     カメラ上端より余裕を持って出現させ、元の落下範囲に向かう軌道を保つ
        /// </summary>
        private Vector3 GetOffscreenSpawnPosition(Camera camera, Vector3 position, Vector2 velocity)
        {
            if (camera == null)
            {
                return position;
            }

            var playPlane = new Plane(Vector3.forward, position);
            Ray upperLeft = camera.ViewportPointToRay(new Vector3(0f, 1.12f, 0f));
            Ray upperRight = camera.ViewportPointToRay(new Vector3(1f, 1.12f, 0f));
            if (!playPlane.Raycast(upperLeft, out float leftDepth) || !playPlane.Raycast(upperRight, out float rightDepth))
            {
                return position;
            }

            float top = Mathf.Max(upperLeft.GetPoint(leftDepth).y, upperRight.GetPoint(rightDepth).y);
            float extraHeight = Mathf.Max(0f, top - position.y);
            float downSpeed = Mathf.Max(0.1f, -velocity.y);
            float gravity = _prefab.useGravity ? Mathf.Max(0f, -Physics.gravity.y) : 0f;
            float extraFlightTime = 2f * extraHeight /
                (downSpeed + Mathf.Sqrt(downSpeed * downSpeed + 2f * gravity * extraHeight));
            position.x -= velocity.x * extraFlightTime;
            position.y += extraHeight;
            return position;
        }
    }
}
