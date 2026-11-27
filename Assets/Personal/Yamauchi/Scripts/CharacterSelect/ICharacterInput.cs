using System;

public interface ICharacterInput
{
    public void Register(Action<string> changeAction);
    public void Unregister(Action<string> changeAction);
}
