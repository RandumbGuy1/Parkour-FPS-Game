using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
public class CollisionDetection 
{
    [Header("Collision")]
    [SerializeField] private LayerMask GroundSnapLayer;
    [SerializeField] private LayerMask Ground;
    [SerializeField] private LayerMask Environment;
    [Space(10)]
    [SerializeField] private float groundCancelDelay;
    [SerializeField] private float wallCancelDelay;
    [Space(10)]
    [SerializeField] [Range(0f, 90f)] private float maxSlopeAngle;

    public Vector3 WallNormal => WallContact.normal;
    public Vector3 GroundNormal => Grounded ? GroundContact.normal : Vector3.up;

    public ContactPoint GroundContact { get; private set; }
    public ContactPoint WallContact { get; private set; }

    public bool Grounded { get; private set; } = false;
    public bool ReachedMaxSlope { get; private set; } = false;

    public bool NearWall { get; private set; } = false;
    public bool IsWallLeft { get; private set; } = false;
    public bool IsWallRight { get; private set; } = false;

    private int wallCancelSteps = -1;
    private int groundCancelSteps = -1;

    public int StepsSinceGrounded { get; private set; } = 0;
    public int StepsSinceLastGrounded { get; private set; } = 0;
    public int StepsSinceLastJumped { get; private set; } = 0;
    public int StepsSinceLastWallJumped { get; private set; } = 0;

    public void UpdateCollisionChecks(ScriptManager s) {

        ReachedMaxSlope = Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, out var slopeHit, 1.5f, Ground) && Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle;

        if (Grounded) {
             groundCancelSteps++;

             if (groundCancelSteps > groundCancelDelay) {
                GroundContact = new ContactPoint();
                Grounded = false;
             }
        }

        if (NearWall) {
            float dot = Vector3.Dot(s.orientation.right, WallContact.normal);

            IsWallLeft = dot > 0.8f;
            IsWallRight = dot < -0.8f;

            wallCancelSteps++;

            if (wallCancelSteps > wallCancelDelay) {
                WallContact = new ContactPoint();
                NearWall = false;
                IsWallLeft = false;
                IsWallRight = false;
            }
        }
    }

    public void EvaluateCollisionEnter(ScriptManager s, Collision col) {
        int layer = col.gameObject.layer;
        ContactPoint contact = col.GetContact(0);

        if (IsFloor(contact.normal) && Ground == (Ground | 1 << layer) && !Grounded)
        {
            Grounded = true;
            s.CameraHeadBob.BobOnce(Mathf.Min(0, s.PlayerMovement.Velocity.y));
        }

        if (!ReachedMaxSlope) s.PlayerMovement.CheckForVault(contact, Environment, maxSlopeAngle);
    }

    public void EvaluateCollisionStay(Collision col) {
        int layer = col.gameObject.layer;

        for (int i = 0; i < col.contactCount; i++) {
            ContactPoint contact = col.GetContact(i);

            if (IsFloor(contact.normal)) {
                if (Ground != (Ground | 1 << layer)) continue;

                Grounded = true;
                GroundContact = contact;
                groundCancelSteps = -1;
            }

            if (IsWall(contact.normal, 0.1f)) {
                if (Environment != (Environment | 1 << layer)) continue;

                NearWall = true;
                WallContact = contact;
                wallCancelSteps = -1;
            }
        }
    }

    public void RecordMovementSteps(ScriptManager s, int maxJumpSteps) {
        if (StepsSinceLastJumped < maxJumpSteps) StepsSinceLastJumped++;
        if (StepsSinceLastWallJumped < 100) StepsSinceLastWallJumped++;

        if (Grounded || SnapToGround(s, maxJumpSteps))
        {
            if (StepsSinceGrounded < 10) StepsSinceGrounded++;
            StepsSinceLastGrounded = 0;
        }
        else
        {
            if (StepsSinceLastGrounded < 10) StepsSinceLastGrounded++;
            StepsSinceGrounded = 0;
        }
    }

    public bool SnapToGround(ScriptManager s, int maxJumpSteps) {
        float speed = s.PlayerMovement.Magnitude;
        Rigidbody rb = s.rb;

        if (speed < 3f || StepsSinceLastGrounded > 3 || StepsSinceLastJumped < maxJumpSteps || s.PlayerMovement.Vaulting || Grounded) return false;
        if (!Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, out var snapHit, 1.8f, GroundSnapLayer)) return false;

        Grounded = true;

        float dot = Vector3.Dot(rb.velocity, Vector3.up);

        if (dot > 0) rb.velocity = (rb.velocity - (snapHit.normal * dot)).normalized * speed;
        else rb.velocity = (rb.velocity - snapHit.normal).normalized * speed;
        MonoBehaviour.print(1);
        return true;
    }

    public bool CanWallJump(ScriptManager s, float minimumJumpHeight) {
        if (!NearWall || ReachedMaxSlope || Grounded || s.PlayerMovement.Vaulting || !s.PlayerMovement.CanCrouchWalk) return false;
        return !Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, minimumJumpHeight, Ground);
    }

    public void SetGrounded(bool value) => Grounded = value;
    public void SetNearWall(bool value) => NearWall = value;
    public void ResetJumpSteps(int amount = 0) => StepsSinceLastJumped = amount;
    public void ResetWallJumpSteps(int amount = 0) => StepsSinceLastWallJumped = amount;

    public bool IsFloor(Vector3 normal) => Vector3.Angle(Vector3.up, normal) < maxSlopeAngle;
    public bool IsWall(Vector3 normal, float threshold) => Math.Abs(normal.y) < threshold;

    public bool CeilingAbove(ScriptManager s, bool crouched) {

        if (!crouched) return false;

        return Physics.CheckCapsule(s.BottomCapsuleSphereOrigin, s.playerHead.position, s.cc.radius * (NearWall ? 0.95f : 1.1f), Environment);
    }
}
