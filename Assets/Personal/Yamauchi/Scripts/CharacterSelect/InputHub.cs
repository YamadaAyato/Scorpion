using System.Collections.Generic;

public class InputHub
{
    private List<ICharacterInput> _charaInputList = new();

    public IReadOnlyList<ICharacterInput> CharaInputList => _charaInputList;

    public InputHub(CharaceterSelectAction action)
    {
        _charaInputList.Add(new InputPlayer1(action));
        _charaInputList.Add(new WaitPlayer1(action));
        _charaInputList.Add(new InputPlayer2(action));
        _charaInputList.Add(new WaitPlayer2(action));
    }
}
