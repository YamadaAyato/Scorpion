using System;
using System.Collections.Generic;
using UnityEngine;

public class BigGear : EventBase
{
    [Header("ギミック")]
    [SerializeField] private BigGearHub _bHub = new();

    [Header("場所情報")]
    [SerializeField] private BigGearTransform _transform;

    private void Start()
    {
        Execute();
    }

    public override void Execute()
    {
        foreach (var hub in _bHub.BigGearList)
            hub.Gimmick(_transform);
    }
}

[Serializable]
public class BigGearHub
{
    [Header("オブジェクト落下")]
    [SerializeField] private FallDown _falldown = new();

    [Header("回転速度変化")]
    [SerializeField] private RotateSpeed _rotateSpeed = new();

    [Header("回転の向き変化")]
    [SerializeField] private RotateReverse _rotateReverse = new();

    private List<IBigGearGimmick> _bList = new();

    public IReadOnlyList<IBigGearGimmick> BigGearList => _bList;

    public BigGearHub()
    {
        _bList.Add(_falldown);
        _bList.Add(_rotateSpeed);
        _bList.Add(_rotateReverse);
    }
}
