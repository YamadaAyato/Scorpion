using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterInfo", menuName = "Character/CharacterInfo")]
public class CharacterInfo : ScriptableObject
{
    [Header("リスト")]
    [SerializeField] private List<Chara> _charaList = new();

    public List<Chara> CharaList => _charaList;
}

[Serializable]
public struct Chara
{
    [Header("Sprite")]
    [SerializeField] private Sprite _sprite;

    [Header("Prefab")]
    [SerializeField] private GameObject _charaPrefab;

    [Header("キャラ名")]
    [SerializeField] private string _characterName;

    public Sprite Sprite => _sprite;
    public GameObject Character => _charaPrefab;
    public string CharacterName => _characterName;
}
