using UnityEngine;

public abstract class MovementState
{
    public MovementState PreviousState { get; private set; }
    public void SetPreviousState(MovementState prevState) => PreviousState = prevState;

    public PlayerManager S { get; private set; }
    public void SetController(PlayerManager controller) => S = controller;

    public abstract void EnterState(PlayerManager controller);
    public abstract void StateInput(PlayerInput input);
    public abstract void FixedUpdateState();
    public abstract void UpdateState();
    public abstract void ExitState();
}
