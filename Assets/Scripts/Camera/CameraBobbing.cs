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
    private float bobOffset = 0f;

    [Header("View Bob Settings")]
    [SerializeField] private float bobSpeed;
    [SerializeField] private float bobAmountHoriz;
    [SerializeField] private float bobAmountVert;
    [Range(0f, 0.5f)] [SerializeField] private float bobSmoothTime;

    private Vector3 bobVel = Vector3.zero;
    private float timer;
    private Vector3 smoothOffset = Vector3.zero;
    public Vector3 SmoothOffset 
    { get { return new Vector3(smoothOffset.y, smoothOffset.x * 0.8f, smoothOffset.z); } }

    [Header("Footstep Settings")]
    [SerializeField] private ShakeData shakeData; 
    [Space(10)]
    [SerializeField] private float stepUpSmoothTime;
    private float footstepDistance = 0f;

    private Vector3 vaultDesync;
    private Vector3 vaultVel = Vector3.zero;

    [Header("Assignables")]
    [SerializeField] private ScriptManager s;

    void LateUpdate()
	{
        timer = (s.PlayerMovement.Grounded && s.PlayerMovement.CanCrouchWalk && s.PlayerMovement.Moving || s.PlayerMovement.WallRunning) && s.PlayerMovement.Magnitude > 0.5f ? timer + Time.deltaTime : 0f;

        smoothOffset = Vector3.SmoothDamp(smoothOffset, HeadBob(), ref bobVel, bobSmoothTime);
        CalculateLandOffset();

        Vector3 newPos = (Vector3.up * bobOffset) + smoothOffset + (s.PlayerMovement.CrouchOffset * 1.8f);
		transform.localPosition = newPos;
	}

    void FixedUpdate()
    {
        CalculateFootsteps();
    }

    private void CalculateLandOffset()
	{
        if (desiredOffset == 0f && bobOffset == 0f) return;

		if (desiredOffset <= 0f) desiredOffset = Mathf.Lerp(desiredOffset, 0f, 7.6f * Time.deltaTime);
        if (bobOffset <= 0f) bobOffset = Mathf.SmoothDamp(bobOffset, desiredOffset, ref landVel, landBobSmoothTime);

        if (desiredOffset >= -0.001f && bobOffset > -0.001f)
        {
            desiredOffset = 0f;
            bobOffset = 0f;
        }
	}

	public void BobOnce(float impactForce)
	{
        if (impactForce < -30f)
        {
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = ObjectPooler.Instance.SpawnParticle("LandFX", s.transform.position, Quaternion.Euler(0, 0, 0)).velocityOverLifetime;

            Vector3 magnitude = s.rb.velocity;

            velocityOverLifetime.x = magnitude.x * 1.3f;
            velocityOverLifetime.z = magnitude.z * 1.3f;
        }

        bool crouched = s.PlayerInput.Crouching;
        float newMag = -impactForce * (crouched ? 0.6f : 0.3f);
        float newSmooth = Mathf.Clamp(newMag * 0.7f, 0.1f, 13.5f);

        landbobShakeData.Intialize(newMag, landbobShakeData.Frequency, landbobShakeData.Duration, newSmooth, landbobShakeData.Type);

        float randomYZ = UnityEngine.Random.Range(-0.2f, 0.2f);
        s.CameraShaker.ShakeOnce(landbobShakeData, new Vector3(1f, randomYZ, randomYZ));

        impactForce = Mathf.Round(impactForce * 100f) * 0.01f;
        impactForce = Mathf.Clamp(impactForce * landBobMultiplier, -maxOffset, 0f);

        if (impactForce > -0.5f) return;
        if (crouched) impactForce = Mathf.Clamp(impactForce * 0.5f, -slideMaxOffset, 0f);

        desiredOffset += impactForce;
	}

    private void CalculateFootsteps()
    {
        if (timer <= 0)
        {
            footstepDistance = 0f;
            return;
        }

        float walkMagnitude = s.PlayerMovement.Magnitude;
        walkMagnitude = Mathf.Clamp(walkMagnitude, 0f, 20f);

        footstepDistance += walkMagnitude * 0.02f * 50f;

        if (footstepDistance > 450f)
        {
            s.CameraShaker.ShakeOnce(shakeData);
            footstepDistance = 0f;
        }    
    }

    private Vector3 HeadBob()
    {
        float speedAmp = s.PlayerMovement.Magnitude * 0.065f;
        speedAmp = Mathf.Clamp(speedAmp, 0.8f, 1.1f);

        float scroller = timer * bobSpeed * speedAmp;

        float magAmp = s.PlayerMovement.Magnitude * 0.07f * (s.PlayerMovement.WallRunning ? 1.5f : 1f) * (s.WeaponControls.Aiming || bobOffset <= -0.05f ? 0.5f : 1f);
        magAmp = Mathf.Clamp(magAmp, 0.8f, 1.3f);

        return (timer <= 0 ? Vector3.zero : (bobAmountHoriz * Mathf.Cos(scroller)) * Vector3.right + (bobAmountVert * Math.Abs(Mathf.Sin(scroller))) * Vector3.up) * magAmp;
    }
    /*
    private void SmoothStepUp()
    {
        if (vaultDesync == Vector3.zero) return;

        vaultDesync = Vector3.SmoothDamp(vaultDesync, Vector3.zero, ref vaultVel, stepUpSmoothTime);

        if (vaultDesync.sqrMagnitude < 0.001f) vaultDesync = Vector3.zero;
    }

    public void StepUp(Vector3 offset) => vaultDesync = Vector3.zero + offset;
    */
}
