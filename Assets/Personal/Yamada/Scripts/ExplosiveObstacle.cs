using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     地面かプレイヤーに触れると爆発し、周囲のプレイヤーを吹き飛ばす
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ExplosiveObstacle : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("爆風が届く半径")]
        private float _radius = 3f;
        [SerializeField, Min(0f), Tooltip("爆心に近いときに与える最大の吹き飛び速度")]
        private float _maxKnockbackSpeed = 10f;
        [SerializeField, Min(0f), Tooltip("爆風に当たったプレイヤーの操作不能時間（秒）")]
        private float _stunSeconds = 0.4f;
        [SerializeField, Tooltip("爆発時に表示するリングの材質")]
        private Material _burstMaterial;

        private Rigidbody _body;
        private bool _hasExploded;

        private const int BURST_SEGMENTS = 32;
        private const float BURST_SECONDS = 0.25f;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            PlayerDeath.RegisterObstacle(_body);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasExploded || (!collision.collider.CompareTag("Ground") &&
                (collision.rigidbody == null || !collision.rigidbody.TryGetComponent(out PlayerMove _))))
            {
                return;
            }

            _hasExploded = true;
            Vector3 center = _body.worldCenterOfMass;
            ApplyBlast(center);
            _body.detectCollisions = false;
            _body.constraints = RigidbodyConstraints.FreezeAll;
            foreach (Renderer item in GetComponentsInChildren<Renderer>())
            {
                item.enabled = false;
            }

            if (_burstMaterial != null)
            {
                StartCoroutine(ShowBurst(center));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        ///     爆心からの距離に応じてプレイヤーを横と上に吹き飛ばす
        /// </summary>
        private void ApplyBlast(Vector3 center)
        {
            var affected = new HashSet<Rigidbody>();
            foreach (Collider candidate in Physics.OverlapSphere(center, _radius, ~0, QueryTriggerInteraction.Ignore))
            {
                Rigidbody target = candidate.attachedRigidbody;
                if (target == null || !target.TryGetComponent(out PlayerMove player) ||
                    (target.TryGetComponent(out PlayerDeath playerDeath) && playerDeath.IsRespawnProtected) ||
                    !affected.Add(target))
                {
                    continue;
                }

                Vector3 offset = target.worldCenterOfMass - center;
                float distance = Vector3.Distance(center, candidate.ClosestPoint(center));
                float speed = _maxKnockbackSpeed * Mathf.Lerp(1f, 0.3f, distance / _radius);
                float side = Mathf.Abs(offset.x) > 0.05f ? Mathf.Sign(offset.x) : 0f;
                Vector3 velocity = target.linearVelocity;
                if (side != 0f)
                {
                    velocity.x = side * Mathf.Max(velocity.x * side, speed);
                }

                velocity.y = Mathf.Max(velocity.y, speed * 0.55f);
                player.ApplyStun(_stunSeconds);
                target.linearVelocity = velocity;
            }
        }

        /// <summary>
        ///     爆風の範囲を一瞬表示して、落下物を消す
        /// </summary>
        private IEnumerator ShowBurst(Vector3 center)
        {
            var effect = new GameObject("Explosion Ring");
            effect.transform.SetParent(transform, false);
            var ring = effect.AddComponent<LineRenderer>();
            ring.sharedMaterial = _burstMaterial;
            ring.loop = true;
            ring.widthMultiplier = 0.12f;
            ring.positionCount = BURST_SEGMENTS;
            float elapsed = 0f;
            while (elapsed < BURST_SECONDS)
            {
                float progress = elapsed / BURST_SECONDS;
                float radius = Mathf.Lerp(_radius * 0.18f, _radius, progress);
                ring.widthMultiplier = Mathf.Lerp(0.16f, 0.025f, progress);
                for (int i = 0; i < BURST_SEGMENTS; i++)
                {
                    float angle = (2f * Mathf.PI * i) / BURST_SEGMENTS;
                    ring.SetPosition(i, center + radius * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), -0.04f));
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            Destroy(gameObject);
        }
    }
}
