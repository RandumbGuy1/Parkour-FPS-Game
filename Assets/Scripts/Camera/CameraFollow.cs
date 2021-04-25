﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraFollow : MonoBehaviour
{
	[Header("Camera Tilt Variables")]
	public float maxWallRunCameraTilt;
	public float maxSlideCameraTilt;
	public float tiltSmoothTime;

	private float tiltVel = 0f;
	public float CameraTilt { get; private set; }

	[Header("Fov")]
	public float fov;
	public float wallFovTime;
	public float returnFovTime;

	private float fovVel = 0f;
	private float maxFov, setFov;

	[Header("Sensitivity")]
	public float playerTurnSpeed;
	public float sensitivity;
	public Vector2 cameraTurnSpeed;

	float multiplier = 0.01f;

	[Header("Clamp Rotation")]
	public float upClampAngle;
	public float downClampAngle;

	private float mouseX;
	private float mouseY;

	private float yRotation;
	private float xRotation;

	private float ySmoothRotation;
	private float xSmoothRotation;

	public Vector3 rotationDelta { get; private set; }
	Vector3 rotationLast;

	public float camVel { get; private set; }

	[Header("Assignables")]
	public Transform player;
	public Transform camera;
	public Transform playerHead;
	public CameraShaker Shake;

	private Camera cam;

	void Start()
    {
		CameraTilt = 0f;
		setFov = fov;
		maxFov = setFov + 20f;
		cam = GetComponentInChildren<Camera>();
	}

	void Update()
	{
		mouseX = Input.GetAxisRaw("Mouse X");
		mouseY = Input.GetAxisRaw("Mouse Y");

		if (Input.GetMouseButtonDown(1)) Shake.ShakeOnce(4f, 8f, 0.8f);
	}

	void LateUpdate()
	{
		CalcRotation();
		SmoothRotation();
		ApplyRotation();

		CalcDelta();

		cam.fieldOfView = fov;
	}

	void CalcRotation()
    {
		yRotation += mouseX * sensitivity * multiplier;
		xRotation -= mouseY * sensitivity * multiplier;

		xRotation = Mathf.Clamp(xRotation, -upClampAngle, downClampAngle);
	}

	void SmoothRotation()
    {
		ySmoothRotation = Mathf.Lerp(ySmoothRotation, yRotation, cameraTurnSpeed.y * Time.smoothDeltaTime);
		xSmoothRotation = Mathf.Lerp(xSmoothRotation, xRotation, cameraTurnSpeed.x * Time.smoothDeltaTime);
	}

	void ApplyRotation()
    {
		camera.localRotation = Quaternion.Euler(xSmoothRotation + Shake.offset.x, ySmoothRotation + Shake.offset.y, CameraTilt + Shake.offset.z);
		player.transform.rotation = Quaternion.Euler(0, ySmoothRotation, 0);
	}

	void CalcDelta()
	{
		rotationDelta = camera.rotation.eulerAngles - rotationLast;
		rotationLast = camera.rotation.eulerAngles;

		camVel = (Math.Abs(rotationDelta.x) + Math.Abs(rotationDelta.y) + Math.Abs(rotationDelta.z)) * 35f;
	}

	public void CameraWallRun(int i)
	{
		CameraTilt = Mathf.SmoothDamp(CameraTilt, maxWallRunCameraTilt * i, ref tiltVel, tiltSmoothTime - 0.1f);
		fov = Mathf.SmoothDamp(fov, maxFov, ref fovVel, wallFovTime);
	}

	public void CameraSlide()
	{
		CameraTilt = Mathf.SmoothDamp(CameraTilt, maxSlideCameraTilt, ref tiltVel, tiltSmoothTime);
	}

	public void ResetCameraTilt()
	{
		CameraTilt = Mathf.SmoothDamp(CameraTilt, 0, ref tiltVel, tiltSmoothTime + 0.05f);
		fov = Mathf.SmoothDamp(fov, setFov, ref fovVel, returnFovTime);

		if (Math.Abs(CameraTilt) < 0.1f) CameraTilt = 0f;
		if (fov <= 80.1f) fov = 80f;
	}
}
