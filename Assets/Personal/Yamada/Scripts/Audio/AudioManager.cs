using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scorpion.Audio
{
    /// <summary>
    ///     BGMとSEを2Dで再生し、音量設定を保持する
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour
    {
        /// <summary> シーンをまたいで利用する音声管理を取得する </summary>
        public static AudioManager Instance { get; private set; }
        /// <summary> 全体音量を取得する </summary>
        public float MasterVolume { get; private set; }
        /// <summary> BGM音量を取得する </summary>
        public float BgmVolume { get; private set; }
        /// <summary> SE音量を取得する </summary>
        public float SeVolume { get; private set; }

        /// <summary>
        ///     再生名、クリップ、個別音量を設定する
        /// </summary>
        [Serializable]
        public sealed class SoundData
        {
            /// <summary> 再生時に指定する名前を取得する </summary>
            public string Name => _name;
            /// <summary> 再生する音源を取得する </summary>
            public AudioClip Clip => _clip;
            /// <summary> この音源の基準音量を取得する </summary>
            public float Volume => _volume;

            [SerializeField, Tooltip("再生メソッドに渡す名前")]
            private string _name;
            [SerializeField, Tooltip("再生する音源")]
            private AudioClip _clip;
            [SerializeField, Range(0f, 1f), Tooltip("この音源の基準音量")]
            private float _volume = 1f;
        }

        [SerializeField, Range(0f, 1f), Tooltip("初回起動時の全体音量")]
        private float _defaultMasterVolume = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("初回起動時のBGM音量")]
        private float _defaultBgmVolume = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("初回起動時のSE音量")]
        private float _defaultSeVolume = 1f;
        [SerializeField, Tooltip("名前から再生するBGM")]
        private List<SoundData> _bgmList = new();
        [SerializeField, Tooltip("名前から再生するSE")]
        private List<SoundData> _seList = new();

        private readonly List<SePlayback> _activeSe = new();
        private AudioSource _bgmSource;
        private SoundData _currentBgm;
        private Tween _fadeTween;
        private float _fadeVolume = 1f;

        private const string MASTER_VOLUME_KEY = "Scorpion.Audio.Master";
        private const string BGM_VOLUME_KEY = "Scorpion.Audio.Bgm";
        private const string SE_VOLUME_KEY = "Scorpion.Audio.Se";

        /// <summary>
        ///     試合シーンから直接再生した場合も登録済みの音源を使えるようにする
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeMatchAudio()
        {
            if (Instance == null && SceneManager.GetActiveScene().name == "InGame")
            {
                GameObject prefab = Resources.Load<GameObject>("Audio/GameAudioManager");
                if (prefab != null)
                {
                    Instantiate(prefab);
                }
            }
        }

        /// <summary>
        ///     全体音量を変更する
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, MasterVolume);
        }

        /// <summary>
        ///     BGM音量を変更し、再生中の曲に反映する
        /// </summary>
        public void SetBgmVolume(float volume)
        {
            BgmVolume = Mathf.Clamp01(volume);
            ApplyBgmVolume();
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, BgmVolume);
        }

        /// <summary>
        ///     SE音量を変更し、再生中の効果音に反映する
        /// </summary>
        public void SetSeVolume(float volume)
        {
            SeVolume = Mathf.Clamp01(volume);
            foreach (SePlayback playback in _activeSe)
            {
                if (playback.Source != null)
                {
                    playback.Source.volume = playback.Volume * SeVolume;
                }
            }

            PlayerPrefs.SetFloat(SE_VOLUME_KEY, SeVolume);
        }

        /// <summary>
        ///     現在の音量設定をディスクに保存する
        /// </summary>
        public void SaveVolumeSettings()
        {
            PlayerPrefs.Save();
        }

        /// <summary>
        ///     登録名で効果音を一度再生する
        /// </summary>
        public void PlaySE(string name)
        {
            SoundData sound = FindSound(_seList, name);
            if (sound != null)
            {
                PlaySoundEffect(sound);
            }
        }

        /// <summary>
        ///     指定した接頭辞で登録された効果音から等確率で1つ再生する
        /// </summary>
        public void PlayRandomSE(string namePrefix)
        {
            if (string.IsNullOrEmpty(namePrefix))
            {
                return;
            }

            SoundData selected = null;
            int candidateCount = 0;
            foreach (SoundData sound in _seList)
            {
                if (sound?.Clip == null || sound.Name == null ||
                    !sound.Name.StartsWith(namePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                // 候補を追加するたびに 1 / 候補数の確率で入れ替え、配列を作らず均等に選ぶ
                candidateCount++;
                if (UnityEngine.Random.Range(0, candidateCount) == 0)
                {
                    selected = sound;
                }
            }

            if (selected != null)
            {
                PlaySoundEffect(selected);
            }
        }

        /// <summary>
        ///     登録名でBGMをループ再生する
        /// </summary>
        public void PlayBGM(string name)
        {
            SoundData sound = FindSound(_bgmList, name);
            if (sound == null || (sound == _currentBgm && _bgmSource.isPlaying && _fadeVolume == 1f && _fadeTween == null))
            {
                return;
            }

            StopBGM();
            _currentBgm = sound;
            _bgmSource.clip = sound.Clip;
            ApplyBgmVolume();
            _bgmSource.Play();
        }

        /// <summary>
        ///     指定秒数でBGMをフェードアウトして停止する
        /// </summary>
        public void FadeBGM(float fadeTime)
        {
            if (_currentBgm == null)
            {
                return;
            }

            if (fadeTime <= 0f)
            {
                StopBGM();
                return;
            }

            CancelFade();
            // ポーズ中もフェードを進め、BGMつまみの設定値は維持する
            _fadeTween = DOTween.To(() => _fadeVolume, SetFadeVolume, 0f, fadeTime)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _fadeTween = null;
                    StopBGM();
                });
        }

        /// <summary>
        ///     再生中のBGMを停止する
        /// </summary>
        public void StopBGM()
        {
            CancelFade();
            _bgmSource.Stop();
            _bgmSource.clip = null;
            _currentBgm = null;
            _fadeVolume = 1f;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _bgmSource = GetComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.spatialBlend = 0f;
            _bgmSource.loop = true;

            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, _defaultMasterVolume));
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BGM_VOLUME_KEY, _defaultBgmVolume));
            SeVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SE_VOLUME_KEY, _defaultSeVolume));
            AudioListener.volume = MasterVolume;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (Instance == this)
            {
                HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }
        }

        private void Update()
        {
            // 一時停止中の効果音は再開できるように残す
            if (AudioListener.pause)
            {
                return;
            }

            for (int i = _activeSe.Count - 1; i >= 0; i--)
            {
                AudioSource source = _activeSe[i].Source;
                if (source != null && source.isPlaying)
                {
                    continue;
                }

                if (source != null)
                {
                    Destroy(source.gameObject);
                }

                _activeSe.RemoveAt(i);
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused && Instance == this)
            {
                SaveVolumeSettings();
            }
        }

        private void OnApplicationQuit()
        {
            if (Instance == this)
            {
                SaveVolumeSettings();
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            CancelFade();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        ///     タイトルに戻ったらタイトル曲へ切り替え、試合のカウントダウン前には止める
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Title")
            {
                PlayBGM("music_title");
            }
            else if (scene.name == "InGame" && _currentBgm?.Name == "music_title")
            {
                StopBGM();
            }
        }

        /// <summary>
        ///     曲別の音量とSE音量を使い、効果音を2Dで再生する
        /// </summary>
        private void PlaySoundEffect(SoundData sound)
        {
            var player = new GameObject("SEPlayer");
            player.transform.SetParent(transform, false);
            AudioSource source = player.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.clip = sound.Clip;
            source.volume = sound.Volume * SeVolume;
            _activeSe.Add(new SePlayback(source, sound.Volume));
            source.Play();
        }

        /// <summary>
        ///     基準音量、BGM音量、フェード音量を再生中の曲に反映する
        /// </summary>
        private void ApplyBgmVolume()
        {
            if (_currentBgm != null)
            {
                _bgmSource.volume = _currentBgm.Volume * BgmVolume * _fadeVolume;
            }
        }

        /// <summary>
        ///     再生中のフェード処理を中断する
        /// </summary>
        private void CancelFade()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
        }

        /// <summary>
        ///     音量つまみの設定とは別にフェードの進行度を更新する
        /// </summary>
        private void SetFadeVolume(float volume)
        {
            _fadeVolume = volume;
            ApplyBgmVolume();
        }

        /// <summary>
        ///     名前に対応する再生可能な音源を取得する
        /// </summary>
        private static SoundData FindSound(List<SoundData> sounds, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                foreach (SoundData sound in sounds)
                {
                    if (sound != null && sound.Clip != null && sound.Name == name)
                    {
                        return sound;
                    }
                }
            }

            return null;
        }

        private readonly struct SePlayback
        {
            public SePlayback(AudioSource source, float volume)
            {
                Source = source;
                Volume = volume;
            }

            public AudioSource Source { get; }
            public float Volume { get; }
        }
    }
}
