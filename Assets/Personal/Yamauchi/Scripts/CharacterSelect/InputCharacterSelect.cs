using UnityEngine;
using UnityEngine.InputSystem;

public class InputCharacterSelect : MonoBehaviour
{
    private InputHub _hub;

    private static CharaceterSelectAction _selectAction;

    public static CharaceterSelectAction SelectAction => _selectAction;

    private void Awake()
    {
        _selectAction = new();
        _selectAction?.Enable();

        foreach (var l in _selectAction.asset.actionMaps.ToArray())
        {
            if (l.name == "Select1" || l.name == "Select2")
                continue;

            InputActionMap map = _selectAction.asset.FindActionMap(name);
            map?.Disable();
        }
    }

    private void OnDestroy() => _selectAction?.Disable();

    private void OnEnable()
    {
        _hub = new(_selectAction);

        foreach (var h in _hub.CharaInputList)
            h.Register(ChangeMap);

        SceneChanger.HandlerAction += SceneChange;
    }

    private void OnDisable()
    {
        foreach (var h in _hub.CharaInputList)
            h.Unregister(ChangeMap);

        SceneChanger.HandlerAction -= SceneChange;
    }

    private void ChangeMap(string mapName)
    {
        string diable = mapName.Split('/')[0];
        string enable = mapName.Split("/")[1];

        InputActionMap dMap = _selectAction.asset.FindActionMap(diable);
        dMap?.Disable();

        InputActionMap eMap = _selectAction.asset.FindActionMap(enable);
        eMap?.Enable();
    }

    private void SceneChange() => _selectAction?.Disable();
}
