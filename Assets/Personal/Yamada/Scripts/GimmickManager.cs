using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     ステージの残り時間からギミックを抽選し、発動中の効果を管理する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GimmickManager : MonoBehaviour
    {
        /// <summary> ギミック発動時に発動IDと設定を通知する。 </summary>
        public event Action<int, GimmickDefinition> OnGimmickStarted;
        /// <summary> ギミック終了時に発動IDと設定を通知する。 </summary>
        public event Action<int, GimmickDefinition> OnGimmickEnded;
        /// <summary> 残り10秒以下に入り、ラッシュが始まったことを通知する。 </summary>
        public event Action OnRushStarted;

        /// <summary> 有効な効果のうち最大の得点倍率を取得する。通常は1。 </summary>
        public int ScoreMultiplier
        {
            get
            {
                int multiplier = 1;
                foreach (ActiveGimmick active in _activeGimmicks)
                {
                    multiplier = Mathf.Max(multiplier, active.Context.ScoreMultiplier);
                }

                return multiplier;
            }
        }

        [SerializeField, Tooltip("妨害物の出現基準。未指定の場合はこのコンポーネントの位置を使用します。")]
        private Transform _spawnOrigin;

        private readonly List<ActiveGimmick> _activeGimmicks = new();
        private StageGimmickSettings _settings;
        private float _remainingSeconds;
        private float _nextDrawAt;
        private bool _isRush;
        private int _nextActivationId;
        private int _stageVersion;

        private const float RUSH_REMAINING_SECONDS = 10f;

        /// <summary>
        ///     ステージのプレイ開始時に、使用する設定と初期残り時間を登録する。
        /// </summary>
        public void StartStage(StageGimmickSettings settings, float remainingSeconds)
        {
            int version = _stageVersion + 1;
            EndStage();
            if (version != _stageVersion || !isActiveAndEnabled || settings == null ||
                !float.IsFinite(remainingSeconds) || remainingSeconds <= 0f)
            {
                return;
            }

            _settings = settings;
            _remainingSeconds = remainingSeconds;
            _nextDrawAt = remainingSeconds - settings.NormalInterval;
            _isRush = false;
            UpdateStageTime(remainingSeconds);
        }

        /// <summary>
        ///     ステージタイマーの残り時間を受け取り、効果の終了と次の発動を処理する。
        /// </summary>
        public void UpdateStageTime(float remainingSeconds)
        {
            if (_settings == null || !float.IsFinite(remainingSeconds) ||
                remainingSeconds > _remainingSeconds)
            {
                return;
            }

            if (remainingSeconds <= 0f)
            {
                EndStage();
                return;
            }

            _remainingSeconds = remainingSeconds;
            int version = _stageVersion;
            EndExpiredGimmicks(version);
            if (version != _stageVersion)
            {
                return;
            }

            if (!_isRush && remainingSeconds <= RUSH_REMAINING_SECONDS)
            {
                StartRush(version);
            }

            if (version != _stageVersion || remainingSeconds > _nextDrawAt)
            {
                return;
            }

            // フレームが遅れた場合も、過ぎた抽選を一度に発動しない。
            _nextDrawAt = remainingSeconds - (_isRush ? _settings.RushInterval : _settings.NormalInterval);
            Activate(Draw(_isRush ? _settings.RushEntries : _settings.NormalEntries));
        }

        /// <summary>
        ///     ステージを終了し、効果をすべて解除してから終了イベントを通知する。
        /// </summary>
        public void EndStage()
        {
            _stageVersion++;
            _settings = null;
            if (_activeGimmicks.Count == 0)
            {
                return;
            }

            ActiveGimmick[] finished = _activeGimmicks.ToArray();
            _activeGimmicks.Clear();
            foreach (ActiveGimmick active in finished)
            {
                active.Context.Clear();
            }

            foreach (ActiveGimmick active in finished)
            {
                OnGimmickEnded?.Invoke(active.Id, active.Definition);
            }
        }

        private void OnDisable()
        {
            EndStage();
        }

        /// <summary>
        ///     ラッシュに切り替え、突入時のギミックを一度だけ発動する。
        /// </summary>
        private void StartRush(int version)
        {
            _isRush = true;
            _nextDrawAt = _remainingSeconds - _settings.RushInterval;
            IReadOnlyList<GimmickDefinition> guaranteed = _settings.RushStartGimmicks;
            OnRushStarted?.Invoke();

            foreach (GimmickDefinition definition in guaranteed)
            {
                if (version != _stageVersion)
                {
                    return;
                }

                Activate(definition);
            }
        }

        /// <summary>
        ///     正の重みを持つ有効な候補からギミックを1つ抽選する。
        /// </summary>
        private static GimmickDefinition Draw(IReadOnlyList<GimmickSpawnEntry> entries)
        {
            double totalWeight = 0;
            foreach (GimmickSpawnEntry entry in entries)
            {
                if (entry != null && entry.CanDraw)
                {
                    totalWeight += entry.Weight;
                }
            }

            double roll = UnityEngine.Random.value * totalWeight;
            GimmickDefinition lastCandidate = null;
            foreach (GimmickSpawnEntry entry in entries)
            {
                if (entry == null || !entry.CanDraw)
                {
                    continue;
                }

                lastCandidate = entry.Definition;
                roll -= entry.Weight;
                if (roll < 0)
                {
                    return lastCandidate;
                }
            }

            // Random.value が1を返した場合も、最後の候補を選べるようにする。
            return lastCandidate;
        }

        /// <summary>
        ///     発動用の状態を作成して、効果を適用できた場合だけ通知する。
        /// </summary>
        private void Activate(GimmickDefinition definition)
        {
            if (definition == null || !definition.HasAction)
            {
                return;
            }

            Vector3 spawnOrigin = _spawnOrigin != null ? _spawnOrigin.position : transform.position;
            var context = new GimmickContext(spawnOrigin);
            int version = _stageVersion;
            bool activated;
            try
            {
                activated = definition.TryActivate(context);
            }
            catch (Exception exception)
            {
                context.Clear();
                Debug.LogException(exception, this);
                return;
            }

            if (!activated || version != _stageVersion)
            {
                context.Clear();
                return;
            }

            var active = new ActiveGimmick(++_nextActivationId, definition, context,
                _remainingSeconds - definition.Duration);
            _activeGimmicks.Add(active);
            OnGimmickStarted?.Invoke(active.Id, definition);
        }

        /// <summary>
        ///     指定された効果時間を過ぎたギミックを解除する。
        /// </summary>
        private void EndExpiredGimmicks(int version)
        {
            for (int i = _activeGimmicks.Count - 1; i >= 0; i--)
            {
                ActiveGimmick active = _activeGimmicks[i];
                if (_remainingSeconds > active.EndAt)
                {
                    continue;
                }

                _activeGimmicks.RemoveAt(i);
                active.Context.Clear();
                OnGimmickEnded?.Invoke(active.Id, active.Definition);
                if (version != _stageVersion)
                {
                    return;
                }
            }
        }

        /// <summary>
        ///     同じ設定から複数回発動しても、発動ごとの終了時刻と状態を保持する。
        /// </summary>
        private sealed class ActiveGimmick
        {
            public ActiveGimmick(int id, GimmickDefinition definition, GimmickContext context, float endAt)
            {
                Id = id;
                Definition = definition;
                Context = context;
                EndAt = endAt;
            }

            public int Id { get; }
            public GimmickDefinition Definition { get; }
            public GimmickContext Context { get; }
            public float EndAt { get; }
        }
    }
}
