using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraEffects : MonoBehaviour
{
	[Header("Offset Settings")]
	[SerializeField] private float offsetSmoothTime;
	[SerializeField] private float backSmoothTime;

	[Header("Offset Calculation Settings")]
	[SerializeField] private float offsetMultiplier;
	[SerializeField] private float maxOffset;

	private float maxSpeed;
	private float vel = 0;
	private float magnitude;
	private float setSmoothTime;

	public bool landed { get; private set; }

	void Start()
	{
		landed = false;
		maxSpeed = Mathf.Infinity;
		setSmoothTime = offsetSmoothTime;
	}

	void LateUpdate()
	{
		transform.localPosition = CalculateLandOffset();
	}

	private Vector3 CalculateLandOffset()
    {
		Vector3 offsetPos = Vector3.zero;
		float threshold = -magnitude + 0.1f;

		if (landed)
		{
			offsetPos.y = Mathf.SmoothDamp(transform.localPosition.y, -magnitude, ref vel, offsetSmoothTime, maxSpeed, Time.smoothDeltaTime);
			if (transform.localPosition.y <= threshold) Invoke("StopCameraLand", 0.05f);
		}
		else if (transform.localPosition.y < -0.01f) 
			offsetPos.y = Mathf.SmoothDamp(transform.localPosition.y, 0.01f, ref vel, backSmoothTime, maxSpeed, Time.smoothDeltaTime);

		return offsetPos;
	}

	public void CameraLand(float mag)
	{
		if (!landed)
		{
			magnitude = (mag * offsetMultiplier);
			magnitude = Mathf.Round(magnitude * 10.0f) * 0.1f;
			magnitude = Mathf.Clamp(magnitude, 0f, maxOffset);

			if (magnitude < 0.6f) magnitude = 0f;
			if (Input.GetKey(KeyCode.LeftControl)) magnitude = 1f;
			
			offsetSmoothTime = setSmoothTime / (magnitude + 0.3f);
			offsetSmoothTime = Mathf.Round(offsetSmoothTime * 1000.0f) * 0.001f;
			offsetSmoothTime = Mathf.Clamp(offsetSmoothTime, 0.04f, 0.06f);
			
			landed = true;
		}
	}

	private void StopCameraLand()
	{
		if (landed)
		{
			offsetSmoothTime = setSmoothTime;
			magnitude = 0f;
			landed = false;
		}
	}
}
