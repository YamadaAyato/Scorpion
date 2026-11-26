using System;
using UnityEngine;

[Serializable]
public struct SeesawTransform
{
    [Header("全体")]
    [SerializeField] private Transform _seesaw;

    [Header("乗る部分")]
    [SerializeField] private Transform _ride;

    [Header("柱部分")]
    [SerializeField] private Transform _pole;

    [Header("スポーン")]
    [SerializeField] private Transform[] _points;

    [Header("風")]
    [SerializeField] private WindCollider _windCol;

    public Transform Seesaw => _seesaw;
    public Transform Ride => _ride;
    public Transform Pole => _pole;
    public Transform[] SpawnPoints => _points;
    public WindCollider WindCol => _windCol;
}
