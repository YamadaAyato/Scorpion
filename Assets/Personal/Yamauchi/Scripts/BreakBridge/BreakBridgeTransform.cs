using System;
using UnityEngine;

[Serializable]
public struct BreakBridgeTransform
{
    [Header("足場")]
    [SerializeField] private Transform[] _scaffolds;

    [Header("スポーン")]
    [SerializeField] private Transform[] _spawnPoints;

    public Transform[] Scaffolds => _scaffolds;
    public Transform[] SpawnPoints => _spawnPoints;
}
