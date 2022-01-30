using UnityEngine;

public abstract class MovementState
{
    PlayerController controller;

    public abstract void EnterState(PlayerController controller);
    public abstract void StateInput(PlayerInput input);
    public abstract void FixedUpdateState();
    public abstract void UpdateState();
    public abstract void ExitState();
}
