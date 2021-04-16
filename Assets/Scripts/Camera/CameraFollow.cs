using System.Collections;
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

	void Start()
    {
		//Save 3.1
		CameraTilt = 0f;
	}

	void Update()
	{
		mouseX = Input.GetAxisRaw("Mouse X");
		mouseY = Input.GetAxisRaw("Mouse Y");

		if (Input.GetMouseButtonDown(1)) Shake.ShakeOnce(6f, 10f, 1.5f);
	}

	void LateUpdate()
	{
		CalcRotation();
		SmoothRotation();
		ApplyRotation();

		CalcDelta();
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
		CameraTilt = Mathf.SmoothDamp(CameraTilt, maxWallRunCameraTilt * i, ref tiltVel, tiltSmoothTime);
	}

	public void CameraSlide()
	{
		CameraTilt = Mathf.SmoothDamp(CameraTilt, maxSlideCameraTilt, ref tiltVel, tiltSmoothTime);
	}

	public void ResetCameraTilt()
	{
		CameraTilt = Mathf.SmoothDamp(CameraTilt, 0, ref tiltVel, tiltSmoothTime);
		if (Math.Abs(CameraTilt) < 0.1f) CameraTilt = 0f;
	}
}
