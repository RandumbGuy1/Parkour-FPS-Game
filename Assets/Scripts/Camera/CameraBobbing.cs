﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraBobbing : MonoBehaviour
{
	[Header("Land Bob Settings")]
    [SerializeField] private ShakeData landbobShakeData;
    [SerializeField] private float landBobSmoothTime;
	[SerializeField] private float landBobMultiplier;
	[SerializeField] private float maxOffset;
    [SerializeField] private float slideMaxOffset;

    private float landVel = 0f;
	private float desiredOffset = 0f;
    private float landBobOffset = 0f;

    [Header("View Bob Settings")]
    [SerializeField] private float viewBobSpeed;
    [SerializeField] private float viewBobAmountHoriz;
    [SerializeField] private float viewBobAmountVert;
    [Range(0f, 0.5f)] [SerializeField] private float viewBobSmoothTime;

    public float BobTimer { get; private set; }
    private Vector3 bobVel = Vector3.zero;
  
    private Vector3 viewBobOffset = Vector3.zero;
    public Vector3 ViewBobOffset { get { return new Vector3(viewBobOffset.y * 1.1f, viewBobOffset.x * 1.35f, viewBobOffset.z * 0.3f); } }

    [Header("Footstep Settings")]
    [SerializeField] private ShakeData shakeData; 
    private float footstepDistance = 0f;

    private float stepUpSmoothTime = 0.05f;
    private Vector3 vaultVel = Vector3.zero;
    private Vector3 vaultDesync = Vector3.zero;

    [Header("Assignables")]
    [SerializeField] private PlayerManager s;
    [SerializeField] private Transform playerGraphics;

    void Awake()
    {
        s.PlayerMovement.OnStopMoving += LastBobStep;
        s.PlayerMovement.Collision.OnColliderLand += BobOnce;
    }

    void LateUpdate()
	{
        float speedAmp = 1 / Mathf.Clamp(s.PlayerMovement.Magnitude * 0.06f, 1f, 5f);
        viewBobOffset = Vector3.SmoothDamp(viewBobOffset, HeadBob(), ref bobVel, viewBobSmoothTime * speedAmp * (BobTimer <= 0 ? 5f : 1f));
       
        CalculateLandOffset();
        SmoothPlayerBackToCollider();

        Vector3 newPos = (Vector3.up * landBobOffset) + viewBobOffset * 0.6f + s.PlayerMovement.CrouchOffset + vaultDesync;
		transform.localPosition = newPos;

        BobTimer = (s.PlayerMovement.Collision.Grounded || s.PlayerMovement.WallRunning) && s.PlayerMovement.CanCrouchWalk && s.PlayerMovement.Collision.StepsSinceLastJumped > 4 && s.PlayerMovement.Magnitude > 0.5f && s.PlayerMovement.Moving ? BobTimer + Time.deltaTime : 0f;
    }

    void FixedUpdate()
    {
        CalculateFootsteps();
    }

    private void LastBobStep(float mag)
    {
        if (!s.PlayerMovement.Collision.Grounded || !s.PlayerMovement.CanCrouchWalk) return;

        s.CameraShaker.ShakeOnce(new PerlinShake(ShakeData.Create(Mathf.Clamp(mag * 0.3f, 0.2f, 2.5f), 3f, 1.5f, 5f)));
    }

    private void CalculateLandOffset()
	{
        if (desiredOffset == 0f && landBobOffset == 0f) return;

		if (desiredOffset <= 0f) desiredOffset = Mathf.Lerp(desiredOffset, 0f, 7.6f * Time.deltaTime);
        if (landBobOffset <= 0f) landBobOffset = Mathf.SmoothDamp(landBobOffset, desiredOffset, ref landVel, landBobSmoothTime);

        if (desiredOffset >= -0.001f && landBobOffset > -0.001f)
        {
            desiredOffset = 0f;
            landBobOffset = 0f;
        }
	}

    private void CalculateFootsteps()
    {
        if (BobTimer <= 0 || s.WeaponControls.Aiming)
        {
            footstepDistance = 0f;
            return;
        }

        float walkMagnitude = s.PlayerMovement.Magnitude;
        walkMagnitude = Mathf.Clamp(walkMagnitude, 0f, 20f);

        footstepDistance += walkMagnitude * 0.02f * 50f;

        if (footstepDistance > 450f)
        {
            s.CameraShaker.ShakeOnce(new KickbackShake(shakeData));
            footstepDistance = 0f;
        }    
    }

    private Vector3 HeadBob()
    {
        if (BobTimer <= 0) return Vector3.zero;

        float amp = s.PlayerMovement.Magnitude * 0.057f * (s.PlayerMovement.WallRunning ? 2f : 1f);
        amp = Mathf.Clamp(amp, 0.8f, 1.4f) * Mathf.Max(1f + landBobOffset * 1f, 0f);

        float scroller = BobTimer * viewBobSpeed;
        return (viewBobAmountHoriz * Mathf.Cos(scroller) * s.orientation.right + viewBobAmountVert * Math.Abs(Mathf.Sin(scroller)) * Vector3.up) * amp;
    }

    public void BobOnce(float impactForce)
    {
        if (impactForce < -30f)
        {
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = ObjectPooler.Instance.Spawn("LandFX", s.transform.position, Quaternion.Euler(0, 0, 0)).GetComponent<ParticleSystem>().velocityOverLifetime;

            Vector3 magnitude = s.rb.velocity;

            velocityOverLifetime.x = magnitude.x * 2f;
            velocityOverLifetime.z = magnitude.z * 2f;
        }

        bool crouched = s.PlayerMovement.Crouched;
        float newMag = -impactForce * (crouched ? 0.8f : 0.33f);
        float newSmooth = Mathf.Clamp(newMag * 2f, 3f, 15f) * 0.7f;

        landbobShakeData.Magnitude = newMag;
        landbobShakeData.SmoothSpeed = newSmooth;

        landbobShakeData.Frequency = 0f;
        landbobShakeData.Duration = 0.7f;

        s.CameraShaker.ShakeOnce(new KickbackShake(landbobShakeData, Vector3.right));
        s.CameraShaker.ShakeOnce(new PerlinShake(ShakeData.Create(newMag * 0.3f, 5f, 0.7f, 6f)));

        impactForce = Mathf.Round(impactForce * 100f) * 0.01f;
        impactForce = Mathf.Clamp(impactForce * landBobMultiplier, -maxOffset, 0f);

        if (impactForce > -0.5f) return;
        if (crouched) impactForce = Mathf.Clamp(impactForce * 0.5f, -slideMaxOffset, 0f);

        desiredOffset += impactForce;
    }

    public void PlayerDesyncFromCollider(Vector3 offset, float smoothTime)
    {
        stepUpSmoothTime = smoothTime;
        vaultDesync += offset;

        playerGraphics.SetParent(null);
    }
    
    private void SmoothPlayerBackToCollider()
    {
        if (vaultDesync == Vector3.zero) return;

        vaultDesync = Vector3.SmoothDamp(vaultDesync, Vector3.zero, ref vaultVel, stepUpSmoothTime);
        playerGraphics.SetPositionAndRotation(s.orientation.position + vaultDesync, s.orientation.rotation);

        if (vaultDesync.sqrMagnitude <= 0.001f)
        {
            vaultDesync = Vector3.zero;
            playerGraphics.SetParent(s.orientation);
            playerGraphics.localPosition = Vector3.zero;
            playerGraphics.localRotation = Quaternion.Euler(Vector3.zero);
        }
    }

    /* Old Vault code
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
    */
}
