using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float maxGroundSpeed;
    [Space(10)]
    [SerializeField] private float maxAirSpeed;
    [SerializeField] private float maxSlideSpeed;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private int maxJumpSteps;
    
    [Header("Sliding")]
    [SerializeField] private float crouchScale;
    [SerializeField] private float crouchSmoothTime;
    [SerializeField] private float slideForce;
    private float crouchVel = 0f;
    private bool crouched = false;
    private bool canUnCrouch = true;
    private Vector3 playerScale;

    public bool canCrouchWalk { get; private set; } = true;

    [Header("WallRunning")]
    [SerializeField] private float wallRunGravityForce;
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallHoldForce;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallClimbForce;
    [Space(10)]
    [SerializeField] private float minimumJumpHeight;
    private Vector3 wallMoveDir = Vector3.zero;
    private bool canAddWallRunForce = true;
    private bool readyToWallJump = true;
    private float camTurnVel = 0f;

    public bool nearWall { get; private set; } = false;
    public bool isWallLeft { get; private set; } = false;
    public bool isWallRight { get; private set; } = false;
    public bool wallRunning { get; private set; } = false;

    [Header("Vaulting")]
    [SerializeField] private float vaultDuration;
    [SerializeField] private float vaultForce;
    [SerializeField] private float vaultOffset;

    public bool vaulting { get; private set; } = false;

    [Header("Movement Control")]
    [SerializeField] private float friction;
    [SerializeField] private float slideFriction;

    [Header("Collision")]
    [SerializeField] private LayerMask GroundSnapLayer;
    [SerializeField] private LayerMask Ground;
    [SerializeField] private LayerMask Environment;
    [Space(10)]
    [SerializeField] private float groundCancelDelay;
    [SerializeField] private float wallCancelDelay;
    [Space(10)]
    [SerializeField] [Range(0f, 90f)] private float maxSlopeAngle;

    private bool cancelWall = true;
    private int wallCancelSteps = 0;

    private bool cancelGround = false;
    private int groundCancelSteps = 0;

    private int stepsSinceLastGrounded = 0;
    private int stepsSinceLastJumped = 0;

    public Vector3 groundNormal { get; private set; }
    public Vector3 wallNormal { get; private set; }

    public bool grounded { get; private set; } = false;
    public bool reachedMaxSlope { get; private set; } = false;

    private Vector2 input;
    private bool jumping;
    private bool crouching;

    public Vector3 inputDir { get { return CalculateInputDir(input, Vector2.one); } }
    public bool moving { get; private set; }

    public float magnitude { get; private set; }
    public Vector3 relativeVel { get; private set; }
    public Vector3 velocity { get; private set; }

    private ScriptManager s;
    private Rigidbody rb;
    
    void Awake()
    {
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerScale = transform.localScale;
    }

    void FixedUpdate()
    {
        UpdateGroundCollisions();
        UpdateWallCollisions();

        Movement();
    }

    #region Movement
    private void Movement()
    {
        reachedMaxSlope = (Physics.Raycast(s.bottomCapsuleSphereOrigin, Vector3.down, out var slopeHit, 1.5f, Ground) ? Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle : false);
        if (reachedMaxSlope) rb.AddForce(Vector3.down * 35f, ForceMode.Acceleration);

        if ((rb.velocity.y < 0f || rb.IsSleeping()) && !wallRunning && !vaulting) rb.AddForce(Vector3.up * Physics.gravity.y * (1.68f - 1f), ForceMode.Acceleration);

        rb.useGravity = !(vaulting || wallRunning);
        relativeVel = s.orientation.InverseTransformDirection(rb.velocity);

        ControlSpeed();
        ProcessInput();

        rb.AddForce((grounded ? GroundMovement() : AirMovement()) * moveSpeed * 0.1f, ForceMode.Impulse);

        magnitude = rb.velocity.magnitude;
        velocity = rb.velocity;
    }

    private Vector3 GroundMovement()
    {
        if (stepsSinceLastGrounded < 3 && jumping) Jump();

        Friction();
        SlopeMovement();

        Vector3 inputDir = CalculateInputDir(input, CalculateMultiplier());
        Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, groundNormal);

        float dot = Vector3.Dot(Vector3.up, slopeDir);
        return dot < 0f ? slopeDir : inputDir;
    }

    private Vector3 AirMovement()
    {
        if (nearWall && isWallLeft && CanWallJump() && input.x < 0 || nearWall && isWallRight && CanWallJump() && input.x > 0) wallRunning = true;

        if (CanWallJump() && jumping && readyToWallJump) WallJump();
        if (wallRunning) WallRun();

        if (isWallLeft && input.x > 0 && wallRunning || isWallRight && input.x < 0 && wallRunning && readyToWallJump) StopWallRun();

        Vector2 inputTemp = input;

        if (inputTemp.x > 0 && relativeVel.x > 23f || inputTemp.x < 0 && relativeVel.x < -23f) inputTemp.x = 0f;
        if (inputTemp.y > 0 && relativeVel.z > 23f || inputTemp.y < 0 && relativeVel.z < -23f) inputTemp.y = 0f;

        return CalculateInputDir(inputTemp, CalculateMultiplier());
    }

    private void SlopeMovement()
    {
        if (groundNormal.y >= 1f) return;

        Vector3 gravityForce = Physics.gravity - Vector3.Project(Physics.gravity, groundNormal);
        rb.AddForce(-gravityForce * (rb.velocity.y > 0 ? 0.9f : 1.4f), ForceMode.Acceleration);
    }
    #endregion

    #region Surface Contact
    private bool SnapToGround(float speed)
    {
        if (speed < 3f || stepsSinceLastGrounded > 3 || stepsSinceLastJumped < maxJumpSteps || vaulting || grounded) return false;
        if (!Physics.Raycast(s.bottomCapsuleSphereOrigin, Vector3.down, out var snapHit, 1.8f, GroundSnapLayer)) return false;

        grounded = true;

        float dot = Vector3.Dot(rb.velocity, Vector3.up);

        if (dot > 0) rb.velocity = (rb.velocity - (snapHit.normal * dot)).normalized * speed;
        else rb.velocity = (rb.velocity - snapHit.normal).normalized * speed;

        return true;
    }

    private void RecordMovementSteps()
    {
        if (stepsSinceLastJumped < maxJumpSteps) stepsSinceLastJumped++;

        if (grounded || SnapToGround(magnitude)) stepsSinceLastGrounded = 0;
        else if (stepsSinceLastGrounded < 10) stepsSinceLastGrounded++;
    }

    #endregion

    #region Collision Calculations
    private void UpdateGroundCollisions()
    {
        if (!grounded) return;

        if (!cancelGround) cancelGround = true;
        else
        {
            groundCancelSteps++;

            if ((float)groundCancelSteps > groundCancelDelay)
            {
                groundNormal = Vector3.up;
                grounded = false;
            }
        }
    }

    private void UpdateWallCollisions()
    {
        if (!nearWall) return;

        float dot = Vector3.Dot(s.orientation.right, wallNormal);

        isWallLeft = dot > 0.8f;
        isWallRight = dot < -0.8f;

        if (!cancelWall) cancelWall = true;
        else
        {
            wallCancelSteps++;

            if ((float)wallCancelSteps > wallCancelDelay)
            {
                nearWall = false;
                wallNormal = Vector3.zero;
                isWallLeft = false;
                isWallRight = false;
            }
        }
    }
    #endregion

    #region Collision Detection
    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | 1 << layer)) return;

        Vector3 normal = col.GetContact(0).normal;

        if (IsFloor(normal)) if (!grounded) Land(Math.Abs(velocity.y));

        if (Environment != (Environment | 1 << layer)) return;

        CheckForVault(normal);
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

            if (IsWall(normal, 0.1f))
            {
                if (Environment != (Environment | 1 << layer)) continue;

                nearWall = true;
                cancelWall = false;
                wallCancelSteps = 0;
                wallNormal = normal;
            }
        }
    }

    private void Land(float impactForce)
    {
        if (impactForce > 30f)
        {
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = ObjectPooler.Instance.SpawnParticle("LandFX", transform.position, Quaternion.Euler(0, 0, 0)).velocityOverLifetime;

            Vector3 magnitude = rb.velocity;

            velocityOverLifetime.x = magnitude.x;
            velocityOverLifetime.z = magnitude.z;
        }

        s.CameraHeadBob.CameraLand(impactForce);
    }

    bool IsFloor(Vector3 normal) => Vector3.Angle(Vector3.up, normal) < maxSlopeAngle;
    bool IsWall(Vector3 normal, float threshold) => Math.Abs(Vector3.Dot(normal, Vector3.up)) < threshold;
    #endregion

    #region Jumping
    private void Jump()
    {
        stepsSinceLastJumped = 0;

        grounded = false;
        rb.useGravity = true;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce * 0.7f, ForceMode.Impulse);
    }
    #endregion

    #region Vaulting
    private void CheckForVault(Vector3 normal)
    {
        if (!IsWall(normal, 0.31f)) return;

        if (vaulting || wallRunning || crouching || reachedMaxSlope) return;

        Vector3 vaultDir = normal;
        vaultDir.y = 0f;
        vaultDir.Normalize();

        Vector3 vel = velocity;
        vel.y = 0f;

        Vector3 moveDir = inputDir;
        Vector3 vaultCheck = transform.position + Vector3.up * 1.5f;

        if (Vector3.Dot(-vaultDir, moveDir) < 0.65f) return;
        if (Physics.Raycast(vaultCheck, Vector3.up, 2f, Environment)) return;
        if (!Physics.Raycast(vaultCheck - vaultDir, Vector3.down, out var vaultHit, 3f, Environment)) return;
        if (Vector3.Angle(Vector3.up, vaultHit.normal) > maxSlopeAngle) return;

        Vector3 vaultPoint = vaultHit.point + (Vector3.up * 2f) + (vaultDir);
        float distance = vaultPoint.y - s.bottomCapsuleSphereOrigin.y;

        if (distance > vaultOffset + 0.1f) return;

        if (distance < 4f)
        {
            s.CameraHeadBob.StepUp(transform.position - vaultPoint);
            transform.position = vaultPoint;
            rb.velocity = vel;
            //StartCoroutine(Vault2(vaultPoint - vaultDir * 0.4f, vel, distance));
            return;
        }

        StartCoroutine(Vault(vaultPoint, -vaultDir, distance));
    }
    /*
    private IEnumerator Vault2(Vector3 pos, Vector3 vel, float distance)
    {
        rb.isKinematic = true;
        rb.detectCollisions = false;
        vaulting = true;

        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration - duration * 0.15f) 
        {
            rb.position = Vector3.Lerp(transform.position, pos, elapsed/ duration);
            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        rb.isKinematic = false;
        rb.detectCollisions = true;
        vaulting = false;
        grounded = true;

        rb.velocity = vel;
    }
    */
    private IEnumerator Vault(Vector3 pos, Vector3 normal, float distance)
    {
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        vaulting = true;

        distance = (distance * distance) * 0.05f;
        distance = Mathf.Round(distance * 100.0f) * 0.01f;

        Vector3 vaultOriginalPos = transform.position;
        float elapsed = 0f;
        float vaultDuration = this.vaultDuration + distance;

        grounded = false;

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
        rb.interpolation = RigidbodyInterpolation.Interpolate;

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
            grounded = false;

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            rb.AddForce(transform.up * jumpForce * 0.7f, ForceMode.Impulse);
            rb.AddForce(wallNormal * wallJumpForce, ForceMode.Impulse);

            CancelInvoke("ResetWallJump");
            Invoke("ResetWallJump", 0.3f);
        }
    }

    private void WallRun()
    {
        float wallClimb = 0f;

        Vector3 wallUpCross = Vector3.Cross(-s.orientation.forward, wallNormal);
        wallMoveDir = Vector3.Cross(wallUpCross, wallNormal);

        if (canAddWallRunForce)
        {
            canAddWallRunForce = false;
            rb.useGravity = false;
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.8f, rb.velocity.z);

            float wallUpSpeed = velocity.y;
            float wallMagnitude = magnitude;

            wallMagnitude = Mathf.Clamp(wallMagnitude, 0f, 25f);

            wallClimb = wallUpSpeed + wallClimbForce * 0.3f;
            wallClimb = Mathf.Clamp(wallClimb, -5f, 10f);
            rb.AddForce(Vector3.up * wallClimb);
            rb.AddForce(wallMoveDir * wallMagnitude * 0.15f, ForceMode.VelocityChange);
        }

        rb.AddForce(-wallNormal * wallHoldForce);
        rb.AddForce(-transform.up * wallRunGravityForce, ForceMode.Acceleration);
        rb.AddForce(wallMoveDir * wallRunForce, ForceMode.Acceleration);
    }

    private void StopWallRun()
    {
        if (wallRunning && readyToWallJump)
        {
            readyToWallJump = false;
            nearWall = false;

            rb.AddForce(wallNormal * wallJumpForce * 0.8f, ForceMode.Impulse);

            CancelInvoke("ResetWallJump");
            Invoke("ResetWallJump", 0.3f);
        }
    }

    public bool CanWallJump()
    {
        if (!nearWall) return false;
        if (reachedMaxSlope || vaulting || grounded || crouching) return false;

        return !Physics.CheckSphere(s.bottomCapsuleSphereOrigin + Vector3.down * minimumJumpHeight, 0.3f, Ground);
    }

    public float CalculateWallRunRotation(float rot)
    {
        if (!wallRunning || Vector3.Dot(s.orientation.forward, wallNormal) > 0.35f || input.y < 0) return 0f;

        return Mathf.SmoothDampAngle(rot, Vector3.SignedAngle(s.orientation.forward, (wallMoveDir + wallNormal * 0.25f).normalized, Vector3.up), ref camTurnVel, 0.35f);
    }
    #endregion 

    #region Crouching And Sliding
    private void Crouch(Vector3 dir)
    {
        crouched = true;
        if (grounded && magnitude > 0.5f) rb.AddForce(dir * slideForce * magnitude);
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

        Vector3 frictionForce = (-s.orientation.right * relativeVel.x * Convert.ToInt32(input.x == 0f || CounterMomentum(input.x, relativeVel.x)) + -s.orientation.forward * relativeVel.z * Convert.ToInt32(input.y == 0f || CounterMomentum(input.y, relativeVel.z))) * friction * 2f;
        frictionForce = Vector3.ProjectOnPlane(frictionForce, groundNormal);

        if (frictionForce != Vector3.zero) rb.AddForce(frictionForce, ForceMode.Acceleration);
    }
    #endregion

    #region Limiting Speed
    void ControlSpeed()
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float maxSpeed = CalculateMaxSpeed();
        float coefficientOfFriction = moveSpeed * 0.045f / maxSpeed;

        if (vel.sqrMagnitude > maxSpeed * maxSpeed) rb.AddForce(-vel * coefficientOfFriction, ForceMode.VelocityChange);
    }

    private float CalculateMaxSpeed()
    {
        if (crouched && canCrouchWalk) return maxGroundSpeed * 0.6f;
        if (crouched) return maxSlideSpeed;
        if (jumping) return maxAirSpeed;
        if (grounded) return maxGroundSpeed;

        return maxAirSpeed;
    }
    #endregion

    #region Input
    public void SetInput(Vector2 input, bool jumping, bool crouching)
    {
        this.input = input;
        this.input = Vector2.ClampMagnitude(this.input, 1f);

        this.jumping = jumping;
        this.crouching = crouching;

        moving = input != Vector2.zero;
    }

    private void ProcessInput()
    {
        RecordMovementSteps();

        if (crouching && !wallRunning && !crouched) Crouch(inputDir);

        if (crouched)
        {
            canUnCrouch = !Physics.CheckSphere(s.playerHead.position + Vector3.up, 0.6f, Environment);
            canCrouchWalk = magnitude < maxGroundSpeed * 0.65f;

            rb.AddForce(Vector3.down * 25f);
        }

        if (!crouching && crouched && canUnCrouch) UnCrouch();

        UpdateCrouchScale();

        if (!CanWallJump() || !isWallRight && !isWallLeft)
        {
            wallRunning = false;
            canAddWallRunForce = true;
        }
    }

    public Vector2 CalculateMultiplier()
    {
        if (vaulting || wallRunning) return new Vector2(0f, 0f);

        if (grounded) return (crouched ? new Vector2(0.08f, 0.08f) : new Vector2(1f, 1.05f));

        if (crouched) return new Vector2(0.4f, 0.3f);

        return new Vector2(0.4f, 0.6f);
    }

    private Vector3 CalculateInputDir(Vector2 input, Vector2 multiplier)
    {
        return s.orientation.forward * input.y * multiplier.y + s.orientation.right * input.x * multiplier.x;
    }
    #endregion

    void ResetWallJump() => readyToWallJump = true;

    bool CounterMomentum(float input, float mag)
    {
        float threshold = 0.05f;
        return (input > 0 && mag < -threshold || input < 0 && mag > threshold);
    }
}
