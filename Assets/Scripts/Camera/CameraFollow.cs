using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraFollow : MonoBehaviour
{
	[Header("Camera Tilt Variables")]
	public float maxWallRunCameraTilt;
	public float maxSlideCameraTilt;
	public float tiltTime;
	public float returnTiltTime;

	private float cameraTilt;

	float tiltVel = 0f;
	float fovVel = 0f;

	[Header("Fov")]
	public float fov;
	public float fovTime;
	public float returnFovTime;

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

	Vector3 rotationLast;

	public Vector3 rotationDelta { get; private set; }
	public float camVel { get; private set; }

	[Header("Assignables")]
	public Transform player;
	public CameraShaker Shake;
	private Camera cam;

	void Start()
    {
		cameraTilt = 0f;
		setFov = fov;
		maxFov = setFov + 20f;
		cam = GetComponentInChildren<Camera>();
	}

	void Update()
	{
		mouseX = Input.GetAxisRaw("Mouse X");
		mouseY = Input.GetAxisRaw("Mouse Y");

		if (Input.GetMouseButtonDown(1)) Shake.ShakeOnce(10f, 8f, 3f);

		CalcDelta();
	}

	void LateUpdate()
	{
		CalcRotation();
		SmoothRotation();
		ApplyRotation();

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
		cam.transform.localRotation = Quaternion.Euler(xSmoothRotation + Shake.offset.x, ySmoothRotation + Shake.offset.y, cameraTilt + Shake.offset.z);
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
		cameraTilt = Mathf.SmoothDamp(cameraTilt, maxWallRunCameraTilt * i, ref tiltVel, tiltTime + 0.05f);
		fov = Mathf.SmoothDamp(fov, maxFov, ref fovVel, fovTime);
	}

	public void CameraSlide()
	{
		cameraTilt = Mathf.SmoothDamp(cameraTilt, maxSlideCameraTilt, ref tiltVel, tiltTime + 0.1f);
	}

	public void ResetCameraTilt()
	{
		if (cameraTilt == 0 && fov == setFov) return;

		cameraTilt = Mathf.SmoothDamp(cameraTilt, 0, ref tiltVel, returnTiltTime);
		fov = Mathf.SmoothDamp(fov, setFov, ref fovVel, returnFovTime);
		
		if (Math.Abs(cameraTilt) < 0.1f) cameraTilt = 0f;
		if (fov < setFov + 0.02f)  fov = setFov;
	}
}
