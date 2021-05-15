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
	public float returnTiltSmoothTime;

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

	Vector3 rotationDelta;
	Vector3 rotationLast;

	public float camVel { get; private set; }

	[Header("Assignables")]
	public Transform player;
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

		if (Input.GetMouseButtonDown(1)) Shake.ShakeOnce(10f, 8f, 3f);
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
		cam.transform.localRotation = Quaternion.Euler(xSmoothRotation + Shake.offset.x, ySmoothRotation + Shake.offset.y, CameraTilt + Shake.offset.z);
		player.transform.rotation = Quaternion.Euler(0, ySmoothRotation, 0);
	}

	void CalcDelta()
	{
		rotationDelta = cam.transform.rotation.eulerAngles - rotationLast;
		rotationLast = cam.transform.rotation.eulerAngles;

		camVel = rotationDelta.sqrMagnitude;
	}

	public void CameraWallRun(int i)
	{
		CameraTilt = Mathf.SmoothDamp(CameraTilt, maxWallRunCameraTilt * i, ref tiltVel, tiltSmoothTime);
		fov = Mathf.SmoothDamp(fov, maxFov, ref fovVel, wallFovTime);
	}

	public void CameraSlide()
	{
		CameraTilt = Mathf.SmoothDamp(CameraTilt, maxSlideCameraTilt, ref tiltVel, tiltSmoothTime);
	}

	public void ResetCameraTilt()
	{
		CameraTilt = Mathf.SmoothDamp(CameraTilt, 0, ref tiltVel, returnTiltSmoothTime);
		fov = Mathf.SmoothDamp(fov, setFov, ref fovVel, returnFovTime);

		if (Math.Abs(CameraTilt) < 0.1f) CameraTilt = 0f;
		if (fov <= 80.1f) fov = 80f;
	}
}
