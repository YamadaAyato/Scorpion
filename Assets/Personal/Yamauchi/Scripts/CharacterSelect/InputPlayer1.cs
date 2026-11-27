
using System;
using UnityEngine.InputSystem;

public class InputPlayer1 : ICharacterInput
{
    private CharaceterSelectAction _action;

    private Action<string> OnChangeActon;

    public event Action<string> HadlerChangeAction
    {
        add => OnChangeActon += value;
        remove => OnChangeActon -= value;
    }

    private static Action<string> OnInputPlayer1;

    public static event Action<string> HandlerInputPlayer1
    {
        add => OnInputPlayer1 += value;
        remove => OnInputPlayer1 -= value;
    }

    public InputPlayer1(CharaceterSelectAction action) => _action = action;

    public void Register(Action<string> changeAction)
    {
        HadlerChangeAction += changeAction;

        _action.Select1.Left.started += OnLeftKey;
        _action.Select1.Right.started += OnRightKey;
        _action.Select1.Decision.started += OnDecision;
    }

    public void Unregister(Action<string> changeAction)
    {
        HadlerChangeAction -= changeAction;

        _action.Select1.Left.started -= OnLeftKey;
        _action.Select1.Right.started -= OnRightKey;
        _action.Select1.Decision.started -= OnDecision;
    }

    private void OnLeftKey(InputAction.CallbackContext c) => OnInputPlayer1.Invoke("Left");
    private void OnRightKey(InputAction.CallbackContext c) => OnInputPlayer1.Invoke("Right");
    private void OnDecision(InputAction.CallbackContext c)
    {
        OnInputPlayer1.Invoke("Decision");
        OnChangeActon.Invoke("Select1/Wait1");
    }
}

public class WaitPlayer1 : ICharacterInput
{
    private CharaceterSelectAction _action;

    private Action<string> OnChangeActon;

    public event Action<string> HadlerChangeAction
    {
        add => OnChangeActon += value;
        remove => OnChangeActon -= value;
    }
    private static Action<string> OnWaitPlayer1;

    public static event Action<string> HandlerWaitPlayer1
    {
        add => OnWaitPlayer1 += value;
        remove => OnWaitPlayer1 -= value;
    }

    public WaitPlayer1(CharaceterSelectAction actions) => _action = actions;

    public void Register(Action<string> changeAction)
    {
        HadlerChangeAction += changeAction;

        _action.Wait1.Return.started += OnReturn;
    }

    public void Unregister(Action<string> changeAction)
    {
        HadlerChangeAction -= changeAction;

        _action.Wait1.Return.started -= OnReturn;
    }

    private void OnReturn(InputAction.CallbackContext c)
    {
        OnWaitPlayer1.Invoke("Return");
        OnChangeActon.Invoke("Wait1/Select1");
    }
}
