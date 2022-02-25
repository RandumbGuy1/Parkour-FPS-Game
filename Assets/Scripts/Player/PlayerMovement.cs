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
    public float WallRunTiltOffset { get { return (wallRunning ? wallRunTilt * (collision.IsWallRight ? 1 : -1) : 0); } }

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

    [Header("Collision Detection")]
    [SerializeField] private CollisionDetection collision = new CollisionDetection();
    public CollisionDetection MovementCollision => collision;

    private Vector2 input;
    private bool jumping;
    private bool crouching;

    public Vector3 InputDir { get { return CalculateInputDir(input).normalized; } }
    public bool Moving { get; private set; }
    private bool movingLastFrame = false;

    public delegate void StopMove(float mag);
    public event StopMove OnStopMoving;

    public float Magnitude { get; private set; }
    public Vector3 RelativeVel { get; private set; }
    public Vector3 Velocity { get; private set; }

    private ScriptManager s;
    private Rigidbody rb;
    private CollisionDetectionMode detection;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        rb = GetComponent<Rigidbody>();

        detection = rb.collisionDetectionMode;
        playerScale = s.cc.height;
        rb.freezeRotation = true;
    }

    void Update() => SetInput(s.PlayerInput.InputVector, s.PlayerInput.Jumping, s.PlayerInput.Crouching);

    void FixedUpdate()
    {
        collision.UpdateCollisionChecks(s);

        Movement();
    }

    #region Movement
    private void Movement()
    {
        float movementMultiplier = collision.Grounded ? (crouched ? (CanCrouchWalk ? 0.1f : 0.07f) : 1f) : airMultiplier * (crouched ? 0.5f : 1f) * (Sprinting ? 1f : 0.7f);

        if (collision.ReachedMaxSlope) rb.AddForce(Vector3.down * 35f, ForceMode.Acceleration);
        if (rb.velocity.y <= 0f && !WallRunning && !Vaulting) rb.AddForce((1.7f - 1f) * Physics.gravity.y * Vector3.up, ForceMode.Acceleration);

        rb.useGravity = !(Vaulting || WallRunning);
        RelativeVel = s.orientation.InverseTransformDirection(rb.velocity);

        ProcessCrouching();
        collision.RecordMovementSteps(s, maxJumpSteps);
        ClampSpeed(movementMultiplier);

        if (collision.Grounded) GroundMovement(movementMultiplier);
        else AirMovement(movementMultiplier);

        if (!collision.CanWallJump(s, minimumJumpHeight) || !collision.IsWallRight && !collision.IsWallLeft) WallRunning = false;

        Magnitude = rb.velocity.magnitude;
        Velocity = rb.velocity;
    }

    private void GroundMovement(float movementMultiplier)
    {
        Friction();
        SlopeMovement();

        if (collision.StepsSinceLastGrounded < 3 && jumping) Jump();

        Vector3 inputDir = CalculateInputDir(input);
        Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, collision.GroundNormal);
        float dot = Vector3.Dot(slopeDir, Vector3.up);

        rb.AddForce(8.5f * movementMultiplier * acceleration * (dot > 0 ? inputDir : slopeDir), ForceMode.Force);
    }

    private void AirMovement(float movementMultiplier)
    {
        if (WallRunning) WallRun();
        if (collision.CanWallJump(s, minimumJumpHeight) && collision.NearWall && (collision.IsWallLeft && input.x < 0 || collision.IsWallRight && input.x > 0)) WallRunning = true;

        if (collision.CanWallJump(s, minimumJumpHeight) && jumping) Jump(false);
        else if (WallRunning && (collision.IsWallLeft && input.x > 0 || collision.IsWallRight && input.x < 0)) DetachFromWallRun();

        if (WallRunning || Vaulting) return;

        Vector2 inputTemp = input;
        float speedCap = Sprinting ? 25f : 18f;

        if (crouched && Vector3.Dot(s.orientation.forward, InputDir) < 0.5f) inputTemp *= 0.25f;
        if (inputTemp.x > 0 && RelativeVel.x > speedCap || inputTemp.x < 0 && RelativeVel.x < -speedCap) inputTemp.x = 0f;
        if (inputTemp.y > 0 && RelativeVel.z > speedCap || inputTemp.y < 0 && RelativeVel.z < -speedCap) inputTemp.y = 0f;

        rb.AddForce(8.5f * movementMultiplier * acceleration * CalculateInputDir(inputTemp), ForceMode.Force);
    }

    private void SlopeMovement()
    {
        if (collision.GroundNormal.y >= 1f || collision.GroundNormal.y <= 0f) return;

        Vector3 gravityForce = Physics.gravity - Vector3.Project(Physics.gravity, collision.GroundNormal);
        rb.AddForce(-gravityForce * (rb.velocity.y > 0 ? 0.9f : 1.4f), ForceMode.Acceleration);
    }
    #endregion

    private void Jump(bool normalJump = true)
    {
        collision.SetGrounded(false);

        if (normalJump)
        {
            collision.ResetJumpSteps();
            collision.SetGrounded(false);
            rb.useGravity = true;

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce((crouched ? 0.64f : 0.8f) * jumpForce * Vector3.up, ForceMode.Impulse);
        }
        else
        {
            if (collision.StepsSinceLastWallJumped < wallJumpCooldownSteps) return;
            collision.ResetWallJumpSteps();

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(0.75f * jumpForce * Vector3.up, ForceMode.Impulse); ;
            rb.AddForce(collision.WallContact.normal * wallJumpForce, ForceMode.Impulse);
        }

        s.CameraShaker.ShakeOnce(new KickbackShake(ShakeData.Create(Mathf.Clamp(Magnitude * 0.5f, 4.25f, 5.5f), 2f, 0.8f, 9f), Vector3.right));
    }

    #region Vaulting And Stepping
    public void CheckForVault(ContactPoint contact, LayerMask VaultEnvironment, float maxSlopeAngle)
    {
        if (Vaulting || WallRunning || crouched) return;

        Vector3 vaultDir = contact.normal;
        vaultDir.y = 0f;

        //Physics.Raycast(contact.point + vaultDir * 0.01f, -vaultDir, out var checkWallHit, 1f, VaultEnvironment);
        if (!collision.IsWall(contact.normal, 0.35f)) return;

        Vector3 vel = Velocity;
        vel.y = 0;
        
        Vector3 vaultCheck = transform.position + s.cc.center + Vector3.up * 2f;
        Vector3 lastPos = transform.position;

        if (Vector3.Dot(-vaultDir.normalized, vel.normalized) < 0.4f && Vector3.Dot(-vaultDir.normalized, InputDir) < 0.4f) return;
        if (Physics.Raycast(vaultCheck, Vector3.up, 2f, VaultEnvironment)) return;
        if (!Physics.Raycast(vaultCheck + (vel.normalized * 0.5f - vaultDir.normalized).normalized, Vector3.down, out var vaultHit, 3.5f, VaultEnvironment)) return;
        if (Vector3.Angle(Vector3.up, vaultHit.normal) > maxSlopeAngle) return;

        Vector3 vaultPoint = vaultHit.point + Vector3.up * 2f;
        float verticalDistance = vaultPoint.y - s.BottomCapsuleSphereOrigin.y;

        if (verticalDistance > vaultOffset + 0.1f) return;

        float distance = Vector3.Distance(lastPos, vaultPoint);
        float duration = distance / Magnitude;

        if (verticalDistance < 4f)
        {
            StepUpDesyncSmoothing(vaultPoint, lastPos, Mathf.Clamp(duration * 0.55f, 0.04f, 0.1f));
            rb.velocity = vel * (Sprinting ? 1f : 0.6f);
            return;
        }

        if (crouched || Vector3.Dot(s.orientation.forward, -vaultDir.normalized) < 0.6f) return;

        StepUpDesyncSmoothing(vaultPoint + vaultDir, lastPos, Mathf.Clamp(duration * 0.75f, 0.06f, 0.15f));
        StartCoroutine(Vault(duration * 0.9f, -vaultDir));
    }

    private void StepUpDesyncSmoothing(Vector3 point, Vector3 lastPos, float duration)
    {
        transform.position = point;
        Vector3 offsetDir = lastPos - point;

        s.CameraHeadBob.PlayerDesyncFromCollider(offsetDir, duration);
    }

    private IEnumerator Vault(float duration, Vector3 normal)
    {
        s.CameraHeadBob.BobOnce(Mathf.Min(0, Velocity.y) * 0.6f);
        s.CameraShaker.ShakeOnce(new PerlinShake(ShakeData.Create(Math.Abs(Velocity.y) * 0.3f, 4f, 0.6f, 5f)));
        s.CameraShaker.ShakeOnce(new KickbackShake(ShakeData.Create(Mathf.Clamp(Magnitude * 0.5f, 5f, 20f), 4f, 0.6f, 9f), Vector3.forward));

        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        Vaulting = true;

        yield return new WaitForSeconds(duration * 0.8f);

        Vaulting = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = detection;

        rb.velocity = 0.5f * vaultForce * normal;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    }
    #endregion

    #region Wall Movement
    private void InitialWallClimb()
    {
        camTurnVel = 0f;

        Vector3 wallUpCross = Vector3.Cross(-s.orientation.forward, collision.WallContact.normal);
        wallMoveDir = Vector3.Cross(wallUpCross, collision.WallContact.normal);

        s.CameraLook.SetTiltSmoothing(0.18f);
        s.CameraLook.SetFovSmoothing(0.18f);

        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.65f, rb.velocity.z);

        float horizontalImpact = RelativeVel.x;
        float wallUpSpeed = Velocity.y;
        float wallMagnitude = Magnitude;

        wallMagnitude = Mathf.Clamp(wallMagnitude, 0f, 20f);

        float wallClimb = wallUpSpeed + wallClimbForce * 0.3f;
        wallClimb = Mathf.Clamp(wallClimb, -3f, 12f);

        rb.AddForce(Vector3.up * wallClimb);
        rb.AddForce(0.25f * wallMagnitude * wallMoveDir, ForceMode.VelocityChange);

        s.CameraShaker.ShakeOnce(new PerlinShake(ShakeData.Create(4.5f, 5f, 1.25f, 7f)));
        s.CameraShaker.ShakeOnce(new KickbackShake(ShakeData.Create(12f, 3f, 0.9f, 6.5f), Vector3.left));
        s.CameraShaker.ShakeOnce(new KickbackShake(ShakeData.Create(14f, 0f, 0.4f, 8f), Vector3.forward));
    }

    private void WallRun()
    {
        Vector3 wallUpCross = Vector3.Cross(-s.orientation.forward, collision.WallContact.normal);
        wallMoveDir = Vector3.Cross(wallUpCross, collision.WallContact.normal);

        rb.AddForce(-collision.WallContact.normal * wallHoldForce);
        rb.AddForce(0.8f * wallRunGravityForce * -transform.up, ForceMode.Acceleration);
        rb.AddForce(Mathf.Clamp(input.y, 0f, 1f) * wallRunForce * wallMoveDir, ForceMode.Acceleration);
    }

    public void DetachFromWallRun()
    {
        if (!WallRunning || collision.StepsSinceLastWallJumped < wallJumpCooldownSteps) return;

        collision.ResetWallJumpSteps();
        collision.SetNearWall(false);

        rb.AddForce(1.1f * wallJumpForce * collision.WallContact.normal, ForceMode.Impulse);
        rb.AddForce(0.15f * wallJumpForce * Vector3.down, ForceMode.Impulse);
    }

    public float CalculateWallRunRotation(float rot)
    {
        return (!WallRunning || Vector3.Dot(s.orientation.forward, collision.WallContact.normal) > 0.4f) ? 0f : 
            Mathf.SmoothDampAngle(rot, Vector3.SignedAngle(s.orientation.forward, (wallMoveDir + collision.WallContact.normal * 0.4f).normalized, Vector3.up), ref camTurnVel, 0.3f);
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
            if (collision.Grounded) rb.velocity *= 0.65f;
            return;
        }
            
        if (collision.Grounded) s.CameraHeadBob.BobOnce(-Magnitude * 0.65f);
        if (Magnitude > 5f) rb.AddForce(Magnitude * slideForce * (collision.Grounded ? 0.8f : 0.3f) * dir);
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
        if (!crouching && crouched && !collision.CeilingAbove(s, crouched)) Crouch(InputDir, false);
        if (!crouched) return;

        if (CanCrouchWalk) slideAngledTilt = 0;
        else if (slideAngledTilt == 0 && collision.Grounded && collision.StepsSinceLastJumped > 5)
        {
            float tiltDir = input.x != 0f ? input.x : 1f;
            s.CameraShaker.ShakeOnce(new KickbackShake(ShakeData.Create(Mathf.Clamp(Magnitude * 0.9f, 5f, 20f), 1f, 0.4f, 9f), Vector3.left));
            s.CameraShaker.ShakeOnce(new KickbackShake(ShakeData.Create(Mathf.Clamp(Magnitude * 0.7f, 5f, 17.5f), 2f, 0.6f, 8f), Vector3.forward * tiltDir));
            s.CameraShaker.ShakeOnce(new PerlinShake(ShakeData.Create(1f, 8f, 1.5f, 9f)));
            slideAngledTilt = tiltDir * slideTilt;
        }

        rb.AddForce(Vector3.up * 5f, ForceMode.Acceleration);
    }
    #endregion

    #region Friction
    private void Friction()
    {
        if (jumping) return;

        float multiplier = frictionMultiplier < 1f ? frictionMultiplier * 0.1f : frictionMultiplier;

        if (crouched && !collision.CeilingAbove(s, crouched) && !CanCrouchWalk)
        {
            rb.AddForce(Magnitude * 0.08f * 1.5f * multiplier * slideFriction * -rb.velocity.normalized); 
            return;
        }

        Vector3 frictionForce = Vector3.zero;

        if (Math.Abs(RelativeVel.x) > 0.05f && input.x == 0f && readyToCounter.x > counterThresold) frictionForce -= s.orientation.right * RelativeVel.x;
        if (Math.Abs(RelativeVel.z) > 0.05f && input.y == 0f && readyToCounter.y > counterThresold) frictionForce -= s.orientation.forward * RelativeVel.z;

        if (CounterMomentum(input.x, RelativeVel.x)) frictionForce -= s.orientation.right * RelativeVel.x;
        if (CounterMomentum(input.y, RelativeVel.z)) frictionForce -= s.orientation.forward * RelativeVel.z;

        frictionForce = Vector3.ProjectOnPlane(frictionForce, collision.GroundContact.normal);
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

    void OnCollisionEnter(Collision col) => collision.EvaluateCollisionEnter(s, col);
    void OnCollisionStay(Collision col) => collision.EvaluateCollisionStay(col);

    public void SetInput(Vector2 input, bool jumping, bool crouching)
    {
        this.input = input;
        this.input = Vector2.ClampMagnitude(this.input, 1f);

        this.jumping = jumping;
        this.crouching = crouching;

        Moving = input != Vector2.zero;

        if (!Moving && movingLastFrame) OnStopMoving?.Invoke(Magnitude);
        movingLastFrame = Moving;

        UpdateCrouchScale();
        HandleSprinting();
    }

    private void ClampSpeed(float movementMultiplier)
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float maxSpeed = CalculateMaxSpeed();
        float coefficientOfFriction = acceleration / maxSpeed;
        float groundFrictionAccelTime = 3f;

        if (vel.sqrMagnitude > maxSpeed * maxSpeed) rb.AddForce(8.5f * coefficientOfFriction * frictionMultiplier * (collision.Grounded ? Mathf.Clamp(collision.StepsSinceGrounded / groundFrictionAccelTime, 0.3f, 1f) : 0.8f) * movementMultiplier * -vel, ForceMode.Force);
    }

    private float CalculateMaxSpeed()
    {
        if (crouched && CanCrouchWalk) return maxGroundSpeed * 0.4f;
        if (crouched) return maxSlideSpeed;
        if (jumping || !collision.Grounded) return maxAirSpeed;

        if (Sprinting || autoSprint) return maxGroundSpeed * sprintMultiplier;
        return maxGroundSpeed;
    }

    private Vector3 CalculateInputDir(Vector2 input) => 1.05f * input.y * s.orientation.forward + s.orientation.right * input.x;

    private void HandleSprinting()
    {
        if (autoSprint) return;
        if (!Moving) Sprinting = false;
        if (!collision.Grounded) return;

        if (s.PlayerInput.SprintTap)
        {
            if (Time.time - timeSinceLastTap <= sprintDoubleTapTime) Sprinting = true;
            timeSinceLastTap = Time.time;
        }
    }

    public void OnPlayerStateChanged(UnitState newState)
    {
        if (newState != UnitState.Dead) return;

        enabled = false;

        WallRunning = false;
        Vaulting = false;

        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;
        rb.detectCollisions = false;

        ObjectPooler.Instance.Spawn("ShatteredPlayer", transform.position, transform.rotation);
    }
}
