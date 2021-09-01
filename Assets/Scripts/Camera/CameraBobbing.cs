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

	private float landVel = 0f;
	private float desiredOffset = 0f;
    private float bobOffset = 0f;

    [Header("View Bob Settings")]
    [SerializeField] private float bobSpeed;
    [SerializeField] private float bobAmountHoriz;
    [SerializeField] private float bobAmountVert;
    [Range(0f, 0.5f)] [SerializeField] private float bobSmoothTime;

    private float timer;
    private Vector3 bobVel = Vector3.zero;
    private Vector3 smoothOffset = Vector3.zero;

    [Header("Step Settings")]
    [SerializeField] private float stepSmoothTime;

    public Vector3 vaultDesync { get; private set; } = Vector3.zero;
    private Vector3 vaultVel = Vector3.zero;

    [Header("Assignables")]
    [SerializeField] private ScriptManager s;

    void LateUpdate()
	{
        timer = (s.PlayerMovement.grounded && s.PlayerMovement.canCrouchWalk || s.PlayerMovement.wallRunning) && s.PlayerMovement.moving && s.PlayerMovement.magnitude > 0.5f ? timer + Time.deltaTime : 0f;

        smoothOffset = Vector3.SmoothDamp(smoothOffset, HeadBob(), ref bobVel, bobSmoothTime);

        SmoothStepUp();

        Vector3 newPos = CalculateLandOffset() + smoothOffset + vaultDesync;
		transform.localPosition = newPos;
	}

	private Vector3 CalculateLandOffset()
	{
		if (desiredOffset >= 0f) return Vector3.zero;

		desiredOffset = Mathf.Lerp(desiredOffset, 0f, 8f * Time.deltaTime);
		bobOffset = Mathf.SmoothDamp(bobOffset, desiredOffset, ref landVel, landBobSmoothTime);

		if (desiredOffset >= -0.01f) desiredOffset = 0f;

		return Vector3.up * bobOffset;
	}

	public void CameraLand(float mag)
	{
		float magnitude = (mag * landBobMultiplier);
		magnitude = Mathf.Round(magnitude * 100f) * 0.01f;
		magnitude = Mathf.Clamp(magnitude, 0f, maxOffset);

		if (magnitude < 0.5f) magnitude = 0f;
		if (s.PlayerInput.crouching) magnitude *= 0.83f;

		desiredOffset -= magnitude;
	}

    private Vector3 HeadBob()
    {
        float amp = s.PlayerMovement.magnitude * 0.068f * (s.PlayerMovement.wallRunning ? 1.3f : 1f);
        amp = Mathf.Clamp(amp, 1f, 2.1f);

        return (timer <= 0 ? Vector3.zero : s.orientation.right * Mathf.Cos(timer * bobSpeed) * bobAmountHoriz + Vector3.up * Math.Abs(Mathf.Sin(timer * bobSpeed)) * bobAmountVert * amp);
    }

    private void SmoothStepUp()
    {
        if (vaultDesync == Vector3.zero) return;

        vaultDesync = Vector3.SmoothDamp(vaultDesync, Vector3.zero, ref vaultVel, stepSmoothTime);

        if (vaultDesync.sqrMagnitude < 0.001f) vaultDesync = Vector3.zero;
    }

    public void StepUp(Vector3 offset) => vaultDesync = Vector3.zero + offset;
}
