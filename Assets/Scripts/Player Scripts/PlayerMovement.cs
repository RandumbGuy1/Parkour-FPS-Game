﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Movement")]
    public float moveSpeed;
    public float maxGroundSpeed;

    [Header("Air Movement")]
    public float jumpForce;
    public float jumpCooldown;
    public float maxAirSpeed;

    bool jumped = false;

    [Header("Sliding")]
    public Vector3 crouchScale;
    public float slideForce;
    public float crouchJumpForce;
    public float slideFriction;
    public float maxSlideSpeed;

    private float crouchOffset;
    private Vector3 playerScale;

    [Header("WallRunning")]
    public float wallJumpForce;
    public float wallRunForce;
    public float wallClimbForce;

    [Header("Movement Control")]
    public float friction;
    public float threshold;
    private float maxSpeed;

    bool crouched = false;
    bool cancelWallRun = true;

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
        crouchOffset = crouchScale.y * 0.5f;
    }

    void FixedUpdate()
    {
        Movement();
    }

    void LateUpdate()
    {
        MovementControl();
    }

    private void Movement()
    {
        if (s.PlayerInput.reachedMaxSlope) rb.AddForce(Vector3.down * 75f);

        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float speed = Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2)));

        CounterMovement(s.PlayerInput.input.x, s.PlayerInput.input.y, vel);
        if (speed > maxSpeed) rb.AddForce(-vel * (speed * 0.3f));

        if (s.PlayerInput.grounded && s.PlayerInput.jumping && !s.PlayerInput.canWallJump) Jump();

        if (s.PlayerInput.hitGround && !s.PlayerInput.grounded && !jumped && !s.PlayerInput.vaulting) SnapToGround(vel);

        if (s.PlayerInput.canWallJump && s.PlayerInput.jumping && !s.PlayerInput.grounded) WallJump();
        if (s.PlayerInput.wallRunning && !s.PlayerInput.grounded) WallRun();
        if (s.PlayerInput.stopWallRun && cancelWallRun) StopWallRun();

        if (s.PlayerInput.crouching && !s.PlayerInput.wallRunning && !crouched) StartCrouch(s.PlayerInput.moveDir.normalized);
        if (!s.PlayerInput.crouching && crouched) StopCrouch();

        rb.AddForce(s.PlayerInput.moveDir * (moveSpeed * 0.02f), ForceMode.VelocityChange);
    }

    private void SnapToGround(Vector3 vel)
    {
        if (s.PlayerInput.stepsSinceLastGrounded > 5) return;

        rb.AddForce(Vector3.down * 2.3f, ForceMode.VelocityChange);
        rb.AddForce(-vel * 15f);
    }

    private void Jump()
    {
        jumped = true;

        if (s.PlayerInput.grounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (!crouched) rb.AddForce(transform.up * jumpForce * 0.7f, ForceMode.Impulse);
            else if (crouched) rb.AddForce(transform.up * crouchJumpForce * 0.7f, ForceMode.Impulse);

            CancelInvoke("ResetJump");
            Invoke("ResetJump", 0.3f);
        }
    }

    private void WallJump()
    {
        s.PlayerInput.wallRunning = false;
        s.PlayerInput.grounded = false;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce * 0.7f, ForceMode.Impulse);
        rb.AddForce(s.PlayerInput.wallNormal * wallJumpForce, ForceMode.Impulse);

        cancelWallRun = false;
    }

    private void WallRun()
    {
        float wallClimb = 0f;

        if (s.PlayerInput.canAddWallRunForce)
        {
            cancelWallRun = true;
            s.PlayerInput.canAddWallRunForce = false;
            rb.useGravity = false;

            float wallMagnitude = (rb.velocity.y * 2f);
            if (wallMagnitude < 0) wallMagnitude *= 1.2f;

            wallClimb = wallMagnitude + wallClimbForce;
            wallClimb = Mathf.Clamp(wallClimb, -15f, 15f);
            rb.velocity = new Vector3(rb.velocity.x, wallClimb, rb.velocity.z);
        }

        rb.AddForce(-s.PlayerInput.wallNormal * wallRunForce * 0.8f);
        rb.AddForce(-transform.up * wallRunForce * 0.6f);
    }

    private void StopWallRun()
    {
        if (s.PlayerInput.wallRunning && cancelWallRun)
        {
            s.PlayerInput.wallRunning = false;
            s.PlayerInput.stopWallRun = false;

            rb.AddForce(s.PlayerInput.wallNormal * wallJumpForce * 0.8f, ForceMode.Impulse);

            cancelWallRun = false;
        }
    }

    private void StartCrouch(Vector3 dir)
    {
        crouched = true;

        if (s.PlayerInput.grounded) if (s.magnitude > 0.5f) rb.AddForce(dir * slideForce * Math.Abs(s.magnitude));

        transform.localScale = crouchScale;
        rb.position += (Vector3.down * crouchOffset);
    }

    private void StopCrouch()
    {
        crouched = false;
        rb.position += (Vector3.up * crouchOffset);
        transform.localScale = playerScale;

        if (s.PlayerInput.grounded) rb.velocity *= 0.8f;
    }

    private void CounterMovement(float x, float z, Vector3 slideDir)
    {
        if (!s.PlayerInput.grounded) return;

        if (crouched)
        {
            if (s.PlayerInput.grounded) rb.AddForce(-slideDir * slideFriction);
            return;
        }

        Vector3 mag = s.orientation.InverseTransformDirection(rb.velocity);

        if (x == 0 && Math.Abs(mag.x) > threshold || x > 0 && mag.x < -threshold || x < 0 && mag.x > threshold)
            rb.AddForce(s.orientation.right * -mag.x * friction);

        if (z == 0 && Math.Abs(mag.z) > threshold || z > 0 && mag.z < -threshold || z < 0 && mag.z > threshold)
            rb.AddForce(s.orientation.forward * -mag.z * friction);
    }

    private void MovementControl()
    {
        if (s.PlayerInput.grounded)
        {
            if (!crouched && !s.PlayerInput.wallRunning && maxSpeed != maxGroundSpeed) maxSpeed = maxGroundSpeed;            
            if (crouched && !s.PlayerInput.wallRunning && maxSpeed != maxSlideSpeed) maxSpeed = maxSlideSpeed;
        }

        if (!s.PlayerInput.grounded)
        {
            if (!s.PlayerInput.wallRunning && !crouched && maxSpeed != maxAirSpeed) maxSpeed = maxAirSpeed;
            if (!s.PlayerInput.wallRunning && crouched && maxSpeed != maxSlideSpeed) maxSpeed = maxSlideSpeed;
            if (s.PlayerInput.wallRunning && maxSpeed != maxAirSpeed) maxSpeed = maxAirSpeed;
        }
    }

    void ResetJump()
    {
        jumped = false;
    }
}
