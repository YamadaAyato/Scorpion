using System;
using System.Collections.Generic;
using UnityEngine;

public class Seesaw : EventBase
{
    [Header("ギミック")]
    [SerializeField] private SeesawHub _sHub = new();

    [Header("場所情報")]
    [SerializeField] private SeesawTransform _seesawPlaceInfo;

    private void Start()
    {
        Execute();
    }

    public override void Execute()
    {
        foreach (var s in _sHub.SeesawGimmickList)
            s.Gimmick(_seesawPlaceInfo);
    }
}

[Serializable]
public class SeesawHub
{
    [Header("横の動き")]
    [SerializeField] private SeesawAround _around = new();

    [Header("オブジェクト落下")]
    [SerializeField] private FallDown _fallDown = new();

    [Header("風")]
    [SerializeField] private Wind _wind = new();

    private List<ISeesawGimmick> _gimmickList = new();
    public IReadOnlyList<ISeesawGimmick> SeesawGimmickList => _gimmickList;

    public SeesawHub()
    {
        _gimmickList.Add(_around);
        _gimmickList.Add(_fallDown);
        _gimmickList.Add(_wind);
    }
}
