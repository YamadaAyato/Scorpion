using DG.Tweening;
using UnityEngine;

public class BigGearController : MonoBehaviour
{
    [Header("回転の種類")]
    [SerializeField] private Ease _ease;

    [Header("1回転にかかる時間")]
    [SerializeField] private float _speed;

    private Tween _rotateTween;

    private void OnEnable()
    {
        RotateSpeed.HandlerHandOverSpeed += SpeedChange;
        RotateReverse.HandlerHandOverReverse += RotateChange;
    }

    private void OnDisable()
    {
        RotateSpeed.HandlerHandOverSpeed -= SpeedChange;
        RotateReverse.HandlerHandOverReverse -= RotateChange;
        _rotateTween?.Kill();
    }

    private void Start() => RotateLoop();

    private void SpeedChange(float speed)
    {
        _rotateTween?.Kill();
        _speed = speed;
        _rotateTween = transform.DORotate(new Vector3(0f, 0f, 360f), _speed, RotateMode.LocalAxisAdd).SetEase(_ease).SetLoops(-1);
    }

    private void RotateChange()
    {
        _rotateTween?.Kill();
        _rotateTween = transform.DORotate(new Vector3(0f, 0f, -360f), _speed, RotateMode.LocalAxisAdd).SetEase(_ease).SetLoops(-1);
    }

    private void RotateLoop() => _rotateTween = transform.DORotate(new Vector3(0f, 0f, 360f), _speed, RotateMode.LocalAxisAdd).SetEase(_ease).SetLoops(-1);
}
