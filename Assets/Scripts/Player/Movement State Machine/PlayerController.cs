using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float acceleration;
    [SerializeField] private float airMultiplier;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float wallJumpForce;

    [Header("Movement States")]
    [SerializeField] private StandingState standingState;
    [SerializeField] private WallRunningState wallRunningState;
    [SerializeField] private SlidingState slidingState;

    [Header("Movement Control")]
    [SerializeField] private float friction;
    [SerializeField] private float frictionMultiplier;
    [SerializeField] private float slideFriction;
    [SerializeField] private int counterThresold;
    private Vector2Int readyToCounter = Vector2Int.zero;

    [Header("Collision")]
    [SerializeField] private LayerMask GroundSnapLayer;
    [SerializeField] private LayerMask Ground;
    [SerializeField] private LayerMask Environment;
    [Space(10)]
    [SerializeField] private float groundCancelDelay;
    [SerializeField] private float wallCancelDelay;
    [Space(10)]
    [SerializeField] [Range(0f, 90f)] private float maxSlopeAngle;

    public float PlayerHeight { get; private set; }

    public int StepsSinceGrounded { get; private set; } = 0;
    public int StepsSinceLastGrounded { get; private set; } = 0;
    public int StepsSinceLastJumped { get; private set; } = 0;

    private bool cancelWall = true;
    private int wallCancelSteps = 0;

    private bool cancelGround = false;
    private int groundCancelSteps = 0;

    public Vector3 GroundNormal { get; private set; }
    public Vector3 WallNormal { get; private set; }

    public bool Grounded { get; private set; } = false;
    public bool NearWall { get; private set; } = false;
    public bool IsWallLeft { get; private set; } = false;
    public bool IsWallRight { get; private set; } = false;
    public bool ReachedMaxSlope { get; private set; } = false;

    public Vector2 Input { get; private set; }
    public Vector3 InputDir { get { return CalculateInputDir(Input).normalized; } }
    public bool Moving { get; private set; }

    public float Magnitude { get; private set; }
    public Vector3 RelativeVel { get; private set; }
    public Vector3 Velocity { get; private set; }

    private PlayerManager s;
    private Rigidbody rb;

    public MovementState CurrentState { get; private set; }
    public StandingState StandingState { get { return standingState; } }
    public WallRunningState WallRunningState { get { return wallRunningState; } }
    public SlidingState SlidingState { get { return slidingState; } }

    public bool InStandingState => CurrentState == StandingState;
    public bool InWallRunningState => CurrentState == WallRunningState;
    public bool InSlidingState => CurrentState == SlidingState;

    void Awake()
    {
        s = GetComponent<PlayerManager>();
        rb = GetComponent<Rigidbody>();

        PlayerHeight = s.cc.height;
        rb.freezeRotation = true;

        SetState(StandingState);
    }

    void Update()
    {
        CurrentState.StateInput(s.PlayerInput);
        CurrentState.UpdateState();
    }

    void FixedUpdate()
    {
        CurrentState.FixedUpdateState();

        UpdateCollisionChecks();
        SetInput(s.PlayerInput.InputVector);
        RecordMovementSteps();

        RelativeVel = s.orientation.InverseTransformDirection(rb.velocity);

        if (Grounded) GroundMovement(CalculateMultiplier());
        else AirMovement(CalculateMultiplier(), Input);

        Magnitude = rb.velocity.magnitude;
        Velocity = rb.velocity;
    }

    #region Movement
    private void AirMovement(float movementMultiplier, Vector2 inputTemp)
    {
        float speedCap = 25f;

        if (inputTemp.x > 0 && RelativeVel.x > speedCap || inputTemp.x < 0 && RelativeVel.x < -speedCap) inputTemp.x = 0f;
        if (inputTemp.y > 0 && RelativeVel.z > speedCap || inputTemp.y < 0 && RelativeVel.z < -speedCap) inputTemp.y = 0f;
        if (InSlidingState && Vector3.Dot(s.orientation.forward, InputDir) < 0.5f) inputTemp *= 0.25f;

        rb.AddForce(8.5f * movementMultiplier * acceleration * CalculateInputDir(inputTemp), ForceMode.Force);
    }

    private void GroundMovement(float movementMultiplier)
    {
        Friction();
        SlopeMovement();

        Vector3 inputDir = CalculateInputDir(Input);
        Vector3 slopeDir = inputDir - Vector3.Project(inputDir, GroundNormal);
        float dot = Vector3.Dot(slopeDir, Vector3.up);

        rb.AddForce(8.5f * movementMultiplier * acceleration * (dot > 0 ? inputDir : inputDir), ForceMode.Force);
    }

    private void SlopeMovement()
    {
        if (GroundNormal.y >= 1f) return;

        Vector3 gravityForce = Physics.gravity - Vector3.Project(Physics.gravity, GroundNormal);
        rb.AddForce(-gravityForce * (rb.velocity.y > 0 ? 0.9f : 1.4f), ForceMode.Acceleration);
    }

    public void Jump(bool normalJump = true)
    {
        if (normalJump)
        {
            ResetJumpSteps();

            SetGrounded(false);
            rb.useGravity = true;

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce((InSlidingState ? 0.64f : 0.8f) * jumpForce * Vector3.up, ForceMode.Impulse);
        }
        else
        {
            SetGrounded(false);

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            rb.AddForce(0.75f * jumpForce * GroundNormal, ForceMode.Impulse);
            rb.AddForce(WallNormal * wallJumpForce, ForceMode.Impulse);
        }

        s.CameraShaker.ShakeOnce(new KickbackShake(ShakeData.Create(Mathf.Clamp(Magnitude * 0.27f, 3.5f, 4.5f), 4f, 0.8f, 9f), Vector3.right));
    }
    #endregion

    #region SurfaceContact
    private bool SnapToGround(float speed)
    {
        if (speed < 3f || StepsSinceLastGrounded > 3 || StepsSinceLastJumped < 10 || Grounded) return false;
        if (!Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, out var snapHit, 1.8f, GroundSnapLayer)) return false;

        SetGrounded(true);

        float dot = Vector3.Dot(rb.velocity, Vector3.up);
        if (dot > 0) rb.velocity = (rb.velocity - (snapHit.normal * dot)).normalized * speed;
        else rb.velocity = (rb.velocity - snapHit.normal).normalized * speed;

        return true;
    }

    private void RecordMovementSteps()
    {
        StepsSinceLastJumped++;

        if (Grounded || SnapToGround(Magnitude))
        {
            if (StepsSinceGrounded < 10) StepsSinceGrounded++;
            StepsSinceLastGrounded = 0;
        }
        else
        {
            if (StepsSinceLastGrounded < 10) StepsSinceLastGrounded++;
            StepsSinceGrounded = 0;
        }
    }

    public void ResetJumpSteps(int steps = 0) => StepsSinceLastJumped = steps;
    #endregion

    #region Collision
    private void UpdateCollisionChecks()
    {
        if (Grounded)
        {
            if (!cancelGround) cancelGround = true;
            else
            {
                groundCancelSteps++;

                if ((float)groundCancelSteps > groundCancelDelay)
                {
                    GroundNormal = Vector3.up;
                    Grounded = false;
                }
            }
        }

        if (NearWall)
        {
            float dot = Vector3.Dot(s.orientation.right, WallNormal);

            IsWallLeft = dot > 0.8f;
            IsWallRight = dot < -0.8f;

            if (!cancelWall) cancelWall = true;
            else
            {
                wallCancelSteps++;

                if ((float)wallCancelSteps > wallCancelDelay)
                {
                    WallNormal = Vector3.zero;
                    NearWall = false;
                    IsWallLeft = false;
                    IsWallRight = false;
                }
            }
        }
    }

    void OnCollisionStay(Collision col)
    {
        int layer = col.gameObject.layer;

        for (int i = 0; i < col.contactCount; i++)
        {
            Vector3 normal = col.GetContact(i).normal;

            if (IsFloor(normal))
            {
                if (Ground != (Ground | 1 << layer)) continue;

                Grounded = true;
                cancelGround = false;
                groundCancelSteps = 0;
                GroundNormal = normal;
            }

            if (IsWall(normal, 0.1f))
            {
                if (Environment != (Environment | 1 << layer)) continue;

                NearWall = true;
                cancelWall = false;
                wallCancelSteps = 0;
                WallNormal = normal;
            }
        }
    }

    bool IsFloor(Vector3 normal) => Vector3.Angle(Vector3.up, normal) < maxSlopeAngle;
    bool IsWall(Vector3 normal, float threshold) => Math.Abs(normal.y) < threshold;

    public void SetGrounded(bool grounded) => Grounded = grounded;
    #endregion

    private void Friction()
    {
        //float multiplier = frictionMultiplier < 1f ? frictionMultiplier * 0.1f : frictionMultiplier;

        if (InSlidingState && !SlidingState.CanCrouchWalk)
        {
            rb.AddForce((Magnitude * 0.08f) * 1.5f /* * multiplier */ * slideFriction * -rb.velocity.normalized);
            return;
        }

        Vector3 frictionForce = Vector3.zero;

        if (Math.Abs(RelativeVel.x) > 0.05f && Input.x == 0f && readyToCounter.x > counterThresold) frictionForce -= s.orientation.right * RelativeVel.x;
        if (Math.Abs(RelativeVel.z) > 0.05f && Input.y == 0f && readyToCounter.y > counterThresold) frictionForce -= s.orientation.forward * RelativeVel.z;

        if (CounterMomentum(Input.x, RelativeVel.x)) frictionForce -= s.orientation.right * RelativeVel.x;
        if (CounterMomentum(Input.y, RelativeVel.z)) frictionForce -= s.orientation.forward * RelativeVel.z;

        frictionForce = Vector3.ProjectOnPlane(frictionForce, GroundNormal);
        if (frictionForce != Vector3.zero) rb.AddForce(0.2f * friction * acceleration/* * multiplier*/ * frictionForce);

        readyToCounter.x = Input.x == 0f ? readyToCounter.x + 1 : 0;
        readyToCounter.y = Input.y == 0f ? readyToCounter.y + 1 : 0;
    }

    bool CounterMomentum(float input, float mag)
    {
        float threshold = 0.05f;
        return (input > 0 && mag < -threshold || input < 0 && mag > threshold);
    }

    private float CalculateMultiplier()
    {
        if (InWallRunningState) return 0f;
        if (InSlidingState) return Grounded ? 0.1f : 0.5f;

        return Grounded ? 1f : airMultiplier;
    }

    public void UpdateScale(float targetScale, float smoothTime, Vector2 vel)
    {
        float targetCenter = (targetScale - PlayerHeight) * 0.5f;

        if (s.cc.height == targetScale && s.cc.center.y == targetCenter) return;
        if (Mathf.Abs(targetScale - s.cc.height) < 0.01f && Mathf.Abs(targetCenter - s.cc.center.y) < 0.01f)
        {
            s.cc.height = targetScale;
            s.cc.center = Vector3.one * targetCenter;
        }

        s.cc.height = Mathf.SmoothDamp(s.cc.height, targetScale, ref vel.x, smoothTime);
        s.cc.center = new Vector3(0, Mathf.SmoothDamp(s.cc.center.y, targetCenter, ref vel.y, smoothTime), 0);
    }

    public void ClampSpeed(float maxSpeed)
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float coefficientOfFriction = acceleration / maxSpeed;
        float groundFrictionAccelTime = 3f;
        float movementMultiplier = CalculateMultiplier();

        if (vel.sqrMagnitude > maxSpeed * maxSpeed) rb.AddForce(8.5f * coefficientOfFriction * (Grounded ? Mathf.Clamp(StepsSinceGrounded / groundFrictionAccelTime, 0.3f, 1f) : 0.8f) * movementMultiplier * -vel, ForceMode.Force);
    }

    public void SetState(MovementState newState)
    {
        newState.EnterState(s);
        newState.SetPreviousState(CurrentState);
        CurrentState = newState;
    }

    private void SetInput(Vector2 input)
    {
        this.Input = input;
        this.Input = Vector2.ClampMagnitude(this.Input, 1f);
        Moving = input != Vector2.zero;
    }

    private Vector3 CalculateInputDir(Vector2 input) => 1.05f * input.y * s.orientation.forward + s.orientation.right * input.x;
}
