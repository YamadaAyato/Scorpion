using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Scorpion.Effects
{
    /// <summary>
    ///     強化ギミック（移動速度・ジャンプ力・吹っ飛ばし力アップ）が効いている間、
    ///     プレイヤーに種類ごとの色と動きのオーラをまとわせる
    /// </summary>
    /// <remarks>
    ///     PlayerMove / PlayerAttack に掛かっている倍率を毎フレーム読むだけで、ギミック側・プレイヤー側のコードは変更しない。
    ///     粒はプレイヤーの拡大率（4倍）の影響を受けないよう、シーン直下に作る
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMove))]
    public sealed class BuffAuraEffect : MonoBehaviour
    {
        [Header("素材")]
        [SerializeField, Tooltip("粒に使うマテリアル（Tex_BuffAura の 2×2 アトラス: 丸/山形/輪っか/キラッ）")]
        private Material _auraMaterial;
        [SerializeField, Tooltip("移動速度アップの残像ラインに使うマテリアル（未設定なら粒のマテリアル）")]
        private Material _trailMaterial;

        [Header("共通")]
        [SerializeField, Min(0.1f), Tooltip("すべての粒の大きさの倍率")]
        private float _sizeScale = 1f;
        [SerializeField, Range(0.3f, 1f), Tooltip("2つ以上同時にかかっているとき、1つ増えるごとに粒の量に掛ける倍率（ごちゃつき防止）")]
        private float _stackedRateScale = 0.75f;
        [SerializeField, Min(0f), Tooltip("オーラがふわっと出る／消えるまでの秒数")]
        private float _fadeSeconds = 0.25f;
        [SerializeField, Tooltip("強化がかかった瞬間に、輪っかとキラッを弾けさせる")]
        private bool _playStartBurst = true;
        [SerializeField, Min(0.1f), Tooltip("かかった瞬間の輪っかの大きさ（体の高さに対する倍率）")]
        private float _startBurstRingSize = 2.1f;
        [SerializeField, Tooltip("強化が切れた瞬間に、小さな粒をぽろっと散らす")]
        private bool _playEndPuff = true;
        [SerializeField, Min(1f), Tooltip("この倍率を超えたら「強化中」とみなす（1未満の弱体化には出さない）")]
        private float _minMultiplierToShow = 1.01f;

        [Header("移動速度アップ（黄・後ろへ流れる線）")]
        [SerializeField, Tooltip("線のメインの色")]
        private Color _speedColor = new(1f, 0.8f, 0.12f, 1f);
        [SerializeField, Tooltip("線のサブの色（ときどき混ざる明るい色）")]
        private Color _speedAccentColor = new(1f, 0.95f, 0.55f, 1f);
        [SerializeField, Min(0f), Tooltip("走っているときに出す線の数（毎秒）")]
        private float _speedLinesPerSecond = 16f;
        [SerializeField, Min(0f), Tooltip("止まっているときに出す線の数（毎秒）")]
        private float _speedIdleLinesPerSecond = 4f;
        [SerializeField, Tooltip("線が後ろへ流れる速さ（最小・最大）")]
        private Vector2 _speedLineSpeed = new(3.5f, 5.5f);
        [SerializeField, Tooltip("線の太さ（最小・最大）")]
        private Vector2 _speedLineThickness = new(0.08f, 0.13f);
        [SerializeField, Min(1f), Tooltip("線の長さ（太さに対する倍率）")]
        private float _speedLineStretch = 4.5f;
        [SerializeField, Tooltip("線が消えるまでの秒数（最小・最大）")]
        private Vector2 _speedLineLifetime = new(0.18f, 0.28f);
        [SerializeField, Tooltip("走っている間、体から後ろに残像ラインを引く")]
        private bool _useSpeedTrails = true;
        [SerializeField, Min(0f), Tooltip("残像ラインの太さ")]
        private float _speedTrailWidth = 0.1f;
        [SerializeField, Min(0.01f), Tooltip("残像ラインの長さ（秒）")]
        private float _speedTrailSeconds = 0.16f;
        [SerializeField, Tooltip("残像ライン2本の高さ（体の下端0〜上端1）")]
        private Vector2 _speedTrailHeights = new(0.3f, 0.68f);
        [SerializeField, Min(0f), Tooltip("この横速度より速いときだけ残像ラインを引く")]
        private float _speedTrailMinVelocity = 1.2f;

        [Header("ジャンプ力アップ（緑・上に昇る山形）")]
        [SerializeField, Tooltip("山形と粒のメインの色")]
        private Color _jumpColor = new(0.25f, 0.88f, 0.42f, 1f);
        [SerializeField, Tooltip("サブの色（ときどき混ざる明るい色）")]
        private Color _jumpAccentColor = new(0.72f, 1f, 0.45f, 1f);
        [SerializeField, Min(0f), Tooltip("足元から昇る山形の数（毎秒）")]
        private float _jumpArrowsPerSecond = 3.5f;
        [SerializeField, Min(0f), Tooltip("足元から昇る小さな粒の数（毎秒）")]
        private float _jumpDotsPerSecond = 7f;
        [SerializeField, Tooltip("昇る速さ（最小・最大）")]
        private Vector2 _jumpRiseSpeed = new(1.4f, 2.2f);
        [SerializeField, Tooltip("山形の大きさ（最小・最大）")]
        private Vector2 _jumpArrowSize = new(0.2f, 0.28f);
        [SerializeField, Tooltip("粒の大きさ（最小・最大）")]
        private Vector2 _jumpDotSize = new(0.07f, 0.12f);
        [SerializeField, Tooltip("消えるまでの秒数（最小・最大）")]
        private Vector2 _jumpLifetime = new(0.55f, 0.8f);
        [SerializeField, Min(0), Tooltip("強化中にジャンプした瞬間、足元から追加で出す山形の数")]
        private int _jumpTakeoffArrows = 3;
        [SerializeField, Min(0), Tooltip("強化中にジャンプした瞬間、足元から追加で出す粒の数")]
        private int _jumpTakeoffDots = 8;

        [Header("吹っ飛ばし力アップ（赤橙・脈打つオーラ）")]
        [SerializeField, Tooltip("オーラのメインの色")]
        private Color _knockbackColor = new(1f, 0.38f, 0.1f, 1f);
        [SerializeField, Tooltip("サブの色（火花や2重の輪っか）")]
        private Color _knockbackAccentColor = new(1f, 0.76f, 0.2f, 1f);
        [SerializeField, Min(0.05f), Tooltip("輪っかがドクンと広がる間隔（秒）")]
        private float _pulseIntervalSeconds = 0.55f;
        [SerializeField, Tooltip("輪っかの大きさ（出始め・消えるとき、体の高さに対する倍率）")]
        private Vector2 _pulseSize = new(1f, 1.45f);
        [SerializeField, Min(0.05f), Tooltip("輪っかが消えるまでの秒数")]
        private float _pulseLifetimeSeconds = 0.45f;
        [SerializeField, Range(0f, 1f), Tooltip("輪っかの濃さ")]
        private float _pulseAlpha = 0.7f;
        [SerializeField, Min(0f), Tooltip("体のまわりから弾ける火花の数（毎秒）")]
        private float _knockbackSparksPerSecond = 6f;
        [SerializeField, Tooltip("火花の大きさ（最小・最大）")]
        private Vector2 _knockbackSparkSize = new(0.12f, 0.2f);
        [SerializeField, Tooltip("強化中にスピン攻撃したとき、大きな衝撃の輪を出す")]
        private bool _attackShockwave = true;
        [SerializeField, Min(0.1f), Tooltip("攻撃時の衝撃の輪の大きさ（体の高さに対する倍率）")]
        private float _attackShockwaveSize = 2.3f;

        [Header("テスト")]
        [SerializeField, Tooltip("エディタと開発ビルドで、キーを押すとエフェクトだけを切り替える（強化そのものはかからない）")]
        private bool _enablePreviewKeys = true;
#if ENABLE_INPUT_SYSTEM
        [SerializeField, Tooltip("移動速度アップのテスト表示を切り替えるキー")]
        private Key _previewSpeedKey = Key.F8;
        [SerializeField, Tooltip("ジャンプ力アップのテスト表示を切り替えるキー")]
        private Key _previewJumpKey = Key.F9;
        [SerializeField, Tooltip("吹っ飛ばし力アップのテスト表示を切り替えるキー")]
        private Key _previewKnockbackKey = Key.F10;
#endif

        private PlayerMove _movement;
        private PlayerAttack _attack;
        private Rigidbody _rigidbody;
        private Collider _bodyCollider;
        private LinkedList<float> _moveSpeedMultipliers;
        private LinkedList<float> _jumpPowerMultipliers;
        private ParticleSystem _lines;
        private ParticleSystem _arrows;
        private ParticleSystem _dots;
        private ParticleSystem _rings;
        private ParticleSystem _sparkles;
        private TrailRenderer[] _trails;
        private Material _runtimeMaterial;
        private readonly float[] _weights = new float[BUFF_COUNT];
        private readonly bool[] _wasActive = new bool[BUFF_COUNT];
        private readonly bool[] _preview = new bool[BUFF_COUNT];
        private float _lineBudget;
        private float _arrowBudget;
        private float _dotBudget;
        private float _sparkBudget;
        private float _pulseTimer;
        private float _facing = 1f;
        private Vector3 _lastCenter;

        private static readonly FieldInfo MOVE_SPEED_FIELD =
            typeof(PlayerMove).GetField("_moveSpeedMultipliers", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo JUMP_POWER_FIELD =
            typeof(PlayerMove).GetField("_jumpPowerMultipliers", BindingFlags.Instance | BindingFlags.NonPublic);

        private const int BUFF_COUNT = 3;
        private const int SPEED = 0;
        private const int JUMP = 1;
        private const int KNOCKBACK = 2;

        // Tex_BuffAura のコマ（左上から右へ、上の段から順）
        private const int ATLAS_TILES = 2;
        private const int FRAME_DOT = 0;
        private const int FRAME_ARROW = 1;
        private const int FRAME_RING = 2;
        private const int FRAME_SPARKLE = 3;

        private const float ACCENT_CHANCE = 0.3f;
        private const float TELEPORT_DISTANCE = 2f;
        private const string FALLBACK_SHADER_NAME = "Universal Render Pipeline/Particles/Unlit";

        /// <summary> 移動速度アップのオーラを出しているか（フェード中も含む） </summary>
        public bool IsSpeedVisible => _weights[SPEED] > 0.001f;
        /// <summary> ジャンプ力アップのオーラを出しているか（フェード中も含む） </summary>
        public bool IsJumpVisible => _weights[JUMP] > 0.001f;
        /// <summary> 吹っ飛ばし力アップのオーラを出しているか（フェード中も含む） </summary>
        public bool IsKnockbackVisible => _weights[KNOCKBACK] > 0.001f;

        [ContextMenu("テスト: 移動速度アップ 切替")]
        private void TogglePreviewSpeed() => TogglePreview(SPEED);

        [ContextMenu("テスト: ジャンプ力アップ 切替")]
        private void TogglePreviewJump() => TogglePreview(JUMP);

        [ContextMenu("テスト: 吹っ飛ばし力アップ 切替")]
        private void TogglePreviewKnockback() => TogglePreview(KNOCKBACK);

        [ContextMenu("テスト: 3つ全部オン")]
        private void PreviewAll()
        {
            for (int i = 0; i < BUFF_COUNT; i++)
            {
                _preview[i] = true;
            }
        }

        [ContextMenu("テスト: 全部オフ")]
        private void PreviewNone()
        {
            for (int i = 0; i < BUFF_COUNT; i++)
            {
                _preview[i] = false;
            }
        }

        private void TogglePreview(int buff)
        {
            if (!Application.isPlaying)
            {
                Debug.Log($"{nameof(BuffAuraEffect)}: テスト表示はプレイ中に使えます", this);
                return;
            }

            _preview[buff] = !_preview[buff];
        }

        private void Awake()
        {
            _movement = GetComponent<PlayerMove>();
            TryGetComponent(out _attack);
            TryGetComponent(out _rigidbody);
            foreach (Collider candidate in GetComponents<Collider>())
            {
                if (!candidate.isTrigger)
                {
                    _bodyCollider = candidate;
                    break;
                }
            }

            _moveSpeedMultipliers = MOVE_SPEED_FIELD?.GetValue(_movement) as LinkedList<float>;
            _jumpPowerMultipliers = JUMP_POWER_FIELD?.GetValue(_movement) as LinkedList<float>;
            if (_moveSpeedMultipliers == null || _jumpPowerMultipliers == null)
            {
                Debug.LogWarning($"{nameof(BuffAuraEffect)}: PlayerMove の倍率リストが見つからないため、移動速度/ジャンプ力アップはテストキーでのみ表示します", this);
            }

            Material material = ResolveMaterial();
            _lines = CreateParticles("Buff Speed Lines", 96, FRAME_DOT, material, ParticleSystemSimulationSpace.World);
            _arrows = CreateParticles("Buff Jump Arrows", 32, FRAME_ARROW, material, ParticleSystemSimulationSpace.World);
            _dots = CreateParticles("Buff Dots", 96, FRAME_DOT, material, ParticleSystemSimulationSpace.World);
            // 輪っかは体についていくよう、毎フレーム体の中心へ動かすローカル空間で出す
            _rings = CreateParticles("Buff Rings", 16, FRAME_RING, material, ParticleSystemSimulationSpace.Local);
            _sparkles = CreateParticles("Buff Sparkles", 64, FRAME_SPARKLE, material, ParticleSystemSimulationSpace.World);
            SetupLines();
            SetupArrows();
            SetupDots();
            SetupRings();
            SetupSparkles();

            if (_useSpeedTrails)
            {
                Material trailMaterial = _trailMaterial != null ? _trailMaterial : material;
                _trails = new[] { CreateTrail("Buff Speed Trail A", trailMaterial), CreateTrail("Buff Speed Trail B", trailMaterial) };
            }
        }

        private void OnEnable()
        {
            _movement.OnJumpStarted += HandleJumpStarted;
            if (_attack != null)
            {
                _attack.OnAttackStarted += HandleAttackStarted;
            }

            for (int i = 0; i < BUFF_COUNT; i++)
            {
                _weights[i] = 0f;
                _wasActive[i] = false;
            }

            _pulseTimer = 0f;
            GetBody(out _lastCenter, out _);
            ClearTrails();
        }

        private void OnDisable()
        {
            _movement.OnJumpStarted -= HandleJumpStarted;
            if (_attack != null)
            {
                _attack.OnAttackStarted -= HandleAttackStarted;
            }

            // 撃破で非表示になったとき、その場にオーラを残さない
            ClearParticles(_lines);
            ClearParticles(_arrows);
            ClearParticles(_dots);
            ClearParticles(_rings);
            ClearParticles(_sparkles);
            ClearTrails();
        }

        private void OnDestroy()
        {
            DestroyIfExists(_lines != null ? _lines.gameObject : null);
            DestroyIfExists(_arrows != null ? _arrows.gameObject : null);
            DestroyIfExists(_dots != null ? _dots.gameObject : null);
            DestroyIfExists(_rings != null ? _rings.gameObject : null);
            DestroyIfExists(_sparkles != null ? _sparkles.gameObject : null);
            if (_trails != null)
            {
                foreach (TrailRenderer trail in _trails)
                {
                    DestroyIfExists(trail != null ? trail.gameObject : null);
                }
            }

            DestroyIfExists(_runtimeMaterial);
        }

        private void LateUpdate()
        {
            UpdatePreviewKeys();
            GetBody(out Vector3 center, out Vector3 size);
            if (_rings != null)
            {
                _rings.transform.position = center;
            }

            // 復活などで瞬間移動したときは、残像ラインがつながらないよう消す
            if ((center - _lastCenter).sqrMagnitude > TELEPORT_DISTANCE * TELEPORT_DISTANCE)
            {
                ClearTrails();
            }

            _lastCenter = center;

            float velocityX = _rigidbody != null ? _rigidbody.linearVelocity.x : 0f;
            if (Mathf.Abs(velocityX) > 0.3f)
            {
                _facing = Mathf.Sign(velocityX);
            }

            int activeCount = 0;
            float fadeStep = _fadeSeconds > 0f ? Time.deltaTime / _fadeSeconds : 1f;
            for (int i = 0; i < BUFF_COUNT; i++)
            {
                bool active = IsBuffActive(i);
                if (active)
                {
                    activeCount++;
                }

                if (active && !_wasActive[i])
                {
                    PlayStartBurst(i, center, size);
                }
                else if (!active && _wasActive[i])
                {
                    PlayEndPuff(i, center, size);
                }

                _wasActive[i] = active;
                _weights[i] = Mathf.MoveTowards(_weights[i], active ? 1f : 0f, fadeStep);
            }

            float rateScale = Mathf.Pow(_stackedRateScale, Mathf.Max(0, activeCount - 1));
            float deltaTime = Time.deltaTime;
            UpdateSpeed(center, size, velocityX, rateScale, deltaTime);
            UpdateJump(center, size, rateScale, deltaTime);
            UpdateKnockback(center, size, rateScale, deltaTime);
        }

        /// <summary>
        ///     強化がかかっているか（テスト表示を含む）
        /// </summary>
        private bool IsBuffActive(int buff)
        {
            if (_preview[buff])
            {
                return true;
            }

            float multiplier = buff switch
            {
                SPEED => _moveSpeedMultipliers?.Last?.Value ?? 1f,
                JUMP => _jumpPowerMultipliers?.Last?.Value ?? 1f,
                _ => _attack != null ? _attack.KnockbackMultiplier : 1f
            };
            return multiplier >= _minMultiplierToShow;
        }

        // ───────── 移動速度アップ ─────────

        private void UpdateSpeed(Vector3 center, Vector3 size, float velocityX, float rateScale, float deltaTime)
        {
            float weight = _weights[SPEED];
            float run = Mathf.InverseLerp(0.3f, 4f, Mathf.Abs(velocityX));
            UpdateTrails(center, size, weight, Mathf.Abs(velocityX) >= _speedTrailMinVelocity);
            if (weight <= 0f)
            {
                _lineBudget = 0f;
                return;
            }

            _lineBudget += Mathf.Lerp(_speedIdleLinesPerSecond, _speedLinesPerSecond, run) * weight * rateScale * deltaTime;
            while (_lineBudget >= 1f)
            {
                _lineBudget -= 1f;
                EmitSpeedLine(center, size, Mathf.Lerp(0.6f, 1f, run), -_facing);
            }
        }

        /// <summary>
        ///     体の前〜中ほどから、後ろへびゅっと流れる線を1本出す
        /// </summary>
        private void EmitSpeedLine(Vector3 center, Vector3 size, float speedScale, float backDirection)
        {
            Vector3 position = center + new Vector3(
                -backDirection * Random.Range(-0.2f, 0.5f) * size.x,
                Random.Range(-0.42f, 0.45f) * size.y,
                Random.Range(-0.35f, 0.4f) * size.z);
            float speed = Random.Range(_speedLineSpeed.x, _speedLineSpeed.y) * speedScale;
            Vector3 velocity = new(backDirection * speed, Random.Range(-0.15f, 0.15f), 0f);
            Emit(_lines, position, velocity, Random.Range(_speedLineThickness.x, _speedLineThickness.y) * _sizeScale,
                Random.Range(_speedLineLifetime.x, _speedLineLifetime.y), PickColor(_speedColor, _speedAccentColor));
        }

        private void UpdateTrails(Vector3 center, Vector3 size, float weight, bool isRunning)
        {
            if (_trails == null)
            {
                return;
            }

            float bottom = center.y - size.y * 0.5f;
            for (int i = 0; i < _trails.Length; i++)
            {
                TrailRenderer trail = _trails[i];
                if (trail == null)
                {
                    continue;
                }

                float height = i == 0 ? _speedTrailHeights.x : _speedTrailHeights.y;
                // 体より少し奥に置き、体の後ろから伸びて見えるようにする
                trail.transform.position = new Vector3(center.x, bottom + size.y * height, center.z + size.z * 0.3f);
                trail.emitting = weight > 0.05f && isRunning;
                trail.widthMultiplier = _speedTrailWidth * _sizeScale * Mathf.Max(weight, 0.01f);
                trail.time = _speedTrailSeconds;
            }
        }

        // ───────── ジャンプ力アップ ─────────

        private void UpdateJump(Vector3 center, Vector3 size, float rateScale, float deltaTime)
        {
            float weight = _weights[JUMP];
            if (weight <= 0f)
            {
                _arrowBudget = 0f;
                _dotBudget = 0f;
                return;
            }

            _arrowBudget += _jumpArrowsPerSecond * weight * rateScale * deltaTime;
            _dotBudget += _jumpDotsPerSecond * weight * rateScale * deltaTime;
            while (_arrowBudget >= 1f)
            {
                _arrowBudget -= 1f;
                EmitJumpArrow(center, size, 1f);
            }

            while (_dotBudget >= 1f)
            {
                _dotBudget -= 1f;
                EmitJumpDot(center, size, 1f);
            }
        }

        /// <summary>
        ///     足元〜腰あたりから上へ昇る山形を1つ出す
        /// </summary>
        private void EmitJumpArrow(Vector3 center, Vector3 size, float speedScale)
        {
            Vector3 position = new(
                center.x + Random.Range(-0.45f, 0.45f) * size.x,
                center.y + Random.Range(-0.5f, 0.05f) * size.y,
                center.z + Random.Range(-0.2f, 0.4f) * size.z);
            Vector3 velocity = Vector3.up * (Random.Range(_jumpRiseSpeed.x, _jumpRiseSpeed.y) * speedScale);
            Emit(_arrows, position, velocity, Random.Range(_jumpArrowSize.x, _jumpArrowSize.y) * _sizeScale,
                Random.Range(_jumpLifetime.x, _jumpLifetime.y), PickColor(_jumpColor, _jumpAccentColor));
        }

        private void EmitJumpDot(Vector3 center, Vector3 size, float speedScale)
        {
            Vector3 position = new(
                center.x + Random.Range(-0.55f, 0.55f) * size.x,
                center.y + Random.Range(-0.5f, 0.2f) * size.y,
                center.z + Random.Range(-0.4f, 0.4f) * size.z);
            Vector3 velocity = new(Random.Range(-0.15f, 0.15f),
                Random.Range(_jumpRiseSpeed.x, _jumpRiseSpeed.y) * Random.Range(0.6f, 1f) * speedScale, 0f);
            Emit(_dots, position, velocity, Random.Range(_jumpDotSize.x, _jumpDotSize.y) * _sizeScale,
                Random.Range(_jumpLifetime.x, _jumpLifetime.y), PickColor(_jumpColor, _jumpAccentColor));
        }

        /// <summary>
        ///     強化中に踏み切ったら、足元から勢いよく山形と粒を噴き上げる
        /// </summary>
        private void HandleJumpStarted()
        {
            if (_weights[JUMP] < 0.5f)
            {
                return;
            }

            GetBody(out Vector3 center, out Vector3 size);
            Vector3 feet = new(center.x, center.y - size.y * 0.5f, center.z);
            for (int i = 0; i < _jumpTakeoffArrows; i++)
            {
                EmitJumpArrow(feet + Vector3.up * (size.y * 0.35f), size, 1.8f);
            }

            for (int i = 0; i < _jumpTakeoffDots; i++)
            {
                float side = Random.Range(-1f, 1f);
                Vector3 velocity = new(side * 2.2f, Random.Range(1.2f, 2.4f), 0f);
                Emit(_dots, feet + new Vector3(side * size.x * 0.3f, 0.05f, 0f), velocity,
                    Random.Range(_jumpDotSize.x, _jumpDotSize.y) * _sizeScale * 1.2f,
                    Random.Range(_jumpLifetime.x, _jumpLifetime.y) * 0.7f, PickColor(_jumpColor, _jumpAccentColor));
            }
        }

        // ───────── 吹っ飛ばし力アップ ─────────

        private void UpdateKnockback(Vector3 center, Vector3 size, float rateScale, float deltaTime)
        {
            float weight = _weights[KNOCKBACK];
            if (weight <= 0f)
            {
                _sparkBudget = 0f;
                _pulseTimer = 0f;
                return;
            }

            _pulseTimer -= deltaTime;
            if (_pulseTimer <= 0f)
            {
                _pulseTimer += _pulseIntervalSeconds;
                Color color = _knockbackColor;
                color.a *= _pulseAlpha * weight;
                EmitRing(_pulseSize.y * size.y, _pulseLifetimeSeconds, color);
            }

            _sparkBudget += _knockbackSparksPerSecond * weight * rateScale * deltaTime;
            while (_sparkBudget >= 1f)
            {
                _sparkBudget -= 1f;
                EmitKnockbackSpark(center, size, 1f, _knockbackSparkSize);
            }
        }

        /// <summary>
        ///     体のふちから外へ弾ける火花を1つ出す
        /// </summary>
        private void EmitKnockbackSpark(Vector3 center, Vector3 size, float speedScale, Vector2 sizeRange)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            Vector3 position = center + new Vector3(direction.x * size.x * 0.55f, direction.y * size.y * 0.55f, -0.05f);
            Vector3 velocity = direction * (Random.Range(1.2f, 2.2f) * speedScale) + Vector3.up * 0.4f;
            Emit(_sparkles, position, velocity, Random.Range(sizeRange.x, sizeRange.y) * _sizeScale,
                Random.Range(0.3f, 0.45f), PickColor(_knockbackColor, _knockbackAccentColor), Random.Range(-20f, 20f));
        }

        /// <summary>
        ///     強化中にスピン攻撃したら、大きな衝撃の輪と火花を出す
        /// </summary>
        private void HandleAttackStarted()
        {
            if (!_attackShockwave || _weights[KNOCKBACK] < 0.5f)
            {
                return;
            }

            GetBody(out Vector3 center, out Vector3 size);
            if (_rings != null)
            {
                _rings.transform.position = center;
            }

            Color main = _knockbackColor;
            main.a *= 0.85f;
            EmitRing(_attackShockwaveSize * size.y, 0.38f, main);
            Color accent = _knockbackAccentColor;
            accent.a *= 0.8f;
            EmitRing(_attackShockwaveSize * 0.72f * size.y, 0.3f, accent);
            for (int i = 0; i < 10; i++)
            {
                EmitKnockbackSpark(center, size, 2.6f, _knockbackSparkSize * 1.2f);
            }
        }

        // ───────── かかった瞬間・切れた瞬間 ─────────

        private void PlayStartBurst(int buff, Vector3 center, Vector3 size)
        {
            if (!_playStartBurst)
            {
                return;
            }

            GetColors(buff, out Color main, out Color accent);
            Color ringColor = main;
            ringColor.a *= 0.9f;
            EmitRing(_startBurstRingSize * size.y, 0.4f, ringColor);
            for (int i = 0; i < 8; i++)
            {
                float angle = (i + Random.Range(-0.3f, 0.3f)) / 8f * Mathf.PI * 2f;
                Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Emit(_sparkles, center + direction * (size.y * 0.3f) + Vector3.back * 0.1f, direction * Random.Range(3f, 4.5f),
                    Random.Range(0.16f, 0.24f) * _sizeScale, Random.Range(0.35f, 0.5f), PickColor(main, accent),
                    Random.Range(-20f, 20f));
            }

            // 種類ごとの動きを、かかった瞬間に一度はっきり見せる
            switch (buff)
            {
                case SPEED:
                    for (int i = 0; i < 6; i++)
                    {
                        EmitSpeedLine(center, size, 1.2f, i % 2 == 0 ? -1f : 1f);
                    }

                    break;
                case JUMP:
                    for (int i = 0; i < 4; i++)
                    {
                        EmitJumpArrow(center, size, 1.6f);
                    }

                    break;
                default:
                    Color second = accent;
                    second.a *= 0.8f;
                    EmitRing(_startBurstRingSize * 0.7f * size.y, 0.32f, second);
                    break;
            }
        }

        private void PlayEndPuff(int buff, Vector3 center, Vector3 size)
        {
            if (!_playEndPuff)
            {
                return;
            }

            GetColors(buff, out Color main, out Color accent);
            for (int i = 0; i < 6; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Emit(_dots, center + direction * (size.y * 0.35f), direction * Random.Range(0.8f, 1.5f) + Vector3.up * 0.5f,
                    Random.Range(0.08f, 0.13f) * _sizeScale, Random.Range(0.3f, 0.45f), PickColor(main, accent));
            }
        }

        private void GetColors(int buff, out Color main, out Color accent)
        {
            switch (buff)
            {
                case SPEED:
                    main = _speedColor;
                    accent = _speedAccentColor;
                    break;
                case JUMP:
                    main = _jumpColor;
                    accent = _jumpAccentColor;
                    break;
                default:
                    main = _knockbackColor;
                    accent = _knockbackAccentColor;
                    break;
            }
        }

        /// <summary>
        ///     体の中心から広がって消える輪っかを1つ出す（大きさは消えるときの直径）
        /// </summary>
        private void EmitRing(float diameter, float lifetime, Color color)
        {
            // 輪っかはローカル空間。少し奥に置き、体に重なる部分は体で隠れるようにする
            Emit(_rings, new Vector3(0f, 0f, 0.05f), Vector3.zero, diameter * _sizeScale, lifetime, color);
        }

        // ───────── 共通処理 ─────────

        private void UpdatePreviewKeys()
        {
#if ENABLE_INPUT_SYSTEM
            if (!_enablePreviewKeys || !(Application.isEditor || Debug.isDebugBuild))
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasPressed(keyboard, _previewSpeedKey))
            {
                _preview[SPEED] = !_preview[SPEED];
            }

            if (WasPressed(keyboard, _previewJumpKey))
            {
                _preview[JUMP] = !_preview[JUMP];
            }

            if (WasPressed(keyboard, _previewKnockbackKey))
            {
                _preview[KNOCKBACK] = !_preview[KNOCKBACK];
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool WasPressed(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }
#endif

        /// <summary>
        ///     本体コライダーの中心と大きさを取得する
        /// </summary>
        private void GetBody(out Vector3 center, out Vector3 size)
        {
            if (_bodyCollider != null && _bodyCollider.enabled)
            {
                Bounds bounds = _bodyCollider.bounds;
                center = bounds.center;
                size = bounds.size;
            }
            else
            {
                center = transform.position + Vector3.up * 0.5f;
                size = Vector3.one;
            }

            size = new Vector3(Mathf.Max(size.x, 0.2f), Mathf.Max(size.y, 0.2f), Mathf.Max(size.z, 0.2f));
        }

        private static Color PickColor(Color main, Color accent)
        {
            return Random.value < ACCENT_CHANCE ? accent : main;
        }

        private static void Emit(ParticleSystem particles, Vector3 position, Vector3 velocity, float size, float lifetime,
            Color color, float rotation = 0f)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.EmitParams emitParams = new()
            {
                position = position,
                velocity = velocity,
                startSize = size,
                startLifetime = lifetime,
                startColor = color,
                rotation = rotation,
                applyShapeToPosition = false
            };
            particles.Emit(emitParams, 1);
        }

        /// <summary>
        ///     後ろへ細長く伸びた線。出た直後にくっきり、最後に細く薄くなる
        /// </summary>
        private void SetupLines()
        {
            ParticleSystemRenderer particleRenderer = _lines.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.lengthScale = Mathf.Max(_speedLineStretch, 1f);
            particleRenderer.velocityScale = 0f;
            particleRenderer.cameraVelocityScale = 0f;

            SetSizeCurve(_lines, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.35f)));
            SetAlphaCurve(_lines, 0.08f, 0.6f);
        }

        /// <summary>
        ///     ぽんっと出て昇り、少しゆっくりになって小さく消える山形
        /// </summary>
        private void SetupArrows()
        {
            ParticleSystem.MainModule main = _arrows.main;
            main.startRotation = 0f;
            ApplyDrag(_arrows, 1.2f);
            SetSizeCurve(_arrows, new AnimationCurve(
                new Keyframe(0f, 0.4f), new Keyframe(0.15f, 1.1f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.55f)));
            SetAlphaCurve(_arrows, 0.05f, 0.7f);
        }

        /// <summary>
        ///     ゆらゆら漂いながら小さくなる丸い粒
        /// </summary>
        private void SetupDots()
        {
            ApplyDrag(_dots, 2f);
            ParticleSystem.NoiseModule noise = _dots.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.8f;
            noise.scrollSpeed = 0.4f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            SetSizeCurve(_dots, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.12f, 1.1f), new Keyframe(1f, 0f)));
            SetAlphaCurve(_dots, 0.05f, 0.8f);
        }

        /// <summary>
        ///     中心から広がって薄くなる輪っか。広がりはじめが速く、最後はゆっくり
        /// </summary>
        private void SetupRings()
        {
            ParticleSystem.MainModule main = _rings.main;
            main.startRotation = 0f;
            float startRatio = _pulseSize.y > 0f ? Mathf.Clamp(_pulseSize.x / _pulseSize.y, 0.05f, 1f) : 0.7f;
            AnimationCurve grow = new(new Keyframe(0f, startRatio, 0f, 2.5f * (1f - startRatio)), new Keyframe(1f, 1f, 0f, 0f));
            SetSizeCurve(_rings, grow);

            Gradient alpha = new();
            alpha.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.7f, 0.55f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _rings.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(alpha);
        }

        /// <summary>
        ///     きらっと光って、くるっと回りながら小さく消えるキラッ
        /// </summary>
        private void SetupSparkles()
        {
            ApplyDrag(_sparkles, 4f);
            ParticleSystem.RotationOverLifetimeModule rotation = _sparkles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            SetSizeCurve(_sparkles, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.12f, 1.15f), new Keyframe(1f, 0f)));
            SetAlphaCurve(_sparkles, 0.03f, 0.85f);
        }

        private static void SetSizeCurve(ParticleSystem particles, AnimationCurve curve)
        {
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        /// <summary>
        ///     出た直後に少しだけフェードインし、指定の割合から先で消えていく
        /// </summary>
        private static void SetAlphaCurve(ParticleSystem particles, float fadeInEnd, float fadeOutStart)
        {
            Gradient alpha = new();
            alpha.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeInEnd),
                    new GradientAlphaKey(1f, fadeOutStart), new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(alpha);
        }

        private static void ApplyDrag(ParticleSystem particles, float drag)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule limitVelocity = particles.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.limit = 100f;
            limitVelocity.dampen = 0f;
            limitVelocity.drag = drag;
            limitVelocity.multiplyDragByParticleSize = false;
            limitVelocity.multiplyDragByParticleVelocity = false;
        }

        /// <summary>
        ///     アトラスの1コマだけを表示する ParticleSystem をシーン直下に作る
        /// </summary>
        private ParticleSystem CreateParticles(string label, int maxParticles, int frame, Material material,
            ParticleSystemSimulationSpace space)
        {
            GameObject particleObject = new($"{name} {label}");
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.maxParticles = maxParticles;
            main.simulationSpace = space;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startSpeed = 0f;
            main.startLifetime = 0.5f;
            main.startSize = 0.2f;
            main.startColor = Color.white;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;

            ParticleSystem.TextureSheetAnimationModule sheet = particles.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = ATLAS_TILES;
            sheet.numTilesY = ATLAS_TILES;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.cycleCount = 1;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve((frame + 0.5f) / (ATLAS_TILES * ATLAS_TILES));

            ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particleRenderer.sharedMaterial = material;
            particles.Play();
            return particles;
        }

        /// <summary>
        ///     体の後ろに伸びる細い残像ライン。先は白っぽく、尾に向かって細く薄くなる
        /// </summary>
        private TrailRenderer CreateTrail(string label, Material material)
        {
            GameObject trailObject = new($"{name} {label}");
            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = _speedTrailSeconds;
            trail.minVertexDistance = 0.05f;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.numCapVertices = 3;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            trail.widthMultiplier = 0f;
            trail.emitting = false;

            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(_speedAccentColor, 0f), new GradientColorKey(_speedColor, 0.3f), new GradientColorKey(_speedColor, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.75f, 0.5f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            return trail;
        }

        private void ClearTrails()
        {
            if (_trails == null)
            {
                return;
            }

            GetBody(out Vector3 center, out _);
            foreach (TrailRenderer trail in _trails)
            {
                if (trail == null)
                {
                    continue;
                }

                trail.emitting = false;
                trail.transform.position = center;
                trail.Clear();
            }
        }

        private static void ClearParticles(ParticleSystem particles)
        {
            if (particles != null)
            {
                particles.Clear(true);
            }
        }

        /// <summary>
        ///     マテリアル未設定の場合は URP のパーティクル用シェーダーから仮のものを作る
        /// </summary>
        private Material ResolveMaterial()
        {
            if (_auraMaterial != null)
            {
                return _auraMaterial;
            }

            Shader shader = Shader.Find(FALLBACK_SHADER_NAME);
            if (shader == null)
            {
                Debug.LogWarning($"{nameof(BuffAuraEffect)}: マテリアルが未設定で、{FALLBACK_SHADER_NAME} も見つかりません", this);
                return null;
            }

            Debug.LogWarning($"{nameof(BuffAuraEffect)}: マテリアルが未設定のため仮のマテリアルを使います（M_BuffAura を設定してください）", this);
            _runtimeMaterial = new Material(shader);
            return _runtimeMaterial;
        }

        private static void DestroyIfExists(Object target)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
    }
}
