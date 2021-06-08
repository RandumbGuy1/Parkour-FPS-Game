using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraEffects : MonoBehaviour
{
	[Header("Land Bob Settings")]
	[SerializeField] private float offsetSmoothTime;
	[SerializeField] private float backSmoothTime;
	[SerializeField] private float offsetMultiplier;
	[SerializeField] private float maxOffset;

	[Header("Assignables")]
	[SerializeField] private ScriptManager s;

	private float vel = 0;
	private float magnitude;

	public bool landed { get; private set; }

	void LateUpdate()
	{
		Vector3 newPos = CalculateLandOffset();
		transform.localPosition = newPos;
	}

	private Vector3 CalculateLandOffset()
    {
		Vector3 offsetPos = Vector3.zero;
	
		if (landed) offsetPos.y = Mathf.SmoothDamp(transform.localPosition.y, -magnitude, ref vel, offsetSmoothTime);
		else if (transform.localPosition.y < -0.01f) offsetPos.y = Mathf.SmoothDamp(transform.localPosition.y, 0.01f, ref vel, backSmoothTime);

		if (transform.localPosition.y <= -magnitude + 0.1f && landed) Invoke("StopCameraLand", 0.05f);

		return offsetPos;
	}

	public void CameraLand(float mag)
	{
		if (!landed)
		{
			magnitude = (mag * offsetMultiplier);
			magnitude = Mathf.Round(magnitude * 10.0f) * 0.1f;
			magnitude = Mathf.Clamp(magnitude, 0f, maxOffset);

			if (magnitude < 0.5f) magnitude = 0f;
			if (s.PlayerInput.crouching) magnitude = 0.9f;

			landed = true;
		}
	}

	private void StopCameraLand()
	{
		if (landed)
		{
			magnitude = 0f;
			landed = false;
		}
	}
}
