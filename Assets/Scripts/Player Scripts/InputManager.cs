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
    public Vector3 wallJump { get; private set; }

    float multiplier, multiplierV;

    [Header("Thresholds")]
    [Range(0f, 90f)]
    public float maxSlopeAngle;
    public float angle { get; private set; }

    [Header("States")]
    public bool grounded;
    public bool crouching;
    public bool nearWall;

    [HideInInspector] public bool wallRunning;
    [HideInInspector] public bool canWallJump;
    [HideInInspector] public bool canAddWallRunForce;
    [HideInInspector] public bool stopWallRun;
    public bool isWallLeft { get; private set; }
    public bool isWallRight { get; private set; }

    public bool onSlope;
    public bool reachedMaxSlope;
    public bool jumping { get; private set; }
    public bool moving { get; private set; }

    public bool startCrouch { get; private set; }
    public bool stopCrouch { get; private set; }

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

    float lastVel;
    float lastYVel;

    [Header("Assignables")]
    public ParticleSystem sprintEffect;
    public Transform orientation;
    
    public RaycastHit hit;

    private ScriptManager s;
    private Rigidbody rb;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        grounded = Physics.CheckSphere(s.groundCheck.position, groundRadius, Ground) && !reachedMaxSlope;
        if (grounded && !landed) Land(LandVel(lastVel, Math.Abs(lastYVel)));
        if (!grounded && landed) landed = false;

        lastVel = rb.velocity.magnitude;
        lastYVel = rb.velocity.y;
    }

    void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input.Normalize();

        jumping = Input.GetKeyDown(jumpKey);
        crouching = Input.GetKey(crouchKey) && !wallRunning;
        startCrouch = Input.GetKeyDown(crouchKey) && !wallRunning;
        stopCrouch = Input.GetKeyUp(crouchKey) && !wallRunning;
        moving = input.x != 0f || input.y != 0f;

        if (nearWall && isWallLeft && !grounded && !crouching || nearWall && isWallRight && !grounded && !crouching) wallRunning = true;
        stopWallRun = input.x > 0 && isWallLeft && wallRunning && canWallJump || input.x < 0 && isWallRight && wallRunning && canWallJump;

        inputDir = (orientation.forward * input.y * multiplier * multiplierV + orientation.right * input.x * multiplier);
        moveDir = Vector3.ProjectOnPlane(inputDir, hit.normal);

        CalcSlope();
        CheckForWall();
        MovementControl();

        CameraTilt();
        SprintEffect();
    }

    #region Movement Calculations

    private void CalcSlope()
    {
        if (Physics.Raycast(transform.position, -Vector3.up, out hit, 2.7f, Ground))
        {
            if (hit.normal != Vector3.up)
            {
                onSlope = true;
                angle = Vector3.Angle(Vector3.up, hit.normal);
                reachedMaxSlope = (angle > maxSlopeAngle);
            }
            else
            {
                angle = 0f;
                onSlope = false;
                reachedMaxSlope = false;
            }
        }
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
            wallJump = hit.normal;
        }

        if (!nearWall && !isWallRight && !isWallLeft || grounded)
        {
            canWallJump = false;
            wallJump = Vector3.zero;
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

    private void Land(float impactForce)
    {
        s.Effects.CameraLand(impactForce);
        if (impactForce > 55f) ObjectPooler.Instance.Spawn("Land Effects", transform.position - transform.up * 1.5f, Quaternion.Euler(-90, 0, 0));
        
        rb.velocity = Vector3.ProjectOnPlane(rb.velocity, hit.normal);

        landed = true;
    }

    private void SprintEffect()
    {
        if (rb.velocity.magnitude >= 25f && !fast)
        {
            sprintEffect.Play();
            fast = true;
        }
        else if (rb.velocity.magnitude < 25f && fast)
        {
            sprintEffect.Stop();
            fast = false;
        }

        if (rb.velocity.magnitude >= 25f)
        {
            var em = sprintEffect.emission;
            em.rateOverTime = rb.velocity.magnitude;
        }
    }
    #endregion

    float LandVel(float mag, float yMag)
    {
        return (mag * 0.5f) + Math.Abs(yMag * 2f);
    }
}
