using UnityEngine;

[System.Serializable]
public class StandingState : MovementState
{
    [Header("Movement Settings")]
    [SerializeField] private bool autoSprint;
    [SerializeField] private float standMaxSpeed;
    [SerializeField] private float airMaxSpeed;
    public bool Sprinting { get; private set; } = false;

    public StandingState()
    {

    }

    public override void EnterState(ScriptManager controller) => SetController(controller);
    public override void ExitState() { }

    public override void StateInput(PlayerInput input)
    {
        if (input.Crouching) S.PlayerController.SetState(S.PlayerController.SlidingState);
        if (input.Jumping) S.PlayerController.Jump();
    }

    public override void UpdateState()
    {
        S.PlayerController.UpdateScale(S.PlayerController.PlayerHeight, 0.06f, Vector2.zero);
    }

    public override void FixedUpdateState()
    {
        if (S.rb.velocity.y <= 0f) S.rb.AddForce((1.7f - 1f) * Physics.gravity.y * Vector3.up, ForceMode.Acceleration);

        S.PlayerController.ClampSpeed(S.PlayerController.Grounded ? standMaxSpeed : airMaxSpeed);

        if (S.PlayerController.Grounded)
        {
            bool movingTowardWall = (S.PlayerController.Input.x > 0f && S.PlayerController.IsWallRight) || (S.PlayerController.Input.x < 0f && S.PlayerController.IsWallLeft);
            if (S.PlayerController.NearWall && movingTowardWall) S.PlayerController.SetState(S.PlayerController.WallRunningState);
        }
    }


}
