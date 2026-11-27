using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音量スライダーの横のスピーカーマーク。音量が 0 なら消音マーク、それ以外なら音量マークに切り替える。
/// 2つの絵は大きさが違うので、切り替えるときにスピーカーの本体（絵の左側）が同じ場所に残るよう、横幅と Pivot を合わせ直す。
/// 音量設定パネルは、開いたときに値を「通知なし」で入れるので、値の変化は毎フレーム見て確かめる。
/// </summary>
[RequireComponent(typeof(Image))]
public class VolumeIcon : MonoBehaviour
{
    [Tooltip("音量を見るスライダー")]
    [SerializeField] private Slider _slider;

    [Tooltip("音量が 0 のときの絵（消音マーク）")]
    [SerializeField] private Sprite _muteSprite;

    [Tooltip("音が出ているときの絵（音量マーク）")]
    [SerializeField] private Sprite _loudSprite;

    private Image _image;
    private RectTransform _rect;
    private bool? _isMuted;

    // 2つの絵で共通の、スピーカー本体の左端（ピクセル。絵の左端から）。絵を描き直したら測り直す
    private const float ART_CONTENT_LEFT = 718f;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect = (RectTransform)transform;
    }

    private void LateUpdate()
    {
        if (_slider == null) return;

        bool isMuted = _slider.value <= _slider.minValue;
        if (_isMuted == isMuted) return;

        _isMuted = isMuted;
        Apply(isMuted ? _muteSprite : _loudSprite);
    }

    /// <summary> 絵を差し替え、高さはそのままで横幅を絵の比率に合わせ、Pivot をスピーカー本体の左端に置く。 </summary>
    private void Apply(Sprite sprite)
    {
        if (sprite == null) return;

        Rect art = sprite.rect;
        float height = _rect.sizeDelta.y;
        _image.sprite = sprite;
        _rect.pivot = new Vector2(ART_CONTENT_LEFT / art.width, _rect.pivot.y);
        _rect.sizeDelta = new Vector2(height * art.width / art.height, height);
    }
}
