using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float maxGroundSpeed;

    [Header("Air Movement")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float maxAirSpeed;

    bool jumped = false;

    [Header("Sliding")]
    [SerializeField] private Vector3 crouchScale;
    [SerializeField] private float slideForce;
    [SerializeField] private float crouchJumpForce;
    [SerializeField] private float maxSlideSpeed;
    private float crouchOffset;
    private Vector3 playerScale;

    [Header("WallRunning")]
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallClimbForce;
    private bool canAddWallRunForce = true;

    [Header("Vaulting")]
    [SerializeField] private float vaultDuration;
    public bool vaulting = false;

    [Header("Movement Control")]
    [SerializeField] private float friction;
    [SerializeField] private float slideFriction;
    [SerializeField] private float sharpness;
    [SerializeField] private float threshold;
    private float maxSpeed;

    bool crouched = false;
    bool readyToWallJump = true;
    int stepsSinceLastGrounded = 0;

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
        crouchOffset = crouchScale.y * 0.6f;
    }

    void FixedUpdate()
    {
        Movement();
    }

    void Update()
    {
        maxSpeed = ControlMaxSpeed();
    }

    private void Movement()
    {
        rb.AddForce(Vector3.down * 0.8f);

        if (s.PlayerInput.reachedMaxSlope) rb.AddForce(Vector3.down * 70f);

        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float speed = Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2)));

        CounterMovement(s.PlayerInput.input.x, s.PlayerInput.input.y, vel);
        if (speed > maxSpeed) rb.AddForce(-vel * (speed * 0.5f));

        if (s.PlayerInput.grounded && s.PlayerInput.jumping) Jump();

        if (s.PlayerInput.grounded || SnapToGround(vel)) stepsSinceLastGrounded = 0;
        else if (stepsSinceLastGrounded < 10) stepsSinceLastGrounded++;

        if (!s.PlayerInput.CanWallJump()) canAddWallRunForce = true;

        if (s.PlayerInput.CanWallJump() && s.PlayerInput.jumping && readyToWallJump) WallJump();
        if (s.PlayerInput.wallRunning && !s.PlayerInput.grounded) WallRun();
        if (s.PlayerInput.stopWallRun && readyToWallJump) StopWallRun();

        if (s.PlayerInput.crouching && !s.PlayerInput.wallRunning && !crouched) StartCrouch(s.PlayerInput.moveDir.normalized);
        if (!s.PlayerInput.crouching && crouched) StopCrouch();

        rb.AddForce(s.PlayerInput.moveDir * (moveSpeed * 2.5f), ForceMode.Acceleration);
    }

    private bool SnapToGround(Vector3 vel)
    {
        if (s.magnitude < 5f) return false;

        if (stepsSinceLastGrounded > 2 || vaulting || jumped || s.PlayerInput.grounded) return false;

        if (!Physics.Raycast(s.groundCheck.position, Vector3.down, out var snapHit, 2f, s.PlayerInput.Ground)) return false;

        rb.velocity = new Vector3(rb.velocity.x, -snapHit.distance * 10f, rb.velocity.z);
        rb.AddForce(-vel * 5f);
        s.PlayerInput.grounded = true;

        return true;
    }

    private void Jump()
    {
        jumped = true;

        if (s.PlayerInput.grounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            Vector3 jumpDir = (Vector3.up + s.PlayerInput.groundNormal).normalized;

            if (!crouched) rb.AddForce(jumpDir * jumpForce * 0.7f, ForceMode.Impulse);
            else if (crouched) rb.AddForce(jumpDir * crouchJumpForce * 0.7f, ForceMode.Impulse);

            CancelInvoke("ResetJump");
            Invoke("ResetJump", 0.3f);
        }
    }

    public IEnumerator VaultMovement(Vector3 newPos, float distance, Vector3 dir)
    {
        rb.useGravity = false;
        rb.velocity = Vector3.zero;

        Vector3 vel = Vector3.zero;
        float elapsed = 0f;

        distance *= 0.018f;
        distance = Mathf.Round(distance * 1000.0f) * 0.001f;

        float duration = vaultDuration + distance;

        while (elapsed < (duration * 2f))
        {
            vaulting = true;
            rb.MovePosition(Vector3.SmoothDamp(s.rb.position, newPos, ref vel, duration, 30f, Time.fixedDeltaTime));
            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        rb.AddForce(dir * 8f, ForceMode.VelocityChange);
        rb.AddForce(Vector3.down * (1 / distance) * 0.06f, ForceMode.VelocityChange);
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.y);

        rb.useGravity = true;
        vaulting = false;
    }

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

            float wallMagnitude = (rb.velocity.y * 2f);
            if (wallMagnitude < 0) wallMagnitude *= 1.2f;

            wallClimb = wallMagnitude + wallClimbForce;
            wallClimb = Mathf.Clamp(wallClimb, -20f, 10f);
            rb.AddForce(Vector3.up * wallClimb);
        }

        rb.AddForce(-s.PlayerInput.wallNormal * wallRunForce * 1.5f);
        rb.AddForce(-transform.up * wallRunForce * 0.6f);
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

    private void CounterMovement(float x, float z, Vector3 dir)
    {
        if (!s.PlayerInput.grounded) return;

        if (crouched)
        {
            rb.AddForce(-dir * slideFriction);
            return;
        }

        Vector3 mag = s.orientation.InverseTransformDirection(rb.velocity);

        if (x == 0 && z == 0 && dir.sqrMagnitude > (threshold * threshold))
            rb.AddForce(-dir * friction);

        if (x > 0 && mag.x < -threshold || x < 0 && mag.x > threshold)
            rb.AddForce(s.orientation.right * -mag.x * sharpness);

        if (z > 0 && mag.z < -threshold || z < 0 && mag.z > threshold)
            rb.AddForce(s.orientation.forward * -mag.z * sharpness);
    }

    public Vector2 Multiplier()
    {
        if (s.PlayerInput.grounded)
        {
            if (crouched) return new Vector2 (0.05f, 1f);
            return new Vector2 (1f, 1.05f);
        }

        if(s.PlayerInput.wallRunning) return new Vector2(0.01f, 30f);

        if (crouched) return new Vector2 (0.4f, 0.8f);

        return new Vector2 (0.6f, 0.8f);
    }

    private float ControlMaxSpeed()
    {
        if (crouched) return maxSlideSpeed;
        if (s.PlayerInput.grounded) return maxGroundSpeed;
        return maxAirSpeed;
    }

    void ResetJump()
    {
        jumped = false;
    }

    void ResetWallJump()
    {
        readyToWallJump = true;
    }
}
