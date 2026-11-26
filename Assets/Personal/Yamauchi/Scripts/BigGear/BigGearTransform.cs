using System;
using UnityEngine;

[Serializable]
public struct BigGearTransform
{
    [Header("回転体")]
    [SerializeField] private Transform _rotateObj;

    [Header("スポーン")]
    [SerializeField] private Transform[] _spawnPoints;

    public Transform RotateObj => _rotateObj;
    public Transform[] SpawnPoints => _spawnPoints;
}
