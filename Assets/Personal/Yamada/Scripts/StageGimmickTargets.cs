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
        [SerializeField, Tooltip("プレイヤーの重さで傾くシーソーの足場")]
        private Rigidbody _seesaw;
        [SerializeField, Min(0f), Tooltip("シーソーを水平に戻す強さ")]
        private float _seesawRestoringForce = 30f;
        [SerializeField, Min(0f), Tooltip("重さと支点からの距離に対する傾きの大きさ")]
        private float _seesawDegreesPerMoment = 5f;
        [SerializeField, Range(0f, 45f), Tooltip("シーソーの傾きの上限（度）")]
        private float _seesawMaxTilt = 22f;
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
        private PlayerMove _playerOne;
        private PlayerMove _playerTwo;

        /// <summary>
        ///     シーソーとステータス変化ギミックが参照するプレイヤーを登録する
        /// </summary>
        public void SetPlayers(PlayerMove playerOne, PlayerMove playerTwo)
        {
            _playerOne = playerOne;
            _playerTwo = playerTwo;
        }

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

        /// <summary>
        ///     登録されたプレイヤーに効果を適用し、発動ごとの解除処理を登録する
        /// </summary>
        internal bool TryApplyPlayerEffect(GimmickContext context, System.Func<PlayerMove, System.Action> apply)
        {
            System.Action cleanupOne = _playerOne != null ? apply(_playerOne) : null;
            context.RegisterCleanup(cleanupOne);

            System.Action cleanupTwo = _playerTwo != null && _playerTwo != _playerOne ? apply(_playerTwo) : null;
            context.RegisterCleanup(cleanupTwo);

            return cleanupOne != null || cleanupTwo != null;
        }

        private void Awake()
        {
            if (_seesaw != null)
            {
                _seesaw.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }

        private void FixedUpdate()
        {
            StabilizeSeesaw();
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

        /// <summary>
        ///     重さで傾く足場に復元力を加え、回転し続けないようにする
        /// </summary>
        private void StabilizeSeesaw()
        {
            if (_seesaw == null)
            {
                return;
            }

            float tilt = Mathf.DeltaAngle(0f, _seesaw.rotation.eulerAngles.z);
            float limitedTilt = Mathf.Clamp(tilt, -_seesawMaxTilt, _seesawMaxTilt);
            if (!Mathf.Approximately(tilt, limitedTilt))
            {
                _seesaw.rotation = Quaternion.AngleAxis(limitedTilt, Vector3.forward);
                Vector3 angularVelocity = _seesaw.angularVelocity;
                if (Mathf.Sign(angularVelocity.z) == Mathf.Sign(tilt))
                {
                    angularVelocity.z = 0f;
                    _seesaw.angularVelocity = angularVelocity;
                }
            }

            float moment = GetPlayerMoment(_playerOne) + GetPlayerMoment(_playerTwo);
            float targetTilt = Mathf.Clamp(-moment * _seesawDegreesPerMoment, -_seesawMaxTilt, _seesawMaxTilt);
            float angularAcceleration = (targetTilt - limitedTilt) * Mathf.Deg2Rad * _seesawRestoringForce;
            angularAcceleration -= _seesaw.angularVelocity.z * 6f;
            _seesaw.AddTorque(Vector3.forward * angularAcceleration, ForceMode.Acceleration);
        }

        /// <summary>
        ///     シーソーに接地しているプレイヤーが加える荷重を取得する
        /// </summary>
        private float GetPlayerMoment(PlayerMove player)
        {
            return player != null && player.isActiveAndEnabled ? player.GetSupportMoment(_seesaw) : 0f;
        }
    }
}
