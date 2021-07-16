using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraEffects : MonoBehaviour
{
	[Header("Land Bob Settings")]
	[SerializeField] private float bobSmoothTime;
	[SerializeField] private float bobMultiplier;
	[SerializeField] private float maxOffset;

	[Header("Assignables")]
	[SerializeField] private ScriptManager s;

	private float vel = 0f;
	private float desiredOffset = 0f;

	void LateUpdate()
	{
		Vector3 newPos = CalculateLandOffset();
		transform.localPosition = newPos;
	}

	private Vector3 CalculateLandOffset()
    {
		Vector3 offset = Vector3.zero;

		if (desiredOffset >= 0f) return offset;
		
		desiredOffset = Mathf.Lerp(desiredOffset, 0f, 7f * Time.deltaTime);
		offset.y = Mathf.SmoothDamp(transform.localPosition.y, desiredOffset, ref vel, bobSmoothTime);

		if (desiredOffset >= -0.01f) desiredOffset = 0f;

		return offset;
	}

	public void CameraLand(float mag)
	{
		float magnitude = (mag * bobMultiplier);
		magnitude = Mathf.Round(magnitude * 100f) * 0.01f;
		magnitude = Mathf.Clamp(magnitude, 0f, maxOffset);

		if (magnitude < 0.5f) magnitude = 0f;
		if (s.PlayerInput.crouching) magnitude = 0.9f;

		desiredOffset = -magnitude;
	}
}
