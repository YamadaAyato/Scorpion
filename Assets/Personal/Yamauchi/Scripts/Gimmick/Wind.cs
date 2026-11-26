using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

[Serializable]
public class Wind : ISeesawGimmick
{
    [SerializeField] private float _time;

    public void Gimmick(SeesawTransform s) => WindEnable(s).Forget();

    private async UniTask WindEnable(SeesawTransform s)
    {
        s.WindCol.enabled = true;
        await UniTask.Delay(TimeSpan.FromSeconds(_time));
        s.WindCol.enabled = false;
    }
}
