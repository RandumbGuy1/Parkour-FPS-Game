﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InputManager : MonoBehaviour
{
    public Vector2 input { get; private set; }

    public Vector3 groundNormal { get; private set; }
    public Vector3 wallNormal { get; private set; }

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
    private ScriptManager s;
    private RaycastHit slopeHit;

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
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (nearWall && isWallLeft && CanWallJump() && input.x < 0 || nearWall && isWallRight && CanWallJump() && input.x > 0) wallRunning = true;
        stopWallRun = isWallLeft && input.x > 0 && wallRunning || isWallRight && input.x < 0 && wallRunning;

        if (Physics.Raycast(s.groundCheck.position, Vector3.down, out slopeHit, 1.5f, Ground))
            reachedMaxSlope = Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle;
        else reachedMaxSlope = false;

        CheckForWall();
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
        if (!nearWall || s.PlayerMovement.vaulting || grounded || crouching) return false;
        if (reachedMaxSlope) return false;
        return !Physics.Raycast(s.groundCheck.position, Vector3.down, minimumJumpHeight, Ground);
    }

    private void CheckForWall()
    {
        if (nearWall)
        {
            float dot = Vector3.Dot(s.orientation.right, wallNormal);

            isWallLeft = dot > 0.8f;
            isWallRight = dot < -0.8f;
        }

        if (!CanWallJump() || !isWallRight && !isWallLeft)
        {
            wallRunning = false;
            if (!s.PlayerMovement.vaulting) s.rb.useGravity = true;
        }    
    }
    #endregion

    #region Visual Effects
    private void CameraTilt()
    {
        if (crouching) s.CameraInput.CameraSlide();

        if (wallRunning && isWallLeft) s.CameraInput.CameraWallRun(-1);
        if (wallRunning && isWallRight) s.CameraInput.CameraWallRun(1);

        if (!wallRunning)
            s.CameraInput.ResetCameraTilt();
    }

    private void SprintEffect()
    {
        if (s.magnitude >= 25f)
        {
            if (!fast)
            {
                fast = true;
                sprintEffect.Play();
            }

            var em = sprintEffect.emission;
            em.rateOverTime = s.magnitude;
        }
        else if (fast)
        {
            sprintEffect.Stop();
            fast = false;
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
            if (s.PlayerMovement.vaulting || wallRunning || crouching || reachedMaxSlope || Environment != (Environment | 1 << layer)) return;

            Vector3 vaultDir = normal;
            vaultDir.y = 0f;
            vaultDir.Normalize();

            Vector3 dir = s.PlayerMovement.moveDir;
            dir.y = 0f;
            dir.Normalize();

            Vector3 vaultHeight = s.playerHead.position + (Vector3.down * 0.5f);
            Vector3 vaultCheck = s.playerHead.position + Vector3.up * 0.5f + Vector3.down * 0.05f;

            if (Vector3.Dot(dir.normalized, -vaultDir) < 0.3f) return;
            if (Physics.Raycast(vaultHeight, Vector3.up, 2f, Environment)) return;
            if (Physics.Raycast(vaultHeight, dir, 1.3f, Environment) || Physics.Raycast(vaultHeight, -vaultDir, 1.3f, Environment)) return;

            if (!Physics.Raycast(vaultCheck - vaultDir, Vector3.down, out var vaultHit, 3.8f, Environment)) return;

            s.rb.AddForce(vaultDir * 10f, ForceMode.VelocityChange);

            Vector3 vaultPoint = vaultHit.point + (Vector3.up * 3f) + (vaultDir * 0.8f);
            float distance = vaultPoint.y - s.groundCheck.position.y;

            s.PlayerMovement.Vault(vaultPoint, -vaultDir, distance);
        }
        #endregion
    }

    void OnCollisionStay(Collision col)
    {
        int layer = col.gameObject.layer;

        for (int i = 0; i < col.contactCount; i++)
        {
            ContactPoint contact = col.GetContact(i);

            if (IsFloor(contact.normal, contact.point))
            {
                if (Ground != (Ground | 1 << layer)) continue;

                grounded = true;
                cancelGround = false;
                groundCancelSteps = 0;
                groundNormal = contact.normal;
            }

            if (IsWall(contact.normal))
            {
                if (Environment != (Environment | 1 << layer)) continue;

                nearWall = true;
                cancelWall = false;
                wallCancelSteps = 0;
                wallNormal = contact.normal;
            }
        }
    }
    #endregion

    private void Land(float impactForce)
    {
        s.CameraLandBob.CameraLand(impactForce);
        if (impactForce > 70f) ObjectPooler.Instance.Spawn("Land Effects", transform.position + Vector3.down, Quaternion.Euler(-90, 0, 0));
    }

    float LandVel(float mag, float yMag)
    {
        return (mag * 0.4f) + Math.Abs(yMag * 2.8f);
    }

    bool IsFloor(Vector3 normal, Vector3 point)
    {
        return Vector3.Angle(Vector3.up, normal) < maxSlopeAngle && point.y <= s.rb.position.y;
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
        return Math.Abs(Vector3.Dot(normal, Vector3.up)) < 0.34f;
    }
}
