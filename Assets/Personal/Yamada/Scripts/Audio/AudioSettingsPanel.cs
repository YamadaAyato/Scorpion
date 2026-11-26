using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Scorpion.Audio
{
    /// <summary>
    ///     タイトルの音量設定パネルと3つのスライダーを操作する
    /// </summary>
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        [SerializeField, Tooltip("音量設定を開くボタン")]
        private Button _openButton;
        [SerializeField, Tooltip("音量設定を閉じるボタン")]
        private Button _closeButton;
        [SerializeField, Tooltip("音量設定のパネル")]
        private GameObject _panel;
        [SerializeField, Tooltip("全体音量のスライダー")]
        private Slider _masterSlider;
        [SerializeField, Tooltip("BGM音量のスライダー")]
        private Slider _bgmSlider;
        [SerializeField, Tooltip("SE音量のスライダー")]
        private Slider _seSlider;
        [SerializeField, Tooltip("全体音量を百分率で表示するテキスト")]
        private Text _masterValue;
        [SerializeField, Tooltip("BGM音量を百分率で表示するテキスト")]
        private Text _bgmValue;
        [SerializeField, Tooltip("SE音量を百分率で表示するテキスト")]
        private Text _seValue;

        private void OnEnable()
        {
            _openButton.onClick.AddListener(HandleOpen);
            _closeButton.onClick.AddListener(HandleClose);
            _masterSlider.onValueChanged.AddListener(HandleMasterChanged);
            _bgmSlider.onValueChanged.AddListener(HandleBgmChanged);
            _seSlider.onValueChanged.AddListener(HandleSeChanged);
        }

        private void Update()
        {
            if (_panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleClose();
            }
        }

        private void OnDisable()
        {
            _openButton.onClick.RemoveListener(HandleOpen);
            _closeButton.onClick.RemoveListener(HandleClose);
            _masterSlider.onValueChanged.RemoveListener(HandleMasterChanged);
            _bgmSlider.onValueChanged.RemoveListener(HandleBgmChanged);
            _seSlider.onValueChanged.RemoveListener(HandleSeChanged);
        }

        /// <summary>
        ///     保存済みの音量を表示して設定を開く
        /// </summary>
        private void HandleOpen()
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null)
            {
                return;
            }

            _masterSlider.SetValueWithoutNotify(audio.MasterVolume);
            _bgmSlider.SetValueWithoutNotify(audio.BgmVolume);
            _seSlider.SetValueWithoutNotify(audio.SeVolume);
            _masterValue.text = FormatVolume(audio.MasterVolume);
            _bgmValue.text = FormatVolume(audio.BgmVolume);
            _seValue.text = FormatVolume(audio.SeVolume);

            _openButton.interactable = false;
            _panel.SetActive(true);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_masterSlider.gameObject);
            }
        }

        /// <summary>
        ///     設定を保存してパネルを閉じる
        /// </summary>
        private void HandleClose()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SaveVolumeSettings();
            }

            _panel.SetActive(false);
            _openButton.interactable = true;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_openButton.gameObject);
            }
        }

        /// <summary>
        ///     全体音量を変更する
        /// </summary>
        private void HandleMasterChanged(float volume)
        {
            AudioManager.Instance.SetMasterVolume(volume);
            _masterValue.text = FormatVolume(volume);
        }

        /// <summary>
        ///     BGM音量を変更する
        /// </summary>
        private void HandleBgmChanged(float volume)
        {
            AudioManager.Instance.SetBgmVolume(volume);
            _bgmValue.text = FormatVolume(volume);
        }

        /// <summary>
        ///     SE音量を変更する
        /// </summary>
        private void HandleSeChanged(float volume)
        {
            AudioManager.Instance.SetSeVolume(volume);
            _seValue.text = FormatVolume(volume);
        }

        /// <summary>
        ///     音量を整数の百分率に変換する
        /// </summary>
        private static string FormatVolume(float volume)
        {
            return $"{Mathf.RoundToInt(volume * 100f)}%";
        }
    }
}
