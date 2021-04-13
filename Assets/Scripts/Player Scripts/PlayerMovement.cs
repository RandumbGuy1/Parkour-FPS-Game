using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Movement")]
    public float moveSpeed;
    public float maxGroundSpeed;
    private float multiplier = 1f;

    [Header("Air Movement")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMulti;
    public float maxAirSpeed;

    [Header("Sliding")]
    public Vector3 crouchScale;
    public float slideForce;
    public float crouchJumpForce;
    public float slideCounterMovement;
    public float maxSlideSpeed;
    public float crouchMulti;
    private float crouchOffset;
    private Vector3 playerScale;

    [Header("WallRunning")]
    public float wallJumpForce;
    public float wallRunForce;
    public float wallClimbForce;
    public float wallMulti;
    private float setClimbForce;

    [Header("Movement Control")]
    public float counterMovement;
    public float threshold;
    private float maxSpeed;

    bool crouched = false;
    bool cancelWallRun = true;

    [Header("Assignables")]
    private ScriptManager s;
    private Rigidbody rb;

    void Awake()
    {
        //Save 3.8
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerScale = transform.localScale;
        crouchOffset = crouchScale.y * 0.5f;
        setClimbForce = wallClimbForce;
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
        if (s.PlayerInput.reachedMaxSlope) rb.AddForce(-transform.up * s.PlayerInput.angle * 0.5f);
        
        //Movement Control
        CounterMovement(new Vector3(-rb.velocity.x, 0, -rb.velocity.z));

        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed)
        {
            Vector3 n = (rb.velocity.normalized * maxSpeed);
            rb.velocity = new Vector3(n.x, rb.velocity.y, n.z);
        }

        //Input
        if (s.PlayerInput.grounded && s.PlayerInput.jumping) Jump();
        if (s.PlayerInput.canWallJump && s.PlayerInput.jumping && !s.PlayerInput.grounded) WallJump();

        if (s.PlayerInput.wallRunning && !s.PlayerInput.grounded) WallRun();
        if (s.PlayerInput.stopWallRun && cancelWallRun) StopWallRun();

        if (s.PlayerInput.startCrouch && !s.PlayerInput.wallRunning) StartCrouch(s.PlayerInput.moveDir);
        if (s.PlayerInput.stopCrouch && crouched) StopCrouch();

        //Move Player
        rb.AddForce(s.PlayerInput.moveDir * (moveSpeed * 0.02f), ForceMode.VelocityChange);
    }

    private void Jump()
    {
        if (s.PlayerInput.grounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (!crouched) rb.AddForce(transform.up * jumpForce * 0.7f, ForceMode.Impulse);
            else if (crouched) rb.AddForce(transform.up * crouchJumpForce * 0.7f, ForceMode.Impulse);
        }
    }

    private void WallJump()
    {
        s.PlayerInput.wallRunning = false;
        s.PlayerInput.canWallJump = false;
        s.PlayerInput.grounded = false;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(s.PlayerInput.wallJump * wallJumpForce, ForceMode.Impulse);
        rb.AddForce(transform.up * jumpForce * 0.7f, ForceMode.Impulse);

        cancelWallRun = false;
    }

    private void WallRun()
    {
        if (s.PlayerInput.canAddWallRunForce)
        {
            cancelWallRun = true;
            s.PlayerInput.canAddWallRunForce = false;
            rb.useGravity = false;
            rb.AddForce(s.orientation.forward * s.PlayerInput.input.y * wallRunForce * 100f);

            float wallMagnitude = (rb.velocity.y * 0.8f);

            wallClimbForce = setClimbForce;
            wallClimbForce = wallMagnitude + wallClimbForce;
            wallClimbForce = Mathf.Clamp(wallClimbForce, -10f, 10f);
            rb.velocity = (s.orientation.forward * s.PlayerInput.input.y * (wallRunForce * 0.5f)) + new Vector3(0, wallClimbForce, 0);
        }

        rb.AddForce(-s.PlayerInput.wallJump * wallRunForce * 0.2f);
        rb.AddForce(-transform.up * wallRunForce * 0.3f);
    }

    private void StopWallRun()
    {
        if (s.PlayerInput.wallRunning && cancelWallRun)
        {
            s.PlayerInput.wallRunning = false;
            s.PlayerInput.stopWallRun = false;

            rb.AddForce(s.PlayerInput.wallJump * wallJumpForce * 0.6f, ForceMode.Impulse);

            cancelWallRun = false;
        }
    }

    private void StartCrouch(Vector3 dir)
    {
        crouched = true;
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - crouchOffset, transform.position.z);

        if (s.PlayerInput.grounded) if (rb.velocity.magnitude > 0.5f) rb.AddForce(dir * slideForce * Math.Abs(rb.velocity.magnitude) / 2);
    }

    private void StopCrouch()
    {
        crouched = false;
        transform.position = new Vector3(transform.position.x, transform.position.y + (crouchOffset), transform.position.z);
        transform.localScale = playerScale;

        if (s.PlayerInput.grounded) rb.velocity *= 0.8f;
    }

    private void CounterMovement(Vector3 dir)
    {
        if (crouched && s.PlayerInput.grounded)
        {
            if (s.PlayerInput.grounded) rb.AddForce(dir * slideCounterMovement);
            return;
        }

        if (!s.PlayerInput.grounded || s.PlayerInput.moving) return;

        if (rb.velocity.magnitude >= threshold) rb.AddForce(dir * counterMovement);
    }

    private void MovementControl()
    {
        if (s.PlayerInput.grounded)
        {
            if (!crouched && !s.PlayerInput.wallRunning) maxSpeed = maxGroundSpeed;            
            if (crouched && !s.PlayerInput.wallRunning) maxSpeed = maxSlideSpeed;
        }

        if (!s.PlayerInput.grounded)
        {
            if (!s.PlayerInput.wallRunning && !crouched) maxSpeed = maxAirSpeed;
            if (!s.PlayerInput.wallRunning && crouched) maxSpeed = maxSlideSpeed;
            if (s.PlayerInput.wallRunning) maxSpeed = maxAirSpeed;
        }
    }
}
