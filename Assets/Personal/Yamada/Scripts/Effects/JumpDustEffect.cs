using UnityEngine;

namespace Scorpion.Effects
{
    /// <summary>
    ///     プレイヤーのジャンプと着地に合わせて、足元からポップな煙のパフとキラキラ星を出す
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMove), typeof(Rigidbody))]
    public sealed class JumpDustEffect : MonoBehaviour
    {
        [Header("パフ（丸い煙）")]
        [SerializeField, Tooltip("パフに使うマテリアル（URP Particles/Unlit の半透明、丸いテクスチャ）")]
        private Material _puffMaterial;
        [SerializeField, Tooltip("パフの色。粒ごとにこの中からランダムに選ぶ（最大8色）")]
        private Color[] _puffColors =
        {
            new(1f, 1f, 1f, 1f),
            new(1f, 0.55f, 0.78f, 1f),
            new(0.45f, 0.82f, 1f, 1f),
            new(1f, 0.88f, 0.35f, 1f),
            new(0.5f, 1f, 0.68f, 1f),
            new(0.78f, 0.6f, 1f, 1f)
        };
        [SerializeField, Tooltip("パフの大きさ（最小・最大）")]
        private Vector2 _puffSize = new(0.45f, 0.75f);
        [SerializeField, Tooltip("パフが消えるまでの秒数（最小・最大）")]
        private Vector2 _puffLifetimeSeconds = new(0.4f, 0.6f);
        [SerializeField, Tooltip("パフが足元から横へ広がる速さ（最小・最大）")]
        private Vector2 _puffSpeed = new(2.8f, 4.8f);
        [SerializeField, Tooltip("パフがふわっと浮き上がる上向きの速さ")]
        private float _puffRiseSpeed = 0.6f;
        [SerializeField, Min(0f), Tooltip("パフの空気抵抗。大きいほど広がった後すぐ止まる")]
        private float _puffDrag = 6f;
        [SerializeField, Min(1f), Tooltip("出た直後にぽんっと膨らむ大きさの倍率")]
        private float _puffPopScale = 1.4f;

        [Header("キラキラ（星）")]
        [SerializeField, Tooltip("キラキラを出すか")]
        private bool _useSparkles = true;
        [SerializeField, Tooltip("キラキラに使うマテリアル（星型テクスチャ）")]
        private Material _sparkleMaterial;
        [SerializeField, Tooltip("キラキラの色。粒ごとにこの中からランダムに選ぶ（最大8色）")]
        private Color[] _sparkleColors =
        {
            new(1f, 0.85f, 0.1f, 1f),
            new(1f, 0.3f, 0.62f, 1f),
            new(0.2f, 0.8f, 1f, 1f),
            new(0.45f, 1f, 0.3f, 1f),
            new(1f, 0.55f, 0.15f, 1f)
        };
        [SerializeField, Tooltip("キラキラの大きさ（最小・最大）")]
        private Vector2 _sparkleSize = new(0.22f, 0.38f);
        [SerializeField, Tooltip("キラキラが消えるまでの秒数（最小・最大）")]
        private Vector2 _sparkleLifetimeSeconds = new(0.5f, 0.8f);
        [SerializeField, Tooltip("キラキラが飛び出す速さ（最小・最大）")]
        private Vector2 _sparkleSpeed = new(3.4f, 5.4f);
        [SerializeField, Min(0f), Tooltip("キラキラにかかる重力の倍率")]
        private float _sparkleGravity = 0.9f;
        [SerializeField, Range(0f, 90f), Tooltip("キラキラが飛び出す範囲の角度（真上からの開き）")]
        private float _sparkleSpreadAngle = 55f;

        [Header("発生位置")]
        [SerializeField, Min(0f), Tooltip("足元の発生範囲の半径")]
        private float _spawnRadius = 0.2f;
        [SerializeField, Range(0f, 1f), Tooltip("奥行き方向への広がりの割合。0 で左右だけ、1 で円形")]
        private float _depthSpread = 0.35f;
        [SerializeField, Tooltip("足元からの位置のずれ。床にめり込むときは y を上げる")]
        private Vector3 _footOffset = new(0f, 0.06f, 0f);

        [Header("ジャンプ")]
        [SerializeField, Min(0), Tooltip("ジャンプ時に出すパフの数")]
        private int _jumpPuffCount = 10;
        [SerializeField, Min(0), Tooltip("ジャンプ時に出すキラキラの数")]
        private int _jumpSparkleCount = 6;
        [SerializeField, Min(0f), Tooltip("ジャンプ時の広がる速さの倍率")]
        private float _jumpSpeedScale = 0.9f;

        [Header("着地")]
        [SerializeField, Tooltip("着地時にも出すか")]
        private bool _emitOnLanding = true;
        [SerializeField, Tooltip("着地時に出すパフの数（ゆっくり着地・勢いよく着地）")]
        private Vector2Int _landingPuffCount = new(8, 18);
        [SerializeField, Tooltip("着地時に出すキラキラの数（ゆっくり着地・勢いよく着地）")]
        private Vector2Int _landingSparkleCount = new(3, 10);
        [SerializeField, Tooltip("着地時の広がる速さの倍率（ゆっくり着地・勢いよく着地）")]
        private Vector2 _landingSpeedScale = new(0.9f, 1.7f);
        [SerializeField, Tooltip("この落下速度より遅い着地では出さない／この速度以上で最大量になる")]
        private Vector2 _landingFallSpeedRange = new(1.5f, 8f);
        [SerializeField, Min(0f), Tooltip("この秒数より短い空中時間の着地では出さない（段差でのちらつき防止）")]
        private float _minAirTimeSeconds = 0.12f;

        private PlayerMove _movement;
        private Rigidbody _rigidbody;
        private Collider _bodyCollider;
        private ParticleSystem _puffParticles;
        private ParticleSystem _sparkleParticles;
        private Material _runtimePuffMaterial;
        private Material _runtimeSparkleMaterial;
        private bool _wasGrounded;
        private float _airTimeSeconds;
        private float _lastAirVelocityY;

        private const int MAX_PUFFS = 64;
        private const int MAX_SPARKLES = 32;
        private const int MAX_GRADIENT_KEYS = 8;
        private const string FALLBACK_SHADER_NAME = "Universal Render Pipeline/Particles/Unlit";

        /// <summary>
        ///     足元からパフとキラキラを出す。速さの倍率で広がり方を変えられる
        /// </summary>
        public void EmitDust(int puffCount, int sparkleCount, float speedScale)
        {
            Vector3 footPosition = GetFootPosition();
            EmitFrom(_puffParticles, footPosition, puffCount, _puffSpeed * speedScale);
            if (_useSparkles)
            {
                EmitFrom(_sparkleParticles, footPosition, sparkleCount, _sparkleSpeed * speedScale);
            }
        }

        private void Awake()
        {
            _movement = GetComponent<PlayerMove>();
            _rigidbody = GetComponent<Rigidbody>();
            foreach (Collider candidate in GetComponents<Collider>())
            {
                if (!candidate.isTrigger)
                {
                    _bodyCollider = candidate;
                    break;
                }
            }

            _puffParticles = CreatePuffParticles();
            _sparkleParticles = CreateSparkleParticles();
        }

        private void OnEnable()
        {
            _movement.OnJumpStarted += HandleJumpStarted;
            _wasGrounded = _movement.IsGrounded;
            _airTimeSeconds = 0f;
        }

        private void FixedUpdate()
        {
            bool isGrounded = _movement.IsGrounded;
            if (isGrounded && !_wasGrounded)
            {
                TryEmitLandingDust();
            }

            if (isGrounded)
            {
                _airTimeSeconds = 0f;
            }
            else
            {
                _airTimeSeconds += Time.fixedDeltaTime;
                // 接地判定の直前、地面にぶつかる前の落下速度を残しておく
                _lastAirVelocityY = _rigidbody.linearVelocity.y;
            }

            _wasGrounded = isGrounded;
        }

        private void OnDisable()
        {
            _movement.OnJumpStarted -= HandleJumpStarted;
        }

        private void OnDestroy()
        {
            DestroyIfExists(_puffParticles != null ? _puffParticles.gameObject : null);
            DestroyIfExists(_sparkleParticles != null ? _sparkleParticles.gameObject : null);
            DestroyIfExists(_runtimePuffMaterial);
            DestroyIfExists(_runtimeSparkleMaterial);
        }

        /// <summary>
        ///     踏み切った瞬間に足元からパフを出す
        /// </summary>
        private void HandleJumpStarted()
        {
            EmitDust(_jumpPuffCount, _jumpSparkleCount, _jumpSpeedScale);
            _wasGrounded = false;
            _airTimeSeconds = 0f;
        }

        /// <summary>
        ///     落下の勢いに応じた量のパフとキラキラを着地点に出す
        /// </summary>
        private void TryEmitLandingDust()
        {
            // 短い接地切れや落下速度の小さい着地では、足元の粒を出さない
            float fallSpeed = -_lastAirVelocityY;
            if (!_emitOnLanding || _airTimeSeconds < _minAirTimeSeconds || fallSpeed < _landingFallSpeedRange.x)
            {
                return;
            }

            float strength = Mathf.InverseLerp(_landingFallSpeedRange.x, _landingFallSpeedRange.y, fallSpeed);
            int puffCount = Mathf.RoundToInt(Mathf.Lerp(_landingPuffCount.x, _landingPuffCount.y, strength));
            int sparkleCount = Mathf.RoundToInt(Mathf.Lerp(_landingSparkleCount.x, _landingSparkleCount.y, strength));
            EmitDust(puffCount, sparkleCount, Mathf.Lerp(_landingSpeedScale.x, _landingSpeedScale.y, strength));
        }

        /// <summary>
        ///     本体コライダーの底面中央を足元として取得する
        /// </summary>
        private Vector3 GetFootPosition()
        {
            if (_bodyCollider == null)
            {
                return transform.position + _footOffset;
            }

            Bounds bounds = _bodyCollider.bounds;
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) + _footOffset;
        }

        /// <summary>
        ///     指定位置から指定の速さの範囲で粒を出す
        /// </summary>
        private static void EmitFrom(ParticleSystem particles, Vector3 position, int count, Vector2 speedRange)
        {
            if (particles == null || count <= 0)
            {
                return;
            }

            particles.transform.position = position;
            ParticleSystem.MainModule main = particles.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
            particles.Emit(count);
        }

        /// <summary>
        ///     横に弾けて、ぽんっと膨らんでから縮んで消える丸い煙を組み立てる
        /// </summary>
        private ParticleSystem CreatePuffParticles()
        {
            ParticleSystem particles = CreateBaseParticles("JumpPuff", MAX_PUFFS, _puffLifetimeSeconds, _puffSize,
                _puffColors, ResolveMaterial(_puffMaterial, ref _runtimePuffMaterial));

            // 円を水平に寝かせ、奥行き方向を潰して横長に広がるようにする
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = _spawnRadius;
            shape.radiusThickness = 1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.scale = new Vector3(1f, Mathf.Max(_depthSpread, 0.01f), 1f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(_puffRiseSpeed * 0.5f, _puffRiseSpeed);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            ApplyDrag(particles, _puffDrag);

            // 小さく出て一気に膨らみ、少し戻ってから縮んで消える
            float popScale = Mathf.Max(_puffPopScale, 1f);
            AnimationCurve popCurve = new(
                new Keyframe(0f, 0.35f / popScale),
                new Keyframe(0.15f, 1f),
                new Keyframe(0.35f, 1f / popScale),
                new Keyframe(1f, 0f));
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(popScale, popCurve);

            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-1f, 1f);

            ApplyFadeOut(particles, 0.85f);
            return particles;
        }

        /// <summary>
        ///     上向きに飛び出し、くるくる回りながら落ちて消える星を組み立てる
        /// </summary>
        private ParticleSystem CreateSparkleParticles()
        {
            ParticleSystem particles = CreateBaseParticles("JumpSparkle", MAX_SPARKLES, _sparkleLifetimeSeconds,
                _sparkleSize, _sparkleColors, ResolveMaterial(_sparkleMaterial, ref _runtimeSparkleMaterial));

            ParticleSystem.MainModule main = particles.main;
            main.gravityModifier = _sparkleGravity;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = _sparkleSpreadAngle;
            shape.radius = _spawnRadius * 0.5f;
            shape.radiusThickness = 1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.scale = new Vector3(1f, Mathf.Max(_depthSpread, 0.01f), 1f);

            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-8f, 8f);

            // 一瞬大きく光ってから、だんだん小さくなる
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.3f,
                new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.1f, 1f), new Keyframe(1f, 0f)));

            ApplyFadeOut(particles, 0.9f);
            return particles;
        }

        /// <summary>
        ///     両方の粒に共通する設定で ParticleSystem を作る
        /// </summary>
        /// <remarks>
        ///     プレイヤーの拡大率の影響を受けないよう、シーン直下にワールド空間で作る
        /// </remarks>
        private ParticleSystem CreateBaseParticles(string label, int maxParticles, Vector2 lifetime, Vector2 size,
            Color[] colors, Material material)
        {
            GameObject particleObject = new($"{name} {label}");
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = CreateRandomColor(colors);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;

            ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particleRenderer.sharedMaterial = material;

            return particles;
        }

        /// <summary>
        ///     色リストの中から粒ごとに1色を選ぶ設定を作る
        /// </summary>
        private static ParticleSystem.MinMaxGradient CreateRandomColor(Color[] colors)
        {
            if (colors == null || colors.Length == 0)
            {
                return new ParticleSystem.MinMaxGradient(Color.white);
            }

            int count = Mathf.Min(colors.Length, MAX_GRADIENT_KEYS);
            GradientColorKey[] colorKeys = new GradientColorKey[count];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[count];
            for (int i = 0; i < count; i++)
            {
                // Fixed モードでは各キーの時刻までがその色になるので、区間を均等に割り当てる
                float time = (i + 1f) / count;
                colorKeys[i] = new GradientColorKey(colors[i], time);
                alphaKeys[i] = new GradientAlphaKey(colors[i].a, time);
            }

            Gradient palette = new() { mode = GradientMode.Fixed };
            palette.SetKeys(colorKeys, alphaKeys);
            return new ParticleSystem.MinMaxGradient(palette) { mode = ParticleSystemGradientMode.RandomColor };
        }

        /// <summary>
        ///     速度の上限はかけず、空気抵抗だけで減速させる
        /// </summary>
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
        ///     指定した割合の時点まではくっきり表示し、最後だけ薄くして消す
        /// </summary>
        private static void ApplyFadeOut(ParticleSystem particles, float fadeStart)
        {
            Gradient alphaGradient = new();
            alphaGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(alphaGradient);
        }

        /// <summary>
        ///     マテリアル未設定の場合は URP のパーティクル用シェーダーから仮のものを作る
        /// </summary>
        private Material ResolveMaterial(Material material, ref Material runtimeMaterial)
        {
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(FALLBACK_SHADER_NAME);
            if (shader == null)
            {
                Debug.LogWarning($"{nameof(JumpDustEffect)}: マテリアルが未設定で、{FALLBACK_SHADER_NAME} も見つかりません", this);
                return null;
            }

            Debug.LogWarning($"{nameof(JumpDustEffect)}: マテリアルが未設定のため仮のマテリアルを使います", this);
            runtimeMaterial = new Material(shader);
            return runtimeMaterial;
        }

        /// <summary>
        ///     実行時に作ったオブジェクトが残っていれば破棄する
        /// </summary>
        private static void DestroyIfExists(Object target)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
    }
}
