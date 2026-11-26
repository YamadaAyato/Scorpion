using UnityEngine;

namespace Scorpion.Effects
{
    /// <summary>
    ///     スピン攻撃に合わせて、キャラの周りを回るカラフルなリボンと、そこから飛び散るパフとキラキラ星を出す
    /// </summary>
    /// <remarks>
    ///     PlayerAttack.OnAttackStarted（攻撃が成立した瞬間）を合図に再生する。
    ///     リボンと粒はプレイヤーの拡大率の影響を受けないよう、シーン直下にワールド空間で作る。
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerAttack))]
    public sealed class SpinAttackEffect : MonoBehaviour
    {
        [Header("全体")]
        [SerializeField, Min(0.05f), Tooltip("エフェクトの長さ（秒）。スピンのアニメ（0.64秒）に合わせている")]
        private float _durationSeconds = 0.64f;
        [SerializeField, Min(0f), Tooltip("エフェクトの間にリボンが回る回数")]
        private float _revolutions = 2.5f;
        [SerializeField, Tooltip("回る進み具合（横: 時間 0〜1、縦: 回転 0〜1）。坂が急なところほど速く回る")]
        private AnimationCurve _spinProgress = new(
            new Keyframe(0f, 0f, 0f, 2.2f),
            new Keyframe(0.5f, 0.62f, 1.4f, 1.4f),
            new Keyframe(1f, 1f, 0.4f, 0f));
        [SerializeField, Tooltip("上から見て時計回りに回すか。アニメの回転と逆に見えたら切り替える")]
        private bool _clockwiseFromAbove;

        [Header("リング（回る軌道）")]
        [SerializeField, Tooltip("攻撃の当たり判定（子の球コライダー）の半径に合わせて回すか。オフなら体の大きさから決める")]
        private bool _matchHitboxRadius = true;
        [SerializeField, Min(0f), Tooltip("当たり判定に合わせないときの半径（体の横幅の半分に対する倍率）")]
        private float _bodyRadiusScale = 1.6f;
        [SerializeField, Tooltip("半径に足す量。大きくするとリングが外に広がる")]
        private float _radiusOffset = 0.05f;
        [SerializeField, Range(0f, 60f), Tooltip("リングをカメラ側へ傾ける角度。0 だと横から見て線になる")]
        private float _ringTiltDegrees = 22f;
        [SerializeField, Tooltip("リングの大きさの変化（横: 時間 0〜1、縦: 半径の倍率）。出だしで広がり、最後に外へ弾ける")]
        private AnimationCurve _radiusOverTime = new(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.14f, 1.12f),
            new Keyframe(0.28f, 1f),
            new Keyframe(0.82f, 1f),
            new Keyframe(1f, 1.3f));

        [Header("リボン")]
        [SerializeField, Tooltip("リボンに使うマテリアル（URP Particles/Unlit の半透明、帯状テクスチャ）")]
        private Material _ribbonMaterial;
        [SerializeField, Tooltip("リボンの色。この数だけリボンが出る（高さ・半径と同じ順番）")]
        private Color[] _ribbonColors =
        {
            new(1f, 0.36f, 0.7f, 1f),
            new(0.3f, 0.8f, 1f, 1f),
            new(1f, 0.82f, 0.2f, 1f)
        };
        [SerializeField, Tooltip("各リボンの高さ（体の足元 0 〜 頭 1）")]
        private float[] _ribbonHeights = { 0.12f, 0.5f, 0.88f };
        [SerializeField, Tooltip("各リボンの半径の倍率")]
        private float[] _ribbonRadiusScales = { 1.1f, 1f, 0.85f };
        [SerializeField, Min(0f), Tooltip("リボンの太さ")]
        private float _ribbonWidth = 0.24f;
        [SerializeField, Min(0.01f), Tooltip("リボンの長さ（尾が残る秒数）。長いほど輪っかに近づく")]
        private float _ribbonTrailSeconds = 0.16f;
        [SerializeField, Tooltip("リボンの太さの変化（横: 時間 0〜1、縦: 太さの倍率）")]
        private AnimationCurve _ribbonWidthOverTime = new(
            new Keyframe(0f, 0f),
            new Keyframe(0.1f, 1f),
            new Keyframe(0.78f, 1f),
            new Keyframe(1f, 0f));
        [SerializeField, Min(0f), Tooltip("リボンが回りながら上下に波打つ幅")]
        private float _ribbonWobble = 0.06f;
        [SerializeField, Range(0f, 1f), Tooltip("リボンの先頭を白く光らせる割合。0 で先頭も色のまま")]
        private float _ribbonHeadWhite = 0.8f;

        [Header("パフ（丸い煙）")]
        [SerializeField, Tooltip("パフに使うマテリアル（ジャンプと同じ M_JumpDust）")]
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
        private Vector2 _puffSize = new(0.35f, 0.6f);
        [SerializeField, Tooltip("パフが消えるまでの秒数（最小・最大）")]
        private Vector2 _puffLifetimeSeconds = new(0.35f, 0.55f);
        [SerializeField, Min(0f), Tooltip("回転中、リボンの先頭から1秒あたりに出すパフの数（全リボン合計）")]
        private float _puffsPerSecond = 45f;
        [SerializeField, Tooltip("パフが外へ飛ぶ速さ（最小・最大）")]
        private Vector2 _puffSpeed = new(1.6f, 3f);
        [SerializeField, Min(0f), Tooltip("パフの空気抵抗。大きいほど飛んだ後すぐ止まる")]
        private float _puffDrag = 6f;
        [SerializeField, Min(0), Tooltip("回り始めに足元から渦巻き状に出すパフの数")]
        private int _startPuffCount = 14;
        [SerializeField, Min(0f), Tooltip("回り始めのパフが足元から広がる速さ")]
        private float _startPuffSpeed = 4f;

        [Header("キラキラ（星）")]
        [SerializeField, Tooltip("キラキラに使うマテリアル（ジャンプと同じ M_JumpSparkle）")]
        private Material _sparkleMaterial;
        [SerializeField, Tooltip("キラキラの色。粒ごとにこの中からランダムに選ぶ（最大8色）")]
        private Color[] _sparkleColors =
        {
            new(1f, 0.85f, 0.1f, 1f),
            new(1f, 0.3f, 0.62f, 1f),
            new(0.2f, 0.8f, 1f, 1f),
            new(0.45f, 1f, 0.3f, 1f),
            new(1f, 0.55f, 0.15f, 1f),
            new(0.7f, 0.45f, 1f, 1f)
        };
        [SerializeField, Tooltip("キラキラの大きさ（最小・最大）")]
        private Vector2 _sparkleSize = new(0.24f, 0.42f);
        [SerializeField, Tooltip("キラキラが消えるまでの秒数（最小・最大）")]
        private Vector2 _sparkleLifetimeSeconds = new(0.45f, 0.75f);
        [SerializeField, Min(0f), Tooltip("回転中、リボンの先頭から1秒あたりに出すキラキラの数（全リボン合計）")]
        private float _sparklesPerSecond = 30f;
        [SerializeField, Tooltip("キラキラが回転の勢いで飛ぶ速さ（最小・最大）")]
        private Vector2 _sparkleSpeed = new(2.5f, 4.5f);
        [SerializeField, Min(0f), Tooltip("キラキラにかかる重力の倍率")]
        private float _sparkleGravity = 0.7f;
        [SerializeField, Min(0), Tooltip("回り終わりに外へ弾けるキラキラの数")]
        private int _finishSparkleCount = 14;
        [SerializeField, Min(0f), Tooltip("回り終わりのキラキラが弾ける速さ")]
        private float _finishSparkleSpeed = 5.5f;

        private PlayerAttack _attack;
        private Collider _bodyCollider;
        private SphereCollider _hitbox;
        private Camera _camera;
        private TrailRenderer[] _ribbons;
        private ParticleSystem _puffParticles;
        private ParticleSystem _sparkleParticles;
        private Material _runtimeRibbonMaterial;
        private Material _runtimePuffMaterial;
        private Material _runtimeSparkleMaterial;
        private float _elapsedSeconds = -1f;
        private float _puffBudget;
        private float _sparkleBudget;
        private int _emitCursor;
        private Vector3[] _ribbonPositions;
        private Vector3[] _ribbonTangents;

        private const int MAX_PUFFS = 96;
        private const int MAX_SPARKLES = 64;
        private const int MAX_GRADIENT_KEYS = 8;
        private const float SPARKLE_SPIN_SPEED = 8f;
        private const string FALLBACK_SHADER_NAME = "Universal Render Pipeline/Particles/Unlit";

        /// <summary> スピンのエフェクトを再生中か </summary>
        public bool IsPlaying => _elapsedSeconds >= 0f;

        /// <summary>
        ///     スピンのエフェクトを最初から再生する
        /// </summary>
        public void Play()
        {
            _elapsedSeconds = 0f;
            _puffBudget = 0f;
            _sparkleBudget = 0f;
            _camera = Camera.main;

            for (int i = 0; i < _ribbons.Length; i++)
            {
                TrailRenderer ribbon = _ribbons[i];
                ribbon.transform.position = GetRibbonPosition(i, 0f, out _);
                ribbon.widthMultiplier = 0f;
                ribbon.Clear();
                ribbon.emitting = true;
            }

            EmitStartSwirl();
        }

        /// <summary>
        ///     再生中のエフェクトを止める。出ている粒とリボンの尾はそのまま消えていく
        /// </summary>
        public void Stop()
        {
            _elapsedSeconds = -1f;
            if (_ribbons == null)
            {
                return;
            }

            foreach (TrailRenderer ribbon in _ribbons)
            {
                if (ribbon != null)
                {
                    ribbon.emitting = false;
                }
            }
        }

        private void Awake()
        {
            _attack = GetComponent<PlayerAttack>();
            foreach (Collider candidate in GetComponents<Collider>())
            {
                if (!candidate.isTrigger)
                {
                    _bodyCollider = candidate;
                    break;
                }
            }

            // PlayerAttack の当たり判定は非公開なので、子にある球のトリガーを当たり判定とみなす
            foreach (SphereCollider candidate in GetComponentsInChildren<SphereCollider>(true))
            {
                if (candidate.isTrigger && candidate.gameObject != gameObject)
                {
                    _hitbox = candidate;
                    break;
                }
            }

            _ribbons = CreateRibbons();
            _ribbonPositions = new Vector3[_ribbons.Length];
            _ribbonTangents = new Vector3[_ribbons.Length];
            _puffParticles = CreatePuffParticles();
            _sparkleParticles = CreateSparkleParticles();
        }

        private void OnEnable()
        {
            _attack.OnAttackStarted += Play;
        }

        private void OnDisable()
        {
            _attack.OnAttackStarted -= Play;
            Stop();
        }

        /// <summary>
        ///     アニメーションで動いた後の位置に合わせて、リボンを回し粒を出す
        /// </summary>
        private void LateUpdate()
        {
            if (!IsPlaying)
            {
                return;
            }

            _elapsedSeconds += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsedSeconds / _durationSeconds);
            float width = _ribbonWidth * Mathf.Max(_ribbonWidthOverTime.Evaluate(t), 0f);

            int ribbonCount = _ribbons.Length;
            for (int i = 0; i < ribbonCount; i++)
            {
                _ribbonPositions[i] = GetRibbonPosition(i, t, out _ribbonTangents[i]);
                _ribbons[i].transform.position = _ribbonPositions[i];
                _ribbons[i].widthMultiplier = width;
            }

            // 出す数を各リボンに順番に割り振り、どのリボンの先頭からも途切れず出るようにする
            _puffBudget += _puffsPerSecond * Time.deltaTime;
            while (_puffBudget >= 1f)
            {
                _puffBudget -= 1f;
                int i = _emitCursor++ % ribbonCount;
                EmitFlung(_puffParticles, _ribbonPositions[i], _ribbonTangents[i], _puffSpeed, 0.6f);
            }

            _sparkleBudget += _sparklesPerSecond * Time.deltaTime;
            while (_sparkleBudget >= 1f)
            {
                _sparkleBudget -= 1f;
                int i = _emitCursor++ % ribbonCount;
                EmitFlung(_sparkleParticles, _ribbonPositions[i], _ribbonTangents[i], _sparkleSpeed, 0.3f);
            }

            if (_elapsedSeconds >= _durationSeconds)
            {
                EmitFinishBurst();
                Stop();
            }
        }

        private void OnDestroy()
        {
            if (_ribbons != null)
            {
                foreach (TrailRenderer ribbon in _ribbons)
                {
                    DestroyIfExists(ribbon != null ? ribbon.gameObject : null);
                }
            }

            DestroyIfExists(_puffParticles != null ? _puffParticles.gameObject : null);
            DestroyIfExists(_sparkleParticles != null ? _sparkleParticles.gameObject : null);
            DestroyIfExists(_runtimeRibbonMaterial);
            DestroyIfExists(_runtimePuffMaterial);
            DestroyIfExists(_runtimeSparkleMaterial);
        }

        /// <summary>
        ///     指定したリボンの、進み具合 t（0〜1）での位置と進行方向を求める
        /// </summary>
        private Vector3 GetRibbonPosition(int index, float t, out Vector3 tangent)
        {
            GetRingFrame(out Vector3 center, out float height, out Vector3 axisA, out Vector3 axisB, out Vector3 normal);

            int count = _ribbons.Length;
            float heightRatio = GetOrDefault(_ribbonHeights, index, (index + 0.5f) / count);
            float radius = GetBaseRadius() * GetOrDefault(_ribbonRadiusScales, index, 1f) *
                           Mathf.Max(_radiusOverTime.Evaluate(t), 0f);

            float direction = _clockwiseFromAbove ? -1f : 1f;
            float phase = index * Mathf.PI * 2f / count;
            float angle = phase + direction * _spinProgress.Evaluate(t) * _revolutions * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            Vector3 offset = (axisA * cos + axisB * sin) * radius;
            tangent = (axisB * cos - axisA * sin) * direction;
            float wobble = Mathf.Sin(angle * 2f + phase) * _ribbonWobble;
            return center + Vector3.up * ((heightRatio - 0.5f) * height) + normal * wobble + offset;
        }

        /// <summary>
        ///     リングの中心・体の高さ・リング面の向きを求める。リング面はカメラ側へ少し傾ける
        /// </summary>
        private void GetRingFrame(out Vector3 center, out float height, out Vector3 axisA, out Vector3 axisB,
            out Vector3 normal)
        {
            if (_bodyCollider != null)
            {
                Bounds bounds = _bodyCollider.bounds;
                center = bounds.center;
                height = bounds.size.y;
            }
            else
            {
                center = transform.position;
                height = 1f;
            }

            Vector3 right = Vector3.right;
            if (_camera != null)
            {
                right = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up);
                if (right.sqrMagnitude < 0.0001f)
                {
                    right = Vector3.right;
                }

                right.Normalize();
            }

            // 上向きを、カメラの右方向を軸にカメラ側へ倒す（奥側が上、手前側が下に見える）
            normal = Quaternion.AngleAxis(-_ringTiltDegrees, right) * Vector3.up;
            axisA = right;
            axisB = Vector3.Cross(axisA, normal).normalized;
        }

        /// <summary>
        ///     リングの基準半径を、当たり判定または体の大きさから求める
        /// </summary>
        private float GetBaseRadius()
        {
            float radius;
            if (_matchHitboxRadius && _hitbox != null)
            {
                Vector3 scale = _hitbox.transform.lossyScale;
                float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                radius = _hitbox.radius * maxScale;
            }
            else if (_bodyCollider != null)
            {
                Vector3 extents = _bodyCollider.bounds.extents;
                radius = Mathf.Max(extents.x, extents.z) * _bodyRadiusScale;
            }
            else
            {
                radius = 0.8f;
            }

            return Mathf.Max(radius + _radiusOffset, 0.05f);
        }

        /// <summary>
        ///     リボンの進行方向へ振り飛ばしつつ、少し外側へも飛ばす
        /// </summary>
        private void EmitFlung(ParticleSystem particles, Vector3 position, Vector3 tangent, Vector2 speedRange,
            float outwardRatio)
        {
            if (particles == null)
            {
                return;
            }

            GetRingFrame(out Vector3 center, out _, out _, out _, out Vector3 normal);
            Vector3 outward = Vector3.ProjectOnPlane(position - center, normal).normalized;
            Vector3 direction = (tangent + outward * outwardRatio + Random.insideUnitSphere * 0.25f).normalized;
            ParticleSystem.EmitParams emitParams = new()
            {
                position = position,
                velocity = direction * Random.Range(speedRange.x, speedRange.y),
                applyShapeToPosition = false
            };
            particles.Emit(emitParams, 1);
        }

        /// <summary>
        ///     回り始めに、足元から渦を巻くようにパフを広げる
        /// </summary>
        private void EmitStartSwirl()
        {
            if (_puffParticles == null || _startPuffCount <= 0)
            {
                return;
            }

            GetRingFrame(out Vector3 center, out float height, out _, out _, out _);
            Vector3 foot = center + Vector3.down * (height * 0.45f);
            float direction = _clockwiseFromAbove ? -1f : 1f;
            for (int i = 0; i < _startPuffCount; i++)
            {
                float angle = (i + Random.value * 0.5f) * Mathf.PI * 2f / _startPuffCount;
                Vector3 outward = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-outward.z, 0f, outward.x) * direction;
                ParticleSystem.EmitParams emitParams = new()
                {
                    position = foot + outward * 0.15f,
                    velocity = (outward + tangent * 0.8f).normalized * (_startPuffSpeed * Random.Range(0.75f, 1.15f)) +
                               Vector3.up * 0.4f,
                    applyShapeToPosition = false
                };
                _puffParticles.Emit(emitParams, 1);
            }
        }

        /// <summary>
        ///     回り終わりに、リングの位置から星を外へ弾けさせる
        /// </summary>
        private void EmitFinishBurst()
        {
            if (_sparkleParticles == null || _finishSparkleCount <= 0)
            {
                return;
            }

            GetRingFrame(out Vector3 center, out _, out Vector3 axisA, out Vector3 axisB, out _);
            float radius = GetBaseRadius() * 0.8f;
            for (int i = 0; i < _finishSparkleCount; i++)
            {
                float angle = (i + Random.value * 0.6f) * Mathf.PI * 2f / _finishSparkleCount;
                Vector3 outward = axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle);
                ParticleSystem.EmitParams emitParams = new()
                {
                    position = center + outward * radius,
                    velocity = (outward + Vector3.up * 0.35f).normalized *
                               (_finishSparkleSpeed * Random.Range(0.8f, 1.2f)),
                    applyShapeToPosition = false
                };
                _sparkleParticles.Emit(emitParams, 1);
            }
        }

        /// <summary>
        ///     リボン（TrailRenderer）を色の数だけ作る。先頭は白く光り、尾に向かって細く透明になる
        /// </summary>
        private TrailRenderer[] CreateRibbons()
        {
            Material material = ResolveMaterial(_ribbonMaterial, ref _runtimeRibbonMaterial);
            Color[] colors = _ribbonColors != null && _ribbonColors.Length > 0
                ? _ribbonColors
                : new[] { Color.white };
            TrailRenderer[] ribbons = new TrailRenderer[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                GameObject ribbonObject = new($"{name} SpinRibbon {i}");
                TrailRenderer ribbon = ribbonObject.AddComponent<TrailRenderer>();
                ribbon.emitting = false;
                ribbon.time = _ribbonTrailSeconds;
                ribbon.minVertexDistance = 0.02f;
                ribbon.widthMultiplier = 0f;
                ribbon.widthCurve = new AnimationCurve(
                    new Keyframe(0f, 0.75f),
                    new Keyframe(0.12f, 1f),
                    new Keyframe(1f, 0f));
                ribbon.colorGradient = CreateRibbonGradient(colors[i]);
                ribbon.numCapVertices = 4;
                ribbon.numCornerVertices = 2;
                ribbon.textureMode = LineTextureMode.Stretch;
                ribbon.alignment = LineAlignment.View;
                ribbon.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ribbon.receiveShadows = false;
                ribbon.sharedMaterial = material;
                ribbons[i] = ribbon;
            }

            return ribbons;
        }

        /// <summary>
        ///     先頭が白く、すぐ指定色になり、尾の終わりで透明になる色の変化を作る
        /// </summary>
        private Gradient CreateRibbonGradient(Color color)
        {
            Color head = Color.Lerp(color, Color.white, _ribbonHeadWhite);
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(head, 0f),
                    new GradientColorKey(color, 0.18f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(color.a, 0f),
                    new GradientAlphaKey(color.a, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        /// <summary>
        ///     ぽんっと膨らんでから縮んで消える丸い煙を組み立てる
        /// </summary>
        private ParticleSystem CreatePuffParticles()
        {
            ParticleSystem particles = CreateBaseParticles("SpinPuff", MAX_PUFFS, _puffLifetimeSeconds, _puffSize,
                _puffColors, ResolveMaterial(_puffMaterial, ref _runtimePuffMaterial));

            ApplyDrag(particles, _puffDrag);

            AnimationCurve popCurve = new(
                new Keyframe(0f, 0.25f),
                new Keyframe(0.15f, 1f),
                new Keyframe(0.35f, 0.72f),
                new Keyframe(1f, 0f));
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.4f, popCurve);

            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

            ApplyFadeOut(particles, 0.85f);
            return particles;
        }

        /// <summary>
        ///     くるくる回りながら飛んで落ちて消える星を組み立てる
        /// </summary>
        private ParticleSystem CreateSparkleParticles()
        {
            ParticleSystem particles = CreateBaseParticles("SpinSparkle", MAX_SPARKLES, _sparkleLifetimeSeconds,
                _sparkleSize, _sparkleColors, ResolveMaterial(_sparkleMaterial, ref _runtimeSparkleMaterial));

            ParticleSystem.MainModule main = particles.main;
            main.gravityModifier = _sparkleGravity;

            ApplyDrag(particles, 1.5f);

            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-SPARKLE_SPIN_SPEED, SPARKLE_SPIN_SPEED);

            // 一瞬大きく光ってから、だんだん小さくなる
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.3f,
                new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.1f, 1f), new Keyframe(1f, 0f)));

            ApplyFadeOut(particles, 0.9f);
            return particles;
        }

        /// <summary>
        ///     パフとキラキラに共通する設定で ParticleSystem を作る
        /// </summary>
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
            main.startSpeed = 0f;
            main.startColor = CreateRandomColor(colors);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;

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
                Debug.LogWarning($"{nameof(SpinAttackEffect)}: マテリアルが未設定で、{FALLBACK_SHADER_NAME} も見つかりません", this);
                return null;
            }

            Debug.LogWarning($"{nameof(SpinAttackEffect)}: マテリアルが未設定のため仮のマテリアルを使います", this);
            runtimeMaterial = new Material(shader);
            return runtimeMaterial;
        }

        /// <summary>
        ///     配列の範囲内ならその値を、範囲外なら既定値を返す
        /// </summary>
        private static float GetOrDefault(float[] values, int index, float fallback)
        {
            return values != null && index < values.Length ? values[index] : fallback;
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
