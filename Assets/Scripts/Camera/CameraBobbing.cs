using System.Collections;
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
    public Vector3 ViewBobOffset { get { return new Vector3(viewBobOffset.y, viewBobOffset.x * 0.8f, viewBobOffset.z); } }

    [Header("Footstep Settings")]
    [SerializeField] private ShakeData shakeData; 
    [SerializeField] private float stepUpSmoothTime;
    private float footstepDistance = 0f;

    [Header("Assignables")]
    [SerializeField] private ScriptManager s;

    void LateUpdate()
	{
        BobTimer = (s.PlayerMovement.Grounded || s.PlayerMovement.WallRunning) && s.PlayerMovement.CanCrouchWalk && s.PlayerMovement.Magnitude > 0.5f ? BobTimer + Time.deltaTime : 0f;

        float speedAmp = 1 / Mathf.Clamp(s.PlayerMovement.Magnitude * 0.08f, 1f, Mathf.Infinity);
        viewBobOffset = Vector3.SmoothDamp(viewBobOffset, HeadBob(), ref bobVel, viewBobSmoothTime * speedAmp * (BobTimer <= 0 ? 3f : 1f));
        CalculateLandOffset(); 

        Vector3 newPos = (Vector3.up * landBobOffset) + viewBobOffset * 0.6f + s.PlayerMovement.CrouchOffset;
		transform.localPosition = newPos;
	}

    void FixedUpdate()
    {
        CalculateFootsteps();
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
            s.CameraShaker.ShakeOnce(shakeData);
            footstepDistance = 0f;
        }    
    }

    private Vector3 HeadBob()
    {
        if (BobTimer <= 0) return Vector3.zero;

        float amp = s.PlayerMovement.Magnitude * 0.055f * (s.PlayerMovement.WallRunning ? 2f : 1f);
        amp = Mathf.Clamp(amp, 0.85f, 1.35f) * Mathf.Max(1f + landBobOffset * 1f, 0f);

        float scroller = BobTimer * viewBobSpeed;
        return (viewBobAmountHoriz * Mathf.Cos(scroller) * Vector3.right + viewBobAmountVert * Math.Abs(Mathf.Sin(scroller)) * Vector3.up) * amp;
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
        float newMag = -impactForce * (crouched ? 0.5f : 0.3f);
        float newSmooth = Mathf.Clamp(newMag * 0.7f, 0.1f, 14f);

        landbobShakeData.Intialize(newMag, landbobShakeData.Frequency, landbobShakeData.Duration, newSmooth, landbobShakeData.Type);
        s.CameraShaker.ShakeOnce(landbobShakeData, new Vector3(1f, UnityEngine.Random.Range(-0.05f, 0.05f), UnityEngine.Random.Range(-0.05f, 0.05f)));

        impactForce = Mathf.Round(impactForce * 100f) * 0.01f;
        impactForce = Mathf.Clamp(impactForce * landBobMultiplier, -maxOffset, 0f);

        if (impactForce > -0.5f) return;
        if (crouched) impactForce = Mathf.Clamp(impactForce * 0.5f, -slideMaxOffset, 0f);

        desiredOffset += impactForce;
    }
}
