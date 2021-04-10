using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InputManager : MonoBehaviour
{
    [HideInInspector] public float x, y;
    public Vector3 moveDir { get; private set; }
    public Vector3 inputDir { get; private set; }
    public Vector3 wallJump { get; private set; }

    [HideInInspector] public float wallCast = 1f;

    [Header("Thresholds")]
    [Range(0f, 90f)]
    public float maxSlopeAngle;
    [Range(0f, 30f)]
    public float maxVaultHeight;
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
    public bool landed { get; private set; }

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
        //Save 1.38
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!Physics.CheckCapsule(transform.position, s.groundCheck.position, groundRadius, Ground) || reachedMaxSlope)
        {
            grounded = false;
            landed = false;
        }
    }

    void Update()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");

        jumping = Input.GetKey(jumpKey);
        crouching = Input.GetKey(crouchKey) && !wallRunning;
        startCrouch = Input.GetKeyDown(crouchKey) && !wallRunning;
        stopCrouch = Input.GetKeyUp(crouchKey) && !wallRunning;
        moving = x != 0f || y != 0f;

        if (nearWall && isWallLeft && !grounded && !crouching || nearWall && isWallRight && !grounded && !crouching) wallRunning = true;
        stopWallRun = x > 0 && isWallLeft && wallRunning && canWallJump || x < 0 && isWallRight && wallRunning && canWallJump;

        inputDir = (orientation.forward * y + orientation.right * x);
        moveDir = Vector3.ProjectOnPlane(inputDir, hit.normal);

        CalcSlope();
        CheckForWall();

        CameraTilt();
        SprintEffect();
    }

    void LateUpdate()
    {
        if (landed && !grounded) landed = false;

        lastMagVel = rb.velocity.magnitude;
        lastYVel = rb.velocity.y;
    }

    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | (1 << layer))) return;

        grounded = Physics.CheckCapsule(transform.position, s.groundCheck.position, groundRadius, Ground) && !reachedMaxSlope;
        if (grounded && !landed && !jumping) Land();
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
        if (!wallRunning) wallCast = 1f;
        if (wallRunning) wallCast = 1.5f;

        RaycastHit hit;
        nearWall = Physics.Raycast(transform.position + Vector3.down, moveDir, out hit, 1f, Environment);

        isWallLeft = Physics.Raycast(transform.position, -orientation.right, wallCast, Environment) && !isWallRight;
        isWallRight = Physics.Raycast(transform.position, orientation.right, wallCast, Environment) && !isWallLeft;

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

    /*
    public bool CanVault()
    {
        if (Physics.CheckSphere(s.groundCheck.position + (orientation.forward * 0.1f), 1f, Environment))
        {
            if (Physics.Raycast(s.groundCheck.position, orientation.forward, out vaultHit, 1f, Environment))
            {
                return !Physics.Raycast(transform.position + Vector3.up * maxVaultHeight, -vaultHit.normal, 1f, Environment) && !Physics.Raycast(transform.position + Vector3.up * maxVaultHeight, orientation.forward, 1f, Environment);
            }
            else return false;
        }
        else return false;          
    }
    */

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
        return (mag * 0.3f) + Math.Abs(yMag * 2f);
    }

    void ResetWallJump()
    {
        if (!grounded) canWallJump = true;
    }
}
