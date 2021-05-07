using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InputManager : MonoBehaviour
{
    public Vector2 input { get; private set; }
    private Vector3 inputDir;
    private Vector3 slopeDir;
    public Vector3 moveDir { get; private set; }

    public Vector3 groundNormal { get; private set; }
    public Vector3 wallNormal { get; private set; }

    float multiplier, multiplierV;

    [Header("Thresholds")]
    [Range(0f, 90f)] public float maxSlopeAngle;
    public float minimumJumpHeight;

    [Header("States")]
    public bool grounded;
    public bool nearWall;

    [HideInInspector] public bool wallRunning;
    [HideInInspector] public bool stopWallRun;

    public bool isWallLeft { get; private set; }
    public bool isWallRight { get; private set; }

    public bool crouching { get; private set; }
    public bool jumping { get; private set; }
    public bool moving { get; private set; }
    public bool interacting { get; private set; }

    bool fast = false;
    bool landed = false;

    [Header("KeyBinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Collision")]
    public LayerMask Ground;
    public LayerMask Environment;
    [SerializeField] private float groundCancelDelay;
    [SerializeField] private float wallCancelDelay;

    private bool cancelWall = true;
    private int wallCancelSteps = 0;

    private bool cancelGround = false;
    private int groundCancelSteps = 0;

    [Header("Assignables")]
    [SerializeField] private ParticleSystem sprintEffect;
    [SerializeField] private Transform orientation;
    private RaycastHit slopeHit;
    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
    }

    void FixedUpdate()
    {
        UpdateCollisions();
    }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        jumping = Input.GetKeyDown(jumpKey);
        crouching = Input.GetKey(crouchKey);
        moving = input.x != 0f || input.y != 0f;
        interacting = Input.GetKeyDown(interactKey);

        if (nearWall && isWallLeft && CanWallJump() && input.x < 0 || nearWall && isWallRight && CanWallJump() && input.x > 0) wallRunning = true;
        stopWallRun = isWallLeft && input.x > 0 && wallRunning || isWallRight && input.x < 0 && wallRunning || crouching && wallRunning;

        inputDir = (orientation.forward * input.y * multiplier * multiplierV + orientation.right * input.x * multiplier);
        slopeDir = Vector3.ProjectOnPlane(inputDir, groundNormal);

        float dot = Vector3.Dot(Vector3.up, slopeDir);
        moveDir = dot < 0f ? slopeDir : inputDir;

        CheckForWall();
        MovementControl();

        CameraTilt();
        SprintEffect();
    }

    #region Movement Calculations
    private void UpdateCollisions()
    {
        if (s.rb.IsSleeping()) s.rb.WakeUp();

        if (grounded) 
        {
            if (!cancelGround) cancelGround = true;
            else 
            {
                groundCancelSteps++;
                if ((float) groundCancelSteps > groundCancelDelay)
                {
                    groundNormal = Vector3.up;
                    landed = false;
                    grounded = false;
                }
            }
        }

        if (nearWall)
        {
            if (!cancelWall) cancelWall = true;
            else
            {
                wallCancelSteps++;
                if ((float) wallCancelSteps > wallCancelDelay)
                {
                    nearWall = false;
                    wallNormal = Vector3.zero;
                    isWallLeft = false;
                    isWallRight = false;
                }
            }
        }
    }

    public bool ReachedMaxSlope()
    {
        if (!Physics.Raycast(s.groundCheck.position, Vector3.down, out slopeHit, 1.5f, Ground)) return false;
        if (slopeHit.normal == Vector3.up) return false;
        return Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle;
    }

    public bool CanWallJump()
    {
        if (!nearWall || s.PlayerMovement.vaulting) return false;
        if (ReachedMaxSlope()) return false;
        return !Physics.Raycast(s.groundCheck.position, Vector3.down, minimumJumpHeight, Ground);
    }

    private void CheckForWall()
    {
        if (nearWall)
        {
            isWallLeft = Physics.Raycast(transform.position, -orientation.right, 1f, Environment) && !isWallRight;
            isWallRight = Physics.Raycast(transform.position, orientation.right, 1f, Environment) && !isWallLeft;
        }

        if (!CanWallJump() || !isWallRight && !isWallLeft)
        {
            wallRunning = false;
            s.rb.useGravity = true;
        }    
    }

    private void MovementControl()
    {
        if (grounded)
        {
            if (multiplierV != 1.05f) multiplierV = 1.1f;
            if (!crouching && !wallRunning && multiplier != 1f) multiplier = 1f;
            if (crouching && !wallRunning && multiplier != 0.05f) multiplier = 0.05f;
        }

        if (!grounded)
        {
            if (!wallRunning && !crouching && multiplier != 0.5f && multiplierV != 0.9f)
            {
                multiplier = 0.5f;
                multiplierV = 0.9f;
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
        if (crouching) s.CamInput.CameraSlide();

        if (wallRunning && isWallLeft) s.CamInput.CameraWallRun(-1);
        if (wallRunning && isWallRight) s.CamInput.CameraWallRun(1);

        if (!wallRunning && Math.Abs(s.CamInput.CameraTilt) > 0f && !crouching || !wallRunning && s.CamInput.fov > 80f)
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

    #region Collision Detection
    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | 1 << layer)) return;

        Vector3 normal = col.GetContact(0).normal;
        
        if (IsFloor(normal)) if (!landed) Land(LandVel(s.magnitude, Math.Abs(s.velocity.y)));

        if (IsVaultable(normal))
        {
            if (wallRunning || crouching || nearWall || ReachedMaxSlope() || Environment != (Environment | 1 << layer)) return;

            Vector3 vaultDir = new Vector3(-normal.x, 0, -normal.z);
            Vector3 dir = inputDir;
            Vector3 vaultCheck = s.playerHead.position + (Vector3.down * 0.4f);

            if (Vector3.Dot(dir.normalized, normal) > -0.6f) return;
            if (Physics.Raycast(vaultCheck, dir.normalized, 1.3f, Environment) || Physics.Raycast(vaultCheck, -normal, 1.3f, Environment)) return;

            RaycastHit hit;
            if (!Physics.Raycast(vaultCheck + (dir + vaultDir).normalized, Vector3.down, out hit, 3f, Environment)) return;

            Vector3 vaultPoint = hit.point + (Vector3.up * 2.1f);
            float distance = Vector3.Distance(s.rb.position, vaultPoint);
            vaultPoint += (Vector3.up * (distance * 0.3f));

            StartCoroutine(s.PlayerMovement.VaultMovement(vaultPoint, distance, dir.normalized));
        }   
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

            if (IsWall(normal))
            {
                if (Environment != (Environment | 1 << layer)) continue;

                nearWall = true;
                wallCancelSteps = 0;
                cancelWall = false;
                wallNormal = normal;
            }
        }
    }
    #endregion

    private void Land(float impactForce)
    {
        s.Effects.CameraLand(impactForce);

        if (impactForce > 70f) 
            ObjectPooler.Instance.Spawn("Land Effects", transform.position + Vector3.down, Quaternion.Euler(-90, 0, 0));

        landed = true;
    }

    float LandVel(float mag, float yMag)
    {
        return (mag * 0.4f) + Math.Abs(yMag * 2.8f);
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

    bool IsVaultable(Vector3 normal)
    {
        return Math.Abs(Vector3.Dot(normal, Vector3.up)) < 0.3f;
    }
}
