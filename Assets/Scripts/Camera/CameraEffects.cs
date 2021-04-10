using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraEffects : MonoBehaviour
{
	[Header("Offset Variables")]
	public float offsetSmoothTime;
	public float backSmoothTime;
	public float maxSpeed;
	private float setSmoothTime;

	private float vel = 0;
	private float magnitude;
	private float duration;

	private float maxFov, currentFov;
	Vector3 offsetPos = Vector3.zero;

	[Header("Fov")]
	public float fov;
	public float wallFovSpeed;
	public float returnFovSpeed;

	[Header("Assignables")]
	public Transform playerHead;
	public Transform orientation;
	public Camera cam { get; private set; }

	public bool landed { get; private set; }

	void Start()
	{
		//Save 4.6
		currentFov = fov;
		maxFov = currentFov + 20f;
		landed = false;
		setSmoothTime = offsetSmoothTime;
		cam = GetComponent<Camera>();
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

		cam.fieldOfView = fov;
	}

	public void CameraLand(float mag)
	{
		if (!landed)
		{
			magnitude = (mag * 0.05f) - 0.5f;
			magnitude = Mathf.Round(magnitude * 10.0f) * 0.1f;
			magnitude = Mathf.Clamp(magnitude, 0f, 2.5f);

			if (magnitude < 0.5f) magnitude = 0f;
			if (Input.GetKey(KeyCode.LeftControl)) magnitude *= 0.6f;

			offsetSmoothTime = setSmoothTime / (magnitude + 3f);
			offsetSmoothTime = Mathf.Round(offsetSmoothTime * 100.0f) * 0.01f;
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

	public void CameraWallRun()
	{
		fov = Mathf.Lerp(fov, maxFov, wallFovSpeed * Time.smoothDeltaTime);
	}

	public void ResetCameraWallRun()
	{
		fov = Mathf.Lerp(fov, currentFov, returnFovSpeed * Time.smoothDeltaTime);
		if (fov <= 81f) fov = 80f;
	}
}
