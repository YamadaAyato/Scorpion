using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public struct MoveSetting
{
    [SerializeField] private Vector3 _localMove;
    [SerializeField] private float _dulation;
    [SerializeField] private Ease _ease;

    public Vector3 LocalMove => _localMove;
    public float Dulation => _dulation;
    public Ease Ease => _ease;
}
