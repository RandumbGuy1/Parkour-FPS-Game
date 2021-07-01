﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float maxGroundSpeed;
    [SerializeField] private float maxAirSpeed;
    [SerializeField] private float maxSlideSpeed;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private int maxJumpSteps;
    
    [Header("Sliding")]
    [SerializeField] private float crouchScale;
    [SerializeField] private float crouchSmoothTime;
    [SerializeField] private float slideForce;
    private float crouchVel = 0f;
    private float crouchOffset;
    private bool crouched = false;
    private Vector3 playerScale;

    [Header("WallRunning")]
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallClimbForce;
    private bool canAddWallRunForce = true;
    private bool readyToWallJump = true;

    [Header("Vaulting")]
    [SerializeField] private float vaultDuration;
    [SerializeField] private float vaultForce;
    public bool vaulting = false;

    [Header("Movement Control")]
    [SerializeField] private float friction;
    [SerializeField] private float slideFriction;
    [SerializeField] private float threshold;
    private float maxSpeed;

    private int stepsSinceLastGrounded = 0;
    private int stepsSinceLastJumped = 0;

    private Vector2 input;
    private bool jumping;
    private bool crouching;
    private bool grounded;
    private Vector3 moveDir;
    public bool moving { get; private set; }

    public Vector3 relativeVel { get; private set; }
    public float magnitude { get; private set; }
    public Vector3 velocity { get; private set; }

    [Header("Assignables")]
    private ScriptManager s;
    private Rigidbody rb;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerScale = transform.localScale;
        crouchOffset = crouchScale * 0.2f;
    }

    #region Movement
    public void Movement()
    {
        rb.AddForce(Vector3.down * 3f);

        if (s.PlayerInput.reachedMaxSlope) rb.AddForce(Vector3.down * 70f);

        relativeVel = s.orientation.InverseTransformDirection(rb.velocity);
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float maxSpeed = ControlMaxSpeed();
        float coefficientOfFriction = moveSpeed * 3f / maxSpeed;

        if (vel.sqrMagnitude > maxSpeed * maxSpeed) rb.AddForce(-vel * coefficientOfFriction, ForceMode.Acceleration);
        rb.useGravity = UseGravity();

        ProcessInput();

        moveDir = (grounded ? GroundMove(CalculateMultiplier()) : AirMove(CalculateMultiplier(), input));
        rb.AddForce(moveDir * moveSpeed * 3f, ForceMode.Acceleration);

        magnitude = rb.velocity.magnitude;
        velocity = rb.velocity;
    }

    private Vector3 GroundMove(Vector2 multiplier)
    {
        Friction(relativeVel);

        Vector3 inputDir = (s.orientation.forward * input.y * multiplier.y + s.orientation.right * input.x * multiplier.x);
        Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, s.PlayerInput.groundNormal);

        float dot = Vector3.Dot(Vector3.up, slopeDir);
        return dot < 0f ? slopeDir : inputDir;
    }

    private Vector3 AirMove(Vector2 multiplier, Vector2 input)
    {
        if (input.x > 0 && relativeVel.x > 25 || input.x < 0 && relativeVel.x < -25) input.x *= 0.1f;
        if (input.y > 0 && relativeVel.z > 25 || input.y < 0 && relativeVel.z < -25) input.y *= 0.1f;

        return s.orientation.forward * input.y * multiplier.y + s.orientation.right * input.x * multiplier.x;
    }
    #endregion

    #region Surface Contact
    private bool SnapToGround()
    {
        float speed = magnitude;

        if (speed < 3f || stepsSinceLastGrounded > 3 || stepsSinceLastJumped < maxJumpSteps || vaulting || grounded) return false;
        if (!Physics.Raycast(s.groundCheck.position, Vector3.down, out var snapHit, 1.8f, s.PlayerInput.Ground)) return false;

        s.PlayerInput.grounded = true;

        float dot = Vector3.Dot(rb.velocity, Vector3.up);
        if (dot > 0) rb.velocity = (rb.velocity - (snapHit.normal * dot * 1.1f)).normalized * speed;
        else rb.velocity = (rb.velocity - snapHit.normal).normalized * speed;

        return true;
    }

    private bool UseGravity()
    {
        if (vaulting || s.PlayerInput.wallRunning) return false;

        if (grounded && s.PlayerInput.onRamp && !crouching && stepsSinceLastJumped >= maxJumpSteps && !moving)
        {
            if (velocity.y > 0) rb.velocity = new Vector3(rb.velocity.x, -1f, rb.velocity.z);
            if (velocity.y < 0 && magnitude < 2f) rb.velocity = Vector3.zero;

            return false;
        }

        return true;
    }
    #endregion

    #region Jumping
    private void Jump()
    {
        stepsSinceLastJumped = 0;

        s.PlayerInput.grounded = false;
        rb.useGravity = true;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce * 0.7f, ForceMode.Impulse);
    }
    #endregion

    #region Vaulting
    public IEnumerator Vault(Vector3 pos, Vector3 normal, float distance)
    {
        rb.isKinematic = true;
        vaulting = true;

        distance = (distance * distance) * 0.05f;
        distance = Mathf.Round(distance * 100.0f) * 0.01f;

        Vector3 vaultOriginalPos = transform.position;
        float elapsed = 0f;
        float vaultDuration = this.vaultDuration + distance;

        s.PlayerInput.grounded = false;

        while (elapsed < vaultDuration)
        {
            float t = elapsed / vaultDuration;
            t = t * t * t * (t * (6f * t - 15f) + 10f);

            transform.position = Vector3.Lerp(vaultOriginalPos, pos, t);
            elapsed += Time.deltaTime * 2f;

            yield return null;
        }

        vaulting = false;
        rb.isKinematic = false;
        rb.velocity = normal * vaultForce * 0.5f;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    }
    #endregion

    #region Wall Movement
    private void WallJump()
    {
        if (readyToWallJump)
        {
            readyToWallJump = false;
            s.PlayerInput.grounded = false;
            s.PlayerInput.nearWall = false;

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            rb.AddForce(transform.up * jumpForce * 0.7f, ForceMode.Impulse);
            rb.AddForce(s.PlayerInput.wallNormal * wallJumpForce, ForceMode.Impulse);

            CancelInvoke("ResetWallJump");
            Invoke("ResetWallJump", 0.3f);
        }
    }

    private void WallRun()
    {
        float wallClimb = 0f;

        if (canAddWallRunForce)
        {
            canAddWallRunForce = false;
            rb.useGravity = false;

            float wallMagnitude = (rb.velocity.y);
            if (wallMagnitude < 0) wallMagnitude *= 1.2f;

            wallClimb = wallMagnitude + wallClimbForce;
            wallClimb = Mathf.Clamp(wallClimb, -20f, 10f);
            rb.AddForce(Vector3.up * wallClimb);
        }

        rb.AddForce(-s.PlayerInput.wallNormal * wallRunForce * 2f);
        rb.AddForce(-transform.up * wallRunForce * 0.5f);
    }

    private void StopWallRun()
    {
        if (s.PlayerInput.wallRunning && readyToWallJump)
        {
            readyToWallJump = false;
            s.PlayerInput.nearWall = false;

            rb.AddForce(s.PlayerInput.wallNormal * wallJumpForce * 0.8f, ForceMode.Impulse);

            CancelInvoke("ResetWallJump");
            Invoke("ResetWallJump", 0.3f);
        }
    }
    #endregion 

    #region Sliding
    private void Crouch(Vector3 dir)
    {
        crouched = true;
        if (grounded) if (magnitude > 0.5f) rb.AddForce(dir * slideForce * magnitude);
    }

    private void UpdateCrouchScale()
    {
        if (crouching && transform.localScale.y == crouchScale || !crouching && transform.localScale.y == playerScale.y) return;

        transform.localScale = new Vector3(playerScale.x, Mathf.SmoothDamp(transform.localScale.y, (crouched ? crouchScale : playerScale.y), ref crouchVel, crouchSmoothTime), playerScale.z);
    
        if (crouching && transform.localScale.y < crouchScale + 0.01f) transform.localScale = new Vector3(playerScale.x, crouchScale, playerScale.z);
        if (!crouching && transform.localScale.y > playerScale.y - 0.01f) transform.localScale = playerScale;
    }
    #endregion

    #region Friction
    private void Friction(Vector3 mag)
    {
        if (jumping) return;

        if (crouched)
        {
            rb.AddForce(-rb.velocity.normalized * slideFriction * 3f * (magnitude * 0.1f));
            return;
        }

        if (Math.Abs(input.x) < threshold && Math.Abs(mag.x) > threshold || CounterMomentum(input.x, mag.x))
            rb.AddForce(s.orientation.right * -mag.x * friction * 2f, ForceMode.Acceleration);

        if (Math.Abs(input.y) < threshold && Math.Abs(mag.z) > threshold || CounterMomentum(input.y, mag.z))
            rb.AddForce(s.orientation.forward * -mag.z * friction * 2f, ForceMode.Acceleration);
    }

    private bool CounterMomentum(float input, float mag)
    {
        if (input > 0 && mag < -threshold || input < 0 && mag > threshold) return true;
        else return false;
    }

    #endregion

    #region Input
    public void SetInput(Vector2 input, bool jumping, bool crouching, bool grounded)
    {
        this.input = input;
        this.jumping = jumping;
        this.crouching = crouching;
        this.grounded = grounded;

        moving = input != Vector2.zero;
    }

    private void ProcessInput()
    {
        if (stepsSinceLastGrounded < 3 && jumping) Jump();
        if (stepsSinceLastJumped < maxJumpSteps) stepsSinceLastJumped++;

        if (grounded || SnapToGround()) stepsSinceLastGrounded = 0;
        else if (stepsSinceLastGrounded < 10) stepsSinceLastGrounded++;

        if (crouching && !s.PlayerInput.wallRunning && !crouched) Crouch(moveDir);
        if (!crouching && crouched)
        {
            crouched = false;
            rb.velocity *= 0.8f;
        }

        UpdateCrouchScale();

        if (s.PlayerInput.CanWallJump() && jumping && readyToWallJump) WallJump();
        if (!s.PlayerInput.CanWallJump()) canAddWallRunForce = true;
        if (s.PlayerInput.wallRunning && !grounded) WallRun();

        if (s.PlayerInput.isWallLeft && input.x > 0 && s.PlayerInput.wallRunning || s.PlayerInput.isWallRight && input.x < 0 && s.PlayerInput.wallRunning && readyToWallJump) 
            StopWallRun();
    }
    #endregion

    public Vector2 CalculateMultiplier()
    {
        if (vaulting) return new Vector2(0f, 0f);

        if (grounded) return (crouched ? new Vector2(0.01f, 0.01f) : new Vector2(1f, 1.05f));

        if (s.PlayerInput.wallRunning) return new Vector2(0.01f, 0.4f);
        if (crouched) return new Vector2(0.4f, 0.3f);

        return new Vector2(0.5f, 0.5f);
    }

    private float ControlMaxSpeed()
    {
        if (crouched) return maxSlideSpeed;
        if (jumping) return maxAirSpeed;
        if (grounded) return maxGroundSpeed;

        return maxAirSpeed;
    }

    void ResetWallJump()
    {
        readyToWallJump = true;
    }
}
