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
    bool readyToWallJump = true;

    [Header("KeyBinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Collision")]
    public LayerMask Ground;
    public LayerMask Environment;

    public float groundRadius;
    float lastYVel;
    float lastMagVel;

    [Header("Assignables")]
    public GameObject landEffect;
    public ParticleSystem sprintEffect;
    public Transform orientation;
    
    public RaycastHit hit;

    private ScriptManager s;
    private Rigidbody rb;

    void Awake()
    {
        //Save 1.39
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input.Normalize();

        if (grounded)
            if (!Physics.CheckCapsule(transform.position, s.groundCheck.position, groundRadius, Ground) || reachedMaxSlope)
                grounded = false;

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

    void LateUpdate()
    {
        lastMagVel = rb.velocity.magnitude;
        lastYVel = rb.velocity.y;
    }

    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | (1 << layer))) return;

        grounded = Physics.CheckCapsule(transform.position, s.groundCheck.position, groundRadius, Ground) && !reachedMaxSlope;
        if (grounded && !jumping) Land();
    }

    #region Movement Calculations

    private void CalcSlope()
    {
        if (Physics.Raycast(transform.position, -Vector3.up, out hit, 2.5f, Ground))
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

    private void CheckForWall()
    {
        RaycastHit hit;
        nearWall = Physics.Raycast(transform.position + Vector3.down, moveDir.normalized, out hit, 1f, Environment);

        isWallLeft = Physics.Raycast(transform.position, -orientation.right, 1.2f, Environment) && !isWallRight;
        isWallRight = Physics.Raycast(transform.position, orientation.right, 1.2f, Environment) && !isWallLeft;

        if (nearWall && !crouching)
        {
            if (readyToWallJump && !grounded) 
            {
                readyToWallJump = false;
                Invoke("ResetWallJump", 0.1f);
            }

            wallJump = hit.normal;
        }

        if (!nearWall && !isWallRight && !isWallLeft || grounded)
        {
            canWallJump = false;
            wallJump = Vector3.zero;
            wallRunning = false;
            canAddWallRunForce = true;
            readyToWallJump = true;
            rb.useGravity = true;
        }    
    }

    private void MovementControl()
    {
        if (grounded)
        {
            multiplierV = 1f;
            if (!crouching && !wallRunning) multiplier = 1f;
            if (crouching && !wallRunning) multiplier = 0.05f;
        }

        if (!grounded)
        {
            if (!wallRunning && !crouching)
            {
                multiplier = 0.6f;
                multiplierV = 0.7f;
            }
            if (!wallRunning && crouching)
            {
                multiplier = 0.6f; 
                multiplierV = 0.6f;
            }
            if (s.PlayerInput.wallRunning)
            {
                multiplier = 0.01f; 
                multiplierV = 25f;
            }
        }
    }
    #endregion

    #region Visual Effects
    private void CameraTilt()
    {
        if (crouching && grounded) s.CamInput.CameraSlide();
        if (!crouching && !wallRunning) s.CamInput.ResetCameraTilt();

        if (wallRunning && isWallLeft)
        {
            s.CamInput.CameraWallRun(-1);
            s.Effects.CameraWallRun();
        }

        if (wallRunning && isWallRight)
        {
            s.CamInput.CameraWallRun(1);
            s.Effects.CameraWallRun();
        }

        if (!wallRunning && Math.Abs(s.CamInput.CameraTilt) > 0f || !wallRunning && s.Effects.fov > 80f)
        {
            s.CamInput.ResetCameraTilt();
            s.Effects.ResetCameraWallRun();
        }
    }

    private void Land()
    {
        s.Effects.CameraLand(LandVel(lastMagVel, lastYVel));
        if (Math.Abs(rb.velocity.magnitude * 0.5f) + Math.Abs(rb.velocity.y) > 40f) Instantiate(landEffect, transform.position + -transform.up * 1.5f, Quaternion.Euler(-90, 0, 0));
        rb.velocity = Vector3.ProjectOnPlane(rb.velocity, hit.normal);
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
        return (mag * 0.3f) + Math.Abs(yMag * 2f);
    }

    void ResetWallJump()
    {
        if (!grounded) canWallJump = true;
    }
}
