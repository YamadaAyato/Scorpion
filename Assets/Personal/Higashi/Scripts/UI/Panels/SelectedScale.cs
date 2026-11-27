using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 選ばれている間だけ、少し大きくする（コントローラーやキーボードで、今どれを操作しているか分かるようにする）。
/// スライダーなど、選べる UI に付ける。マウスでクリックして選んだときも大きくなる。
/// </summary>
public class SelectedScale : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Tooltip("選ばれている間の大きさ（1 が元の大きさ）")]
    [SerializeField] private float _selectedScale = 1.12f;

    private Tween _tween;

    // 大きさを変える時間（秒）
    private const float SCALE_DURATION = 0.12f;

    private void OnDisable()
    {
        if (_tween != null && _tween.IsActive()) _tween.Kill();
        transform.localScale = Vector3.one;
    }

    /// <summary> 選ばれたとき。少し大きくする。 </summary>
    public void OnSelect(BaseEventData eventData)
    {
        ScaleTo(_selectedScale);
    }

    /// <summary> 選ばれなくなったとき。元の大きさに戻す。 </summary>
    public void OnDeselect(BaseEventData eventData)
    {
        ScaleTo(1f);
    }

    /// <summary> 大きさをなめらかに変える（ポーズ中でも動く）。 </summary>
    private void ScaleTo(float scale)
    {
        if (_tween != null && _tween.IsActive()) _tween.Kill();
        _tween = transform.DOScale(Vector3.one * scale, SCALE_DURATION)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
