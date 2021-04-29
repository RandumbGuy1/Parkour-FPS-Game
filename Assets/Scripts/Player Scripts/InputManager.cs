using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InputManager : MonoBehaviour
{
    [HideInInspector] public Vector2 input = Vector3.zero;
    public Vector3 moveDir { get; private set; }
    public Vector3 inputDir { get; private set; }

    public Vector3 groundNormal { get; private set; }
    public Vector3 wallNormal { get; private set; }

    float multiplier, multiplierV;

    [Header("Thresholds")]
    [Range(0f, 90f)] public float maxSlopeAngle;
    public float minimumJumpHeight;

    [Header("States")]
    public bool grounded;
    public bool nearWall;
    public bool reachedMaxSlope;

    [HideInInspector] public bool wallRunning;
    [HideInInspector] public bool canAddWallRunForce;
    [HideInInspector] public bool stopWallRun;

    public bool isWallLeft { get; private set; }
    public bool isWallRight { get; private set; }
    public bool canWallRun { get; private set; }
    public bool canWallJump { get; private set; }

    public bool crouching { get; private set; }
    public bool jumping { get; private set; }
    public bool moving { get; private set; }
    public bool vaulting { get; private set; }

    public bool hitGround { get; private set; }
    public int stepsSinceLastGrounded { get; private set; }

    bool fast = false;
    bool landed = false;

    [Header("KeyBinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Collision")]
    public LayerMask Ground;
    public LayerMask Environment;
    public float groundRadius;
    public float wallRadius;

    [Header("Vaulting")]
    public float vaultDuration;

    [Header("Assignables")]
    public ParticleSystem sprintEffect;
    public Transform orientation;

    private RaycastHit hit;

    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        vaulting = false;
    }

    void FixedUpdate()
    {
        GroundDetection();
    }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        jumping = Input.GetKeyDown(jumpKey);
        crouching = Input.GetKey(crouchKey) && !wallRunning;
        moving = input.x != 0f || input.y != 0f;

        if (nearWall && isWallLeft && canWallJump && input.x < 0 || nearWall && isWallRight && canWallJump && input.x > 0) wallRunning = true;
        stopWallRun = Vector3.Dot(moveDir.normalized, wallNormal) > 0.3f && wallRunning;

        inputDir = (orientation.forward * input.y * multiplier * multiplierV + orientation.right * input.x * multiplier);
        moveDir = Vector3.ProjectOnPlane(inputDir, groundNormal);

        hitGround = Physics.Raycast(s.groundCheck.position, Vector3.down, out hit, 2f, Ground);
        if (hitGround) reachedMaxSlope = Vector3.Angle(Vector3.up, hit.normal) > maxSlopeAngle;
        else reachedMaxSlope = false;

        CheckForWall();
        MovementControl();

        CameraTilt();
        SprintEffect();
    }

    #region Movement Calculations

    private void GroundDetection()
    {
        if (grounded)
            if (!Physics.CheckSphere(s.groundCheck.position, groundRadius, Ground) || reachedMaxSlope)
            {
                groundNormal = Vector3.zero;
                landed = false;
                grounded = false;
            }

        if (!grounded && stepsSinceLastGrounded < 10) stepsSinceLastGrounded++;
        else if (grounded && stepsSinceLastGrounded > 0) stepsSinceLastGrounded = 0;
    }

    private void CheckForWall()
    {
        if (nearWall)
        {
            Vector3 point1 = transform.position + (Vector3.up * 0.5f);
            Vector3 point2 = transform.position + (Vector3.down * 0.6f);

            isWallLeft = Physics.Raycast(transform.position, -orientation.right, 1f, Environment) && !isWallRight;
            isWallRight = Physics.Raycast(transform.position, orientation.right, 1f, Environment) && !isWallLeft;

            canWallRun = !Physics.Raycast(s.groundCheck.position, Vector3.down, minimumJumpHeight, Ground);
            canWallJump = !crouching && !reachedMaxSlope && !vaulting && canWallRun;

            if (!Physics.CheckCapsule(point1, point2, wallRadius, Environment))
            {
                wallNormal = Vector3.zero;
                isWallLeft = false;
                isWallRight = false;
                canWallJump = false;
                canWallRun = false;
                nearWall = false;
            }
        }

        if (!nearWall || !isWallRight && !isWallLeft || !canWallRun)
        {
            wallRunning = false;
            canAddWallRunForce = true;
            s.rb.useGravity = true;
        }    
    }

    private IEnumerator vaultMovement(Vector3 newPos, Vector3 extraForce)
    {
        s.rb.useGravity = false;

        Vector3 vel = Vector3.zero;
        float elapsed = 0f;

        float distance = Vector3.Distance(s.rb.position, newPos) * 0.01f;
        distance = Mathf.Round(distance * 1000.0f) * 0.001f;

        float duration = vaultDuration + distance;

        while (elapsed < (duration * 2f))
        {
            vaulting = true;
            s.rb.MovePosition(Vector3.SmoothDamp(s.rb.position, newPos, ref vel, duration));
            elapsed += Time.deltaTime;

            yield return null;
        }

        s.rb.useGravity = true;
        s.rb.AddForce(extraForce * 10f, ForceMode.VelocityChange);
        s.rb.AddForce(Vector3.down * (1 / distance) * 0.06f, ForceMode.VelocityChange);

        vaulting = false;
    }

    private void MovementControl()
    {
        if (grounded)
        {
            if (multiplierV != 1.05f) multiplierV = 1.05f;
            if (!crouching && !wallRunning && multiplier != 1f) multiplier = 1f;
            if (crouching && !wallRunning && multiplier != 0.05f) multiplier = 0.05f;
        }

        if (!grounded)
        {
            if (!wallRunning && !crouching && multiplier != 0.6f && multiplierV != 0.8f)
            {
                multiplier = 0.6f;
                multiplierV = 0.8f;
            }
            if (!wallRunning && crouching && multiplier != 0.3f && multiplierV != 0.8f)
            {
                multiplier = 0.3f; 
                multiplierV = 0.8f;
            }
            if (s.PlayerInput.wallRunning && multiplier != 0.01f && multiplierV != 0.30f)
            {
                multiplier = 0.01f; 
                multiplierV = 30f;
            }
        }
    }
    #endregion

    #region Visual Effects
    private void CameraTilt()
    {
        if (crouching && grounded) s.CamInput.CameraSlide();
        if (!crouching && !wallRunning) s.CamInput.ResetCameraTilt();

        if (wallRunning && isWallLeft) s.CamInput.CameraWallRun(-1);
        if (wallRunning && isWallRight) s.CamInput.CameraWallRun(1);

        if (!wallRunning && Math.Abs(s.CamInput.CameraTilt) > 0f || !wallRunning && s.CamInput.fov > 80f)
            s.CamInput.ResetCameraTilt();
    }

    private void SprintEffect()
    {
        if (s.magnitude >= 25f && !fast)
        {
            sprintEffect.Play();
            fast = true;
        }
        else if (s.magnitude < 25f && fast)
        {
            sprintEffect.Stop();
            fast = false;
        }

        if (s.magnitude >= 25f)
        {
            var em = sprintEffect.emission;
            em.rateOverTime = s.magnitude;
        }
    }
    #endregion

    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | 1 << layer)) return;

        Vector3 normal = col.GetContact(0).normal;

        if (IsFloor(normal)) if (!landed) Land(LandVel(s.magnitude, Math.Abs(s.velocity.y)));

        if (IsWall(normal))
        {
            if (col.transform.GetComponent<Rigidbody>() || wallRunning) return;

            Vector3 dir = moveDir;
            Vector3 vaultCheck = s.playerHead.position + (Vector3.down * 0.4f);

            if (Vector3.Dot(dir.normalized, normal) > -0.3f) return;
            if (Physics.Raycast(vaultCheck, dir.normalized, 1.3f, Environment)) return;

            RaycastHit hit;
            if (!Physics.Raycast(vaultCheck + (dir.normalized), Vector3.down, out hit, 3f, Environment)) return;

            Vector3 vaultPoint = hit.point + (Vector3.up * 2.6f);
            if (!vaulting) StartCoroutine(vaultMovement(vaultPoint, dir));
        }   
    }

    void OnCollisionStay(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | 1 << layer)) return;

        Vector3 normal = col.GetContact(0).normal;

        if (IsFloor(normal))
        {
            grounded = true;
            groundNormal = normal;
        }

        if (IsWall(normal))
        {
            nearWall = true;
            wallNormal = normal;
        }
    }

    private void Land(float impactForce)
    {
        s.Effects.CameraLand(impactForce);

        if (impactForce > 70f) 
            ObjectPooler.Instance.Spawn("Land Effects", transform.position + Vector3.down, Quaternion.Euler(-90, 0, 0));

        landed = true;
    }

    float LandVel(float mag, float yMag)
    {
        return (mag * 0.5f) + Math.Abs(yMag * 3f);
    }

    bool IsFloor(Vector3 normal)
    {
        float angle = Vector3.Angle(Vector3.up, normal);
        return angle < maxSlopeAngle;
    }

    bool IsWall(Vector3 normal)
    {
        return Math.Abs(Vector3.Dot(normal, Vector3.up)) < 0.1f;
    }
}
