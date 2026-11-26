using System;
using UnityEngine;

namespace Scorpion.Effects
{
    /// <summary>
    ///     爆発エフェクトの見た目の設定。大きさと飛ぶ距離は爆風半径に対する割合で指定する
    /// </summary>
    [Serializable]
    public sealed class ExplosionEffectSettings
    {
        [Header("全体")]
        [Min(0.1f), Tooltip("見た目の大きさの倍率。1 で爆風の当たり判定半径ぴったりまで広がる")]
        public float RadiusScale = 1f;
        [Tooltip("地面で爆発したときは、粒と集中線を上向きの扇形に広げる（地面に埋もれて見えない分を減らす）")]
        public bool FanUpOnGround = true;
        [Range(90f, 360f), Tooltip("地面で爆発したときの扇の角度")]
        public float GroundFanAngle = 220f;
        [Min(0f), Tooltip("爆心の真下をこの距離まで調べ、Ground タグがあれば地面での爆発とみなす")]
        public float GroundCheckDistance = 0.9f;

        [Header("ドカン（ギザギザの爆発シェイプ）")]
        [Tooltip("重ねるギザギザの色。外側から順に描く（赤→オレンジ→黄）")]
        public Color[] BurstColors =
        {
            new(1f, 0.3f, 0.25f, 1f),
            new(1f, 0.6f, 0.1f, 1f),
            new(1f, 0.95f, 0.4f, 1f)
        };
        [Tooltip("それぞれのギザギザの大きさ（爆風半径に対する割合）。BurstColors と同じ順")]
        public float[] BurstSizes = { 1.05f, 0.8f, 0.55f };
        [Min(0.01f), Tooltip("ギザギザが消えるまでの秒数（外側の層ほど少し長く残る）")]
        public float BurstSeconds = 0.28f;
        [Min(1f), Tooltip("出た直後にどんっと膨らむ倍率")]
        public float BurstPopScale = 1.15f;

        [Header("火の雲（中心）")]
        [Tooltip("火の雲の色（最大8色）")]
        public Color[] FireColors =
        {
            new(1f, 0.85f, 0.25f, 1f),
            new(1f, 0.6f, 0.12f, 1f),
            new(1f, 0.4f, 0.15f, 1f)
        };
        [Min(0), Tooltip("火の雲の数")]
        public int FireCount = 7;
        [Tooltip("火の雲の大きさ（爆風半径に対する割合、最小・最大）")]
        public Vector2 FireSize = new(0.26f, 0.38f);
        [Tooltip("火の雲が消えるまでの秒数（最小・最大）")]
        public Vector2 FireLifetimeSeconds = new(0.32f, 0.48f);
        [Tooltip("火の雲が止まる距離（爆風半径に対する割合、最小・最大）")]
        public Vector2 FireReach = new(0.12f, 0.4f);

        [Header("黒煙（もくもく残る）")]
        [Tooltip("黒煙の色（最大8色）。真っ黒より、少し紫や茶色がかった濃い色がポップに見える")]
        public Color[] SmokeColors =
        {
            new(0.3f, 0.26f, 0.36f, 1f),
            new(0.4f, 0.3f, 0.36f, 1f),
            new(0.24f, 0.22f, 0.3f, 1f)
        };
        [Min(0), Tooltip("黒煙の数")]
        public int SmokeCount = 9;
        [Tooltip("黒煙の大きさ（爆風半径に対する割合、最小・最大）")]
        public Vector2 SmokeSize = new(0.3f, 0.42f);
        [Tooltip("黒煙が消えるまでの秒数（最小・最大）")]
        public Vector2 SmokeLifetimeSeconds = new(0.7f, 1f);
        [Tooltip("黒煙が止まる距離（爆風半径に対する割合、最小・最大）")]
        public Vector2 SmokeReach = new(0.4f, 0.75f);
        [Tooltip("黒煙がもくもく浮き上がる上向きの速さ")]
        public float SmokeRiseSpeed = 0.9f;
        [Min(1f), Tooltip("出た直後にぽんっと膨らむ倍率")]
        public float SmokePopScale = 1.25f;

        [Header("集中線（衝撃波）")]
        [Min(0), Tooltip("外へ飛ぶ線の本数。0 で出さない")]
        public int LineCount = 12;
        [Tooltip("線の色。順番に使う")]
        public Color[] LineColors =
        {
            new(1f, 1f, 0.95f, 1f),
            new(1f, 0.86f, 0.25f, 1f)
        };
        [Min(0f), Tooltip("線の太さ（爆風半径に対する割合）")]
        public float LineWidth = 0.06f;
        [Min(0.01f), Tooltip("線が飛びきるまでの秒数")]
        public float LineSeconds = 0.22f;
        [Tooltip("線の外側の端が動く範囲（爆風半径に対する割合、出始め・飛びきり）。1 で当たり判定の縁")]
        public Vector2 LineReach = new(0.4f, 1.1f);
        [Range(0.05f, 1f), Tooltip("線の長さ（爆風半径に対する割合、出始め）。飛びきると0になる")]
        public float LineLength = 0.45f;

        [Header("破片（隕石のかけら）")]
        [Tooltip("破片の色（最大8色）")]
        public Color[] DebrisColors =
        {
            new(0.62f, 0.42f, 0.32f, 1f),
            new(0.5f, 0.36f, 0.36f, 1f),
            new(0.72f, 0.52f, 0.34f, 1f)
        };
        [Min(0), Tooltip("破片の数")]
        public int DebrisCount = 8;
        [Tooltip("破片の大きさ（爆風半径に対する割合、最小・最大）")]
        public Vector2 DebrisSize = new(0.08f, 0.14f);
        [Tooltip("破片が消えるまでの秒数（最小・最大）")]
        public Vector2 DebrisLifetimeSeconds = new(0.6f, 0.9f);
        [Tooltip("破片が飛び出す速さ（爆風半径に対する割合／秒、最小・最大）")]
        public Vector2 DebrisSpeed = new(1.3f, 2.2f);
        [Min(0f), Tooltip("破片にかかる重力の倍率")]
        public float DebrisGravity = 2.2f;
        [Min(0f), Tooltip("破片の回転の速さ（ラジアン／秒の最大）")]
        public float DebrisSpin = 10f;

        [Header("火の粉")]
        [Tooltip("火の粉の色（最大8色）")]
        public Color[] EmberColors =
        {
            new(1f, 0.55f, 0.1f, 1f),
            new(1f, 0.85f, 0.2f, 1f),
            new(1f, 0.32f, 0.2f, 1f)
        };
        [Min(0), Tooltip("火の粉の数")]
        public int EmberCount = 10;
        [Tooltip("火の粉の大きさ（爆風半径に対する割合、最小・最大）")]
        public Vector2 EmberSize = new(0.035f, 0.06f);
        [Tooltip("火の粉が消えるまでの秒数（最小・最大）")]
        public Vector2 EmberLifetimeSeconds = new(0.45f, 0.75f);
        [Tooltip("火の粉が飛び出す速さ（爆風半径に対する割合／秒、最小・最大）")]
        public Vector2 EmberSpeed = new(1.6f, 2.6f);
        [Min(0f), Tooltip("火の粉にかかる重力の倍率")]
        public float EmberGravity = 1.6f;

        [Header("カメラシェイク")]
        [Min(0f), Tooltip("揺れの大きさ。0 で揺らさない（撃破演出は 0.3）")]
        public float ShakeAmplitude = 0.15f;
        [Min(0.01f), Tooltip("揺れが収まるまでの秒数")]
        public float ShakeSeconds = 0.3f;
        [Min(0f), Tooltip("揺れの細かさ")]
        public float ShakeFrequency = 28f;
    }

    /// <summary>
    ///     爆発1回分のエフェクト。シーン直下に生成され、再生し終わると自分で消える
    /// </summary>
    /// <remarks>
    ///     爆発した落下物はすぐ破棄されるので、エフェクトは落下物とは別のオブジェクトで動かす。
    ///     粒は Tex_Boom（2×2 のアトラス: ギザギザ・雲・破片・火の粉）のコマを種類ごとに使い分ける
    /// </remarks>
    public sealed class ExplosionBurst : MonoBehaviour
    {
        private sealed class SpeedLine
        {
            public LineRenderer Line;
            public Vector3 Direction;
            public float ReachScale;
            public float Delay;
        }

        private ExplosionEffectSettings _settings;
        private float _radius;
        private float _elapsed;
        private float _endSeconds;
        private SpeedLine[] _lines = Array.Empty<SpeedLine>();
        private Material _runtimeMaterial;
        private Transform _shakenTransform;
        private Vector3 _appliedShakeOffset;
        private float _shakeSeed;

        private const float PUFF_DRAG = 8f;
        private const float DEPTH_OFFSET = 0.1f;
        private const int MAX_GRADIENT_KEYS = 8;
        private const string FALLBACK_SHADER_NAME = "Universal Render Pipeline/Particles/Unlit";

        // Tex_Boom のコマ番号（左上から右へ、上の段から順）
        private const int FRAME_BURST = 0;
        private const int FRAME_CLOUD = 1;
        private const int FRAME_DEBRIS = 2;
        private const int FRAME_EMBER = 3;
        private const int ATLAS_TILES = 2;

        // 奥の物から順に描く（数字が大きいほど手前）
        private const int ORDER_SMOKE = 0;
        private const int ORDER_FIRE = 1;
        private const int ORDER_BURST = 2;
        private const int ORDER_LINE = 10;
        private const int ORDER_DEBRIS = 11;
        private const int ORDER_EMBER = 12;

        /// <summary>
        ///     指定位置で爆発エフェクトを1回再生する
        /// </summary>
        /// <param name="center">爆心のワールド座標</param>
        /// <param name="radius">見た目の半径（通常は爆風の当たり判定半径）</param>
        /// <param name="fanUp">地面での爆発として、粒を上向きの扇形に広げるか</param>
        /// <param name="boomMaterial">Tex_Boom を使ったパーティクル用マテリアル</param>
        /// <param name="lineMaterial">集中線に使うマテリアル</param>
        public static ExplosionBurst Spawn(Vector3 center, float radius, bool fanUp, ExplosionEffectSettings settings,
            Material boomMaterial, Material lineMaterial)
        {
            GameObject root = new("Explosion FX");
            // カメラ側へ少し出し、爆心のオブジェクトや足場にめり込みにくくする
            root.transform.position = center + Vector3.back * DEPTH_OFFSET;
            ExplosionBurst burst = root.AddComponent<ExplosionBurst>();
            burst.Play(radius, fanUp, settings ?? new ExplosionEffectSettings(), boomMaterial, lineMaterial);
            return burst;
        }

        private void Play(float radius, bool fanUp, ExplosionEffectSettings settings, Material boomMaterial,
            Material lineMaterial)
        {
            _settings = settings;
            _radius = Mathf.Max(radius, 0.1f);
            boomMaterial = ResolveMaterial(boomMaterial);
            lineMaterial = ResolveMaterial(lineMaterial);
            float arc = fanUp ? Mathf.Clamp(settings.GroundFanAngle, 90f, 360f) : 360f;
            float longest = 0f;

            // 後ろでもくもく広がって、ゆっくり残る黒煙
            if (settings.SmokeCount > 0)
            {
                ParticleSystem smoke = CreateParticles("Smoke", boomMaterial, FRAME_CLOUD, settings.SmokeColors,
                    settings.SmokeCount, settings.SmokeLifetimeSeconds, settings.SmokeSize * _radius, ORDER_SMOKE);
                SetFanShape(smoke, arc, _radius * 0.12f);
                ApplyDrag(smoke, PUFF_DRAG);
                ApplyRise(smoke, settings.SmokeRiseSpeed);
                ApplyPopSize(smoke, settings.SmokePopScale);
                ApplySpin(smoke, 0.6f);
                ApplyFade(smoke, 0.75f);
                EmitWithSpeed(smoke, settings.SmokeCount, settings.SmokeReach * (_radius * PUFF_DRAG));
                longest = Mathf.Max(longest, settings.SmokeLifetimeSeconds.y);
            }

            // 中心で膨らむオレンジの雲。ドカンが消えたあとの火の名残
            if (settings.FireCount > 0)
            {
                ParticleSystem fire = CreateParticles("Fire", boomMaterial, FRAME_CLOUD, settings.FireColors,
                    settings.FireCount, settings.FireLifetimeSeconds, settings.FireSize * _radius, ORDER_FIRE);
                SetFanShape(fire, 360f, _radius * 0.08f);
                ApplyDrag(fire, PUFF_DRAG);
                ApplyRise(fire, settings.SmokeRiseSpeed * 0.5f);
                ApplyPopSize(fire, 1.35f);
                ApplySpin(fire, 1f);
                ApplyFade(fire, 0.8f);
                EmitWithSpeed(fire, settings.FireCount, settings.FireReach * (_radius * PUFF_DRAG));
                longest = Mathf.Max(longest, settings.FireLifetimeSeconds.y);
            }

            // 赤・オレンジ・黄のギザギザを重ねた、マンガの「ドカン」
            int burstLayers = Mathf.Min(settings.BurstColors?.Length ?? 0, settings.BurstSizes?.Length ?? 0);
            for (int i = 0; i < burstLayers; i++)
            {
                float size = settings.BurstSizes[i] * _radius;
                // 外側の層ほど少し長く残り、消えていくときに縁取りが見える
                float seconds = settings.BurstSeconds * Mathf.Lerp(1.15f, 0.85f, burstLayers > 1 ? i / (burstLayers - 1f) : 0f);
                ParticleSystem layer = CreateParticles($"Burst {i}", boomMaterial, FRAME_BURST,
                    new[] { settings.BurstColors[i] }, 1, new Vector2(seconds, seconds), new Vector2(size, size),
                    ORDER_BURST + i);
                ParticleSystem.ShapeModule shape = layer.shape;
                shape.enabled = false;
                float popScale = Mathf.Max(settings.BurstPopScale, 1f);
                ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = layer.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(popScale, new AnimationCurve(
                    new Keyframe(0f, 0.45f / popScale),
                    new Keyframe(0.18f, 1f),
                    new Keyframe(0.45f, 1f / popScale),
                    new Keyframe(1f, 0f)));
                ApplySpin(layer, 1.2f);
                EmitWithSpeed(layer, 1, Vector2.zero);
                longest = Mathf.Max(longest, seconds);
            }

            // 放物線を描いて落ちる隕石のかけら
            if (settings.DebrisCount > 0)
            {
                ParticleSystem debris = CreateParticles("Debris", boomMaterial, FRAME_DEBRIS, settings.DebrisColors,
                    settings.DebrisCount, settings.DebrisLifetimeSeconds, settings.DebrisSize * _radius,
                    ORDER_DEBRIS);
                SetFanShape(debris, Mathf.Min(arc, 300f), _radius * 0.1f);
                ParticleSystem.MainModule main = debris.main;
                main.gravityModifier = settings.DebrisGravity;
                ApplySpin(debris, settings.DebrisSpin);
                ApplyFade(debris, 0.85f);
                EmitWithSpeed(debris, settings.DebrisCount, settings.DebrisSpeed * _radius);
                longest = Mathf.Max(longest, settings.DebrisLifetimeSeconds.y);
            }

            // 小さく弾ける火の粉
            if (settings.EmberCount > 0)
            {
                ParticleSystem embers = CreateParticles("Ember", boomMaterial, FRAME_EMBER, settings.EmberColors,
                    settings.EmberCount, settings.EmberLifetimeSeconds, settings.EmberSize * _radius, ORDER_EMBER);
                SetFanShape(embers, Mathf.Min(arc, 300f), _radius * 0.1f);
                ParticleSystem.MainModule main = embers.main;
                main.gravityModifier = settings.EmberGravity;
                ApplyShrink(embers);
                ApplyFade(embers, 0.85f);
                EmitWithSpeed(embers, settings.EmberCount, settings.EmberSpeed * _radius);
                longest = Mathf.Max(longest, settings.EmberLifetimeSeconds.y);
            }

            CreateSpeedLines(lineMaterial, arc);
            longest = Mathf.Max(longest, settings.LineSeconds * 1.3f);
            StartShake();
            longest = Mathf.Max(longest, settings.ShakeAmplitude > 0f ? settings.ShakeSeconds : 0f);
            _endSeconds = longest + 0.1f;
            UpdateSpeedLines();
        }

        private void LateUpdate()
        {
            if (_settings == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            UpdateSpeedLines();
            UpdateShake();
            if (_elapsed >= _endSeconds)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            RemoveShakeOffset();
            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
            }
        }

        /// <summary>
        ///     爆心から外へ飛んでいく、マンガの集中線のような衝撃波を作る
        /// </summary>
        private void CreateSpeedLines(Material material, float arc)
        {
            int count = _settings.LineCount;
            if (count <= 0 || _settings.LineWidth <= 0f)
            {
                return;
            }

            _lines = new SpeedLine[count];
            float startAngle = 90f - arc * 0.5f;
            float step = arc / count;
            for (int i = 0; i < count; i++)
            {
                GameObject lineObject = new($"Speed Line {i}");
                lineObject.transform.SetParent(transform, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.enabled = false;
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.numCapVertices = 0;
                line.textureMode = LineTextureMode.Stretch;
                line.alignment = LineAlignment.View;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sharedMaterial = material;
                line.sortingOrder = ORDER_LINE;
                // 内側が細く、外側が太いくさび形
                line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(1f, 1f));
                Color color = _settings.LineColors is { Length: > 0 }
                    ? _settings.LineColors[i % _settings.LineColors.Length]
                    : Color.white;
                line.startColor = color;
                line.endColor = color;

                // 等間隔から少しずらして、手描きっぽい不揃いさを出す
                float angle = (startAngle + step * (i + 0.5f + UnityEngine.Random.Range(-0.3f, 0.3f))) * Mathf.Deg2Rad;
                _lines[i] = new SpeedLine
                {
                    Line = line,
                    Direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f),
                    ReachScale = UnityEngine.Random.Range(0.8f, 1.05f),
                    Delay = UnityEngine.Random.Range(0f, 0.04f)
                };
            }
        }

        private void UpdateSpeedLines()
        {
            Vector3 center = transform.position;
            foreach (SpeedLine speedLine in _lines)
            {
                LineRenderer line = speedLine.Line;
                float t = (_elapsed - speedLine.Delay) / _settings.LineSeconds;
                line.enabled = t >= 0f && t <= 1f;
                if (!line.enabled)
                {
                    continue;
                }

                // 外側の端は勢いよく飛び、内側の端が追いついて線が縮んで消える
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float outer = Mathf.Lerp(_settings.LineReach.x, _settings.LineReach.y, eased) * speedLine.ReachScale;
                float length = _settings.LineLength * (1f - t);
                float inner = Mathf.Max(outer - length, 0f);
                line.SetPosition(0, center + speedLine.Direction * (inner * _radius));
                line.SetPosition(1, center + speedLine.Direction * (outer * _radius));
                line.widthMultiplier = _settings.LineWidth * _radius * Mathf.Lerp(1f, 0.4f, t);
            }
        }

        /// <summary>
        ///     カメラを揺らす。他の演出の揺れとぶつからないよう、自分が足した分だけを毎フレーム戻して足し直す
        /// </summary>
        private void StartShake()
        {
            Camera cam = Camera.main;
            if (cam == null || _settings.ShakeAmplitude <= 0f)
            {
                return;
            }

            _shakenTransform = cam.transform;
            _shakeSeed = UnityEngine.Random.value * 100f;
        }

        private void UpdateShake()
        {
            if (_shakenTransform == null)
            {
                return;
            }

            RemoveShakeOffset();
            float t = _elapsed / _settings.ShakeSeconds;
            if (t >= 1f)
            {
                _shakenTransform = null;
                return;
            }

            float decay = (1f - t) * (1f - t);
            float time = _elapsed * _settings.ShakeFrequency;
            float x = Mathf.PerlinNoise(_shakeSeed, time) * 2f - 1f;
            float y = Mathf.PerlinNoise(_shakeSeed + 37f, time) * 2f - 1f;
            Vector3 offset = (_shakenTransform.right * x + _shakenTransform.up * y) *
                             (_settings.ShakeAmplitude * decay);
            _shakenTransform.position += offset;
            _appliedShakeOffset = offset;
        }

        private void RemoveShakeOffset()
        {
            if (_shakenTransform != null)
            {
                _shakenTransform.position -= _appliedShakeOffset;
            }

            _appliedShakeOffset = Vector3.zero;
        }

        /// <summary>
        ///     全種類の粒に共通する設定で ParticleSystem を作る。frame で Tex_Boom のどのコマを使うか決める
        /// </summary>
        private ParticleSystem CreateParticles(string label, Material material, int frame, Color[] colors,
            int maxParticles, Vector2 lifetime, Vector2 size, int sortingOrder)
        {
            GameObject particleObject = new(label);
            particleObject.transform.SetParent(transform, false);
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.maxParticles = Mathf.Max(maxParticles, 1);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = CreateRandomColor(colors);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;

            // アトラスの1コマだけを、寿命の間ずっと表示する
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
            particleRenderer.sortingOrder = sortingOrder;
            return particles;
        }

        /// <summary>
        ///     画面と同じ XY 平面で、上向きを中心にした扇形（360 で全方向）へ飛ばす
        /// </summary>
        private static void SetFanShape(ParticleSystem particles, float arc, float radius)
        {
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Mathf.Max(radius, 0.01f);
            shape.radiusThickness = 1f;
            shape.arc = arc;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
            // 円の扇は +X から反時計回りに広がるので、真上が扇の中心になるよう回す
            shape.rotation = new Vector3(0f, 0f, 90f - arc * 0.5f);
        }

        private static void EmitWithSpeed(ParticleSystem particles, int count, Vector2 speedRange)
        {
            ParticleSystem.MainModule main = particles.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
            particles.Emit(count);
        }

        /// <summary>
        ///     速度の上限はかけず、空気抵抗だけで減速させる（止まる距離はおよそ 初速÷抵抗）
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

        private static void ApplyRise(ParticleSystem particles, float riseSpeed)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Unityは3軸に同じ速度モードを要求するため、動かさない軸も上下限を0にした範囲で指定する
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(riseSpeed * 0.5f, riseSpeed);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        /// <summary>
        ///     小さく出て一気に膨らみ、少し戻ってから縮んで消える
        /// </summary>
        private static void ApplyPopSize(ParticleSystem particles, float popScale)
        {
            popScale = Mathf.Max(popScale, 1f);
            AnimationCurve popCurve = new(
                new Keyframe(0f, 0.35f / popScale),
                new Keyframe(0.15f, 1f),
                new Keyframe(0.4f, 1f / popScale),
                new Keyframe(1f, 0f));
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(popScale, popCurve);
        }

        /// <summary>
        ///     一瞬大きくなってから、だんだん小さくなる
        /// </summary>
        private static void ApplyShrink(ParticleSystem particles)
        {
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.2f,
                new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.1f, 1f), new Keyframe(1f, 0f)));
        }

        private static void ApplySpin(ParticleSystem particles, float maxRadiansPerSecond)
        {
            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-maxRadiansPerSecond, maxRadiansPerSecond);
        }

        /// <summary>
        ///     指定した割合の時点まではくっきり表示し、最後だけ薄くして消す
        /// </summary>
        private static void ApplyFade(ParticleSystem particles, float fadeStart)
        {
            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
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

            if (colors.Length == 1)
            {
                return new ParticleSystem.MinMaxGradient(colors[0]);
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
        ///     マテリアル未設定の場合は URP のパーティクル用シェーダーから仮のものを作る
        /// </summary>
        private Material ResolveMaterial(Material material)
        {
            if (material != null)
            {
                return material;
            }

            if (_runtimeMaterial == null)
            {
                Shader shader = Shader.Find(FALLBACK_SHADER_NAME);
                if (shader == null)
                {
                    Debug.LogWarning($"{nameof(ExplosionBurst)}: マテリアルが未設定で、{FALLBACK_SHADER_NAME} も見つかりません");
                    return null;
                }

                Debug.LogWarning($"{nameof(ExplosionBurst)}: マテリアルが未設定のため仮のマテリアルを使います");
                _runtimeMaterial = new Material(shader);
            }

            return _runtimeMaterial;
        }
    }
}
