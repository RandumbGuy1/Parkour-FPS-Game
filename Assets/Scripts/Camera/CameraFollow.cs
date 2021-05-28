using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraFollow : MonoBehaviour
{
	[Header("Camera Tilt Variables")]
	[SerializeField] private float returnTiltTime;

	private int tiltDirection = 1;
	private float tiltTime = 0;
	private float fovTime = 0;

	private float cameraTilt;
	private float maxCameraTilt;

	float tiltVel = 0f;
	float fovVel = 0f;
	private float maxFov, setFov;
	private bool resetTilt = true;
	private bool resetFov = true;

	[Header("Fov")]
	[SerializeField] private float fov;
	[SerializeField] private float returnFovTime;

	[Header("Sensitivity")]
	[SerializeField] private float playerTurnSpeed;
	[SerializeField] private float sensitivity;
	[SerializeField] private Vector2 cameraTurnSpeed;

	float multiplier = 0.01f;

	[Header("Clamp Rotation")]
	[SerializeField] private float upClampAngle;
	[SerializeField] private float downClampAngle;

	private float mouseX;
	private float mouseY;

	private float yRotation;
	private float xRotation;

	private float ySmoothRotation;
	private float xSmoothRotation;

	public Vector2 rotationDelta { get; private set; }
	public float camVel { get; private set; }

	[Header("Assignables")]
	[SerializeField] private ScriptManager s;
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
		mouseY = Input.GetAxisRaw("Mouse X");
		mouseX = Input.GetAxisRaw("Mouse Y");

		HandleTiltAndFov();
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
		rotationDelta = new Vector2(mouseX * sensitivity * multiplier, mouseY * sensitivity * multiplier);
		camVel = rotationDelta.sqrMagnitude;

		yRotation += rotationDelta.y;
		xRotation -= rotationDelta.x;

		xRotation = Mathf.Clamp(xRotation, -upClampAngle, downClampAngle);
	}

	void SmoothRotation()
    {
		ySmoothRotation = Mathf.Lerp(ySmoothRotation, yRotation, cameraTurnSpeed.y * Time.smoothDeltaTime);
		xSmoothRotation = Mathf.Lerp(xSmoothRotation, xRotation, cameraTurnSpeed.x * Time.smoothDeltaTime);
	}

	void ApplyRotation()
    {
		Vector3 shakeOffset = s.CameraShaker.offset;

		cam.transform.localRotation = Quaternion.Euler(xSmoothRotation + shakeOffset.x, ySmoothRotation + shakeOffset.y, cameraTilt + shakeOffset.z);
		s.orientation.transform.rotation = Quaternion.Euler(0, ySmoothRotation, 0);
	}

	void HandleTiltAndFov()
    {
		if (!resetTilt) cameraTilt = Mathf.SmoothDamp(cameraTilt, maxCameraTilt * tiltDirection, ref tiltVel, tiltTime + 0.05f);
		else if (cameraTilt != 0f)
        {
			cameraTilt = Mathf.SmoothDamp(cameraTilt, 0, ref tiltVel, returnTiltTime);
			if (Math.Abs(cameraTilt) < 0.1f) cameraTilt = 0f;
		}

		if (!resetFov) fov = Mathf.SmoothDamp(fov, maxFov, ref fovVel, fovTime);
		else if (fov != setFov)
		{
			fov = Mathf.SmoothDamp(fov, setFov, ref fovVel, returnFovTime);
			if (maxFov > setFov) if (fov < setFov + 0.01f) fov = setFov;
			if (maxFov < setFov) if (fov > setFov - 0.01f) fov = setFov;
		}
	}

	public void TiltCamera(bool reset, int i = 1, float extension = 0, float speed = 0)
	{
		tiltDirection = i;
		maxCameraTilt = extension;
		tiltTime = speed;
		resetTilt = reset;
	}

	public void ChangeFov(bool reset, float extension = 0, float speed = 0)
    {
		maxFov = setFov + extension;
		fovTime = speed;
		resetFov = reset;
	}
}
