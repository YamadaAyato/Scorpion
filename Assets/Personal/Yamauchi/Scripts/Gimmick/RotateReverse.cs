using System;

public class RotateReverse : IBigGearGimmick
{
    private static Action OnHandOverReverse;

    public static event Action HandlerHandOverReverse
    {
        add => OnHandOverReverse += value;
        remove => OnHandOverReverse -= value;
    }

    public void Gimmick(BigGearTransform b) => OnHandOverReverse.Invoke();
}
