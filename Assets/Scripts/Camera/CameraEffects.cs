using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraEffects : MonoBehaviour
{
	[Header("Offset Variables")]
	public float offsetSmoothTime;
	public float backSmoothTime;

	private float maxSpeed;
	private float vel = 0;
	private float magnitude;
	private float duration;
	private float setSmoothTime;

	Vector3 offsetPos = Vector3.zero;
	public bool landed { get; private set; }

	void Start()
	{
		landed = false;
		maxSpeed = Mathf.Infinity;
		setSmoothTime = offsetSmoothTime;
	}

	void Update()
	{
		#region Camera Land Bob
		if (landed)
		{
			offsetPos.y = Mathf.SmoothDamp(transform.localPosition.y, -magnitude, ref vel, offsetSmoothTime, maxSpeed, Time.smoothDeltaTime);
			transform.localPosition = offsetPos;

			if (transform.localPosition.y <= -magnitude + 0.1f) Invoke("StopCameraLand", 0.05f);
		}

		if (!landed && transform.localPosition.y < -0.01f)
			transform.localPosition = new Vector3(0, Mathf.SmoothDamp(transform.localPosition.y, 0, ref vel, backSmoothTime, maxSpeed, Time.smoothDeltaTime), 0);
		#endregion
	}

	public void CameraLand(float mag)
	{
		if (!landed)
		{
			magnitude = (mag * 0.04f);
			magnitude = Mathf.Round(magnitude * 10.0f) * 0.1f;
			magnitude = Mathf.Clamp(magnitude, 0f, 2.5f);

			if (magnitude < 0.5f) magnitude = 0f;
			if (Input.GetKey(KeyCode.LeftControl)) magnitude = 1f;
			
			offsetSmoothTime = setSmoothTime / magnitude;
			offsetSmoothTime = Mathf.Round(offsetSmoothTime * 1000.0f) * 0.001f;
			offsetSmoothTime = Mathf.Clamp(offsetSmoothTime, 0.05f, 0.06f);
	
			landed = true;
		}
	}

	void StopCameraLand()
	{
		if (landed)
		{
			offsetSmoothTime = setSmoothTime;
			magnitude = 0f;
			landed = false;
		}
	}
}
