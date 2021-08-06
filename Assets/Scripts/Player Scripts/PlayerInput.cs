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
    public bool reachedMaxSlope;

    public bool isWallLeft { get; private set; }
    public bool isWallRight { get; private set; }

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

    void Awake() => s = GetComponent<ScriptManager>();

    void FixedUpdate()
    {
        UpdateGroundCollisions();
        UpdateWallCollisions();

        reachedMaxSlope = (Physics.Raycast(s.bottomCapsuleSphereOrigin, Vector3.down, out var slopeHit, 1.5f, Ground) ? Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle : false);

        s.PlayerMovement.SetInput(input, jumping, crouching, grounded);
        s.PlayerMovement.Movement();
    }

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

    public bool CanWallJump()
    {
        if (!nearWall) return false;
        if (reachedMaxSlope || s.PlayerMovement.vaulting || grounded || crouching) return false;
        return !Physics.Raycast(s.bottomCapsuleSphereOrigin, Vector3.down, minimumJumpHeight, Ground);
    }
    #endregion

    #region Vaulting
    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Ground != (Ground | 1 << layer)) return;

        Vector3 normal = col.GetContact(0).normal;

        float fallSpeed = s.PlayerMovement.velocity.y;
        float magnitude = s.PlayerMovement.magnitude;

        if (IsFloor(normal)) if (!grounded) Land(Math.Abs(fallSpeed));

        if (IsWall(normal, 0.31f))
        {
            if (s.PlayerMovement.vaulting || s.PlayerMovement.wallRunning || crouching || reachedMaxSlope || Environment != (Environment | 1 << layer)) return;
            
            Vector3 vaultDir = normal;
            vaultDir.y = 0f;
            vaultDir.Normalize();

            Vector3 vel = s.PlayerMovement.velocity;
            vel.y = 0f;

            Vector3 moveDir = s.PlayerMovement.moveDir;
            Vector3 vaultCheck = transform.position + Vector3.up * 1.5f;
         
            if (Vector3.Dot(-vaultDir, moveDir) < 0.5f) return;
            if (Physics.Raycast(vaultCheck, Vector3.up, 2f, Environment)) return;
            if (!Physics.Raycast(vaultCheck - vaultDir, Vector3.down, out var vaultHit, 3f, Environment)) return;
            if (Vector3.Angle(Vector3.up, vaultHit.normal) > maxSlopeAngle) return;

            Vector3 vaultPoint = vaultHit.point + (Vector3.up * 2f) + (vaultDir);
            float distance = vaultPoint.y - s.bottomCapsuleSphereOrigin.y;

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
    }
    #endregion

    #region Collision Detection
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
    #endregion

    private void Land(float impactForce)
    {
        if (impactForce > 30f)
        {
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = ObjectPooler.Instance.SpawnParticle("LandFX", transform.position, Quaternion.Euler(0, 0, 0)).velocityOverLifetime;

            Vector3 magnitude = s.rb.velocity;

            velocityOverLifetime.x = magnitude.x;
            velocityOverLifetime.z = magnitude.z;
        }
            
        s.CameraHeadBob.CameraLand(impactForce);
    }

    float LandVel(float mag, float y) => (mag * 0.1f) + Math.Abs(y);

    bool IsFloor(Vector3 normal) => Vector3.Angle(Vector3.up, normal) < maxSlopeAngle;
    bool IsWall(Vector3 normal, float threshold) => Math.Abs(Vector3.Dot(normal, Vector3.up)) < threshold;
}
