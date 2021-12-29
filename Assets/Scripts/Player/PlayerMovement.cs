using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float acceleration;
    [SerializeField] private float maxGroundSpeed;
    [Space(10)]
    [SerializeField] private float airMultiplier;
    [SerializeField] private float maxAirSpeed;
    [SerializeField] private float maxSlideSpeed;

    [Header("Sprinting")]
    [SerializeField] private float sprintMultiplier;
    [SerializeField] private float sprintDoubleTapTime;
    [SerializeField] private bool autoSprint;
    private bool sprinting = false;
    private float timeSinceLastTap = 0f;

    public bool Sprinting { get { return autoSprint || sprinting; } set { sprinting = value; } }

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private int maxJumpSteps;

    [Header("Sliding")]
    [SerializeField] private float crouchScale;
    [SerializeField] private float crouchSmoothTime;
    [SerializeField] private float slideForce;
    [SerializeField] private float slideTilt;
    private Vector2 crouchVel = Vector2.zero;
    private bool crouched = false;
    private bool canUnCrouch = true;
    private float playerScale;
    private float slideAngledTilt = 0;

    public Vector3 CrouchOffset { get { return (playerScale - s.cc.height) * transform.localScale.y * Vector3.down; } }
    public bool CanCrouchWalk { get { return !crouched || (Magnitude < maxGroundSpeed * 0.7f); } }
    public float SlideTiltOffset { get { return slideAngledTilt; } }

    [Header("WallRunning")]
    [SerializeField] private float wallRunGravityForce;
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallHoldForce;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallClimbForce;
    [SerializeField] private int wallJumpCooldownSteps;
    [Space(10)]
    [SerializeField] private float minimumJumpHeight;
    [SerializeField] private float wallRunTilt;
    [SerializeField] private float wallRunFovOffset;
    private Vector3 wallMoveDir = Vector3.zero;
    private float camTurnVel = 0f;

    public bool NearWall { get; private set; } = false;
    public bool IsWallLeft { get; private set; } = false;
    public bool IsWallRight { get; private set; } = false;

    private bool wallRunning = false;
    public bool WallRunning 
    {
        get { return wallRunning;  }

        set
        {
            bool wasWallRunning = wallRunning;
            wallRunning = value;

            if (wasWallRunning != value && value) InitialWallClimb();
        }
    }

    public float WallRunFovOffset { get { return (wallRunning ? wallRunFovOffset : 0); } }
    public float WallRunTiltOffset { get { return (wallRunning ? wallRunTilt * (IsWallRight ? 1 : -1) : 0); } }

    [Header("Vaulting")]
    [SerializeField] private float vaultDuration;
    [SerializeField] private float vaultForce;
    [SerializeField] private float vaultOffset;

    public bool Vaulting { get; private set; } = false;

    [Header("Movement Control")]
    [SerializeField] private float friction;
    [SerializeField] private float frictionMultiplier;
    [SerializeField] private float slideFriction;
    [SerializeField] private int counterThresold;
    private Vector2Int readyToCounter = Vector2Int.zero;

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

    private int stepsSinceGrounded = 0;
    private int stepsSinceLastGrounded = 0;
    private int stepsSinceLastJumped = 0;
    private int stepsSinceLastWallJumped = 0;

    public bool JustJumped { get { return stepsSinceLastJumped < 4; } }

    public Vector3 GroundNormal { get; private set; }
    public Vector3 WallNormal { get; private set; }

    public bool Grounded { get; private set; } = false;
    public bool ReachedMaxSlope { get; private set; } = false;

    private Vector2 input;
    private bool jumping;
    private bool crouching;

    public Vector3 InputDir { get { return CalculateInputDir(input).normalized; } }
    public bool Moving { get; private set; }

    public float Magnitude { get; private set; }
    public Vector3 RelativeVel { get; private set; }
    public Vector3 Velocity { get; private set; }

    private ScriptManager s;
    private Rigidbody rb;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();

        playerScale = s.cc.height;
        rb.freezeRotation = true;
    }

    void OnEnable() => s.PlayerHealth.OnPlayerStateChanged += OnPlayerStateChanged;
    void OnDisable() => s.PlayerHealth.OnPlayerStateChanged -= OnPlayerStateChanged;

    void Update()
    {
        SetInput(s.PlayerInput.InputVector, s.PlayerInput.Jumping, s.PlayerInput.Crouching);
    }

    void FixedUpdate()
    {
        UpdateCollisionChecks();

        Movement();
    }

    #region Movement
    private void Movement()
    {
        float movementMultiplier = Grounded ? (crouched ? (CanCrouchWalk ? 0.1f : 0.07f) : 1f) : airMultiplier * (crouched ? 0.5f : 1f);

        ReachedMaxSlope = (Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, out var slopeHit, 1.5f, Ground) && Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle);
        if (ReachedMaxSlope) rb.AddForce(Vector3.down * 35f, ForceMode.Acceleration);

        if ((rb.velocity.y < 0f || rb.IsSleeping()) && !WallRunning && !Vaulting) rb.AddForce((1.7f - 1f) * Physics.gravity.y * Vector3.up, ForceMode.Acceleration);

        rb.useGravity = !(Vaulting || WallRunning);
        RelativeVel = s.orientation.InverseTransformDirection(rb.velocity);

        RecordMovementSteps();
        ProcessCrouching();

        ClampSpeed(movementMultiplier);

        if (Grounded) GroundMovement(movementMultiplier);
        else AirMovement(movementMultiplier);

        if (!CanWallJump() || !IsWallRight && !IsWallLeft) WallRunning = false;

        Magnitude = rb.velocity.magnitude;
        Velocity = rb.velocity;
    }

    private void GroundMovement(float movementMultiplier)
    {
        Friction();
        SlopeMovement();

        if (stepsSinceLastGrounded < 3 && jumping) Jump();

        Vector3 inputDir = CalculateInputDir(input);
        Vector3 slopeDir = inputDir - Vector3.Project(inputDir, GroundNormal);
        float dot = Vector3.Dot(slopeDir, Vector3.up);

        rb.AddForce(8.5f * movementMultiplier * acceleration * (dot > 0 ? inputDir : inputDir), ForceMode.Force);
    }

    private void AirMovement(float movementMultiplier)
    {
        if (WallRunning) WallRun();
        if (NearWall && IsWallLeft && CanWallJump() && input.x < 0 || NearWall && IsWallRight && CanWallJump() && input.x > 0) WallRunning = true;

        if (CanWallJump() && jumping) Jump(false);
        else if (IsWallLeft && input.x > 0 && WallRunning || IsWallRight && input.x < 0 && WallRunning) DetachFromWallRun();

        if (WallRunning || Vaulting) return;

        Vector2 inputTemp = input;
        if (crouched) if (Vector3.Dot(s.orientation.forward, InputDir) < 0.5f) inputTemp *= 0.25f;

        if (inputTemp.x > 0 && RelativeVel.x > 25f || inputTemp.x < 0 && RelativeVel.x < -25f) inputTemp.x = 0f;
        if (inputTemp.y > 0 && RelativeVel.z > 25f || inputTemp.y < 0 && RelativeVel.z < -25f) inputTemp.y = 0f;

        rb.AddForce(8.5f * movementMultiplier * acceleration * CalculateInputDir(inputTemp), ForceMode.Force);
    }

    private void SlopeMovement()
    {
        if (GroundNormal.y >= 1f) return;

        Vector3 gravityForce = Physics.gravity - Vector3.Project(Physics.gravity, GroundNormal);
        rb.AddForce(-gravityForce * (rb.velocity.y > 0 ? 0.9f : 1.4f), ForceMode.Acceleration);
    }
    #endregion

    #region Surface Contact
    private bool SnapToGround(float speed)
    {
        if (speed < 3f || stepsSinceLastGrounded > 3 || stepsSinceLastJumped < maxJumpSteps || Vaulting || Grounded) return false;
        if (!Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, out var snapHit, 1.8f, GroundSnapLayer)) return false;

        Grounded = true;

        float dot = Vector3.Dot(rb.velocity, Vector3.up);

        if (dot > 0) rb.velocity = (rb.velocity - (snapHit.normal * dot)).normalized * speed;
        else rb.velocity = (rb.velocity - snapHit.normal).normalized * speed;

        return true;
    }

    private void RecordMovementSteps()
    {
        if (stepsSinceLastJumped < maxJumpSteps) stepsSinceLastJumped++;
        if (stepsSinceLastWallJumped < 100) stepsSinceLastWallJumped++;

        if (Grounded || SnapToGround(Magnitude))
        {
            if (stepsSinceGrounded < 10) stepsSinceGrounded++;
            stepsSinceLastGrounded = 0;
        }
        else
        {
            if (stepsSinceLastGrounded < 10) stepsSinceLastGrounded++;
            stepsSinceGrounded = 0;
        }
    }

    public void ResetJumpSteps() => stepsSinceLastJumped = 0;
    #endregion

    #region Collision Calculations
    private void UpdateCollisionChecks()
    {
        if (Grounded)
        {
            if (!cancelGround) cancelGround = true;
            else
            {
                groundCancelSteps++;

                if ((float)groundCancelSteps > groundCancelDelay)
                {
                    GroundNormal = Vector3.up;
                    Grounded = false;
                }
            }
        }

        if (NearWall)
        {
            float dot = Vector3.Dot(s.orientation.right, WallNormal);

            IsWallLeft = dot > 0.8f;
            IsWallRight = dot < -0.8f;

            if (!cancelWall) cancelWall = true;
            else
            {
                wallCancelSteps++;

                if ((float)wallCancelSteps > wallCancelDelay)
                {
                    NearWall = false;
                    WallNormal = Vector3.zero;
                    IsWallLeft = false;
                    IsWallRight = false;
                }
            }
        }
    }
    #endregion

    #region Collision Detection
    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        ContactPoint contact = col.GetContact(0);

        if (IsFloor(contact.normal) && Ground == (Ground | 1 << layer) && !Grounded && stepsSinceGrounded < 1)
        {
            stepsSinceGrounded++;
            s.CameraHeadBob.BobOnce(Mathf.Min(0, Velocity.y));
        }

        if (IsWall(contact.normal, 0.3f) && Environment == (Environment | 1 << layer)) CheckForVault(contact.normal);
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

                Grounded = true;
                cancelGround = false;
                groundCancelSteps = 0;
                GroundNormal = normal;
            }

            if (IsWall(normal, 0.1f))
            {
                if (Environment != (Environment | 1 << layer)) continue;

                NearWall = true;
                cancelWall = false;
                wallCancelSteps = 0;
                WallNormal = normal;
            }
        }
    }

    bool IsFloor(Vector3 normal) => Vector3.Angle(Vector3.up, normal) < maxSlopeAngle;
    bool IsWall(Vector3 normal, float threshold) => Math.Abs(normal.y) < threshold;

    #endregion

    #region Jumping
    private void Jump(bool normalJump = true)
    {
        if (normalJump)
        {
            stepsSinceLastJumped = 0;

            Grounded = false;
            rb.useGravity = true;

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce((crouched ? 0.64f : 0.8f) * jumpForce * Vector3.up, ForceMode.Impulse);
        }
        else
        {
            if (stepsSinceLastWallJumped < wallJumpCooldownSteps) return;

            stepsSinceLastWallJumped = 0;
            Grounded = false;

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            rb.AddForce(0.75f * jumpForce * GroundNormal, ForceMode.Impulse);
            rb.AddForce(WallNormal * wallJumpForce, ForceMode.Impulse);
        }
    }
    #endregion

    #region Vaulting And Stepping
    private void CheckForVault(Vector3 normal)
    {
        if (Vaulting || WallRunning || crouched || ReachedMaxSlope) return;

        Vector3 vaultDir = normal;
        vaultDir.y = 0f;

        Vector3 vel = Velocity;
        vel.y = 0;

        Vector3 vaultCheck = transform.position + Vector3.up * 2f;
        Vector3 lastPos = transform.position;

        if (Vector3.Dot(-vaultDir.normalized, vel.normalized) < 0.4f && Vector3.Dot(-vaultDir.normalized, InputDir) < 0.4f) return;
        if (Physics.Raycast(vaultCheck, Vector3.up, 2f, Environment)) return;
        if (!Physics.Raycast(vaultCheck + (vel.normalized * 0.5f - vaultDir.normalized).normalized, Vector3.down, out var vaultHit, 3.5f, Environment)) return;
        if (Vector3.Angle(Vector3.up, vaultHit.normal) > maxSlopeAngle) return;

        Vector3 vaultPoint = vaultHit.point + Vector3.up * 2f;
        float verticalDistance = vaultPoint.y - s.BottomCapsuleSphereOrigin.y;

        if (verticalDistance > vaultOffset + 0.1f || verticalDistance < 1.65f) return;

        float distance = Vector3.Distance(lastPos, vaultPoint);
        float duration = distance / Magnitude;

        if (verticalDistance < 3.7f)
        {
            StepUpDesyncSmoothing(vaultPoint, lastPos, duration);
            rb.velocity = vel * (Sprinting ? 1f : 0.5f);
            return;
        }

        if (Vector3.Dot(s.orientation.forward, -vaultDir.normalized) < 0.6f) return;

        StepUpDesyncSmoothing(vaultPoint + vaultDir, lastPos, duration * 3f);
        StartCoroutine(Vault(duration * 1.3f, -vaultDir));

        s.CameraHeadBob.BobOnce(Mathf.Min(0, Velocity.y) * 0.6f);
        s.CameraShaker.ShakeOnce(Math.Abs(Velocity.y) * 0.3f, 4f, 0.6f, 5f, ShakeData.ShakeType.Perlin);
    }

    private void StepUpDesyncSmoothing(Vector3 point, Vector3 lastPos, float duration)
    {
        transform.position = point;
        Vector3 offsetDir = lastPos - point;

        s.CameraHeadBob.PlayerDesyncFromCollider(offsetDir, Mathf.Clamp(duration * 0.2f, 0.03f, 0.15f));
    }

    private IEnumerator Vault(float duration, Vector3 normal)
    {
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        Vaulting = true;

        yield return new WaitForSeconds(duration);

        Vaulting = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.velocity = 0.5f * vaultForce * normal;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    }
    #endregion

    #region Wall Movement
    private void InitialWallClimb()
    {
        camTurnVel = 0f;

        Vector3 wallUpCross = Vector3.Cross(-s.orientation.forward, WallNormal);
        wallMoveDir = Vector3.Cross(wallUpCross, WallNormal);

        s.CameraLook.SetTiltSmoothing(0.18f);
        s.CameraLook.SetFovSmoothing(0.18f);

        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.65f, rb.velocity.z);

        float wallUpSpeed = Velocity.y;
        float wallMagnitude = Magnitude;

        wallMagnitude = Mathf.Clamp(wallMagnitude, 0f, 20f);

        float wallClimb = wallUpSpeed + wallClimbForce * 0.3f;
        wallClimb = Mathf.Clamp(wallClimb, -3f, 12f);

        rb.AddForce(Vector3.up * wallClimb);
        rb.AddForce(0.25f * wallMagnitude * wallMoveDir, ForceMode.VelocityChange);
    }

    private void WallRun()
    {
        Vector3 wallUpCross = Vector3.Cross(-s.orientation.forward, WallNormal);
        wallMoveDir = Vector3.Cross(wallUpCross, WallNormal);

        rb.AddForce(-WallNormal * wallHoldForce);
        rb.AddForce(0.8f * wallRunGravityForce * -transform.up, ForceMode.Acceleration);
        rb.AddForce(Mathf.Clamp(input.y, 0f, 1f) * wallRunForce * wallMoveDir, ForceMode.Acceleration);
    }

    public void DetachFromWallRun()
    {
        if (!WallRunning || stepsSinceLastWallJumped < wallJumpCooldownSteps) return;

        stepsSinceLastWallJumped = 0;
        NearWall = false;

        rb.AddForce(WallNormal * wallJumpForce * 1.1f, ForceMode.Impulse);
        rb.AddForce(0.15f * wallJumpForce * Vector3.down, ForceMode.Impulse);
    }

    public bool CanWallJump()
    {
        if (!NearWall || ReachedMaxSlope || Vaulting || Grounded || crouching) return false;

        return !Physics.CheckSphere(s.BottomCapsuleSphereOrigin + Vector3.down * minimumJumpHeight, 0.3f, Ground);
    }

    public float CalculateWallRunRotation(float rot)
    {
        return (!WallRunning || Vector3.Dot(s.orientation.forward, WallNormal) > 0.4f) ? 0f : 
            Mathf.SmoothDampAngle(rot, Vector3.SignedAngle(s.orientation.forward, (wallMoveDir + WallNormal * 0.4f).normalized, Vector3.up), ref camTurnVel, 0.3f);
    }

    public void SetWallrunning(bool wallRunning = true) => WallRunning = wallRunning;
    #endregion 

    #region Crouching And Sliding
    private void Crouch(Vector3 dir, bool crouch = true)
    {
        crouched = crouch;
        s.CameraLook.SetTiltSmoothing(0.15f);

        if (!crouched)
        {
            slideAngledTilt = 0;
            if (Grounded) rb.velocity *= 0.65f;
            return;
        }

        if (Magnitude > 0.5f)
        {
            if (Grounded) s.CameraHeadBob.BobOnce(-Magnitude * 0.65f);
            rb.AddForce(Magnitude * slideForce * (Grounded ? 0.8f : 0.3f) * dir);
        }
    }

    private void UpdateCrouchScale()
    {
        float targetScale = (crouched ? crouchScale : playerScale);
        float targetCenter = (targetScale - playerScale) * 0.5f;

        if (s.cc.height == targetScale && s.cc.center.y == targetCenter) return;
        if (Math.Abs(targetScale - s.cc.height) < 0.01f && Math.Abs(targetCenter - s.cc.center.y) < 0.01f)
        {
            s.cc.height = targetScale;
            s.cc.center = Vector3.one * targetCenter;
        }

        s.cc.height = Mathf.SmoothDamp(s.cc.height, targetScale, ref crouchVel.x, crouchSmoothTime);
        s.cc.center = new Vector3(0, Mathf.SmoothDamp(s.cc.center.y, targetCenter, ref crouchVel.y, crouchSmoothTime), 0);
    }

    private void ProcessCrouching()
    {
        if (crouching && !WallRunning && !crouched) Crouch(InputDir);
        if (!crouching && crouched && canUnCrouch) Crouch(InputDir, false);
        if (!crouched) return;

        if (CanCrouchWalk) slideAngledTilt = 0;
        else if (slideAngledTilt == 0 && Grounded && stepsSinceLastJumped > 5) slideAngledTilt = input.x * slideTilt;

        canUnCrouch = !Physics.CheckCapsule(s.BottomCapsuleSphereOrigin, s.playerHead.position, s.cc.radius * (NearWall ? 0.95f : 1.1f), Environment);
        rb.AddForce(Vector3.up * 5f, ForceMode.Acceleration);
    }
    #endregion

    #region Friction
    private void Friction()
    {
        if (jumping) return;

        float multiplier = frictionMultiplier < 1f ? frictionMultiplier * 0.1f : frictionMultiplier;

        if (crouched && canUnCrouch && !CanCrouchWalk)
        {
            rb.AddForce((Magnitude * 0.08f) * 1.5f * multiplier * slideFriction * -rb.velocity.normalized); 
            return;
        }

        Vector3 frictionForce = Vector3.zero;

        if (Math.Abs(RelativeVel.x) > 0.05f && input.x == 0f && readyToCounter.x > counterThresold) frictionForce -= s.orientation.right * RelativeVel.x;
        if (Math.Abs(RelativeVel.z) > 0.05f && input.y == 0f && readyToCounter.y > counterThresold) frictionForce -= s.orientation.forward * RelativeVel.z;

        if (CounterMomentum(input.x, RelativeVel.x)) frictionForce -= s.orientation.right * RelativeVel.x;
        if (CounterMomentum(input.y, RelativeVel.z)) frictionForce -= s.orientation.forward * RelativeVel.z;

        frictionForce = Vector3.ProjectOnPlane(frictionForce, GroundNormal);
        if (frictionForce != Vector3.zero) rb.AddForce(0.2f * friction * acceleration * multiplier * frictionForce);

        readyToCounter.x = input.x == 0f ? readyToCounter.x + 1 : 0;
        readyToCounter.y = input.y == 0f ? readyToCounter.y + 1 : 0;
    }

    public void SetFrictionMultiplier(float amount = 1f) => frictionMultiplier = amount;

    bool CounterMomentum(float input, float mag)
    {
        float threshold = 0.05f;
        return (input > 0 && mag < -threshold || input < 0 && mag > threshold);
    }
    #endregion

    #region Input
    public void SetInput(Vector2 input, bool jumping, bool crouching)
    {
        this.input = input;
        this.input = Vector2.ClampMagnitude(this.input, 1f);

        this.jumping = jumping;
        this.crouching = crouching;

        Moving = input != Vector2.zero;

        UpdateCrouchScale();
        if (!autoSprint) HandleSprinting();
    }

    private Vector3 CalculateInputDir(Vector2 input) => 1.05f * input.y * s.orientation.forward + s.orientation.right * input.x;
    #endregion

    private void ClampSpeed(float movementMultiplier)
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float maxSpeed = CalculateMaxSpeed();
        float coefficientOfFriction = acceleration / maxSpeed;
        float groundFrictionAccelTime = 3f;

        if (vel.sqrMagnitude > maxSpeed * maxSpeed) rb.AddForce(8.5f * coefficientOfFriction * frictionMultiplier * (Grounded ? Mathf.Clamp(stepsSinceGrounded / groundFrictionAccelTime, 0.3f, 1f) : 0.8f) * movementMultiplier * -vel, ForceMode.Force);
    }

    private float CalculateMaxSpeed()
    {
        if (crouched && CanCrouchWalk) return maxGroundSpeed * 0.4f;
        if (crouched) return maxSlideSpeed;
        if (jumping || !Grounded) return maxAirSpeed;

        if (Sprinting || autoSprint) return maxGroundSpeed * sprintMultiplier;
        return maxGroundSpeed;
    }

    private void HandleSprinting()
    {
        if (!Moving) Sprinting = false;
        if (!Grounded) return;

        if (s.PlayerInput.SprintTap)
        {
            if (Time.time - timeSinceLastTap <= sprintDoubleTapTime) Sprinting = true;
            timeSinceLastTap = Time.time;
        }
    }

    public void OnPlayerStateChanged(PlayerState newState)
    {
        if (newState != PlayerState.Dead) return;

        enabled = false;

        rb.freezeRotation = false;
        rb.AddExplosionForce(25f, s.BottomCapsuleSphereOrigin + Vector3.down + s.orientation.forward, 5f, 1f, ForceMode.VelocityChange);
        rb.drag = 2f;
    }
}
