using System;
using System.Collections.Generic;
using UnityEngine;

public class BreakBridge : EventBase
{
    [Header("ギミック")]
    [SerializeField] private BreakBridgeHub _bHub = new();

    [Header("場所情報")]
    [SerializeField] private BreakBridgeTransform _transform = new();

    private void Start()
    {
        Execute();
    }

    public override void Execute()
    {
        foreach (var b in _bHub.BreakBridgeList)
            b.Gimmick(_transform);
    }
}

[Serializable]
public class BreakBridgeHub
{
    [Header("オブジェクト落下")]
    [SerializeField] private FallDown _falldown = new();

    [Header("足場連続崩壊")]
    [SerializeField] private ScaffoldDestroyer _destroyer = new();

    private List<IBreakBridgeGimmick> _bList = new();

    public IReadOnlyList<IBreakBridgeGimmick> BreakBridgeList => _bList;

    public BreakBridgeHub()
    {
        _bList.Add(_falldown);
        _bList.Add(_destroyer);
    }
}
