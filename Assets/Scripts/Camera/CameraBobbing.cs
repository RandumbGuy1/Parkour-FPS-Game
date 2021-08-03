﻿using System.Collections;
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

    [HideInInspector]
    public Vector3 vaultDesync = Vector3.zero;
    private Vector3 vaultVel = Vector3.zero;

    [Header("Assignables")]
    [SerializeField] private ScriptManager s;

    void LateUpdate()
	{
        timer = s.PlayerMovement.moving && s.PlayerInput.grounded && s.PlayerMovement.canCrouchWalk && s.PlayerMovement.magnitude > 0.5f ? timer + Time.deltaTime : 0f;

        smoothOffset = Vector3.SmoothDamp(smoothOffset, HeadBob(), ref bobVel, bobSmoothTime);

        if (vaultDesync.sqrMagnitude > 0.01f) vaultDesync = Vector3.SmoothDamp(vaultDesync, Vector3.zero, ref vaultVel, stepSmoothTime);
        else if (vaultDesync != Vector3.zero) vaultDesync = Vector3.zero;

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
		if (s.PlayerInput.crouching) magnitude = 0.9f;

		desiredOffset -= magnitude;
	}

    private Vector3 HeadBob()
    {
        if (timer <= 0) return Vector3.zero;

        return s.orientation.right * Mathf.Cos(timer * bobSpeed) * bobAmountHoriz + Vector3.up * Math.Abs(Mathf.Sin(timer * bobSpeed)) * bobAmountVert;
    }

    public void StepUp(Vector3 offset) => vaultDesync = Vector3.zero + offset;
}