using System;
using UnityEngine.InputSystem;

public class InputPlayer2 : ICharacterInput
{
    private CharaceterSelectAction _action;

    private Action<string> OnChangeActon;

    public event Action<string> HadlerChangeAction
    {
        add => OnChangeActon += value;
        remove => OnChangeActon -= value;
    }

    private static Action<string> OnInputPlayer2;

    public static event Action<string> HandlerInputPlayer2
    {
        add => OnInputPlayer2 += value;
        remove => OnInputPlayer2 -= value;
    }

    public InputPlayer2(CharaceterSelectAction action) => _action = action;

    public void Register(Action<string> changeAction)
    {
        HadlerChangeAction += changeAction;

        _action.Select2.Left.started += OnLeftKey;
        _action.Select2.Right.started += OnRightKey;
        _action.Select2.Decision.started += OnDecision;
    }

    public void Unregister(Action<string> changeAction)
    {
        HadlerChangeAction -= changeAction;

        _action.Select2.Left.started -= OnLeftKey;
        _action.Select2.Right.started -= OnRightKey;
        _action.Select2.Decision.started -= OnDecision;
    }

    private void OnLeftKey(InputAction.CallbackContext c) => OnInputPlayer2.Invoke("Left");
    private void OnRightKey(InputAction.CallbackContext c) => OnInputPlayer2.Invoke("Right");
    private void OnDecision(InputAction.CallbackContext c)
    {
        OnInputPlayer2.Invoke("Decision");
        OnChangeActon.Invoke("Select2/Wait2");
    }
}

public class WaitPlayer2 : ICharacterInput
{
    private CharaceterSelectAction _action;

    private Action<string> OnChangeActon;

    public event Action<string> HadlerChangeAction
    {
        add => OnChangeActon += value;
        remove => OnChangeActon -= value;
    }
    private static Action<string> OnWaitPlayer2;

    public static event Action<string> HandlerWaitPlayer2
    {
        add => OnWaitPlayer2 += value;
        remove => OnWaitPlayer2 -= value;
    }

    public WaitPlayer2(CharaceterSelectAction actions) => _action = actions;

    public void Register(Action<string> changeAction)
    {
        HadlerChangeAction += changeAction;

        _action.Wait2.Return.started += OnReturn;
    }

    public void Unregister(Action<string> changeAction)
    {
        HadlerChangeAction -= changeAction;

        _action.Wait2.Return.started -= OnReturn;
    }

    private void OnReturn(InputAction.CallbackContext c)
    {
        OnWaitPlayer2.Invoke("Return");
        OnChangeActon.Invoke("Wait2/Select2");
    }
}
