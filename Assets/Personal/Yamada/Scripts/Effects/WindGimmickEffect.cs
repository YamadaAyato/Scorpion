using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Scorpion.Effects
{
    /// <summary>
    ///     強風エフェクトの見た目の設定
    /// </summary>
    [Serializable]
    public sealed class WindEffectSettings
    {
        [Header("全体")]
        [Min(0.01f), Tooltip("風が吹き始めてから、エフェクトが出そろうまでの秒数")]
        public float FadeInSeconds = 0.35f;
        [Min(0.01f), Tooltip("風が止んでから、風の線が消えきるまでの秒数（葉っぱは飛び去るまで残る）")]
        public float FadeOutSeconds = 0.6f;
        [Tooltip("風の強さによる量・速さの倍率の下限と上限。強さ = WindCollider の風の強さ ÷ 基準の強さ")]
        public Vector2 StrengthClamp = new(0.6f, 1.8f);
        [Range(0f, 1f), Tooltip("突風っぽいムラの強さ。0 で一定、1 で量が大きく波打つ")]
        public float Gustiness = 0.45f;
        [Min(0.01f), Tooltip("突風のムラの速さ（1秒あたりの揺れ）")]
        public float GustFrequency = 0.7f;
        [Min(0f), Tooltip("画面の外のどれだけ手前から出し、どれだけ先まで飛ばすか")]
        public float OffscreenMargin = 1.5f;

        [Header("風の線（くるんと巻く線）")]
        [Min(0f), Tooltip("1秒あたりに出す風の線の本数（強さ1のとき）")]
        public float StreaksPerSecond = 4f;
        [Min(1), Tooltip("同時に出せる風の線の最大本数")]
        public int MaxStreaks = 12;
        [Min(0), Tooltip("吹き始めに一度に出す線の本数")]
        public int GustBurstCount = 4;
        [Tooltip("線の色（ランダムに使う）。白〜薄いミント・水色")]
        public Color[] StreakColors =
        {
            new(1f, 1f, 1f, 0.9f),
            new(0.72f, 0.95f, 0.88f, 0.9f),
            new(0.72f, 0.88f, 1f, 0.9f)
        };
        [Min(0f), Tooltip("線の太さ（ワールド単位）")]
        public float StreakWidth = 0.11f;
        [Tooltip("線の長さ（ワールド単位、最小・最大）")]
        public Vector2 StreakLength = new(2.6f, 4.2f);
        [Tooltip("線が流れる速さ（ワールド単位／秒、最小・最大）")]
        public Vector2 StreakSpeed = new(13f, 17f);
        [Tooltip("線のうねりの高さ（最小・最大）")]
        public Vector2 WaveAmplitude = new(0.1f, 0.28f);
        [Tooltip("線のうねりの波長（最小・最大）")]
        public Vector2 WaveLength = new(3.5f, 6.5f);
        [Range(0f, 1f), Tooltip("線が途中でくるんと渦を巻く確率")]
        public float CurlChance = 0.4f;
        [Tooltip("渦の半径（最小・最大）")]
        public Vector2 CurlRadius = new(0.32f, 0.5f);
        [Tooltip("線を置く奥行き（Z、最小・最大）。プラスほど奥。キャラ（Z=0）より奥にしてプレイの邪魔をしない")]
        public Vector2 StreakDepth = new(0.8f, 3f);
        [Range(0f, 1f), Tooltip("線を置く高さの範囲（画面の高さに対する割合）")]
        public float StreakHeightRange = 0.85f;

        [Header("舞う葉っぱ")]
        [Min(0f), Tooltip("1秒あたりに出す葉っぱの数（強さ1のとき）")]
        public float LeavesPerSecond = 3.5f;
        [Tooltip("葉っぱの色（最大8色）。秋っぽいオレンジ・赤・黄・黄緑")]
        public Color[] LeafColors =
        {
            new(1f, 0.58f, 0.2f, 1f),
            new(0.96f, 0.36f, 0.3f, 1f),
            new(1f, 0.82f, 0.22f, 1f),
            new(0.62f, 0.84f, 0.3f, 1f)
        };
        [Tooltip("葉っぱの大きさ（最小・最大）")]
        public Vector2 LeafSize = new(0.32f, 0.5f);
        [Tooltip("葉っぱが飛ぶ速さ（最小・最大）")]
        public Vector2 LeafSpeed = new(8f, 12f);
        [Min(0f), Tooltip("葉っぱのひらひら（ふらつき）の強さ")]
        public float LeafFlutter = 1.6f;
        [Min(0f), Tooltip("葉っぱの回転の速さ（ラジアン／秒の最大）")]
        public float LeafSpin = 5f;
        [Tooltip("葉っぱを置く奥行き（Z、最小・最大）。少しだけ手前（マイナス）も通る")]
        public Vector2 LeafDepth = new(-0.8f, 2.5f);

        [Header("飛ぶ粒")]
        [Min(0f), Tooltip("1秒あたりに出す粒の数（強さ1のとき）")]
        public float GrainsPerSecond = 14f;
        [Tooltip("粒の色（最大8色）")]
        public Color[] GrainColors =
        {
            new(1f, 1f, 0.96f, 0.9f),
            new(1f, 0.93f, 0.7f, 0.9f),
            new(0.8f, 0.95f, 1f, 0.9f)
        };
        [Tooltip("粒の大きさ（最小・最大）")]
        public Vector2 GrainSize = new(0.07f, 0.12f);
        [Tooltip("粒が飛ぶ速さ（最小・最大）")]
        public Vector2 GrainSpeed = new(15f, 21f);
        [Min(0f), Tooltip("粒が速さの向きに伸びる長さの倍率")]
        public float GrainStretch = 2.2f;
        [Tooltip("粒を置く奥行き（Z、最小・最大）")]
        public Vector2 GrainDepth = new(-0.5f, 3f);
    }

    /// <summary>
    ///     強風ギミック（WindCollider）が吹いている間、風向きと強さに合わせた風エフェクトを画面全体に出す
    /// </summary>
    /// <remarks>
    ///     WindCollider と StageGimmickTargets は変更しない。ステージは試合中に生成・破棄されるので、
    ///     シーン内の WindCollider を定期的に探し、そのオブジェクトが有効になっている間を「風が吹いている」とみなす。
    ///     風向き（_direction）と強さ（_windForce）は WindCollider の値をリフレクションで読む。
    ///     シーンに1つ置けば、どのステージの風にも反応する。
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class WindGimmickEffect : MonoBehaviour
    {
        private sealed class Streak
        {
            public LineRenderer Line;
            public readonly List<Vector3> Path = new();
            public float Head;
            public float Length;
            public float Speed;
            public float PathLength;
            public Color Color;
            public bool IsActive;
        }

        [Header("マテリアル")]
        [SerializeField, Tooltip("風の線に使うマテリアル（Tex_WindStreak）")]
        private Material _streakMaterial;
        [SerializeField, Tooltip("葉っぱと粒に使うマテリアル（Tex_WindLeaf の 2×2 アトラス）")]
        private Material _leafMaterial;

        [Header("風の検知")]
        [SerializeField, Min(0.01f), Tooltip("この強さ（WindCollider の風の強さ）のとき、量・速さが設定どおりになる")]
        private float _referenceForce = 20f;
        [SerializeField, Min(0.1f), Tooltip("シーン内の WindCollider を探し直す間隔（秒）")]
        private float _scanInterval = 0.5f;

        [Header("テスト")]
        [SerializeField, Tooltip("エディタと開発ビルドで、キーを押すとエフェクトだけを再生する（風の力は出ない）")]
        private bool _enablePreviewKey = true;
#if ENABLE_INPUT_SYSTEM
        [SerializeField, Tooltip("テスト再生のキー。押すたびに風向きが左右入れ替わる")]
        private Key _previewKey = Key.F7;
#endif
        [SerializeField, Min(0.1f), Tooltip("テスト再生の秒数（強風ギミックの継続時間は4秒）")]
        private float _previewSeconds = 4f;
        [SerializeField, Min(0.01f), Tooltip("テスト再生のときの風の強さ")]
        private float _previewForce = 20f;

        [SerializeField]
        private WindEffectSettings _settings = new();

        private readonly List<WindCollider> _winds = new();
        private readonly List<Streak> _streaks = new();
        private Vector3[] _pointBuffer = new Vector3[64];
        private ParticleSystem _leaves;
        private ParticleSystem _grains;
        private Material _runtimeMaterial;
        private Camera _camera;
        private float _scanTimer;
        private float _blend;
        private float _strength = 1f;
        private int _direction = 1;
        private bool _wasBlowing;
        private float _streakBudget;
        private float _leafBudget;
        private float _grainBudget;
        private float _gustSeed;
        private float _previewRemaining;
        private int _previewDirection = -1;

        private static readonly FieldInfo DIRECTION_FIELD =
            typeof(WindCollider).GetField("_direction", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FORCE_FIELD =
            typeof(WindCollider).GetField("_windForce", BindingFlags.Instance | BindingFlags.NonPublic);

        private const float PATH_STEP = 0.12f;
        private const string FALLBACK_SHADER_NAME = "Universal Render Pipeline/Particles/Unlit";

        // Tex_WindLeaf のコマ（左上から右へ、上の段から順）。0〜2 が葉っぱ、3 が粒
        private const int ATLAS_TILES = 2;
        private const int LEAF_FRAME_COUNT = 3;
        private const int FRAME_GRAIN = 3;

        /// <summary> 今エフェクトを出しているか（フェード中も含む） </summary>
        public bool IsVisible => _blend > 0.001f;

        /// <summary>
        ///     風の力は出さずに、エフェクトだけを一定時間再生する
        /// </summary>
        /// <param name="direction">風向き。1 で右へ、-1 で左へ</param>
        public void PlayPreview(int direction)
        {
            _previewDirection = direction < 0 ? -1 : 1;
            _previewRemaining = _previewSeconds;
        }

        [ContextMenu("テスト再生（右向き）")]
        private void PlayPreviewRight()
        {
            PlayPreviewFromMenu(1);
        }

        [ContextMenu("テスト再生（左向き）")]
        private void PlayPreviewLeft()
        {
            PlayPreviewFromMenu(-1);
        }

        private void PlayPreviewFromMenu(int direction)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{nameof(WindGimmickEffect)}: テスト再生はプレイ中に行ってください", this);
                return;
            }

            PlayPreview(direction);
        }

        private void Awake()
        {
            _gustSeed = UnityEngine.Random.value * 100f;
            Material leafMaterial = ResolveMaterial(_leafMaterial);
            _leaves = CreateParticles("Wind Leaves", leafMaterial, 128, 2);
            _grains = CreateParticles("Wind Grains", leafMaterial, 128, 1);
            SetupLeaves();
            SetupGrains();
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
            }
        }

        private void Update()
        {
            UpdatePreviewKey();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            bool isBlowing = ReadWind(dt, out int direction, out float force);
            if (isBlowing)
            {
                _direction = direction;
                Vector2 clamp = _settings.StrengthClamp;
                _strength = Mathf.Clamp(force / _referenceForce, clamp.x, Mathf.Max(clamp.x, clamp.y));
            }

            float fadeSeconds = isBlowing ? _settings.FadeInSeconds : _settings.FadeOutSeconds;
            _blend = Mathf.MoveTowards(_blend, isBlowing ? 1f : 0f, dt / Mathf.Max(fadeSeconds, 0.01f));

            if (!TryGetCamera(out Camera cam))
            {
                return;
            }

            // 吹き始めに数本まとめて出し、風が来たことをはっきり見せる
            if (isBlowing && !_wasBlowing)
            {
                for (int i = 0; i < _settings.GustBurstCount; i++)
                {
                    SpawnStreak(cam, UnityEngine.Random.Range(0f, 0.6f));
                }
            }

            _wasBlowing = isBlowing;
            if (isBlowing)
            {
                float rate = _strength * GetGust();
                EmitStreaks(cam, dt * rate * _settings.StreaksPerSecond);
                EmitLeaves(cam, dt * rate * _settings.LeavesPerSecond * _blend);
                EmitGrains(cam, dt * rate * _settings.GrainsPerSecond * _blend);
            }

            UpdateStreaks(dt);
        }

        /// <summary>
        ///     シーン内で吹いている風を探し、風向きと強さを返す。テスト再生中はその値を返す
        /// </summary>
        private bool ReadWind(float dt, out int direction, out float force)
        {
            if (_previewRemaining > 0f)
            {
                _previewRemaining -= dt;
                direction = _previewDirection;
                force = _previewForce;
                return true;
            }

            _scanTimer -= dt;
            if (_scanTimer <= 0f)
            {
                _scanTimer = _scanInterval;
                _winds.Clear();
                _winds.AddRange(FindObjectsByType<WindCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            }

            foreach (WindCollider wind in _winds)
            {
                if (wind == null || !wind.isActiveAndEnabled)
                {
                    continue;
                }

                // WindCollider は有効になった最初のフレームの Start で向きを決めるので、0 の間はまだ待つ
                int windDirection = DIRECTION_FIELD != null && DIRECTION_FIELD.GetValue(wind) is int value ? value : 1;
                if (windDirection == 0)
                {
                    continue;
                }

                direction = windDirection > 0 ? 1 : -1;
                force = FORCE_FIELD != null && FORCE_FIELD.GetValue(wind) is float windForce
                    ? Mathf.Abs(windForce)
                    : _referenceForce;
                return true;
            }

            direction = _direction;
            force = 0f;
            return false;
        }

        private void UpdatePreviewKey()
        {
#if ENABLE_INPUT_SYSTEM
            if (!_enablePreviewKey || !(Application.isEditor || Debug.isDebugBuild))
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _previewKey == Key.None || !keyboard[_previewKey].wasPressedThisFrame)
            {
                return;
            }

            // 1回目は右向き、押すたびに左右を入れ替える
            PlayPreview(-_previewDirection);
#endif
        }

        /// <summary>
        ///     0〜2 くらいで波打つ突風のムラ。平均はおよそ 1
        /// </summary>
        private float GetGust()
        {
            float noise = Mathf.PerlinNoise(_gustSeed, Time.time * _settings.GustFrequency) * 2f - 1f;
            return Mathf.Max(0.1f, 1f + noise * _settings.Gustiness * 1.6f);
        }

        private bool TryGetCamera(out Camera cam)
        {
            if (_camera == null || !_camera.isActiveAndEnabled)
            {
                _camera = Camera.main;
            }

            cam = _camera;
            return cam != null;
        }

        /// <summary>
        ///     指定した奥行き（Z）の平面で、カメラに映る範囲の中心と半分の大きさを求める
        /// </summary>
        private static void GetViewRect(Camera cam, float depth, out Vector2 center, out Vector2 half)
        {
            Vector3 camPos = cam.transform.position;
            center = new Vector2(camPos.x, camPos.y);
            float halfHeight = cam.orthographic
                ? cam.orthographicSize
                : Mathf.Max(depth - camPos.z, 0.5f) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            half = new Vector2(halfHeight * cam.aspect, halfHeight);
        }

        // ───────── 風の線 ─────────

        private void EmitStreaks(Camera cam, float amount)
        {
            _streakBudget += amount;
            while (_streakBudget >= 1f)
            {
                _streakBudget -= 1f;
                SpawnStreak(cam, 0f);
            }
        }

        /// <summary>
        ///     画面の風上の外から風下の外まで流れる線を1本出す
        /// </summary>
        /// <param name="headStart">最初から進んでいる割合（吹き始めに画面の途中から出すため）</param>
        private void SpawnStreak(Camera cam, float headStart)
        {
            Streak streak = GetFreeStreak();
            if (streak == null)
            {
                return;
            }

            WindEffectSettings s = _settings;
            float depth = RandomRange(s.StreakDepth);
            GetViewRect(cam, depth, out Vector2 center, out Vector2 half);
            float speedScale = Mathf.Lerp(1f, _strength, 0.5f);
            streak.Length = Mathf.Min(RandomRange(s.StreakLength), PATH_STEP * (_pointBuffer.Length - 3));
            streak.Speed = RandomRange(s.StreakSpeed) * speedScale;
            float y = center.y + UnityEngine.Random.Range(-1f, 1f) * half.y * s.StreakHeightRange;
            float travel = half.x * 2f + s.OffscreenMargin * 2f + streak.Length;
            Vector3 start = new(center.x - _direction * (half.x + s.OffscreenMargin), y, depth);
            BuildPath(streak.Path, start, travel);
            streak.PathLength = (streak.Path.Count - 1) * PATH_STEP;
            streak.Head = streak.PathLength * Mathf.Clamp01(headStart);
            streak.Color = s.StreakColors is { Length: > 0 }
                ? s.StreakColors[UnityEngine.Random.Range(0, s.StreakColors.Length)]
                : Color.white;
            streak.IsActive = true;
            streak.Line.widthMultiplier = s.StreakWidth;
        }

        /// <summary>
        ///     ゆるくうねりながら進み、ときどき途中でくるんと一回転する道筋を、ほぼ等間隔の点で作る
        /// </summary>
        private void BuildPath(List<Vector3> path, Vector3 start, float travel)
        {
            WindEffectSettings s = _settings;
            path.Clear();
            float amplitude = RandomRange(s.WaveAmplitude);
            float waveLength = Mathf.Max(RandomRange(s.WaveLength), 0.5f);
            float phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            bool curl = UnityEngine.Random.value < s.CurlChance;
            // 渦は画面の中ほど（道のりの 35〜65%）で巻く
            float curlAt = travel * UnityEngine.Random.Range(0.35f, 0.65f);
            float curlRadius = RandomRange(s.CurlRadius);

            float forward = 0f;
            bool curled = false;
            while (forward <= travel)
            {
                float wave = amplitude * Mathf.Sin(forward / waveLength * Mathf.PI * 2f + phase);
                path.Add(ToWorld(start, forward, wave));

                if (curl && !curled && forward >= curlAt)
                {
                    curled = true;
                    // 下から入って上へ回り、少し前へずれながら一周する（バネのような「くるん」）
                    int steps = Mathf.Max(8, Mathf.CeilToInt(curlRadius * Mathf.PI * 2f / PATH_STEP));
                    float drift = curlRadius * 0.7f;
                    for (int i = 1; i <= steps; i++)
                    {
                        float t = i / (float)steps;
                        float angle = -Mathf.PI * 0.5f + t * Mathf.PI * 2f;
                        float fx = forward + Mathf.Cos(angle) * curlRadius + drift * t;
                        float fy = wave + curlRadius + Mathf.Sin(angle) * curlRadius;
                        path.Add(ToWorld(start, fx, fy));
                    }

                    forward += drift;
                }

                forward += PATH_STEP;
            }
        }

        private Vector3 ToWorld(Vector3 start, float forward, float up)
        {
            return new Vector3(start.x + forward * _direction, start.y + up, start.z);
        }

        private void UpdateStreaks(float dt)
        {
            foreach (Streak streak in _streaks)
            {
                if (!streak.IsActive)
                {
                    continue;
                }

                streak.Head += streak.Speed * dt;
                float tail = streak.Head - streak.Length;
                if (tail >= streak.PathLength || _blend <= 0f)
                {
                    streak.IsActive = false;
                    streak.Line.enabled = false;
                    continue;
                }

                int count = SamplePath(streak, Mathf.Max(tail, 0f), Mathf.Min(streak.Head, streak.PathLength));
                streak.Line.enabled = count >= 2;
                if (count < 2)
                {
                    continue;
                }

                streak.Line.positionCount = count;
                streak.Line.SetPositions(_pointBuffer);
                Color head = streak.Color;
                head.a *= _blend;
                Color tailColor = head;
                tailColor.a *= 0.35f;
                streak.Line.startColor = tailColor;
                streak.Line.endColor = head;
            }
        }

        /// <summary>
        ///     道筋の from〜to の区間を点の列にして _pointBuffer に入れ、点の数を返す
        /// </summary>
        private int SamplePath(Streak streak, float from, float to)
        {
            List<Vector3> path = streak.Path;
            if (to - from < PATH_STEP * 0.5f)
            {
                return 0;
            }

            int count = 0;
            _pointBuffer[count++] = SampleAt(path, from);
            int first = Mathf.FloorToInt(from / PATH_STEP) + 1;
            int last = Mathf.CeilToInt(to / PATH_STEP) - 1;
            for (int i = first; i <= last && count < _pointBuffer.Length - 1; i++)
            {
                _pointBuffer[count++] = path[Mathf.Clamp(i, 0, path.Count - 1)];
            }

            _pointBuffer[count++] = SampleAt(path, to);
            return count;
        }

        private static Vector3 SampleAt(List<Vector3> path, float distance)
        {
            float index = distance / PATH_STEP;
            int i = Mathf.Clamp(Mathf.FloorToInt(index), 0, path.Count - 1);
            int j = Mathf.Min(i + 1, path.Count - 1);
            return Vector3.Lerp(path[i], path[j], index - i);
        }

        private Streak GetFreeStreak()
        {
            foreach (Streak streak in _streaks)
            {
                if (!streak.IsActive)
                {
                    return streak;
                }
            }

            if (_streaks.Count >= Mathf.Max(_settings.MaxStreaks, 1))
            {
                return null;
            }

            GameObject lineObject = new($"Wind Streak {_streaks.Count}");
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.enabled = false;
            line.useWorldSpace = true;
            line.numCapVertices = 0;
            line.numCornerVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = ResolveMaterial(_streakMaterial);
            // 尾は細く、頭に向かって太くなり、先端だけ少し細める
            line.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.35f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0.8f));
            Streak created = new() { Line = line };
            _streaks.Add(created);
            return created;
        }

        // ───────── 葉っぱと粒 ─────────

        private void SetupLeaves()
        {
            WindEffectSettings s = _settings;
            ParticleSystem.MainModule main = _leaves.main;
            main.startColor = CreateRandomColor(s.LeafColors);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            // 0〜2 番のコマからランダムに1つ選び、寿命の間ずっと表示する
            ParticleSystem.TextureSheetAnimationModule sheet = _leaves.textureSheetAnimation;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, (LEAF_FRAME_COUNT - 0.01f) / (ATLAS_TILES * ATLAS_TILES));

            // ひらひら: 横幅だけ伸び縮みさせて、裏返るように見せる
            ParticleSystem.SizeOverLifetimeModule size = _leaves.sizeOverLifetime;
            size.enabled = true;
            size.separateAxes = true;
            size.x = new ParticleSystem.MinMaxCurve(1f, CreateFlipCurve(0f), CreateFlipCurve(0.5f));
            size.y = new ParticleSystem.MinMaxCurve(1f);
            size.z = new ParticleSystem.MinMaxCurve(1f);

            ParticleSystem.RotationOverLifetimeModule rotation = _leaves.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-s.LeafSpin, s.LeafSpin);

            ApplyNoise(_leaves, s.LeafFlutter, 0.45f);
            ApplyEdgeFade(_leaves);
        }

        private void SetupGrains()
        {
            WindEffectSettings s = _settings;
            ParticleSystem.MainModule main = _grains.main;
            main.startColor = CreateRandomColor(s.GrainColors);

            ParticleSystem.TextureSheetAnimationModule sheet = _grains.textureSheetAnimation;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve((FRAME_GRAIN + 0.5f) / (ATLAS_TILES * ATLAS_TILES));

            // 速さの向きに細長く伸ばして、びゅっと飛ぶ粒にする
            ParticleSystemRenderer particleRenderer = _grains.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.lengthScale = Mathf.Max(s.GrainStretch, 1f);
            particleRenderer.velocityScale = 0f;
            particleRenderer.cameraVelocityScale = 0f;

            ApplyNoise(_grains, s.LeafFlutter * 0.3f, 0.6f);
            ApplyEdgeFade(_grains);
        }

        private void EmitLeaves(Camera cam, float amount)
        {
            WindEffectSettings s = _settings;
            _leafBudget += amount;
            while (_leafBudget >= 1f)
            {
                _leafBudget -= 1f;
                EmitAcross(cam, _leaves, s.LeafDepth, s.LeafSpeed, RandomRange(s.LeafSize), 1.2f);
            }
        }

        private void EmitGrains(Camera cam, float amount)
        {
            WindEffectSettings s = _settings;
            _grainBudget += amount;
            while (_grainBudget >= 1f)
            {
                _grainBudget -= 1f;
                EmitAcross(cam, _grains, s.GrainDepth, s.GrainSpeed, RandomRange(s.GrainSize), 0.5f);
            }
        }

        /// <summary>
        ///     画面の風上の外から1粒出し、風下の外へ抜けるまでの寿命を付ける
        /// </summary>
        private void EmitAcross(Camera cam, ParticleSystem particles, Vector2 depthRange, Vector2 speedRange,
            float size, float verticalJitter)
        {
            float depth = RandomRange(depthRange);
            GetViewRect(cam, depth, out Vector2 center, out Vector2 half);
            float margin = _settings.OffscreenMargin;
            float speed = RandomRange(speedRange) * Mathf.Lerp(1f, _strength, 0.5f);
            float startX = center.x - _direction * (half.x + margin * UnityEngine.Random.Range(0.6f, 1f));
            float y = center.y + UnityEngine.Random.Range(-1f, 1f) * half.y * 0.95f;
            Vector3 velocity = new(_direction * speed, UnityEngine.Random.Range(-verticalJitter, verticalJitter), 0f);
            float distance = half.x * 2f + margin * 2f;

            ParticleSystem.EmitParams emit = new()
            {
                position = new Vector3(startX, y, depth),
                velocity = velocity,
                startSize = size,
                startLifetime = distance / Mathf.Max(speed, 0.1f)
            };
            particles.Emit(emit, 1);
        }

        private ParticleSystem CreateParticles(string label, Material material, int maxParticles, int sortingOrder)
        {
            GameObject particleObject = new(label);
            particleObject.transform.SetParent(transform, false);
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startSpeed = 0f;

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

            ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortMode = ParticleSystemSortMode.Distance;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particleRenderer.sharedMaterial = material;
            particleRenderer.sortingOrder = sortingOrder;
            particles.Play();
            return particles;
        }

        /// <summary>
        ///     横幅が 1 → 細く → 1 を繰り返す曲線。phase をずらした2本の間でランダムにして、そろわないようにする
        /// </summary>
        private static AnimationCurve CreateFlipCurve(float phase)
        {
            const int keys = 17;
            const float flips = 5f;
            Keyframe[] frames = new Keyframe[keys];
            for (int i = 0; i < keys; i++)
            {
                float t = i / (keys - 1f);
                float wave = Mathf.Cos((t * flips + phase) * Mathf.PI * 2f);
                frames[i] = new Keyframe(t, Mathf.Lerp(0.2f, 1f, wave * 0.5f + 0.5f));
            }

            AnimationCurve curve = new(frames);
            for (int i = 0; i < keys; i++)
            {
                curve.SmoothTangents(i, 0f);
            }

            return curve;
        }

        private static void ApplyNoise(ParticleSystem particles, float strength, float frequency)
        {
            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = strength > 0f;
            noise.strength = strength;
            noise.frequency = frequency;
            noise.scrollSpeed = 0.4f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;
        }

        /// <summary>
        ///     出た直後と消える直前だけ薄くする（画面外で出入りするので、ほぼ見えない保険）
        /// </summary>
        private static void ApplyEdgeFade(ParticleSystem particles)
        {
            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.06f),
                    new GradientAlphaKey(1f, 0.9f), new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

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

            int count = Mathf.Min(colors.Length, 8);
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

        private static float RandomRange(Vector2 range)
        {
            return UnityEngine.Random.Range(range.x, range.y);
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
                    Debug.LogWarning($"{nameof(WindGimmickEffect)}: マテリアルが未設定で、{FALLBACK_SHADER_NAME} も見つかりません");
                    return null;
                }

                Debug.LogWarning($"{nameof(WindGimmickEffect)}: マテリアルが未設定のため仮のマテリアルを使います");
                _runtimeMaterial = new Material(shader);
            }

            return _runtimeMaterial;
        }
    }
}
