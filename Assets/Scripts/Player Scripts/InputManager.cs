using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InputManager : MonoBehaviour
{
    public Vector2 input { get; private set; }

    public Vector3 groundNormal { get; private set; }
    public Vector3 wallNormal { get; private set; }

    [Header("Thresholds")]
    [SerializeField] [Range(0f, 90f)] private float maxSlopeAngle;
    [SerializeField] private float minimumJumpHeight;
    [SerializeField] private float vaultOffset;

    [Header("States")]
    public bool grounded;
    public bool nearWall;
    public bool onRamp;
    public bool reachedMaxSlope;

    public bool isWallLeft { get; private set; }
    public bool isWallRight { get; private set; }
    public bool wallRunning { get; private set; }
    public bool stopWallRun { get; private set; }

    public bool jumping { get; private set; }
    public bool crouching { get; private set; }
    public bool interacting { get; private set; }
    public bool dropping { get; private set; }
    public bool moving { get; private set; }
    public bool rightClick { get; private set; }

    [Header("KeyBinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.Q;

    [Header("Collision")]
    [SerializeField] private LayerMask Ground;
    [SerializeField] private LayerMask Environment;
    [SerializeField] private float groundCancelDelay;
    [SerializeField] private float wallCancelDelay;

    private bool cancelWall = true;
    private int wallCancelSteps = 0;

    private bool cancelGround = false;
    private int groundCancelSteps = 0;

    [Header("Assignables")]
    private ScriptManager s;
    private RaycastHit slopeHit;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
    }

    void FixedUpdate()
    {
        UpdateCollisions();
        s.PlayerMovement.SetInput(input, jumping, crouching, grounded);
        s.PlayerMovement.Movement();
    }

    void Update()
    {
        #region Input
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        jumping = Input.GetKeyDown(jumpKey);
        crouching = Input.GetKey(crouchKey);
        interacting = Input.GetKeyDown(interactKey);
        dropping = Input.GetKeyDown(dropKey);
        moving = input.x != 0f || input.y != 0f;
        rightClick = Input.GetMouseButtonDown(1);

        if (nearWall && isWallLeft && CanWallJump() && input.x < 0 || nearWall && isWallRight && CanWallJump() && input.x > 0) wallRunning = true;
        stopWallRun = isWallLeft && input.x > 0 && wallRunning || isWallRight && input.x < 0 && wallRunning;

        if (Physics.Raycast(s.groundCheck.position, Vector3.down, out slopeHit, 1.5f, Ground))
            reachedMaxSlope = Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle;
        else reachedMaxSlope = false;
        #endregion 

        CheckForWall();
    }

    #region Movement Calculations
    private void UpdateCollisions()
    {
        if (grounded) 
        {
            if (!cancelGround) cancelGround = true;
            else 
            {
                groundCancelSteps++;
                if ((float) groundCancelSteps > groundCancelDelay)
                {
                    onRamp = false;
                    groundNormal = Vector3.up;
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

    public bool CanWallJump()
    {
        if (!nearWall || s.PlayerMovement.vaulting || grounded || crouching) return false;
        if (reachedMaxSlope) return false;
        return !Physics.Raycast(s.groundCheck.position, Vector3.down, minimumJumpHeight, Ground);
    }

    private void CheckForWall()
    {
        if (nearWall)
        {
            float dot = Vector3.Dot(s.orientation.right, wallNormal);

            isWallLeft = dot > 0.8f;
            isWallRight = dot < -0.8f;
        }

        if (!CanWallJump() || !isWallRight && !isWallLeft) wallRunning = false;
    }
    #endregion

    #region Collision Detection
    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | 1 << layer)) return;

        Vector3 normal = col.GetContact(0).normal;

        if (IsFloor(normal)) if (!grounded) Land(LandVel(s.PlayerMovement.magnitude, s.PlayerMovement.velocity.y));

        #region Vaulting
        if (IsVaultable(normal))
        {
            if (s.PlayerMovement.vaulting || wallRunning || crouching || reachedMaxSlope || Environment != (Environment | 1 << layer)) return;
            
            Vector3 vaultDir = normal;
            vaultDir.y = 0f;
            vaultDir.Normalize();

            Vector3 vel = s.PlayerMovement.velocity * 0.8f;
            vel.y = 0f;

            Vector3 moveDir = s.orientation.forward * input.y + s.orientation.right * input.x;
            Vector3 vaultCheck = transform.position + Vector3.up * 1.5f;
         
            if (Vector3.Dot(-vaultDir, moveDir) < 0.5f) return;
            if (Physics.Raycast(vaultCheck, Vector3.up, 2f, Environment)) return;
            if (!Physics.Raycast(vaultCheck - vaultDir, Vector3.down, out var vaultHit, 3f, Environment)) return;

            Vector3 vaultPoint = vaultHit.point + (Vector3.up * 2f) + (vaultDir);
            float distance = vaultPoint.y - s.groundCheck.position.y;

            if (distance > vaultOffset) return;

            if (distance < 3.3f)
            {
                s.CameraHeadBob.StepUp(transform.position - vaultPoint);
                transform.position = vaultPoint;
                s.rb.velocity = vel;
                return;
            }

            StartCoroutine(s.PlayerMovement.Vault(vaultPoint, -vaultDir, distance));
        }
        #endregion
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

                onRamp = normal.y < 1f;

                grounded = true;
                cancelGround = false;
                groundCancelSteps = 0;
                groundNormal = normal;
            }

            if (IsWall(normal))
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
        if (impactForce > 100f)
        {
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = ObjectPooler.Instance.SpawnParticle("LandFX", transform.position, Quaternion.Euler(0, 0, 0)).velocityOverLifetime;

            Vector3 magnitude = s.rb.velocity;

            velocityOverLifetime.x = magnitude.x;
            velocityOverLifetime.z = magnitude.z;
        }
            
        s.CameraLandBob.CameraLand(impactForce);
    }
    #endregion

    #region Vector and speed calculations
    float LandVel(float mag, float yMag)
    {
        return (mag * 0.6f) + Math.Abs(yMag * 4f);
    }

    bool IsFloor(Vector3 normal)
    {
        return Vector3.Angle(Vector3.up, normal) < maxSlopeAngle;
    }

    bool IsWall(Vector3 normal)
    {
        return Math.Abs(Vector3.Dot(normal, Vector3.up)) < 0.1f;
    }

    bool IsVaultable(Vector3 normal)
    {
        return Math.Abs(Vector3.Dot(normal, Vector3.up)) < 0.34f;
    }
    #endregion
}
