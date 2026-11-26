using System.Collections.Generic;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     現在のステージにあるギミック対象と歯車の回転状態を保持する
    /// </summary>
    public sealed class StageGimmickTargets : MonoBehaviour
    {
        /// <summary> 落下物を生成できる地点を取得する </summary>
        public IReadOnlyList<Transform> FallPoints => _fallPoints;
        /// <summary> 横移動させるシーソーを取得する </summary>
        public Transform MovingPlatform => _movingPlatform;
        /// <summary> 崩落の対象にできる橋の足場を取得する </summary>
        public IReadOnlyList<AuthorScaffold> BridgeSupports => _bridgeSupports;

        [SerializeField, Tooltip("落下物を生成する地点")]
        private Transform[] _fallPoints = System.Array.Empty<Transform>();
        [SerializeField, Tooltip("横移動させるシーソーのルート")]
        private Transform _movingPlatform;
        [SerializeField, Tooltip("ギミック中に有効にする風のオブジェクト")]
        private GameObject _windObject;
        [SerializeField, Tooltip("回転させるキネマティックな歯車")]
        private Rigidbody _gear;
        [SerializeField, Tooltip("歯車の基準回転速度（度/秒）")]
        private float _gearDegreesPerSecond = 72f;
        [SerializeField, Tooltip("崩落に使う橋の足場。安全な足場は除く")]
        private AuthorScaffold[] _bridgeSupports = System.Array.Empty<AuthorScaffold>();

        private readonly List<float> _gearSpeedMultipliers = new();
        private int _reverseCount;
        private int _windCount;
        private bool _isRunning;

        /// <summary>
        ///     ステージのプレイ中だけ基本の歯車回転を進める
        /// </summary>
        internal void SetRunning(bool isRunning)
        {
            _isRunning = isRunning;
        }

        /// <summary>
        ///     風を有効にし、最後の発動が終了したときに停止する
        /// </summary>
        public bool TryEnableWind(GimmickContext context)
        {
            if (_windObject == null)
            {
                return false;
            }

            _windCount++;
            _windObject.SetActive(true);
            context.RegisterCleanup(() =>
            {
                if (--_windCount == 0 && _windObject != null)
                {
                    _windObject.SetActive(false);
                }
            });
            return true;
        }

        /// <summary>
        ///     歯車の速度と向きに、この発動中だけ補正を加える
        /// </summary>
        public bool TryModifyGear(GimmickContext context, float multiplier, bool reverse)
        {
            if (!_isRunning || _gear == null)
            {
                return false;
            }

            _gearSpeedMultipliers.Add(multiplier);
            if (reverse)
            {
                _reverseCount++;
            }

            context.RegisterCleanup(() =>
            {
                _gearSpeedMultipliers.Remove(multiplier);
                if (reverse)
                {
                    _reverseCount--;
                }
            });
            return true;
        }

        private void FixedUpdate()
        {
            if (!_isRunning || _gear == null)
            {
                return;
            }

            float speed = 1f;
            foreach (float multiplier in _gearSpeedMultipliers)
            {
                speed = Mathf.Max(speed, multiplier);
            }

            float angle = _gearDegreesPerSecond * speed * Time.fixedDeltaTime;
            if (_reverseCount > 0)
            {
                angle = -angle;
            }

            _gear.MoveRotation(Quaternion.AngleAxis(angle, Vector3.forward) * _gear.rotation);
        }
    }
}
