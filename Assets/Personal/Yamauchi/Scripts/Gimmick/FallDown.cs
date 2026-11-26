using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

[Serializable]
public class FallDown : ISeesawGimmick, IBigGearGimmick, IBreakBridgeGimmick
{
    [SerializeField] private GameObject _spawnObj;

    [Header("生成までのインターバル")]
    [SerializeField] private float _interval;

    [Header("繰り返し回数")]
    [SerializeField] private int _repeat;

    public void Gimmick(SeesawTransform se) => RandomSpawn(se.SpawnPoints).Forget();

    public void Gimmick(BigGearTransform b) => RandomSpawn(b.SpawnPoints).Forget();

    public void Gimmick(BreakBridgeTransform b) => RandomSpawn(b.SpawnPoints).Forget();

    private async UniTask RandomSpawn(Transform[] se)
    {
        if (se.Length == 0) return;

        for (int i = 0; i < _repeat; i++)
        {
            int spawnCount = UnityEngine.Random.Range(1, se.Length);

            for (int j = 0; j < spawnCount; j++)
                UnityEngine.Object.Instantiate(_spawnObj, se[UnityEngine.Random.Range(0, se.Length)].position, Quaternion.identity);
            await UniTask.Delay(TimeSpan.FromSeconds(_interval));
        }
    }
}
