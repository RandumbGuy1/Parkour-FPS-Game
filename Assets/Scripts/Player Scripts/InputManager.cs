﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InputManager : MonoBehaviour
{
    public Vector2 input { get { return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized; } }
    public Vector3 moveDir 
    { 
        get 
        { 
            Vector3 inputDir = (orientation.forward * input.y * multiplier * multiplierV + orientation.right * input.x * multiplier);
            Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, groundNormal);

            float dot = Vector3.Dot(Vector3.up, slopeDir);
            return dot < 0f ? slopeDir : inputDir;
        } 
    }

    public Vector3 groundNormal { get; private set; }
    public Vector3 wallNormal { get; private set; }

    private float multiplier, multiplierV;

    [Header("Thresholds")]
    [Range(0f, 90f)] 
    public float maxSlopeAngle;
    public float minimumJumpHeight;

    [Header("States")]
    public bool grounded;
    public bool nearWall;
    public bool reachedMaxSlope;

    private bool isWallLeft = false;
    private bool isWallRight = false;
    public bool wallRunning { get; private set; }
    public bool stopWallRun { get; private set; }

    public bool jumping { get { return Input.GetKeyDown(jumpKey); } }
    public bool crouching { get { return Input.GetKey(crouchKey); } }
    public bool interacting { get { return Input.GetKeyDown(interactKey); } }
    public bool dropping { get { return Input.GetKeyDown(dropKey); } }
    public bool moving { get { return input.x != 0f || input.y != 0f; } }

    public float mouseScroll { get { return Input.GetAxis("Mouse ScrollWheel"); } }

    private bool fast = false;

    [Header("KeyBinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.Q;

    [Header("Collision")]
    public LayerMask Ground;
    [SerializeField] private LayerMask Environment;
    [SerializeField] private float groundCancelDelay;
    [SerializeField] private float wallCancelDelay;

    private bool cancelWall = true;
    private int wallCancelSteps = 0;

    private bool cancelGround = false;
    private int groundCancelSteps = 0;

    [Header("Assignables")]
    [SerializeField] private ParticleSystem sprintEffect;
    [SerializeField] private Transform orientation;
    private RaycastHit slopeHit;
    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
    }

    void FixedUpdate()
    {
        UpdateCollisions();
    }

    void Update()
    {
        if (nearWall && isWallLeft && CanWallJump() && input.x < 0 || nearWall && isWallRight && CanWallJump() && input.x > 0) wallRunning = true;
        stopWallRun = isWallLeft && input.x > 0 && wallRunning || isWallRight && input.x < 0 && wallRunning || crouching && wallRunning;

        if (Physics.Raycast(s.groundCheck.position, Vector3.down, out slopeHit, 1.5f, Ground))
            reachedMaxSlope = Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle;
        else reachedMaxSlope = false;

        CheckForWall();
        MovementControl();

        CameraTilt();
        SprintEffect();
    }

    #region Movement Calculations
    private void UpdateCollisions()
    {
        if (grounded) 
        {
            if (!cancelGround) cancelGround = true;
            else 
            {
                groundCancelSteps++;
                if ((float) groundCancelSteps > groundCancelDelay)
                {
                    groundNormal = Vector3.up;
                    grounded = false;
                }
            }
        }

        if (nearWall)
        {
            if (!cancelWall) cancelWall = true;
            else
            {
                wallCancelSteps++;
                if ((float) wallCancelSteps > wallCancelDelay)
                {
                    nearWall = false;
                    wallNormal = Vector3.zero;
                    isWallLeft = false;
                    isWallRight = false;
                }
            }
        }
    }

    public bool CanWallJump()
    {
        if (!nearWall || s.PlayerMovement.vaulting || grounded) return false;
        if (reachedMaxSlope) return false;
        return !Physics.Raycast(s.groundCheck.position, Vector3.down, minimumJumpHeight, Ground);
    }

    private void CheckForWall()
    {
        if (nearWall)
        {
            isWallLeft = Physics.Raycast(transform.position, -orientation.right, 1f, Environment) && !isWallRight;
            isWallRight = Physics.Raycast(transform.position, orientation.right, 1f, Environment) && !isWallLeft;
        }

        if (!CanWallJump() || !isWallRight && !isWallLeft)
        {
            wallRunning = false;
            s.rb.useGravity = true;
        }    
    }

    private void MovementControl()
    {
        if (grounded)
        {
            if (multiplierV != 1.05f) multiplierV = 1.1f;
            if (!crouching && !wallRunning && multiplier != 1f) multiplier = 1f;
            if (crouching && !wallRunning && multiplier != 0.05f) multiplier = 0.05f;
        }

        if (!grounded)
        {
            if (!wallRunning && !crouching && multiplier != 0.5f && multiplierV != 0.9f)
            {
                multiplier = 0.5f;
                multiplierV = 0.9f;
            }
            if (!wallRunning && crouching && multiplier != 0.3f && multiplierV != 0.8f)
            {
                multiplier = 0.3f; 
                multiplierV = 0.8f;
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
        if (crouching) s.CamInput.CameraSlide();

        if (wallRunning && isWallLeft) s.CamInput.CameraWallRun(-1);
        if (wallRunning && isWallRight) s.CamInput.CameraWallRun(1);

        if (!wallRunning && Math.Abs(s.CamInput.CameraTilt) > 0f && !crouching || !wallRunning && s.CamInput.fov > 80f)
            s.CamInput.ResetCameraTilt();
    }

    private void SprintEffect()
    {
        if (s.magnitude >= 25f && !fast)
        {
            sprintEffect.Play();
            fast = true;
        }
        else if (s.magnitude < 25f && fast)
        {
            sprintEffect.Stop();
            fast = false;
        }

        if (s.magnitude >= 25f)
        {
            var em = sprintEffect.emission;
            em.rateOverTime = s.magnitude;
        }
    }
    #endregion

    #region Collision Detection
    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | 1 << layer)) return;

        Vector3 normal = col.GetContact(0).normal;
        
        if (IsFloor(normal)) if (!grounded) Land(LandVel(s.magnitude, Math.Abs(s.velocity.y)));

        #region Vaulting
        if (IsVaultable(normal))
        {
            if (wallRunning || crouching || nearWall || reachedMaxSlope || Environment != (Environment | 1 << layer)) return;

            Vector3 vaultDir = new Vector3(-normal.x, 0, -normal.z);
            Vector3 dir = moveDir;
            Vector3 vaultCheck = s.playerHead.position + (Vector3.down * 0.5f);

            if (Vector3.Dot(dir.normalized, normal) > -0.5f) return;
            if (Physics.Raycast(vaultCheck, dir.normalized, 1.3f, Environment) || Physics.Raycast(vaultCheck, -normal, 1.3f, Environment)) return;

            RaycastHit hit;
            if (!Physics.Raycast(vaultCheck + (dir + vaultDir).normalized, Vector3.down, out hit, 3f, Environment)) return;

            Vector3 vaultPoint = hit.point + (Vector3.up * 2.1f);
            float distance = Vector3.Distance(s.rb.position, vaultPoint);
            vaultPoint += (Vector3.up * (distance * 0.3f));

            StartCoroutine(s.PlayerMovement.VaultMovement(vaultPoint, distance, dir.normalized));
        }
        #endregion
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

                grounded = true;
                cancelGround = false;
                groundCancelSteps = 0;
                groundNormal = normal;
            }

            if (IsWall(normal))
            {
                if (Environment != (Environment | 1 << layer)) continue;

                nearWall = true;
                wallCancelSteps = 0;
                cancelWall = false;
                wallNormal = normal;
            }
        }
    }
    #endregion

    private void Land(float impactForce)
    {
        s.Effects.CameraLand(impactForce);
        if (impactForce > 70f) ObjectPooler.Instance.Spawn("Land Effects", transform.position + Vector3.down, Quaternion.Euler(-90, 0, 0));
    }

    float LandVel(float mag, float yMag)
    {
        return (mag * 0.4f) + Math.Abs(yMag * 2.8f);
    }

    bool IsFloor(Vector3 normal)
    {
        return Vector3.Angle(Vector3.up, normal) < maxSlopeAngle;
    }

    bool IsWall(Vector3 normal)
    {
        return Math.Abs(Vector3.Dot(normal, Vector3.up)) < 0.1f;
    }

    bool IsVaultable(Vector3 normal)
    {
        return Math.Abs(Vector3.Dot(normal, Vector3.up)) < 0.33f;
    }
}
