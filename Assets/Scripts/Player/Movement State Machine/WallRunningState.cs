using UnityEngine;

[System.Serializable]
public class WallRunningState : MovementState
{
    [Header("WallRunning")]
    [SerializeField] private LayerMask Ground;
    [SerializeField] private float wallRunGravityForce;
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallHoldForce;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallClimbForce;
    [SerializeField] private int wallJumpCooldownSteps;
    [Space(10)]
    [SerializeField] private float minimumJumpHeight;
    [SerializeField] private float wallRunTilt;
    [SerializeField] private float wallRunFovOffset;
    private Vector3 wallMoveDir = Vector3.zero;
    private float camTurnVel = 0f;

    private bool wallRunning = false;
    public float WallRunFovOffset { get { return (wallRunning ? wallRunFovOffset : 0); } }
    public float WallRunTiltOffset { get { return (wallRunning ? wallRunTilt * (S.PlayerController.IsWallRight ? 1 : -1) : 0); } }

    public WallRunningState()
    {

    }

    public override void EnterState(ScriptManager controller)
    {
        SetController(controller);
        InitialWallClimb();
    }

    public override void ExitState() { }

    public override void StateInput(PlayerInput input) { }
    public override void UpdateState() { }

    public override void FixedUpdateState()
    {
        if (!CanWallRun() || !S.PlayerController.IsWallRight && !S.PlayerController.IsWallLeft)
        {
            S.PlayerController.SetState(S.PlayerController.StandingState);
            return;
        }

        S.PlayerController.ClampSpeed(50f);
        WallRun();
    }

    private void WallRun()
    {
        Vector3 wallUpCross = Vector3.Cross(-S.orientation.forward, S.PlayerController.WallNormal);
        wallMoveDir = Vector3.Cross(wallUpCross, S.PlayerController.WallNormal);

        S.rb.AddForce(-S.PlayerController.WallNormal * wallHoldForce);
        S.rb.AddForce(0.8f * wallRunGravityForce * -S.transform.up, ForceMode.Acceleration);
        S.rb.AddForce(Mathf.Clamp(S.PlayerController.Input.y, 0f, 1f) * wallRunForce * wallMoveDir, ForceMode.Acceleration);
    }

    private void InitialWallClimb()
    {
        camTurnVel = 0f;

        Vector3 wallUpCross = Vector3.Cross(-S.orientation.forward, S.PlayerController.WallNormal);
        wallMoveDir = Vector3.Cross(wallUpCross, S.PlayerController.WallNormal);

        S.CameraLook.SetTiltSmoothing(0.18f);
        S.CameraLook.SetFovSmoothing(0.18f);

        S.rb.useGravity = false;
        S.rb.velocity = new Vector3(S.rb.velocity.x, S.rb.velocity.y * 0.65f, S.rb.velocity.z);

        float wallUpSpeed = S.PlayerController.Velocity.y;
        float wallMagnitude = S.PlayerController.Magnitude;

        wallMagnitude = Mathf.Clamp(wallMagnitude, 0f, 20f);

        float wallClimb = wallUpSpeed + wallClimbForce * 0.3f;
        wallClimb = Mathf.Clamp(wallClimb, -3f, 12f);

        S.rb.AddForce(Vector3.up * wallClimb);
        S.rb.AddForce(0.25f * wallMagnitude * wallMoveDir, ForceMode.VelocityChange);
    }

    public bool CanWallRun()
    {
        if (!S.PlayerController.NearWall || S.PlayerController.ReachedMaxSlope) return false;

        return !Physics.CheckSphere(S.BottomCapsuleSphereOrigin + Vector3.down * minimumJumpHeight, 0.3f, Ground);
    }
}
