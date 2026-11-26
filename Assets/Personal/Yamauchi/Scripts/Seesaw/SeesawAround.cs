using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
/// <summary>全体が横に動く</summary>
public class SeesawAround : ISeesawGimmick
{
    [SerializeField] private List<MoveSetting> _settings = new();

    /// <summary>
    /// 動きは見ながら調整する
    /// </summary>
    /// <param name="seesaw"></param>
    /// <param name="ride"></param>
    /// <param name="pole"></param>
    public void Gimmick(SeesawTransform se)
    {
        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < _settings.Count; i++)
        {
            var s = _settings[i];
            sequence.Append(se.Seesaw.DOMoveX(se.Seesaw.position.x + s.LocalMove.x, s.Dulation).SetEase(s.Ease));
        }
    }
}


