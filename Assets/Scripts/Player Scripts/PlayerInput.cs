using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerInput : MonoBehaviour
{
    public Vector2 input { get { return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); } }

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

    public bool jumping { get { return Input.GetKeyDown(jumpKey); } }
    public bool crouching { get { return Input.GetKey(crouchKey); } }
    public bool interacting { get { return Input.GetKeyDown(interactKey); } }
    public bool dropping { get { return Input.GetKeyDown(dropKey); } }
    public bool reloading { get { return Input.GetKeyDown(reloadKey); } }

    public bool rightClick { get { return Input.GetMouseButtonDown(1); } }
    public bool leftClick { get { return Input.GetMouseButtonDown(0); } }
    public bool leftHoldClick { get { return Input.GetMouseButton(0); } }

    [Header("KeyBinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.Q;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

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
    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
    }

    void FixedUpdate()
    {
        if (nearWall && isWallLeft && CanWallJump() && input.x < 0 || nearWall && isWallRight && CanWallJump() && input.x > 0) wallRunning = true;

        UpdateCollisions();
        s.PlayerMovement.SetInput(input, jumping, crouching, grounded);
        s.PlayerMovement.Movement();
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
                    reachedMaxSlope = false;
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
                    wallRunning = false;
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
            if (Vector3.Angle(Vector3.up, vaultHit.normal) > maxSlopeAngle) return;

            Vector3 vaultPoint = vaultHit.point + (Vector3.up * 2f) + (vaultDir);
            float distance = vaultPoint.y - s.groundCheck.position.y;

            if (distance > vaultOffset) return;

            if (distance < 3.5f)
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

                float dot = Vector3.Dot(s.orientation.right, normal);

                isWallLeft = dot > 0.8f;
                isWallRight = dot < -0.8f;

                if (wallRunning && !CanWallJump() || wallRunning && !isWallLeft && !isWallRight) wallRunning = false;
            }

            reachedMaxSlope = Physics.Raycast(s.groundCheck.position, Vector3.down, out var slopeHit, 1.5f, Ground) && Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle;
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
