using UnityEngine;
using UnityEngine.UI;

namespace Scorpion.Effects
{
    /// <summary>
    ///     場外に落ちたプレイヤーの撃破演出。落ちた側の画面端から、カラフルなポップビームと
    ///     丸いパフ・回転する星を噴き上げ、ヒットストップ・スロー・カメラの揺れ・閃光を添える
    /// </summary>
    /// <remarks>
    ///     ScoreManager.OnScoreChanged（加点された瞬間）を合図に、加点されなかった側を撃破されたとみなして再生する。
    ///     スコア処理や GameManager のコードには手を加えない。
    ///     撃破されたプレイヤーはその瞬間に非表示になるため、位置は毎フレーム記録しておいた最後の位置を使う。
    ///     演出はゲームのスロー中も普通の速さで動くよう、実時間（unscaled）で進める。
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class KnockOutEffect : MonoBehaviour
    {
        /// <summary>
        ///     ビーム1本ぶんの見た目
        /// </summary>
        [System.Serializable]
        private struct BeamLayer
        {
            [SerializeField, Tooltip("撃破されたプレイヤーの色を使うか。オフなら下の色")]
            private bool _usePlayerColor;
            [SerializeField, Tooltip("ビームの色（プレイヤーの色を使わない場合）")]
            private Color _color;
            [SerializeField, Min(0f), Tooltip("太さの倍率")]
            private float _widthScale;
            [SerializeField, Min(0f), Tooltip("長さの倍率")]
            private float _lengthScale;
            [SerializeField, Range(-60f, 60f), Tooltip("中心のビームからの角度。左右に広げると扇形になる")]
            private float _angleDegrees;
            [SerializeField, Min(0f), Tooltip("出始めを遅らせる秒数")]
            private float _delaySeconds;

            public BeamLayer(bool usePlayerColor, Color color, float widthScale, float lengthScale,
                float angleDegrees, float delaySeconds)
            {
                _usePlayerColor = usePlayerColor;
                _color = color;
                _widthScale = widthScale;
                _lengthScale = lengthScale;
                _angleDegrees = angleDegrees;
                _delaySeconds = delaySeconds;
            }

            public Color GetColor(Color playerColor) => _usePlayerColor ? playerColor : _color;
            public float WidthScale => _widthScale;
            public float LengthScale => _lengthScale;
            public float AngleDegrees => _angleDegrees;
            public float DelaySeconds => _delaySeconds;
        }

        /// <summary>
        ///     撃破1回ぶんの再生状態。2人が同時に落ちても重ならないよう、プレイヤーごとに持つ
        /// </summary>
        private sealed class Blast
        {
            public LineRenderer[] Beams;
            public LineRenderer[] Rings;
            public float ElapsedSeconds = -1f;
            public Vector3 Origin;
            public Vector3 Direction;
            public Color PlayerColor;
            public float Strength = 1f;
            public bool ShowerEmitted;

            public bool IsPlaying => ElapsedSeconds >= 0f;
        }

        [Header("対象")]
        [SerializeField, Tooltip("1P本体（GameManager に設定しているものと同じ）")]
        private GameObject _playerOne;
        [SerializeField, Tooltip("2P本体（GameManager に設定しているものと同じ）")]
        private GameObject _playerTwo;
        [SerializeField, Tooltip("揺らすカメラ。未設定ならメインカメラ")]
        private Camera _camera;
        [SerializeField, Tooltip("1Pが撃破されたときの色")]
        private Color _playerOneColor = new(1f, 0.33f, 0.62f, 1f);
        [SerializeField, Tooltip("2Pが撃破されたときの色")]
        private Color _playerTwoColor = new(0.22f, 0.7f, 1f, 1f);
        [SerializeField, Min(0f), Tooltip("スコア倍率ギミックで2点以上入ったとき、1点増えるごとに派手さを増やす割合")]
        private float _extraPointBoost = 0.2f;
        [SerializeField, Min(1f), Tooltip("派手さの上限（倍率）")]
        private float _maxStrength = 1.5f;

        [Header("出る位置")]
        [SerializeField, Range(0f, 0.3f), Tooltip("落ちた位置を画面内へ寄せるときの、画面端からの余白（画面に対する割合）")]
        private float _screenEdgeMargin = 0.03f;
        [SerializeField, Range(0f, 1f), Tooltip("ビームの向き。0 で画面端からまっすぐ内側、1 で画面の中心へ向ける")]
        private float _aimToCenter = 0.3f;
        [SerializeField, Min(0f), Tooltip("パフや星を出す位置を、画面端から内側へずらす距離")]
        private float _burstInset = 0.7f;

        [Header("ビーム（噴き上がる光の柱）")]
        [SerializeField, Tooltip("ビームに使うマテリアル（URP Particles/Unlit の半透明、M_KOBeam）")]
        private Material _beamMaterial;
        [SerializeField, Tooltip("重ねるビーム。上から順に奥→手前に描く")]
        private BeamLayer[] _beamLayers =
        {
            new(true, Color.white, 1f, 1f, 0f, 0f),
            new(false, new Color(1f, 0.85f, 0.2f, 1f), 0.38f, 0.72f, -17f, 0.04f),
            new(false, new Color(0.45f, 1f, 0.62f, 1f), 0.38f, 0.72f, 17f, 0.07f),
            new(false, Color.white, 0.42f, 0.95f, 0f, 0f)
        };
        [SerializeField, Min(0f), Tooltip("ビームの長さ")]
        private float _beamLength = 8f;
        [SerializeField, Min(0f), Tooltip("ビームの太さ")]
        private float _beamWidth = 1.7f;
        [SerializeField, Min(0.05f), Tooltip("ビームが出てから消えるまでの秒数（実時間）")]
        private float _beamSeconds = 0.75f;
        [SerializeField, Min(0f), Tooltip("ビームの根元を画面の外へずらす距離。画面端から噴き出して見えるようにする")]
        private float _beamBackOffset = 1.5f;
        [SerializeField, Tooltip("ビームの長さの変化（横: 時間 0〜1、縦: 長さの倍率）。一気に伸びる")]
        private AnimationCurve _beamLengthOverTime = new(
            new Keyframe(0f, 0f, 0f, 9f),
            new Keyframe(0.12f, 1.05f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 1.1f));
        [SerializeField, Tooltip("ビームの太さの変化（横: 時間 0〜1、縦: 太さの倍率）。ぽんっと太ってから細くなって消える")]
        private AnimationCurve _beamWidthOverTime = new(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.07f, 1.3f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.6f, 0.85f),
            new Keyframe(1f, 0f));

        [Header("衝撃リング")]
        [SerializeField, Min(0f), Tooltip("リングが広がる大きさ（半径）")]
        private float _ringRadius = 3f;
        [SerializeField, Min(0f), Tooltip("リングの太さ")]
        private float _ringWidth = 0.5f;
        [SerializeField, Min(0.05f), Tooltip("リングが広がって消えるまでの秒数（実時間）")]
        private float _ringSeconds = 0.45f;
        [SerializeField, Tooltip("2本目のリングの色（1本目はプレイヤーの色）")]
        private Color _ringAccentColor = new(1f, 0.85f, 0.2f, 1f);
        [SerializeField, Min(0f), Tooltip("2本目のリングを遅らせる秒数")]
        private float _ringAccentDelay = 0.08f;
        [SerializeField, Min(0f), Tooltip("2本目のリングの大きさの倍率")]
        private float _ringAccentScale = 0.7f;

        [Header("大きな星")]
        [SerializeField, Tooltip("星に使うマテリアル（ジャンプと同じ M_JumpSparkle）")]
        private Material _sparkleMaterial;
        [SerializeField, Min(0f), Tooltip("撃破地点にぽんっと出る大きな星の大きさ。0 で出さない")]
        private float _bigStarSize = 3.2f;
        [SerializeField, Min(0.05f), Tooltip("大きな星が消えるまでの秒数")]
        private float _bigStarSeconds = 0.6f;
        [SerializeField, Tooltip("大きな星の回る速さ（度/秒）")]
        private float _bigStarSpinDegrees = 420f;

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
        private Vector2 _puffSize = new(0.7f, 1.4f);
        [SerializeField, Tooltip("パフが消えるまでの秒数（最小・最大）")]
        private Vector2 _puffLifetimeSeconds = new(0.5f, 0.85f);
        [SerializeField, Min(0), Tooltip("撃破地点から扇形に吹き出すパフの数")]
        private int _burstPuffCount = 24;
        [SerializeField, Tooltip("扇形のパフの速さ（最小・最大）")]
        private Vector2 _burstPuffSpeed = new(5f, 11f);
        [SerializeField, Range(0f, 90f), Tooltip("扇形のパフの広がり（片側の角度）")]
        private float _burstPuffSpread = 55f;
        [SerializeField, Min(0), Tooltip("ビームに沿って噴き上がるパフの数")]
        private int _beamPuffCount = 14;
        [SerializeField, Min(0f), Tooltip("パフの空気抵抗。大きいほど飛んだ後すぐ止まる")]
        private float _puffDrag = 4f;

        [Header("キラキラ（星）")]
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
        private Vector2 _sparkleSize = new(0.35f, 0.7f);
        [SerializeField, Tooltip("キラキラが消えるまでの秒数（最小・最大）")]
        private Vector2 _sparkleLifetimeSeconds = new(0.7f, 1.1f);
        [SerializeField, Min(0), Tooltip("撃破地点から扇形に弾けるキラキラの数")]
        private int _burstSparkleCount = 30;
        [SerializeField, Tooltip("扇形のキラキラの速さ（最小・最大）")]
        private Vector2 _burstSparkleSpeed = new(6f, 13f);
        [SerializeField, Range(0f, 90f), Tooltip("扇形のキラキラの広がり（片側の角度）")]
        private float _burstSparkleSpread = 70f;
        [SerializeField, Min(0), Tooltip("少し遅れてビームから降ってくるキラキラの数")]
        private int _showerSparkleCount = 16;
        [SerializeField, Min(0f), Tooltip("降ってくるキラキラを出すまでの秒数")]
        private float _showerDelay = 0.2f;
        [SerializeField, Min(0f), Tooltip("キラキラにかかる重力の倍率")]
        private float _sparkleGravity = 1.2f;

        [Header("閃光")]
        [SerializeField, Tooltip("画面全体を光らせる色。透明度が光る強さ")]
        private Color _flashColor = new(1f, 1f, 1f, 0.45f);
        [SerializeField, Range(0f, 1f), Tooltip("閃光をプレイヤーの色に寄せる割合")]
        private float _flashTint = 0.25f;
        [SerializeField, Min(0.01f), Tooltip("閃光が消えるまでの秒数（実時間）")]
        private float _flashSeconds = 0.2f;
        [SerializeField, Tooltip("閃光の描画順。HUD より小さくすると HUD の下に光る")]
        private int _flashSortingOrder = -50;

        [Header("ヒットストップ・スロー")]
        [SerializeField, Tooltip("撃破の瞬間にゲームの時間を止める・遅くするか")]
        private bool _useTimeScale = true;
        [SerializeField, Min(0f), Tooltip("通常攻撃が命中した瞬間に止める時間（実時間の秒数）。0で無効")]
        private float _attackHitStopSeconds = 0.06f;
        [SerializeField, Min(0f), Tooltip("ほぼ止める時間（実時間の秒数）")]
        private float _hitStopSeconds = 0.08f;
        [SerializeField, Range(0f, 1f), Tooltip("ほぼ止めている間の時間の速さ")]
        private float _hitStopTimeScale = 0.03f;
        [SerializeField, Min(0f), Tooltip("ヒットストップの後、スローから元の速さへ戻るまでの秒数（実時間）")]
        private float _slowSeconds = 0.35f;
        [SerializeField, Range(0.01f, 1f), Tooltip("スローの始まりの速さ")]
        private float _slowTimeScale = 0.3f;

        [Header("カメラの揺れ")]
        [SerializeField, Min(0f), Tooltip("揺れの大きさ。0 で揺らさない")]
        private float _shakeAmplitude = 0.3f;
        [SerializeField, Min(0.01f), Tooltip("揺れが収まるまでの秒数（実時間）")]
        private float _shakeSeconds = 0.4f;
        [SerializeField, Min(0f), Tooltip("揺れの細かさ（1秒あたりの揺れ）")]
        private float _shakeFrequency = 25f;
        [SerializeField, Range(0f, 1f), Tooltip("揺れをビームの向きに寄せる割合。大きいほど縦（ビームの向き）にドンと揺れる")]
        private float _shakeAlongBeam = 0.6f;

        private readonly Blast[] _blasts = new Blast[2];
        private readonly Vector3[] _lastPositions = new Vector3[2];
        private readonly bool[] _hasPosition = new bool[2];
        private readonly int[] _knownScores = new int[2];
        private Collider[] _playerColliders = new Collider[2];
        private ScoreManager _scoreManager;
        private ParticleSystem _puffParticles;
        private ParticleSystem _sparkleParticles;
        private ParticleSystem _bigStarParticles;
        private Material _runtimeBeamMaterial;
        private Material _runtimePuffMaterial;
        private Material _runtimeSparkleMaterial;
        private GameObject _flashCanvasObject;
        private Image _flashImage;
        private Color _currentFlashColor;
        private float _flashElapsed = -1f;
        private float _timeElapsed = -1f;
        private double _attackHitStopEndsAt;
        private float _appliedTimeScale = 1f;
        private float _baseTimeScale = 1f;
        private float _shakeElapsed = -1f;
        private float _shakeStrength;
        private Vector3 _shakeDirection = Vector3.up;
        private Vector3 _appliedShakeOffset;
        private Transform _shakenTransform;
        private float _shakeSeed;

        private const int BEAM_POINTS = 12;
        private const int RING_POINTS = 48;
        private const int MAX_PUFFS = 128;
        private const int MAX_SPARKLES = 128;
        private const int MAX_BIG_STARS = 8;
        private const int MAX_GRADIENT_KEYS = 8;
        private const float SPARKLE_SPIN_SPEED = 9f;
        private const string FALLBACK_SHADER_NAME = "Universal Render Pipeline/Particles/Unlit";

        /// <summary>
        ///     指定したプレイヤーが撃破された演出を、そのプレイヤーの最後の位置で再生する
        /// </summary>
        /// <param name="fallenPlayerIndex"> 撃破されたプレイヤー番号（1 または 2） </param>
        /// <param name="points"> 相手に入った点数。多いほど少し派手になる </param>
        public void Play(int fallenPlayerIndex, int points = 1)
        {
            if (fallenPlayerIndex is not (1 or 2))
            {
                return;
            }

            int index = fallenPlayerIndex - 1;
            Vector3 position;
            if (_hasPosition[index])
            {
                position = _lastPositions[index];
            }
            else
            {
                GameObject player = GetPlayer(index);
                position = player != null ? player.transform.position : Vector3.zero;
            }

            float strength = Mathf.Min(1f + _extraPointBoost * Mathf.Max(points - 1, 0), _maxStrength);
            PlayAt(fallenPlayerIndex, position, strength);
        }

        /// <summary>
        ///     指定した位置で撃破演出を再生する。画面の外の位置は画面端に寄せる
        /// </summary>
        public void PlayAt(int fallenPlayerIndex, Vector3 worldPosition, float strength = 1f)
        {
            if (fallenPlayerIndex is not (1 or 2) || _blasts[0] == null)
            {
                return;
            }

            Camera cam = GetCamera();
            Color playerColor = fallenPlayerIndex == 1 ? _playerOneColor : _playerTwoColor;
            GetScreenEdge(cam, worldPosition, out Vector3 origin, out Vector3 direction);

            Blast blast = _blasts[fallenPlayerIndex - 1];
            blast.ElapsedSeconds = 0f;
            blast.Origin = origin;
            blast.Direction = direction;
            blast.PlayerColor = playerColor;
            blast.Strength = Mathf.Max(strength, 0f);
            blast.ShowerEmitted = false;
            PrepareBeams(blast);
            UpdateBlast(blast, cam, 0f);

            Vector3 burstOrigin = origin + direction * _burstInset;
            Vector3 viewAxis = cam != null ? cam.transform.forward : Vector3.forward;
            EmitBurst(blast, burstOrigin, viewAxis);

            StartFlash(playerColor);
            StartTimeScale();
            StartShake(cam, direction, blast.Strength);
        }

        [ContextMenu("テスト再生（1Pが撃破された想定）")]
        private void TestPlayOne()
        {
            if (Application.isPlaying)
            {
                Play(1);
            }
        }

        [ContextMenu("テスト再生（2Pが撃破された想定）")]
        private void TestPlayTwo()
        {
            if (Application.isPlaying)
            {
                Play(2);
            }
        }

        private void Awake()
        {
            for (int i = 0; i < _blasts.Length; i++)
            {
                _blasts[i] = new Blast
                {
                    Beams = CreateBeams(i),
                    Rings = new[] { CreateRing(i, 0), CreateRing(i, 1) }
                };
            }

            _puffParticles = CreatePuffParticles();
            _sparkleParticles = CreateSparkleParticles();
            _bigStarParticles = CreateBigStarParticles();
            CreateFlash();
            _shakeSeed = Random.value * 100f;
        }

        private void OnEnable()
        {
            Hitbox.OnHitConfirmed += HandleAttackHit;
            TrySubscribe();
        }

        private void OnDisable()
        {
            Hitbox.OnHitConfirmed -= HandleAttackHit;
            Unsubscribe();
            RestoreTimeScale();
            RemoveShakeOffset();
            _shakeElapsed = -1f;
            _flashElapsed = -1f;
            if (_flashImage != null)
            {
                _flashImage.enabled = false;
            }
        }

        private void OnDestroy()
        {
            foreach (Blast blast in _blasts)
            {
                if (blast == null)
                {
                    continue;
                }

                foreach (LineRenderer line in blast.Beams)
                {
                    DestroyIfExists(line != null ? line.gameObject : null);
                }

                foreach (LineRenderer line in blast.Rings)
                {
                    DestroyIfExists(line != null ? line.gameObject : null);
                }
            }

            DestroyIfExists(_puffParticles != null ? _puffParticles.gameObject : null);
            DestroyIfExists(_sparkleParticles != null ? _sparkleParticles.gameObject : null);
            DestroyIfExists(_bigStarParticles != null ? _bigStarParticles.gameObject : null);
            DestroyIfExists(_flashCanvasObject);
            DestroyIfExists(_runtimeBeamMaterial);
            DestroyIfExists(_runtimePuffMaterial);
            DestroyIfExists(_runtimeSparkleMaterial);
        }

        private void Update()
        {
            // ScoreManager の Awake がこちらより後になった場合に備えて、見つかるまで探し続ける
            if (_scoreManager == null)
            {
                TrySubscribe();
            }
        }

        /// <summary>
        ///     プレイヤーの位置を記録し、演出を実時間で進める
        /// </summary>
        private void LateUpdate()
        {
            RecordPlayerPositions();

            // ヒットストップ中も、撃破演出と時間の復帰を実時間で進める
            float deltaTime = Time.unscaledDeltaTime;
            Camera cam = GetCamera();
            foreach (Blast blast in _blasts)
            {
                if (blast != null && blast.IsPlaying)
                {
                    blast.ElapsedSeconds += deltaTime;
                    UpdateBlast(blast, cam, blast.ElapsedSeconds);
                }
            }

            UpdateFlash(deltaTime);
            UpdateTimeScale(deltaTime);
            UpdateShake(deltaTime);
        }

        /// <summary>
        ///     スコアの通知を受け取れるようにする
        /// </summary>
        private void TrySubscribe()
        {
            ScoreManager score = ScoreManager.Instance;
            if (score == null || score == _scoreManager)
            {
                return;
            }

            Unsubscribe();
            _scoreManager = score;
            _knownScores[0] = score.PlayerOneScore;
            _knownScores[1] = score.PlayerTwoScore;
            _scoreManager.OnScoreChanged += HandleScoreChanged;
        }

        private void Unsubscribe()
        {
            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged -= HandleScoreChanged;
                _scoreManager = null;
            }
        }

        /// <summary>
        ///     得点が増えたら、増えなかった側が撃破されたとみなして再生する。リセット（0に戻る）では再生しない
        /// </summary>
        private void HandleScoreChanged(int scoringPlayerIndex, int score)
        {
            if (scoringPlayerIndex is not (1 or 2))
            {
                return;
            }

            int index = scoringPlayerIndex - 1;
            int gained = score - _knownScores[index];
            _knownScores[index] = score;
            if (gained > 0)
            {
                Play(scoringPlayerIndex == 1 ? 2 : 1, gained);
            }
        }

        /// <summary>
        ///     表示中のプレイヤーの体の中心を記録する。撃破された瞬間に非表示になるので、その直前の位置が残る
        /// </summary>
        private void RecordPlayerPositions()
        {
            for (int i = 0; i < 2; i++)
            {
                GameObject player = GetPlayer(i);
                if (player == null || !player.activeInHierarchy)
                {
                    continue;
                }

                if (_playerColliders[i] == null)
                {
                    _playerColliders[i] = FindBodyCollider(player);
                }

                _lastPositions[i] = _playerColliders[i] != null
                    ? _playerColliders[i].bounds.center
                    : player.transform.position;
                _hasPosition[i] = true;
            }
        }

        /// <summary>
        ///     撃破位置を画面端に寄せた位置と、そこから画面の内側へ向かう向きを求める
        /// </summary>
        private void GetScreenEdge(Camera cam, Vector3 worldPosition, out Vector3 origin, out Vector3 direction)
        {
            if (cam == null)
            {
                origin = worldPosition;
                direction = Vector3.up;
                return;
            }

            Transform camTransform = cam.transform;
            Vector3 viewport = cam.WorldToViewportPoint(worldPosition);
            float depth = Mathf.Max(Vector3.Dot(worldPosition - camTransform.position, camTransform.forward),
                cam.nearClipPlane + 1f);

            // カメラの後ろ側にある場合は左右上下が反転するので戻す
            if (viewport.z < 0f)
            {
                viewport.x = 1f - viewport.x;
                viewport.y = 1f - viewport.y;
            }

            float margin = _screenEdgeMargin;
            Vector2 clamped = new(Mathf.Clamp(viewport.x, margin, 1f - margin),
                Mathf.Clamp(viewport.y, margin, 1f - margin));

            // はみ出した辺から内側へ向かう向き。はみ出していなければ一番近い辺から内側へ
            Vector2 inward = new(
                viewport.x < margin ? 1f : viewport.x > 1f - margin ? -1f : 0f,
                viewport.y < margin ? 1f : viewport.y > 1f - margin ? -1f : 0f);
            if (inward == Vector2.zero)
            {
                inward = GetNearestEdgeInward(clamped);
            }

            float aspect = Mathf.Max(cam.aspect, 0.01f);
            Vector2 toCenter = new((0.5f - clamped.x) * aspect, 0.5f - clamped.y);
            Vector2 screenDirection = Vector2.Lerp(inward.normalized,
                toCenter.sqrMagnitude > 0.0001f ? toCenter.normalized : inward.normalized, _aimToCenter);
            if (screenDirection.sqrMagnitude < 0.0001f)
            {
                screenDirection = Vector2.up;
            }

            screenDirection.Normalize();
            origin = cam.ViewportToWorldPoint(new Vector3(clamped.x, clamped.y, depth));
            direction = (camTransform.right * screenDirection.x + camTransform.up * screenDirection.y).normalized;
        }

        private static Vector2 GetNearestEdgeInward(Vector2 viewport)
        {
            float left = viewport.x;
            float right = 1f - viewport.x;
            float bottom = viewport.y;
            float top = 1f - viewport.y;
            float min = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top));
            if (Mathf.Approximately(min, bottom))
            {
                return Vector2.up;
            }

            if (Mathf.Approximately(min, top))
            {
                return Vector2.down;
            }

            return Mathf.Approximately(min, left) ? Vector2.right : Vector2.left;
        }

        /// <summary>
        ///     ビームとリングを、経過時間に合わせて伸ばし・広げ・消す
        /// </summary>
        private void UpdateBlast(Blast blast, Camera cam, float elapsed)
        {
            Vector3 viewAxis = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            Vector3 up = cam != null ? cam.transform.up : Vector3.up;
            bool anyVisible = false;

            // ビームごとの遅延を引いた時間を使い、同じ撃破の中で発光のタイミングをずらす
            int layerCount = Mathf.Min(blast.Beams.Length, _beamLayers != null ? _beamLayers.Length : 0);
            for (int i = 0; i < layerCount; i++)
            {
                LineRenderer beam = blast.Beams[i];
                BeamLayer layer = _beamLayers[i];
                float t = (elapsed - layer.DelaySeconds) / _beamSeconds;
                if (t > 1f)
                {
                    beam.enabled = false;
                    continue;
                }

                anyVisible = true;
                beam.enabled = t >= 0f;
                if (t < 0f)
                {
                    continue;
                }

                Vector3 direction = Quaternion.AngleAxis(layer.AngleDegrees, viewAxis) * blast.Direction;
                float length = _beamLength * blast.Strength * layer.LengthScale *
                               Mathf.Max(_beamLengthOverTime.Evaluate(t), 0f);
                Vector3 start = blast.Origin - direction * _beamBackOffset;
                for (int p = 0; p < BEAM_POINTS; p++)
                {
                    float ratio = p / (BEAM_POINTS - 1f);
                    beam.SetPosition(p, start + direction * ((_beamBackOffset + length) * ratio));
                }

                beam.widthMultiplier = _beamWidth * blast.Strength * layer.WidthScale *
                                       Mathf.Max(_beamWidthOverTime.Evaluate(t), 0f);
            }

            for (int r = 0; r < blast.Rings.Length; r++)
            {
                LineRenderer ring = blast.Rings[r];
                float delay = r == 0 ? 0f : _ringAccentDelay;
                float scale = r == 0 ? 1f : _ringAccentScale;
                float t = (elapsed - delay) / _ringSeconds;
                if (t > 1f)
                {
                    ring.enabled = false;
                    continue;
                }

                anyVisible = true;
                ring.enabled = t >= 0f;
                if (t < 0f)
                {
                    continue;
                }

                // ぽよんと勢いよく広がり、細く透明になって消える
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float radius = Mathf.Max(_ringRadius * blast.Strength * scale * eased, 0.01f);
                Vector3 center = blast.Origin + blast.Direction * _burstInset;
                for (int p = 0; p < RING_POINTS; p++)
                {
                    float angle = p * Mathf.PI * 2f / RING_POINTS;
                    ring.SetPosition(p, center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius);
                }

                Color color = r == 0 ? blast.PlayerColor : _ringAccentColor;
                color.a *= 1f - t * t;
                ring.startColor = color;
                ring.endColor = color;
                ring.widthMultiplier = _ringWidth * blast.Strength * scale * (1f - t);
            }

            if (!blast.ShowerEmitted && elapsed >= _showerDelay)
            {
                blast.ShowerEmitted = true;
                EmitShower(blast, viewAxis);
            }

            // 線が消えた後に粒子の発射が予約されている場合も、発射するまでは更新を続ける
            if (!anyVisible && blast.ShowerEmitted)
            {
                blast.ElapsedSeconds = -1f;
            }
        }

        /// <summary>
        ///     撃破の瞬間に、大きな星・扇形のパフとキラキラ・ビームに沿ったパフを一度に出す
        /// </summary>
        private void EmitBurst(Blast blast, Vector3 origin, Vector3 viewAxis)
        {
            float strength = blast.Strength;

            if (_bigStarParticles != null && _bigStarSize > 0f)
            {
                EmitBigStar(origin, blast.PlayerColor, _bigStarSize * strength);
                EmitBigStar(origin - viewAxis * 0.05f, Color.white, _bigStarSize * strength * 0.5f);
            }

            EmitFan(_puffParticles, origin, blast.Direction, viewAxis, Scale(_burstPuffCount, strength),
                _burstPuffSpeed * strength, _burstPuffSpread, 0.4f);
            EmitFan(_sparkleParticles, origin, blast.Direction, viewAxis, Scale(_burstSparkleCount, strength),
                _burstSparkleSpeed * strength, _burstSparkleSpread, 0.3f);

            // ビームの中をパフが噴き上がっていくように、根元〜途中に並べて上向きに飛ばす
            if (_puffParticles != null)
            {
                int count = Scale(_beamPuffCount, strength);
                float length = _beamLength * strength;
                for (int i = 0; i < count; i++)
                {
                    float along = Random.Range(0f, 0.55f) * length;
                    Vector3 side = Vector3.Cross(blast.Direction, viewAxis).normalized;
                    ParticleSystem.EmitParams emitParams = new()
                    {
                        position = origin + blast.Direction * along + side * Random.Range(-0.4f, 0.4f),
                        velocity = blast.Direction * Random.Range(3f, 8f) * strength +
                                   side * Random.Range(-1.5f, 1.5f),
                        applyShapeToPosition = false
                    };
                    _puffParticles.Emit(emitParams, 1);
                }
            }
        }

        /// <summary>
        ///     少し遅れて、ビームのあちこちから星を左右へこぼし、重力で降らせる
        /// </summary>
        private void EmitShower(Blast blast, Vector3 viewAxis)
        {
            if (_sparkleParticles == null)
            {
                return;
            }

            int count = Scale(_showerSparkleCount, blast.Strength);
            float length = _beamLength * blast.Strength;
            Vector3 side = Vector3.Cross(blast.Direction, viewAxis).normalized;
            for (int i = 0; i < count; i++)
            {
                float sign = i % 2 == 0 ? 1f : -1f;
                ParticleSystem.EmitParams emitParams = new()
                {
                    position = blast.Origin + blast.Direction * (Random.Range(0.3f, 0.95f) * length),
                    velocity = side * (sign * Random.Range(1.5f, 4f)) + blast.Direction * Random.Range(0.5f, 2.5f),
                    applyShapeToPosition = false
                };
                _sparkleParticles.Emit(emitParams, 1);
            }
        }

        /// <summary>
        ///     向きを中心に、画面の面内で扇形に粒を飛ばす
        /// </summary>
        private static void EmitFan(ParticleSystem particles, Vector3 origin, Vector3 direction, Vector3 viewAxis,
            int count, Vector2 speedRange, float spreadDegrees, float positionJitter)
        {
            if (particles == null || count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                // 均等に散らしつつ少し揺らして、左右に偏らないようにする
                float ratio = count == 1 ? 0.5f : (i + Random.Range(-0.4f, 0.4f)) / (count - 1f);
                float angle = Mathf.Lerp(-spreadDegrees, spreadDegrees, Mathf.Clamp01(ratio));
                Vector3 fanDirection = Quaternion.AngleAxis(angle, viewAxis) * direction;
                Vector3 jitter = Vector3.ProjectOnPlane(Random.insideUnitSphere, viewAxis) * positionJitter;
                ParticleSystem.EmitParams emitParams = new()
                {
                    position = origin + jitter,
                    velocity = fanDirection * Random.Range(speedRange.x, speedRange.y),
                    applyShapeToPosition = false
                };
                particles.Emit(emitParams, 1);
            }
        }

        private void EmitBigStar(Vector3 position, Color color, float size)
        {
            ParticleSystem.EmitParams emitParams = new()
            {
                position = position,
                velocity = Vector3.zero,
                startSize = size,
                startColor = color,
                startLifetime = _bigStarSeconds,
                rotation = Random.Range(0f, 360f),
                angularVelocity = _bigStarSpinDegrees * (Random.value < 0.5f ? -1f : 1f),
                applyShapeToPosition = false
            };
            _bigStarParticles.Emit(emitParams, 1);
        }

        private static int Scale(int count, float strength)
        {
            return Mathf.Max(Mathf.RoundToInt(count * strength), 0);
        }

        // ───────── 閃光 ─────────

        private void StartFlash(Color playerColor)
        {
            if (_flashImage == null || _flashColor.a <= 0f)
            {
                return;
            }

            Color tinted = Color.Lerp(_flashColor, playerColor, _flashTint);
            tinted.a = _flashColor.a;
            _currentFlashColor = tinted;
            _flashElapsed = 0f;
            _flashImage.enabled = true;
            UpdateFlash(0f);
        }

        private void UpdateFlash(float deltaTime)
        {
            if (_flashElapsed < 0f || _flashImage == null)
            {
                return;
            }

            _flashElapsed += deltaTime;
            float t = _flashElapsed / _flashSeconds;
            if (t >= 1f)
            {
                _flashElapsed = -1f;
                _flashImage.enabled = false;
                return;
            }

            Color color = _currentFlashColor;
            color.a *= (1f - t) * (1f - t);
            _flashImage.color = color;
        }

        // ───────── ヒットストップ・スロー ─────────

        /// <summary>
        ///     命中時に短く停止する。撃破演出や外部のポーズが動作中ならそちらを優先する
        /// </summary>
        private void HandleAttackHit()
        {
            if (_attackHitStopSeconds <= 0f || _timeElapsed >= 0f || Time.timeScale <= 0f)
            {
                return;
            }

            _baseTimeScale = Time.timeScale;
            _timeElapsed = 0f;
            _attackHitStopEndsAt = Time.realtimeSinceStartupAsDouble + _attackHitStopSeconds;
            ApplyTimeScale(0f);
        }

        private void StartTimeScale()
        {
            if (!_useTimeScale || (_hitStopSeconds <= 0f && _slowSeconds <= 0f))
            {
                return;
            }

            // 演出中に重ねて撃破されたときは、元の速さを上書きしない
            if (_timeElapsed < 0f)
            {
                _baseTimeScale = Time.timeScale;
            }

            _attackHitStopEndsAt = 0d;
            _timeElapsed = 0f;
            ApplyTimeScale(_hitStopSeconds > 0f ? _hitStopTimeScale : _slowTimeScale);
        }

        private void UpdateTimeScale(float deltaTime)
        {
            if (_timeElapsed < 0f)
            {
                return;
            }

            // ポーズなど、ほかの処理が速さを変えた場合はそちらを優先して手を引く
            if (!Mathf.Approximately(Time.timeScale, _appliedTimeScale))
            {
                _timeElapsed = -1f;
                _attackHitStopEndsAt = 0d;
                return;
            }

            // 通常攻撃はスローへ移らず、停止前の速度に直接戻す
            if (_attackHitStopEndsAt > 0d)
            {
                if (Time.realtimeSinceStartupAsDouble >= _attackHitStopEndsAt)
                {
                    RestoreTimeScale();
                }

                return;
            }

            _timeElapsed += deltaTime;
            if (_timeElapsed < _hitStopSeconds)
            {
                ApplyTimeScale(_hitStopTimeScale);
                return;
            }

            float t = _slowSeconds > 0f ? (_timeElapsed - _hitStopSeconds) / _slowSeconds : 1f;
            if (t >= 1f)
            {
                RestoreTimeScale();
                return;
            }

            // スローから、終わりに向かってなめらかに元の速さへ戻す
            float eased = t * t * (3f - 2f * t);
            ApplyTimeScale(Mathf.Lerp(_slowTimeScale, 1f, eased) * _baseTimeScale);
        }

        private void ApplyTimeScale(float scale)
        {
            _appliedTimeScale = Mathf.Max(scale, 0f);
            Time.timeScale = _appliedTimeScale;
        }

        private void RestoreTimeScale()
        {
            if (_timeElapsed >= 0f && Mathf.Approximately(Time.timeScale, _appliedTimeScale))
            {
                Time.timeScale = _baseTimeScale;
            }

            _timeElapsed = -1f;
            _attackHitStopEndsAt = 0d;
        }

        // ───────── カメラの揺れ ─────────

        private void StartShake(Camera cam, Vector3 beamDirection, float strength)
        {
            if (cam == null || _shakeAmplitude <= 0f)
            {
                return;
            }

            if (_shakenTransform != cam.transform)
            {
                RemoveShakeOffset();
                _shakenTransform = cam.transform;
            }

            _shakeDirection = beamDirection;
            _shakeStrength = strength;
            _shakeElapsed = 0f;
        }

        /// <summary>
        ///     前のフレームで足した揺れを戻してから、新しい揺れを足す（ほかの処理によるカメラ移動を邪魔しない）
        /// </summary>
        private void UpdateShake(float deltaTime)
        {
            if (_shakeElapsed < 0f || _shakenTransform == null)
            {
                return;
            }

            RemoveShakeOffset();
            _shakeElapsed += deltaTime;
            float t = _shakeElapsed / _shakeSeconds;
            if (t >= 1f)
            {
                _shakeElapsed = -1f;
                return;
            }

            float time = _shakeElapsed * _shakeFrequency;
            float x = Mathf.PerlinNoise(_shakeSeed, time) * 2f - 1f;
            float y = Mathf.PerlinNoise(_shakeSeed + 37f, time) * 2f - 1f;
            float decay = (1f - t) * (1f - t);
            Vector3 random = _shakenTransform.right * x + _shakenTransform.up * y;
            Vector3 along = _shakeDirection * (Mathf.Sin(time * Mathf.PI) * 1.2f);
            Vector3 offset = Vector3.Lerp(random, along, _shakeAlongBeam) *
                             (_shakeAmplitude * _shakeStrength * decay);

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

        // ───────── 組み立て ─────────

        /// <summary>
        ///     重ねるビーム（LineRenderer）を作る。根元から先端へ向かって太さが絞られ、先端で透明になる
        /// </summary>
        private LineRenderer[] CreateBeams(int playerIndex)
        {
            Material material = ResolveMaterial(_beamMaterial, ref _runtimeBeamMaterial);
            int count = _beamLayers != null ? _beamLayers.Length : 0;
            LineRenderer[] beams = new LineRenderer[count];
            AnimationCurve widthCurve = new(
                new Keyframe(0f, 1f),
                new Keyframe(0.6f, 0.92f),
                new Keyframe(0.9f, 0.6f),
                new Keyframe(1f, 0.15f));
            for (int i = 0; i < count; i++)
            {
                LineRenderer beam = CreateLine($"{name} KOBeam P{playerIndex + 1}-{i}", material, BEAM_POINTS, false);
                beam.widthCurve = widthCurve;
                beam.sortingOrder = i;
                beams[i] = beam;
            }

            return beams;
        }

        private LineRenderer CreateRing(int playerIndex, int ringIndex)
        {
            Material material = ResolveMaterial(_beamMaterial, ref _runtimeBeamMaterial);
            LineRenderer ring = CreateLine($"{name} KORing P{playerIndex + 1}-{ringIndex}", material, RING_POINTS, true);
            ring.sortingOrder = (_beamLayers != null ? _beamLayers.Length : 0) + ringIndex;
            return ring;
        }

        private static LineRenderer CreateLine(string label, Material material, int points, bool loop)
        {
            GameObject lineObject = new(label);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.enabled = false;
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = points;
            line.widthMultiplier = 0f;
            line.numCapVertices = loop ? 0 : 6;
            line.numCornerVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            return line;
        }

        /// <summary>
        ///     撃破のたびにビームの色を、そのプレイヤーの色に合わせて塗り直す
        /// </summary>
        private void PrepareBeams(Blast blast)
        {
            int layerCount = Mathf.Min(blast.Beams.Length, _beamLayers != null ? _beamLayers.Length : 0);
            for (int i = 0; i < layerCount; i++)
            {
                Color color = _beamLayers[i].GetColor(blast.PlayerColor);
                Gradient gradient = new();
                gradient.SetKeys(
                    new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                    new[]
                    {
                        new GradientAlphaKey(color.a, 0f),
                        new GradientAlphaKey(color.a, 0.7f),
                        new GradientAlphaKey(0f, 1f)
                    });
                blast.Beams[i].colorGradient = gradient;
            }
        }

        private void CreateFlash()
        {
            _flashCanvasObject = new GameObject($"{name} KOFlash", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = _flashCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _flashSortingOrder;

            GameObject imageObject = new("Flash", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(_flashCanvasObject.transform, false);
            RectTransform rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _flashImage = imageObject.GetComponent<Image>();
            _flashImage.raycastTarget = false;
            _flashImage.color = Color.clear;
            _flashImage.enabled = false;
        }

        /// <summary>
        ///     ぽんっと膨らんでから縮んで消える丸い煙を組み立てる
        /// </summary>
        private ParticleSystem CreatePuffParticles()
        {
            ParticleSystem particles = CreateBaseParticles("KOPuff", MAX_PUFFS, _puffLifetimeSeconds, _puffSize,
                _puffColors, ResolveMaterial(_puffMaterial, ref _runtimePuffMaterial));

            ApplyDrag(particles, _puffDrag);

            AnimationCurve popCurve = new(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.12f, 1f),
                new Keyframe(0.4f, 0.78f),
                new Keyframe(1f, 0f));
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.4f, popCurve);

            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

            ApplyFadeOut(particles, 0.85f);
            particles.GetComponent<ParticleSystemRenderer>().sortingOrder = 10;
            return particles;
        }

        /// <summary>
        ///     くるくる回りながら飛んで落ちて消える星を組み立てる
        /// </summary>
        private ParticleSystem CreateSparkleParticles()
        {
            ParticleSystem particles = CreateBaseParticles("KOSparkle", MAX_SPARKLES, _sparkleLifetimeSeconds,
                _sparkleSize, _sparkleColors, ResolveMaterial(_sparkleMaterial, ref _runtimeSparkleMaterial));

            ParticleSystem.MainModule main = particles.main;
            main.gravityModifier = _sparkleGravity;

            ApplyDrag(particles, 1.8f);

            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particles.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-SPARKLE_SPIN_SPEED, SPARKLE_SPIN_SPEED);

            // 一瞬大きく光ってから、だんだん小さくなる
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.3f,
                new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.1f, 1f), new Keyframe(1f, 0f)));

            ApplyFadeOut(particles, 0.9f);
            particles.GetComponent<ParticleSystemRenderer>().sortingOrder = 11;
            return particles;
        }

        /// <summary>
        ///     撃破地点でぽんっと膨らみ、回りながらしぼんで消える大きな星を組み立てる
        /// </summary>
        private ParticleSystem CreateBigStarParticles()
        {
            ParticleSystem particles = CreateBaseParticles("KOBigStar", MAX_BIG_STARS,
                new Vector2(_bigStarSeconds, _bigStarSeconds), new Vector2(_bigStarSize, _bigStarSize), null,
                ResolveMaterial(_sparkleMaterial, ref _runtimeSparkleMaterial));

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.15f, 1.15f),
                new Keyframe(0.3f, 0.95f),
                new Keyframe(0.7f, 0.9f),
                new Keyframe(1f, 0f)));

            ApplyFadeOut(particles, 0.75f);
            particles.GetComponent<ParticleSystemRenderer>().sortingOrder = 12;
            return particles;
        }

        /// <summary>
        ///     撃破演出の粒に共通する設定で ParticleSystem を作る。スロー中も普通の速さで動かす
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
            main.useUnscaledTime = true;
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

            if (runtimeMaterial != null)
            {
                return runtimeMaterial;
            }

            Shader shader = Shader.Find(FALLBACK_SHADER_NAME);
            if (shader == null)
            {
                Debug.LogWarning($"{nameof(KnockOutEffect)}: マテリアルが未設定で、{FALLBACK_SHADER_NAME} も見つかりません", this);
                return null;
            }

            Debug.LogWarning($"{nameof(KnockOutEffect)}: マテリアルが未設定のため仮のマテリアルを使います", this);
            runtimeMaterial = new Material(shader);
            return runtimeMaterial;
        }

        private GameObject GetPlayer(int index) => index == 0 ? _playerOne : _playerTwo;

        private Camera GetCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            return _camera;
        }

        private static Collider FindBodyCollider(GameObject player)
        {
            foreach (Collider candidate in player.GetComponents<Collider>())
            {
                if (!candidate.isTrigger)
                {
                    return candidate;
                }
            }

            return null;
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
