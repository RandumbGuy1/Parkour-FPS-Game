using UnityEngine;

[System.Serializable]
public class JumpingState : MovementState
{
    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private int maxJumpSteps;

    public JumpingState()
    {

    }

    public override void EnterState(ScriptManager controller)
    {
        SetController(controller);

        //PreviousState ==
    }

    public override void ExitState() { }

    public override void StateInput(PlayerInput input) { }
    public override void UpdateState() { }

    public override void FixedUpdateState()
    {
       
    }
    /*
    private void Jump(bool normalJump = true, bool crouched, Rigidbody rb)
    {
        if (normalJump)
        {
            stepsSinceLastJumped = 0;

            S.PlayerController.Grounded = false;
            rb.useGravity = true;

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce((crouched ? 0.64f : 0.8f) * jumpForce * Vector3.up, ForceMode.Impulse);
        }
        else
        {
            S.PlayerController.SetGrounded(false);

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            rb.AddForce(0.75f * jumpForce * S.PlayerController.GroundNormal, ForceMode.Impulse);
            rb.AddForce(S.PlayerController.WallNormal * wallJumpForce, ForceMode.Impulse);
        }

        //s.CameraShaker.ShakeOnce(new KickbackShake(ShakeData.Create(Mathf.Clamp(Magnitude * 0.27f, 3.5f, 4.5f), 4f, 0.8f, 9f), Vector3.right));
    }
    */
}
