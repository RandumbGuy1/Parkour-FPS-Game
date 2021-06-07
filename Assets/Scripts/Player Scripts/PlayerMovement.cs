using System.Collections;
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
    private bool jumped = false;

    [Header("Sliding")]
    [SerializeField] private Vector3 crouchScale;
    [SerializeField] private float slideForce;
    private float crouchOffset;
    private Vector3 playerScale;
    private bool crouched = false;

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

    private float vaultTime = 0f;
    private float setVaultDuration;
    private Vector3 vaultPos = Vector3.zero;
    private Vector3 vaultNormal = Vector3.zero;
    private Vector3 vaultOriginalPos = Vector3.zero;
    bool stepUp = false;

    [Header("Movement Control")]
    [SerializeField] private float friction;
    [SerializeField] private float slideFriction;
    [SerializeField] private float sharpness;
    [SerializeField] private float threshold;
    private float maxSpeed;

    [Header("Collision")]
    [SerializeField] private LayerMask Ground;
    [SerializeField] private LayerMask Environment;

    private int stepsSinceLastGrounded = 0;

    private Vector2 multiplier;
    private Vector2 input;
    private bool jumping;
    private bool crouching;
    private bool grounded;
    public Vector3 moveDir { get; private set; }

    public Vector3 mag { get; private set; }

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

    #region Movement
    public void Movement()
    {
        rb.AddForce(Vector3.down);

        if (s.PlayerInput.reachedMaxSlope) rb.AddForce(Vector3.down * 70f);

        mag = s.orientation.InverseTransformDirection(rb.velocity);

        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        Vector2 multiplier = CalculateMultiplier();
        float speed = vel.magnitude;

        if (speed > ControlMaxSpeed()) rb.AddForce(-vel * (speed * 0.5f));
        CounterMovement(input, vel, mag);

        if (stepsSinceLastGrounded < 3 && jumping) Jump();
        if (grounded || SnapToGround()) stepsSinceLastGrounded = 0;
        else if (stepsSinceLastGrounded < 10) stepsSinceLastGrounded++;

        if (!s.PlayerInput.CanWallJump()) canAddWallRunForce = true;

        if (s.PlayerInput.CanWallJump() && jumping && readyToWallJump) WallJump();
        if (s.PlayerInput.wallRunning && !grounded) WallRun();
        if (s.PlayerInput.stopWallRun && readyToWallJump) StopWallRun();

        if (crouching && !s.PlayerInput.wallRunning && !crouched) StartCrouch(vel.normalized);
        if (!crouching && crouched) StopCrouch();

        if (input.x > 0 && mag.x > 25 || input.x < 0 && mag.x < -25) input.x = 0f;
        if (input.y > 0 && mag.z > 25 || input.y < 0 && mag.z < -25) input.y = 0f;

        Vector3 inputDir = (s.orientation.forward * input.y * multiplier.y + s.orientation.right * input.x * multiplier.x);
        Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, s.PlayerInput.groundNormal);

        float dot = Vector3.Dot(Vector3.up, slopeDir);
        moveDir = dot < 0f ? slopeDir : inputDir;

        rb.AddForce(moveDir * moveSpeed * 3f, ForceMode.Acceleration);
    }
    #endregion

    #region Surface Contact
    private bool SnapToGround()
    {
        float speed = s.magnitude;

        if (speed < 3f || stepsSinceLastGrounded > 3 || vaulting || jumped || grounded) return false;
        if (!Physics.Raycast(s.groundCheck.position, Vector3.down, out var snapHit, 1.8f, Ground)) return false;

        s.PlayerInput.grounded = true;

        float dot = Vector3.Dot(rb.velocity, Vector3.up);
        if (dot > 0) rb.velocity = (rb.velocity - snapHit.normal * dot).normalized * (speed);
        else rb.velocity = (rb.velocity - snapHit.normal).normalized * (speed);

        return true;
    }
    #endregion

    #region Jumping
    private void Jump()
    {
        jumped = true;

        s.PlayerInput.grounded = false;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce * 0.7f, ForceMode.Impulse);

        CancelInvoke("ResetJump");
        Invoke("ResetJump", jumpCooldown);
    }
    #endregion

    #region Vaulting
    public void Vault(Vector3 pos, Vector3 normal, float distance)
    {
        vaultPos = pos;
        vaultNormal = normal;
        vaultOriginalPos = transform.position;

        distance = (distance * distance) * 0.04f;
        distance = Mathf.Round(distance * 100.0f) * 0.01f;
        vaultDuration = setVaultDuration + distance;
        vaultTime = 0f;
        vaulting = true;

        stepUp = vaultDuration < 0.5f;
        
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        s.PlayerInput.grounded = false;
    }

    public void VaultMovement()
    {
        if (vaulting)
        {
            float t = vaultTime / vaultDuration;

            if (stepUp) t = Mathf.Sin(t * Mathf.PI * 0.5f);
            else t = t * t * t * (t * (6f * t - 15f) + 10f);

            transform.position = Vector3.Lerp(vaultOriginalPos, vaultPos, t);
            vaultTime += Time.deltaTime * 2;

            if (vaultTime >= vaultDuration)
            {
                vaulting = false;
                rb.velocity = vaultNormal * vaultForce * 0.3f;
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.isKinematic = false;
            }
        }
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

        rb.AddForce(-s.PlayerInput.wallNormal * wallRunForce * 1.7f);
        rb.AddForce(-transform.up * wallRunForce * 0.7f);
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

        if (grounded) if (s.magnitude > 0.5f) rb.AddForce(dir * slideForce * Math.Abs(s.magnitude));

        transform.localScale = crouchScale;
        rb.position += (Vector3.down * crouchOffset);
    }

    private void StopCrouch()
    {
        crouched = false;
        rb.position += (Vector3.up * crouchOffset);
        transform.localScale = playerScale;
    }
    #endregion

    #region Friction
    private void CounterMovement(Vector2 input, Vector3 dir, Vector3 mag)
    {
        if (!grounded) return;

        if (crouched)
        {
            rb.AddForce(-dir * slideFriction);
            return;
        }

        if (input.x == 0 && Math.Abs(mag.x) > threshold)
            rb.AddForce(s.orientation.right * -mag.x * friction);

        if (CounterMomentum(input.x, mag.x))
            rb.AddForce(s.orientation.right * -mag.x * sharpness, ForceMode.Acceleration);

        if (input.y == 0 && Math.Abs(mag.z) > threshold)
            rb.AddForce(s.orientation.forward * -mag.z * friction);

        if (CounterMomentum(input.y, mag.z))
            rb.AddForce(s.orientation.forward * -mag.z * sharpness, ForceMode.Acceleration);
    }

    private bool CounterMomentum(float input, float mag)
    {
        if (input > 0 && mag < -threshold || input < 0 && mag > threshold) return true;
        else return false;
    }

    #endregion

    #region Multipliers and Maxspeed calculation
    public Vector2 CalculateMultiplier()
    {
        if (vaulting) return new Vector2(0f, 0f);

        if (grounded)
        {
            if (crouched) return new Vector2(0.01f, 0.01f);
            else return new Vector2 (1f, 1.05f);
        }

        if (s.PlayerInput.wallRunning) return new Vector2(0.01f, 0.5f);
        if (crouched) return new Vector2(0.4f, 0.3f);

        return new Vector2(0.65f, 0.8f);
    }

    private float ControlMaxSpeed()
    {
        if (crouched) return maxSlideSpeed;
        if (jumping) return maxAirSpeed;
        if (stepsSinceLastGrounded == 0) return maxGroundSpeed;
        return maxAirSpeed;
    }
    #endregion

    #region ProcessInput
    public void SetInput(Vector2 input, bool jumping, bool crouching, bool grounded)
    {
        this.input = input;
        this.jumping = jumping;
        this.crouching = crouching;
        this.grounded = grounded;
    }
    #endregion

    void ResetJump()
    {
        jumped = false;
    }

    void ResetWallJump()
    {
        readyToWallJump = true;
    }
}
