using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InputManager : MonoBehaviour
{
    [HideInInspector] public Vector2 input = Vector3.zero;
    public Vector3 moveDir { get; private set; }
    public Vector3 inputDir { get; private set; }

    public Vector3 groundNormal { get; private set; }
    public Vector3 wallNormal { get; private set; }

    float multiplier, multiplierV;

    [Header("Thresholds")]
    [Range(0f, 90f)] public float maxSlopeAngle;

    [Header("States")]
    public bool grounded;
    public bool crouching;
    public bool nearWall;
    public bool reachedMaxSlope;

    [HideInInspector] public bool wallRunning;
    [HideInInspector] public bool canWallJump;
    [HideInInspector] public bool canAddWallRunForce;
    [HideInInspector] public bool stopWallRun;
    public bool isWallLeft { get; private set; }
    public bool isWallRight { get; private set; }

    public bool jumping { get; private set; }
    public bool moving { get; private set; }

    public bool hitGround { get; private set; }
    public int stepsSinceLastGrounded { get; private set; }

    bool fast = false;
    bool landed = false;

    [Header("KeyBinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Collision")]
    public LayerMask Ground;
    public LayerMask Environment;
    public float groundRadius;

    [Header("Stairs")]
    public float stepHeight;
    public float stepOffset;
    public float stepSpeed;

    public float magnitude { get; private set; }
    public float yMagnitude { get; private set; }

    [Header("Assignables")]
    public ParticleSystem sprintEffect;
    public Transform orientation;

    private RaycastHit hit;

    private ScriptManager s;
    private Rigidbody rb;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (grounded) 
            if (!Physics.CheckSphere(s.groundCheck.position, groundRadius, Ground) || reachedMaxSlope)
            {
                groundNormal = Vector3.zero;
                landed = false;
                grounded = false;
            }

        if (!grounded && stepsSinceLastGrounded < 6) stepsSinceLastGrounded += 1;
        else if (grounded && stepsSinceLastGrounded > 0) stepsSinceLastGrounded = 0;

        magnitude = rb.velocity.magnitude;
        yMagnitude = rb.velocity.y;
    }

    void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input.Normalize();

        jumping = Input.GetKeyDown(jumpKey);
        crouching = Input.GetKey(crouchKey) && !wallRunning;
        moving = input.x != 0f || input.y != 0f;

        if (nearWall && isWallLeft && !grounded && !crouching || nearWall && isWallRight && !grounded && !crouching) wallRunning = true;
        stopWallRun = input.x > 0 && isWallLeft && wallRunning && canWallJump || input.x < 0 && isWallRight && wallRunning && canWallJump;

        inputDir = (orientation.forward * input.y * multiplier * multiplierV + orientation.right * input.x * multiplier);
        moveDir = Vector3.ProjectOnPlane(inputDir, groundNormal);

        CalcSlope();
        CheckForWall();
        MovementControl();

        CameraTilt();
        SprintEffect();
    }

    #region Movement Calculations

    private void CalcSlope()
    {
        hitGround = Physics.Raycast(s.groundCheck.position, Vector3.down, out hit, 2f, Ground);

        if (hitGround)
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            reachedMaxSlope = angle > maxSlopeAngle;
        }
        else reachedMaxSlope = false;
    }

    private void CheckForStep()
    {
        if (wallRunning) return;
             
        if (Physics.Raycast(s.groundCheck.position + (Vector3.down * 0.4f), orientation.forward, 1f, Environment))
            if (!Physics.Raycast(s.groundCheck.position + (Vector3.up * stepHeight), orientation.forward, 1.5f, Environment) && input.y > 0f)
            {
                rb.MovePosition(Vector3.Lerp(rb.position, rb.position + (Vector3.up * stepOffset), stepSpeed));
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            }
    }

    private void CheckForWall()
    {
        RaycastHit hit;
        nearWall = Physics.Raycast(transform.position - Vector3.up, moveDir.normalized, out hit, 1f, Environment);

        isWallLeft = Physics.Raycast(transform.position, -orientation.right, 1f, Environment) && !isWallRight;
        isWallRight = Physics.Raycast(transform.position, orientation.right, 1f, Environment) && !isWallLeft;

        if (nearWall && !crouching && !grounded && !reachedMaxSlope)
        {
            canWallJump = true;
            wallNormal = hit.normal;
        }

        if (!nearWall && !isWallRight && !isWallLeft || grounded)
        {
            canWallJump = false;
            wallNormal = Vector3.zero;
            wallRunning = false;
            canAddWallRunForce = true;
            rb.useGravity = true;
        }    
    }

    private void MovementControl()
    {
        if (grounded)
        {
            if (multiplierV != 1.05f) multiplierV = 1.05f;
            if (!crouching && !wallRunning && multiplier != 1f) multiplier = 1f;
            if (crouching && !wallRunning && multiplier != 0.05f) multiplier = 0.05f;
        }

        if (!grounded)
        {
            if (!wallRunning && !crouching && multiplier != 0.5f && multiplierV != 0.7f)
            {
                multiplier = 0.5f;
                multiplierV = 0.8f;
            }
            if (!wallRunning && crouching && multiplier != 0.6f && multiplierV != 0.6f)
            {
                multiplier = 0.5f; 
                multiplierV = 0.5f;
            }
            if (s.PlayerInput.wallRunning && multiplier != 0.01f && multiplierV != 0.30f)
            {
                multiplier = 0.01f; 
                multiplierV = 30f;
            }
        }
    }
    #endregion

    #region Visual Effects
    private void CameraTilt()
    {
        if (crouching && grounded) s.CamInput.CameraSlide();
        if (!crouching && !wallRunning) s.CamInput.ResetCameraTilt();

        if (wallRunning && isWallLeft) s.CamInput.CameraWallRun(-1);
        if (wallRunning && isWallRight) s.CamInput.CameraWallRun(1);

        if (!wallRunning && Math.Abs(s.CamInput.CameraTilt) > 0f || !wallRunning && s.CamInput.fov > 80f)
            s.CamInput.ResetCameraTilt();
    }

    private void SprintEffect()
    {
        if (magnitude >= 25f && !fast)
        {
            sprintEffect.Play();
            fast = true;
        }
        else if (magnitude < 25f && fast)
        {
            sprintEffect.Stop();
            fast = false;
        }

        if (magnitude >= 25f)
        {
            var em = sprintEffect.emission;
            em.rateOverTime = magnitude;
        }
    }
    #endregion

    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | 1 << layer)) return;

        Vector3 normal = col.GetContact(0).normal;

        if (IsFloor(normal)) if (!landed) Land(LandVel(magnitude, Math.Abs(yMagnitude)));
    }

    void OnCollisionStay(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | 1 << layer)) return;

        Vector3 normal = col.GetContact(0).normal;

        if (IsFloor(normal))
        {
            grounded = true;
            groundNormal = normal;
        }
    }

    private void Land(float impactForce)
    {
        s.Effects.CameraLand(impactForce);

        if (impactForce > 70f) 
            ObjectPooler.Instance.Spawn("Land Effects", transform.position + Vector3.down, Quaternion.Euler(-90, 0, 0));

        landed = true;
    }

    float LandVel(float mag, float yMag)
    {
        return (mag * 0.5f) + Math.Abs(yMag * 3f);
    }

    bool IsFloor(Vector3 normal)
    {
        float angle = Vector3.Angle(Vector3.up, normal);
        return angle < maxSlopeAngle;
    }
}
