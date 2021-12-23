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
    [SerializeField] private float airMultiplier;
    [SerializeField] private float maxAirSpeed;
    [SerializeField] private float maxSlideSpeed;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private int maxJumpSteps;

    [Header("Sliding")]
    [SerializeField] private float crouchScale;
    [SerializeField] private float crouchSmoothTime;
    [SerializeField] private float slideForce;
    [SerializeField] private float slideTilt;
    private float crouchVel = 0f;
    private bool crouched = false;
    private bool canUnCrouch = true;
    private Vector3 playerScale;

    public bool CanCrouchWalk { get; private set; } = true;

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

    public float SlideTiltOffset { get { return (crouched ? slideTilt : 0); } }

    [Header("Vaulting")]
    [SerializeField] private float vaultDuration;
    [SerializeField] private float vaultForce;
    [SerializeField] private float vaultOffset;
    [SerializeField] private float vaultJumpForce;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerScale = transform.localScale;
    }

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
        float movementMultiplier = (Grounded ? (crouched ? (CanCrouchWalk ? 0.1f : 0.07f) : 1f) : (crouched ? airMultiplier * 0.6f : airMultiplier));

        ReachedMaxSlope = (Physics.Raycast(s.bottomCapsuleSphereOrigin, Vector3.down, out var slopeHit, 1.5f, Ground) ? Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle : false);
        if (ReachedMaxSlope) rb.AddForce(Vector3.down * 35f, ForceMode.Acceleration);

        if ((rb.velocity.y < 0f || rb.IsSleeping()) && !WallRunning && !Vaulting && !crouched) rb.AddForce((1.7f - 1f) * Physics.gravity.y * Vector3.up, ForceMode.Acceleration);
        if (crouched) rb.AddForce(Vector3.up * 15f, ForceMode.Acceleration);

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

        rb.AddForce(8.5f * movementMultiplier * moveSpeed * (dot > 0 ? inputDir : inputDir), ForceMode.Force);
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

        rb.AddForce(8.5f * movementMultiplier * moveSpeed * CalculateInputDir(inputTemp), ForceMode.Force);
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
        if (!Physics.Raycast(s.bottomCapsuleSphereOrigin, Vector3.down, out var snapHit, 1.8f, GroundSnapLayer)) return false;

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

        if (IsWall(contact.normal, 0.33f) && Environment == (Environment | 1 << layer)) CheckForVault(contact.normal);
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
            rb.AddForce(Vector3.up * jumpForce * 0.8f, ForceMode.Impulse);
        }
        else
        {
            if (stepsSinceLastWallJumped < wallJumpCooldownSteps) return;

            stepsSinceLastWallJumped = 0;
            Grounded = false;

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            rb.AddForce(transform.up * jumpForce * 0.75f, ForceMode.Impulse);
            rb.AddForce(WallNormal * wallJumpForce, ForceMode.Impulse);
        }
        /*
        float amp = Magnitude * (crouched ? 0.6f : 1f);
        amp *= 0.025f;
        amp = Mathf.Clamp(amp, 0.6f, 3f);
 
        s.CameraShaker.ShakeOnce(amp, 8f, 0.6f, 10f, ShakeData.ShakeType.Perlin);
        */
    }
    #endregion

    #region Vaulting And Stepping
    private void CheckForVault(Vector3 normal)
    {
        if (Vaulting || WallRunning || crouched || ReachedMaxSlope) return;

        Vector3 vaultDir = normal;
        vaultDir.y = 0f;
        vaultDir.Normalize();

        Vector3 vel = Velocity;
        vel.y = 0;
        vel.Normalize();
        vel *= Magnitude;

        Vector3 vaultCheck = transform.position + Vector3.up * 1.5f;

        if (Vector3.Dot(-vaultDir, vel.normalized) < 0.4f && Vector3.Dot(-vaultDir, InputDir) < 0.6f) return;
        if (Physics.Raycast(vaultCheck, Vector3.up, 2f, Environment)) return;
        if (!Physics.Raycast(vaultCheck - vaultDir, Vector3.down, out var vaultHit, 3f, Environment)) return;
        if (Vector3.Angle(Vector3.up, vaultHit.normal) > maxSlopeAngle) return;

        Vector3 vaultPoint = vaultHit.point + (Vector3.up * 2f) + (vaultDir);
        float distance = vaultPoint.y - s.bottomCapsuleSphereOrigin.y;

        if (distance > vaultOffset + 0.1f) return;

        if (distance < 3.7f)
        {
            StartCoroutine(ResolveStepUp(vaultPoint - vaultDir * 0.6f + Vector3.down * 0.1f, Velocity));
            return;
        }

        if (Vector3.Dot(s.orientation.forward, -vaultDir) < 0.6f) return;

        StartCoroutine(Vault(vaultPoint, -vaultDir, distance));
        s.CameraHeadBob.BobOnce(Mathf.Min(0, Velocity.y) * 0.6f);
        s.CameraShaker.ShakeOnce(Math.Abs(Velocity.y) * 0.4f, 4f, 0.6f, 5f, ShakeData.ShakeType.Perlin);
    }

    private IEnumerator ResolveStepUp(Vector3 pos, Vector3 lastVel)
    {
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;
        lastVel.y = 0f;

        float elapsed = 0f;
        float speed = lastVel.magnitude;
        float distance = Mathf.Pow(Vector3.Distance(rb.position, pos), 1.3f);
        float duration = distance / speed;

        while (elapsed < duration)
        {
            rb.MovePosition(Vector3.Lerp(transform.position, pos, elapsed / duration));
            elapsed += Time.fixedDeltaTime * (1f + (elapsed / duration)) * 1.7f;

            rb.velocity = lastVel;
            WallRunning = false;

            yield return new WaitForFixedUpdate();
        }

        rb.velocity = lastVel * 1.1f;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private IEnumerator Vault(Vector3 pos, Vector3 normal, float distance)
    {
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        Vaulting = true;

        distance = (distance * distance) * 0.05f;
        distance = Mathf.Round(distance * 100.0f) * 0.01f;

        Vector3 vaultOriginalPos = transform.position;
        float elapsed = 0f;
        float vaultDuration = this.vaultDuration + distance;

        Grounded = false;

        while (elapsed < vaultDuration)
        {
            transform.position = Vector3.Lerp(vaultOriginalPos, pos, Mathf.SmoothStep(0, 1, elapsed));
            elapsed += Time.deltaTime * 2f;

            yield return null;
        }

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
        rb.AddForce(wallMoveDir * wallMagnitude * 0.25f, ForceMode.VelocityChange);
    }

    private void WallRun()
    {
        Vector3 wallUpCross = Vector3.Cross(-s.orientation.forward, WallNormal);
        wallMoveDir = Vector3.Cross(wallUpCross, WallNormal);

        rb.AddForce(-WallNormal * wallHoldForce);
        rb.AddForce(0.8f * wallRunGravityForce * -transform.up, ForceMode.Acceleration);
        rb.AddForce(wallMoveDir * wallRunForce * Mathf.Clamp(input.y, 0f, 1f), ForceMode.Acceleration);
    }

    public void DetachFromWallRun()
    {
        if (!WallRunning || stepsSinceLastWallJumped < wallJumpCooldownSteps) return;

        stepsSinceLastWallJumped = 0;
        NearWall = false;

        rb.AddForce(WallNormal * wallJumpForce * 1.1f, ForceMode.Impulse);
        rb.AddForce(Vector3.down * wallJumpForce * 0.15f, ForceMode.Impulse);
    }

    public bool CanWallJump()
    {
        if (!NearWall || ReachedMaxSlope || Vaulting || Grounded || crouching) return false;

        return !Physics.CheckSphere(s.bottomCapsuleSphereOrigin + Vector3.down * minimumJumpHeight, 0.3f, Ground);
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
        if (!crouch)
        {
            crouched = false;
            CanCrouchWalk = true;
            rb.velocity *= 0.8f;
            return;
        }

        crouched = true;
        s.CameraLook.SetTiltSmoothing(0.15f);  
        
        if (Grounded && Magnitude > 0.5f) rb.AddForce(dir * slideForce * Magnitude);
    }

    private void UpdateCrouchScale()
    {
        Vector3 targetScale = (crouching ? new Vector3(playerScale.x, crouchScale, playerScale.z) : playerScale);

        if (transform.localScale.y == targetScale.y) return;
        if (Math.Abs(targetScale.y - transform.localScale.y) < 0.01f) transform.localScale = targetScale;

        transform.localScale = new Vector3(playerScale.x, Mathf.SmoothDamp(transform.localScale.y, targetScale.y, ref crouchVel, crouchSmoothTime), playerScale.z);
    }

    private void ProcessCrouching()
    {
        if (crouching && !WallRunning && !crouched) Crouch(InputDir);
        if (!crouching && crouched && canUnCrouch) Crouch(InputDir, false);

        if (crouched)
        {
            canUnCrouch = !Physics.CheckSphere(s.playerHead.position + Vector3.up, 0.6f, Environment);
            CanCrouchWalk = Magnitude < maxGroundSpeed * 0.65f;

            rb.AddForce(Vector3.down * 45f);
        }

        UpdateCrouchScale();
    }
    #endregion

    #region Friction
    private void Friction()
    {
        if (jumping) return;

        float multiplier = frictionMultiplier < 1f ? frictionMultiplier * 0.1f : frictionMultiplier;

        if (crouched && canUnCrouch && !CanCrouchWalk)
        {
            rb.AddForce(-rb.velocity.normalized * slideFriction * 2.5f * multiplier); 
            return;
        }

        Vector3 frictionForce = Vector3.zero;

        if (Math.Abs(RelativeVel.x) > 0.05f && input.x == 0f && readyToCounter.x > counterThresold) frictionForce -= s.orientation.right * RelativeVel.x;
        if (Math.Abs(RelativeVel.z) > 0.05f && input.y == 0f && readyToCounter.y > counterThresold) frictionForce -= s.orientation.forward * RelativeVel.z;

        if (CounterMomentum(input.x, RelativeVel.x)) frictionForce -= s.orientation.right * RelativeVel.x;
        if (CounterMomentum(input.y, RelativeVel.z)) frictionForce -= s.orientation.forward * RelativeVel.z;

        frictionForce = Vector3.ProjectOnPlane(frictionForce, GroundNormal);
        if (frictionForce != Vector3.zero) rb.AddForce(0.2f * friction * moveSpeed * multiplier * frictionForce);

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
    }

    private Vector3 CalculateInputDir(Vector2 input) => s.orientation.forward * input.y * 1.05f + s.orientation.right * input.x;
    #endregion

    private void ClampSpeed(float movementMultiplier)
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float maxSpeed = CalculateMaxSpeed();
        float coefficientOfFriction = moveSpeed / maxSpeed;
        float groundFrictionAccelTime = 3f;

        if (vel.sqrMagnitude > maxSpeed * maxSpeed) rb.AddForce(8.5f * coefficientOfFriction * frictionMultiplier * (Grounded ? Mathf.Clamp(stepsSinceGrounded / groundFrictionAccelTime, 0.3f, 1f) : 0.8f) * movementMultiplier * -vel, ForceMode.Force);
    }

    private float CalculateMaxSpeed()
    {
        if (crouched && CanCrouchWalk) return maxGroundSpeed * 0.5f;
        if (crouched) return maxSlideSpeed;
        if (jumping || !Grounded) return maxAirSpeed;

        return maxGroundSpeed;
    }
}
