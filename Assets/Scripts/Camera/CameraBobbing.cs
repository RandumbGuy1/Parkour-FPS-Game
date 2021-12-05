using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraBobbing : MonoBehaviour
{
	[Header("Land Bob Settings")]
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

        SmoothStepUp();
        CalculateLandOffset();

        Vector3 newPos = (Vector3.up * bobOffset) + smoothOffset + vaultDesync;
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

	public void BobOnce(float mag)
	{
        mag = Mathf.Round(mag * 100f) * 0.01f;
        mag = Mathf.Clamp(mag * landBobMultiplier, -maxOffset, 0f);

        if (mag > -0.5f) return;
        if (s.PlayerInput.Crouching) mag = Mathf.Clamp(mag * 0.83f, -slideMaxOffset, 0f);

        desiredOffset += mag;
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
       
        float amp = s.PlayerMovement.Magnitude * 0.068f * (s.PlayerMovement.WallRunning ? 1.3f : 1f);
        amp = Mathf.Clamp(amp, 1f, 1.4f);

        return (timer <= 0 ? Vector3.zero : s.orientation.right * Mathf.Cos(timer * bobSpeed * speedAmp) * bobAmountHoriz + Vector3.up * Math.Abs(Mathf.Sin(timer * bobSpeed * speedAmp)) * bobAmountVert * amp);
    }

    private void SmoothStepUp()
    {
        if (vaultDesync == Vector3.zero) return;

        vaultDesync = Vector3.SmoothDamp(vaultDesync, Vector3.zero, ref vaultVel, stepUpSmoothTime);

        if (vaultDesync.sqrMagnitude < 0.001f) vaultDesync = Vector3.zero;
    }

    public void StepUp(Vector3 offset) => vaultDesync = Vector3.zero + offset;
}
