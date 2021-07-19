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
    [SerializeField] private int maxJumpSteps;
    
    [Header("Sliding")]
    [SerializeField] private float crouchScale;
    [SerializeField] private float crouchSmoothTime;
    [SerializeField] private float slideForce;
    public bool canCrouchWalk = true;
    private float crouchVel = 0f;
    private float crouchOffset;
    private bool crouched = false;
    private bool canUnCrouch = true;
    private Vector3 playerScale;

    [Header("WallRunning")]
    [SerializeField] private float wallRunGravityForce;
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallHoldForce;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallClimbForce;
    public bool wallRunning = false;
    private Vector3 wallMoveDir = Vector3.zero;
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
        crouchOffset = crouchScale * 0.0f;
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

        moveDir = (grounded ? GroundMove(CalculateMultiplier()) : AirMove(CalculateMultiplier()));
        rb.AddForce(moveDir * moveSpeed * 3f, ForceMode.Acceleration);

        magnitude = rb.velocity.magnitude;
        velocity = rb.velocity;
    }

    private Vector3 GroundMove(Vector2 multiplier)
    {
        Friction();

        Vector3 inputDir = CalculateInputDir(input, multiplier);
        Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, s.PlayerInput.groundNormal);

        float dot = Vector3.Dot(Vector3.up, slopeDir);
        return dot < 0f ? slopeDir : inputDir;
    }

    private Vector3 AirMove(Vector2 multiplier)
    {
        Vector2 inputTemp = input;

        if (inputTemp.x > 0 && relativeVel.x > 23f || inputTemp.x < 0 && relativeVel.x < -23f) inputTemp.x = 0f;
        if (inputTemp.y > 0 && relativeVel.z > 23f || inputTemp.y < 0 && relativeVel.z < -23f) inputTemp.y = 0f;

        return CalculateInputDir(inputTemp, multiplier);
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
        if (dot > 0) rb.velocity = (rb.velocity - (snapHit.normal * dot * 1.05f)).normalized * speed;
        else rb.velocity = (rb.velocity - snapHit.normal).normalized * speed;

        return true;
    }

    private bool UseGravity()
    {
        if (vaulting || wallRunning) return false;

        if (grounded && s.PlayerInput.onRamp && !crouching && stepsSinceLastJumped >= maxJumpSteps && !moving)
        {
            if (velocity.y > 0) rb.velocity = new Vector3(rb.velocity.x, -1f, rb.velocity.z);

            Vector3 dir = s.PlayerInput.groundNormal;
            dir.y = 0f;
            dir.Normalize();

            Vector3 slopeDir = Vector3.ProjectOnPlane(-dir, s.PlayerInput.groundNormal);
            float dot = Vector3.Dot(dir, s.PlayerInput.groundNormal);

            slopeDir.y -= 0.1f;

            rb.AddForce(slopeDir * 52f * dot, ForceMode.Acceleration);

            if (velocity.y < 0 && magnitude < 1f) rb.velocity = Vector3.zero;
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

        Vector3 wallUpCross = Vector3.Cross(-s.orientation.forward * input.y, s.PlayerInput.wallNormal);
        wallMoveDir = Vector3.Cross(wallUpCross, s.PlayerInput.wallNormal);

        if (canAddWallRunForce)
        {
            canAddWallRunForce = false;
            rb.useGravity = false;

            float wallUpSpeed = velocity.y;
            float wallMagnitude = magnitude;

            wallMagnitude = Mathf.Clamp(wallMagnitude, 0f, 25f);

            wallClimb = wallUpSpeed + wallClimbForce;
            wallClimb = Mathf.Clamp(wallClimb, -5f, 10f);
            rb.AddForce(Vector3.up * wallClimb);
            rb.AddForce(wallMoveDir * wallMagnitude * 0.15f, ForceMode.VelocityChange);
        }

        rb.AddForce(-s.PlayerInput.wallNormal * wallHoldForce);
        rb.AddForce(-transform.up * wallRunGravityForce, ForceMode.Acceleration);
        rb.AddForce(wallMoveDir * wallRunForce, ForceMode.Acceleration);
    }

    public float CalculateWallRunRotation()
    {
        if (!wallRunning) return 0f;
        if (Vector3.Dot(s.orientation.forward, s.PlayerInput.wallNormal) > 0f) return 0f;

        return Vector3.Angle(s.orientation.forward, wallMoveDir);
    }

    private void StopWallRun()
    {
        if (wallRunning && readyToWallJump)
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

    private void UnCrouch()
    {
        crouched = false;
        canCrouchWalk = true;

        rb.velocity *= 0.7f;
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
    private void Friction()
    {
        if (jumping) return;

        if (crouched && canUnCrouch && !canCrouchWalk)
        {
            rb.AddForce(-rb.velocity.normalized * slideFriction * 3f * (magnitude * 0.1f)); 
            return;
        }

        Vector3 vel = -new Vector3(relativeVel.x * Convert.ToInt32(input.x == 0f || CounterMomentum(input.x, relativeVel.x)), 0, relativeVel.z * Convert.ToInt32(input.y == 0f || CounterMomentum(input.y, relativeVel.z))) * friction * 2f;

        Vector3 frictionForce = s.orientation.TransformDirection(vel);
        frictionForce = Vector3.ProjectOnPlane(frictionForce, s.PlayerInput.groundNormal);

        if (frictionForce != Vector3.zero) rb.AddForce(frictionForce, ForceMode.Acceleration);
    }

    private bool CounterMomentum(float input, float mag) => (input > 0 && mag < -threshold || input < 0 && mag > threshold);
    #endregion

    #region Input
    public void SetInput(Vector2 input, bool jumping, bool crouching, bool grounded)
    {
        this.input = input;
        this.input = Vector2.ClampMagnitude(this.input, 1f);

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

        if (crouching && !wallRunning && !crouched) Crouch(moveDir);
        if (crouched)
        {
            canUnCrouch = !Physics.CheckSphere(s.playerHead.position + Vector3.up, 0.6f, s.PlayerInput.Environment);
            canCrouchWalk = magnitude < maxGroundSpeed * 0.65f; 
        }

        if (!crouching && crouched && canUnCrouch) UnCrouch();

        UpdateCrouchScale();

        if (s.PlayerInput.nearWall && s.PlayerInput.isWallLeft && s.PlayerInput.CanWallJump() && input.x < 0 || s.PlayerInput.nearWall && s.PlayerInput.isWallRight && s.PlayerInput.CanWallJump() && input.x > 0) wallRunning = true;

        if (s.PlayerInput.CanWallJump() && jumping && readyToWallJump) WallJump();
        if (!s.PlayerInput.CanWallJump() || !s.PlayerInput.isWallRight && !s.PlayerInput.isWallLeft)
        {
            wallRunning = false;
            canAddWallRunForce = true;
        }

        if (wallRunning && !grounded) WallRun();

        if (s.PlayerInput.isWallLeft && input.x > 0 && wallRunning || s.PlayerInput.isWallRight && input.x < 0 && wallRunning && readyToWallJump) 
            StopWallRun();
    }

    private Vector3 CalculateInputDir(Vector2 input, Vector2 multiplier) => s.orientation.forward * input.y * multiplier.y + s.orientation.right * input.x * multiplier.x;
    #endregion

    public Vector2 CalculateMultiplier()
    {
        if (vaulting || wallRunning) return new Vector2(0f, 0f);

        if (grounded) return (crouched ? new Vector2(0.2f, 0.2f) : new Vector2(1f, 1.05f));

        if (crouched) return new Vector2(0.4f, 0.3f);

        return new Vector2(0.4f, 0.6f);
    }

    private float ControlMaxSpeed()
    {
        if (crouched && canCrouchWalk) return maxGroundSpeed * 0.6f;
        if (crouched) return maxSlideSpeed;
        if (jumping) return maxAirSpeed;
        if (grounded) return maxGroundSpeed;

        return maxAirSpeed;
    }

    void ResetWallJump() => readyToWallJump = true;
}
