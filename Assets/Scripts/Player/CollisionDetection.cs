﻿using System;
using UnityEngine;

[Serializable]
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

    private bool cancelWall = false;
    private bool cancelGrounded = false;

    private int wallCancelSteps = 0;
    private int groundCancelSteps = 0;

    public int StepsSinceGrounded { get; private set; } = 0;
    public int StepsSinceLastGrounded { get; private set; } = 0;
    public int StepsSinceLastJumped { get; private set; } = 0;
    public int StepsSinceLastWallJumped { get; private set; } = 0;

    public delegate void OnColliderLandHandler(float impactForce);
    public event OnColliderLandHandler OnColliderLand;

    public void UpdateCollisionChecks(PlayerManager s) {

        ReachedMaxSlope = Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, out var slopeHit, 1.5f, Ground) && Vector3.Angle(Vector3.up, slopeHit.normal) > maxSlopeAngle;

        if (Grounded) {
            if (!cancelGrounded) cancelGrounded = true;
            else {
                groundCancelSteps++;

                if (groundCancelSteps > groundCancelDelay)
                {
                    GroundContact = new ContactPoint();
                    Grounded = false;
                }
            }
        }

        if (NearWall) {
            float dot = Vector3.Dot(s.orientation.right, WallContact.normal);

            IsWallLeft = dot > 0.8f;
            IsWallRight = dot < -0.8f;

            if (!cancelWall) cancelWall = true;
            else {
                wallCancelSteps++;

                if (wallCancelSteps > wallCancelDelay)
                {
                    WallContact = new ContactPoint();
                    NearWall = false;
                    IsWallLeft = false;
                    IsWallRight = false;
                }
            }
        }
    }

    public void EvaluateCollisionEnter(PlayerManager s, Collision col) {
        int layer = col.gameObject.layer;
        ContactPoint contact = col.GetContact(0);

        if (IsFloor(contact.normal) && Ground == (Ground | 1 << layer) && !Grounded)
        {
            OnColliderLand?.Invoke(Mathf.Min(0, s.PlayerMovement.Velocity.y));
            Grounded = true;
        }

        if (!ReachedMaxSlope && CancelEdgeCollision(s, contact, Ground)) s.PlayerMovement.CheckForVault(col, contact, Environment, maxSlopeAngle);
    }

    private bool CancelEdgeCollision(PlayerManager s, ContactPoint contact, LayerMask VaultEnvironment)
    {
        if (Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, out var edgeHit, 1f, VaultEnvironment)
            || Physics.Raycast(s.playerHead.position, Vector3.up, out edgeHit, 1f, VaultEnvironment))
            if (edgeHit.collider == contact.otherCollider) return false;

        s.rb.velocity = s.PlayerMovement.Velocity;

        return true;
    }

    public void EvaluateCollisionStay(Collision col) {
        int layer = col.gameObject.layer;

        for (int i = 0; i < col.contactCount; i++) {
            ContactPoint contact = col.GetContact(i);

            if (IsFloor(contact.normal)) {
                if (Ground != (Ground | 1 << layer)) continue;

                Grounded = true;
                cancelGrounded = false;
                GroundContact = contact;
                groundCancelSteps = 0;
            }

            if (IsWall(contact.normal, 0.1f)) {
                if (Environment != (Environment | 1 << layer)) continue;

                NearWall = true;
                cancelWall = false;
                WallContact = contact;
                wallCancelSteps = 0;
            }
        }
    }

    public void RecordMovementSteps(PlayerManager s, int maxJumpSteps) {
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

    public bool SnapToGround(PlayerManager s, int maxJumpSteps) {
        float speed = s.PlayerMovement.Magnitude;
        Rigidbody rb = s.rb;

        if (speed < 3f || StepsSinceLastGrounded > 3 || StepsSinceLastJumped < maxJumpSteps || s.PlayerMovement.Vaulting || Grounded) return false;
        if (!Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, out var snapHit, 1.8f, GroundSnapLayer)) return false;

        Grounded = true;

        float dot = Vector3.Dot(rb.velocity, Vector3.up);

        if (dot > 0) rb.velocity = (rb.velocity - (snapHit.normal * dot)).normalized * speed;
        else rb.velocity = (rb.velocity - snapHit.normal).normalized * speed;

        return true;
    }

    public bool CanWallJump(PlayerManager s, float minimumJumpHeight) {
        if (!NearWall || ReachedMaxSlope || Grounded || s.PlayerMovement.Vaulting || !s.PlayerMovement.CanCrouchWalk) return false;
        return !Physics.Raycast(s.BottomCapsuleSphereOrigin, Vector3.down, minimumJumpHeight, Ground);
    }

    public void SetGrounded(bool value) => Grounded = value;
    public void SetNearWall(bool value) => NearWall = value;
    public void ResetJumpSteps(int amount = 0) => StepsSinceLastJumped = amount;
    public void ResetWallJumpSteps(int amount = 0) => StepsSinceLastWallJumped = amount;

    public bool IsFloor(Vector3 normal) => Vector3.Angle(Vector3.up, normal) < maxSlopeAngle;
    public bool IsWall(Vector3 normal, float threshold) => Math.Abs(normal.y) < threshold;

    public bool CeilingAbove(PlayerManager s, bool crouched) {

        if (!crouched) return false;

        return Physics.CheckCapsule(s.BottomCapsuleSphereOrigin, s.playerHead.position, s.cc.radius * (NearWall ? 0.95f : 1.1f), Environment);
    }
}
