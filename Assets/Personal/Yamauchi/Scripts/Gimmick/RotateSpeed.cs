using System;
using UnityEngine;

[Serializable]
public class RotateSpeed : IBigGearGimmick
{
    [Header("1回転にかかる時間")]
    [SerializeField] private float _speed;

    private static Action<float> OnHandOverSpeed;

    public static event Action<float> HandlerHandOverSpeed
    {
        add => OnHandOverSpeed += value;
        remove => OnHandOverSpeed -= value;
    }

    public void Gimmick(BigGearTransform b) => OnHandOverSpeed.Invoke(_speed);
}
