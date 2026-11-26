using System;
using UnityEngine;

[Serializable]
public class ScaffoldDestroyer : IBreakBridgeGimmick
{
    public void Gimmick(BreakBridgeTransform b)
    {
        var s = b.Scaffolds;

        if (s.Length == 0) return;

        int breakCount = UnityEngine.Random.Range(1, s.Length + 1);

        for (int i = 0; i < breakCount; i++)
        {
            int number = UnityEngine.Random.Range(0, s.Length);

            if (s[number].TryGetComponent<AuthorScaffold>(out var a))
                if (a.CurrentObject.TryGetComponent<Scaffold>(out var sc))
                    sc.ZeroDurability();
        }
    }
}
