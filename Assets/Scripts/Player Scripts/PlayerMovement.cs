using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    [Header("General Movement")]
    [SerializeField] private float accelRate;
    [SerializeField] private float moveSpeed;

    [Header("Air Movement")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMoveSpeed;

    bool jumped = false;

    public Vector3 moveDir { get; private set; }
    private float acceleration;

    [Header("Sliding")]
    [SerializeField] private Vector3 crouchScale;
    [SerializeField] private float slideForce;
    [SerializeField] private float slideMoveSpeed;
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

    private Vector2 multiplier;

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
        setVaultDuration = vaultDuration;
    }

    void FixedUpdate()
    {
        Movement();
    }

    void Update()
    {
        maxSpeed = ControlMaxSpeed();
        multiplier = CalculateMultiplier();
    }

    private void Movement()
    {
        rb.AddForce(Vector3.down);

        if (s.PlayerInput.reachedMaxSlope) rb.AddForce(Vector3.down * 70f);

        Vector2 input = s.PlayerInput.input;
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        CounterMovement(input.x, input.y, vel);

        float speed = Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2)));
        if (speed > maxSpeed) rb.AddForce(-vel * (speed * 0.5f));

        if (vaulting)
        {
            rb.MovePosition(Vector3.Lerp(vaultOriginalPos, vaultPos, vaultTime / vaultDuration));
            vaultTime += Time.fixedDeltaTime * 10f;

            if (vaultTime >= vaultDuration)
            {
                vaulting = false;
                rb.AddForce(vaultNormal * vaultForce * 0.3f, ForceMode.VelocityChange);
                rb.useGravity = true;

                s.PlayerInput.grounded = true;
            }
        }

        if (s.PlayerInput.grounded && s.PlayerInput.jumping) Jump();

        if (s.PlayerInput.grounded || SnapToGround(vel)) stepsSinceLastGrounded = 0;
        else if (stepsSinceLastGrounded < 10) stepsSinceLastGrounded++;

        if (!s.PlayerInput.CanWallJump()) canAddWallRunForce = true;

        if (s.PlayerInput.CanWallJump() && s.PlayerInput.jumping && readyToWallJump) WallJump();
        if (s.PlayerInput.wallRunning && !s.PlayerInput.grounded) WallRun();
        if (s.PlayerInput.stopWallRun && readyToWallJump) StopWallRun();

        if (s.PlayerInput.crouching && !s.PlayerInput.wallRunning && !crouched) StartCrouch(vel.normalized);
        if (!s.PlayerInput.crouching && crouched) StopCrouch();

        Vector3 inputDir = (s.orientation.forward * input.y * multiplier.y * multiplier.x + s.orientation.right * input.x * multiplier.x);
        Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, s.PlayerInput.groundNormal);

        float dot = Vector3.Dot(Vector3.up, slopeDir);
        moveDir = dot < 0f ? slopeDir : inputDir;

        rb.AddForce(moveDir * accelRate * 3f, ForceMode.Acceleration);
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

    #region Jumping
    private void Jump()
    {
        jumped = true;

        if (s.PlayerInput.grounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce * 0.7f, ForceMode.Impulse);

            CancelInvoke("ResetJump");
            Invoke("ResetJump", 0.3f);
        }
    }
    #endregion

    #region Vaulting
    public void Vault(Vector3 pos, Vector3 normal, float distance)
    {
        vaultPos = pos;
        vaultNormal = normal;
        vaultOriginalPos = transform.position;

        distance *= 0.03f;
        distance = Mathf.Round(distance * 100.0f) * 0.01f;
        vaultDuration = setVaultDuration + distance;
        vaultTime = 0f;
        vaulting = true;

        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        s.PlayerInput.grounded = false;
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

    #region Friction
    private void CounterMovement(float x, float z, Vector3 dir)
    {
        if (!s.PlayerInput.grounded) return;

        if (crouched)
        {
            rb.AddForce(-dir * slideFriction);
            return;
        }

        Vector3 mag = s.orientation.InverseTransformDirection(rb.velocity);

        if (!s.PlayerInput.moving && dir.sqrMagnitude > threshold * threshold)
            rb.AddForce(-dir * friction, ForceMode.Acceleration);

        if (CounterMomentum(x, mag.x))
            rb.AddForce(s.orientation.right * -mag.x * sharpness, ForceMode.Acceleration);

        if (CounterMomentum(z, mag.z))
            rb.AddForce(s.orientation.forward * -mag.z * sharpness, ForceMode.Acceleration);
    }
    #endregion

    private bool CounterMomentum(float input, float mag)
    {
        if (input > 0 && mag < -threshold || input < 0 && mag > threshold) return true;
        else return false;
    }

    public Vector2 CalculateMultiplier()
    {
        if (vaulting) return new Vector2(0.6f, 1f);
        if (s.PlayerInput.grounded)
        {
            if (crouched) return new Vector2 (0.05f, 1f);
            return new Vector2 (1f, 1.05f);
        }

        if(s.PlayerInput.wallRunning) return new Vector2(0.01f, 30f);
        if (crouched) return new Vector2 (0.4f, 0.8f);
        return new Vector2 (0.8f, 0.8f);
    }

    private float ControlMaxSpeed()
    {
        if (crouched) return slideMoveSpeed;
        if (s.PlayerInput.grounded) return moveSpeed;
        return airMoveSpeed;
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
