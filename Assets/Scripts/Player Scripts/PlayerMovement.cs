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
    [SerializeField] private float vaultForce;
    public bool vaulting = false;

    private float vaultMulti = 0f;
    private float vaultTime = 0f;
    private float setVaultDuration;
    private Vector3 vaultPos = Vector3.zero;
    private Vector3 vaultNormal = Vector3.zero;
    private Vector3 vaultOriginalPos = Vector3.zero;

    [Header("Movement Control")]
    [SerializeField] private float friction;
    [SerializeField] private float slideFriction;
    [SerializeField] private float sharpness;
    [SerializeField] private float threshold;
    private float maxSpeed;

    bool crouched = false;
    bool readyToWallJump = true;
    int stepsSinceLastGrounded = 0;

    private Vector3 mag = Vector3.zero;

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
        setVaultDuration = vaultDuration;
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
        rb.AddForce(Vector3.down);

        if (s.PlayerInput.reachedMaxSlope) rb.AddForce(Vector3.down * 70f);

        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float speed = Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2)));

        if (s.PlayerInput.grounded) mag = s.orientation.InverseTransformDirection(rb.velocity);
        else if (mag != Vector3.zero) mag = Vector3.zero;

        CounterMovement(s.PlayerInput.input.x, s.PlayerInput.input.y, vel);
        if (speed > maxSpeed) rb.AddForce(-vel * (speed * 0.5f));

        if (vaulting)
        {
            rb.MovePosition(Vector3.Lerp(vaultOriginalPos, vaultPos, vaultTime / vaultDuration));
            vaultTime += Time.fixedDeltaTime * vaultMulti;

            if (vaultTime >= vaultDuration)
            {
                vaulting = false;
                rb.velocity = new Vector3(rb.velocity.x, -vaultForce * 0.05f, rb.velocity.z);
                rb.AddForce(vaultNormal * vaultForce * 0.2f, ForceMode.VelocityChange);
                rb.useGravity = true;
            }
        }

        if (s.PlayerInput.grounded && s.PlayerInput.jumping) Jump();

        if (s.PlayerInput.grounded || SnapToGround(vel)) stepsSinceLastGrounded = 0;
        else if (stepsSinceLastGrounded < 10) stepsSinceLastGrounded++;

        if (!s.PlayerInput.CanWallJump()) canAddWallRunForce = true;

        if (s.PlayerInput.CanWallJump() && s.PlayerInput.jumping && readyToWallJump) WallJump();
        if (s.PlayerInput.wallRunning && !s.PlayerInput.grounded) WallRun();
        if (s.PlayerInput.stopWallRun && readyToWallJump) StopWallRun();

        if (s.PlayerInput.crouching && !s.PlayerInput.wallRunning && !crouched) StartCrouch(s.PlayerInput.moveDir.normalized);
        if (!s.PlayerInput.crouching && crouched) StopCrouch();

        rb.AddForce(s.PlayerInput.moveDir * (moveSpeed * 3f), ForceMode.Acceleration);
    }

    private bool SnapToGround(Vector3 vel)
    {
        if (s.magnitude < 5f) return false;

        if (stepsSinceLastGrounded > 4 || vaulting || jumped || s.PlayerInput.grounded) return false;

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

    #region Vaulting
    public void Vault(Vector3 pos, Vector3 normal, float distance)
    {
        vaultPos = pos + Vector3.up * 2.8f;
        vaultNormal = normal;
        vaultOriginalPos = transform.position;
        vaultMulti = (1f / distance) * 25f;

        distance *= 0.025f;
        distance = Mathf.Round(distance * 100.0f) * 0.01f;
        vaultDuration = setVaultDuration + distance;
        vaultTime = 0f;
        vaulting = true;

        rb.useGravity = true;
        rb.velocity = new Vector3(0f, -7f, 0f);
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
    #endregion 

    #region Sliding
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
    #endregion

    private void CounterMovement(float x, float z, Vector3 dir)
    {
        if (!s.PlayerInput.grounded) return;

        if (crouched)
        {
            rb.AddForce(-dir * slideFriction);
            return;
        }

        if (!s.PlayerInput.moving && dir.sqrMagnitude > (threshold * threshold))
            rb.AddForce(-dir * friction);

        if (CounterMomentum(x, mag.x))
            rb.AddForce(s.orientation.right * -mag.x * sharpness);

        if (CounterMomentum(z, mag.z))
            rb.AddForce(s.orientation.forward * -mag.z * sharpness);
    }

    private bool CounterMomentum(float input, float mag)
    {
        if (input > 0 && mag < -threshold || input < 0 && mag > threshold) return true;
        else return false;
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
