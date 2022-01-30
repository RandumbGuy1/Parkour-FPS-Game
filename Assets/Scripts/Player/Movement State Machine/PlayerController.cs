using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float acceleration;
    [SerializeField] private float maxGroundSpeed;
    [Space(10)]
    [SerializeField] private float airMultiplier;
    [SerializeField] private float maxAirSpeed;
    [SerializeField] private float maxSlideSpeed;

    [Header("Collision")]
    [SerializeField] private LayerMask GroundSnapLayer;
    [SerializeField] private LayerMask Ground;
    [SerializeField] private LayerMask Environment;
    [Space(10)]
    [SerializeField] private float groundCancelDelay;
    [SerializeField] private float wallCancelDelay;
    [Space(10)]
    [SerializeField] [Range(0f, 90f)] private float maxSlopeAngle;

    private bool cancelWall = true;
    private int wallCancelSteps = 0;

    private bool cancelGround = false;
    private int groundCancelSteps = 0;

    public Vector3 GroundNormal { get; private set; }
    public Vector3 WallNormal { get; private set; }

    public bool Grounded { get; private set; } = false;
    public bool NearWall { get; private set; } = false;
    public bool ReachedMaxSlope { get; private set; } = false;

    private Vector2 input;

    public Vector3 InputDir { get { return CalculateInputDir(input).normalized; } }
    public bool Moving { get; private set; }

    public float Magnitude { get; private set; }
    public Vector3 RelativeVel { get; private set; }
    public Vector3 Velocity { get; private set; }

    private ScriptManager s;
    private Rigidbody rb;

    public MovementState CurrentState { get; private set; }

    public StandingState StandingState { get; private set; } = new StandingState();
    public JumpingState JumpingState { get; private set; } = new JumpingState();
    public WallRunningState WallRunningState { get; private set; } = new WallRunningState();
    public SlidingState SlidingState { get; private set; } = new SlidingState();

    public bool InJumpingState => CurrentState == JumpingState;
    public bool InStandingState => CurrentState == StandingState;
    public bool InWallRunningState => CurrentState == WallRunningState;
    public bool InSlidingState => CurrentState == SlidingState;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();

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
        Movement(CalculateMultiplier());

        Magnitude = rb.velocity.magnitude;
        Velocity = rb.velocity;
    }

    private void Movement(float movementMultiplier)
    {
        Friction();
        SlopeMovement();

        Vector3 inputDir = CalculateInputDir(input);
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
            if (!cancelWall) cancelWall = true;
            else
            {
                wallCancelSteps++;

                if ((float)wallCancelSteps > wallCancelDelay)
                {
                    WallNormal = Vector3.zero;
                    NearWall = false;
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
    #endregion

    private float CalculateMultiplier()
    {
        if (InWallRunningState) return 0f;
        if (InSlidingState) return Grounded ? 0.1f : 0.5f;

        return Grounded ? 1f : airMultiplier;
    }

    public void SetState(MovementState newState)
    {
        newState.EnterState(this);
        CurrentState = newState;
    }

    private void SetInput(Vector2 input)
    {
        this.input = input;
        this.input = Vector2.ClampMagnitude(this.input, 1f);
        Moving = input != Vector2.zero;
    }

    private Vector3 CalculateInputDir(Vector2 input) => 1.05f * input.y * s.orientation.forward + s.orientation.right * input.x;
}
