using UnityEngine;

[System.Serializable]
public class SlidingState : MovementState
{
    [Header("Sliding")]
    [SerializeField] private LayerMask CrouchObstruction;
    [SerializeField] private float maxSlideSpeed;
    [SerializeField] private float crouchScale;
    [SerializeField] private float crouchSmoothTime;
    [SerializeField] private float slideForce;
    [SerializeField] private float slideTilt;
    private bool canUnCrouch = true;
    private float slideAngledTilt = 0;

    public Vector3 CrouchOffset { get { return (S.PlayerController.PlayerHeight - S.cc.height) * S.transform.localScale.y * Vector3.down; } }
    public bool CanCrouchWalk { get { return S.PlayerController.Magnitude < 20f * 0.7f; } }
    public float SlideTiltOffset { get { return slideAngledTilt; } }

    public SlidingState()
    {

    }

    public override void EnterState(ScriptManager controller)
    {
        SetController(controller);
        S.CameraLook.SetTiltSmoothing(0.15f);

        if (S.PlayerController.Magnitude > 0.5f)
        {
            if (S.PlayerController.Grounded) S.CameraHeadBob.BobOnce(-S.PlayerController.Magnitude * 0.65f);
            S.rb.AddForce(S.PlayerController.Magnitude * slideForce * (S.PlayerController.Grounded ? 0.8f : 0.3f) * S.PlayerController.InputDir);
        }
    }

    public override void ExitState() 
    {
        slideAngledTilt = 0;
        if (S.PlayerController.Grounded) S.rb.velocity *= 0.65f;
    }

    public override void StateInput(PlayerInput input)
    {
        if (!input.Crouching && canUnCrouch) S.PlayerController.SetState(S.PlayerController.StandingState);
        if (input.Jumping) S.PlayerController.Jump();
    }

    public override void UpdateState()
    {
        ProcessCrouching();
        S.PlayerController.UpdateScale(crouchScale, crouchSmoothTime, Vector2.zero);
    }

    public override void FixedUpdateState()
    {
        S.PlayerController.ClampSpeed(CanCrouchWalk ? 15f * 0.7f : maxSlideSpeed);
    }

    private void ProcessCrouching()
    {  
        //if (CanCrouchWalk) slideAngledTilt = 0;
        //else if (slideAngledTilt == 0 && S.PlayerController.Grounded) slideAngledTilt = (input.x != 0f ? input.x : 1f) * slideTilt;

        canUnCrouch = !Physics.CheckCapsule(S.BottomCapsuleSphereOrigin, S.playerHead.position, S.cc.radius * (S.PlayerController.NearWall ? 0.95f : 1.1f), CrouchObstruction);
        S.rb.AddForce(Vector3.up * 5f, ForceMode.Acceleration);
    }
}
