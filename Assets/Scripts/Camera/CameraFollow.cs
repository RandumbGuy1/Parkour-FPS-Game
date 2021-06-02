using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraFollow : MonoBehaviour
{
	[Header("Camera Tilt Variables")]
	[SerializeField] private float cameraTilt = 0f;
	[SerializeField] private float returnTiltTime;

	private int tiltDirection = 1;
	private float tiltTime = 0, tiltVel = 0f;
	private float maxCameraTilt = 0f;
	private bool resetTilt = true;

	[Header("Fov")]
	[SerializeField] private float fov;
	[SerializeField] private float returnFovTime;

	private float fovTime = 0, fovVel = 0f;
	private float maxFov = 0f, setFov;
	private bool resetFov = true;

	[Header("Sensitivity")]
	[SerializeField] private float playerTurnSpeed;
	[SerializeField] private float sensitivity;
	[SerializeField] private Vector2 cameraTurnSpeed;

	[Header("Clamp Rotation")]
	[SerializeField] private float upClampAngle;
	[SerializeField] private float downClampAngle;

	private Vector2 mouse;
	private Vector2 rotation;
	private Vector3 smoothRotation;

	public Vector2 rotationDelta { get; private set; }

	[Header("Assignables")]
	[SerializeField] private ScriptManager s;
	private Camera cam;

	void Awake()
    {
		cam = GetComponentInChildren<Camera>();
		setFov = fov;
	}

	void Update()
	{
		mouse.y = Input.GetAxisRaw("Mouse X");
		mouse.x = Input.GetAxisRaw("Mouse Y");

		ChangeTilt();
		ChangeFov();
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
		rotationDelta = mouse * sensitivity * 0.01f;

		rotation.y += rotationDelta.y;
		rotation.x -= rotationDelta.x;
		rotation.x = Mathf.Clamp(rotation.x, -upClampAngle, downClampAngle);
	}

	void SmoothRotation()
    {
		smoothRotation.y = Mathf.Lerp(smoothRotation.y, rotation.y, cameraTurnSpeed.y * Time.smoothDeltaTime);
		smoothRotation.x = Mathf.Lerp(smoothRotation.x, rotation.x, cameraTurnSpeed.x * Time.smoothDeltaTime);
		smoothRotation.z = cameraTilt;
	}

	void ApplyRotation()
    {
		Quaternion newCamRot = Quaternion.Euler(smoothRotation + s.CameraShaker.offset);
		Quaternion newPlayerRot = Quaternion.Euler(0, smoothRotation.y, 0);

		cam.transform.localRotation = newCamRot;
		s.orientation.transform.rotation = newPlayerRot;
	}

	private void ChangeTilt()
    {
		if (cameraTilt == 0 && resetTilt) return;
		cameraTilt = Mathf.SmoothDamp(cameraTilt, (resetTilt ? 0 : maxCameraTilt) * tiltDirection, ref tiltVel, (resetTilt ? returnTiltTime : tiltTime + 0.05f));

		if (!resetTilt) return;
		if (Math.Abs(cameraTilt) < 0.1f) cameraTilt = 0f;
	}

	private void ChangeFov()
	{
		if (fov == setFov && resetFov) return;
		fov = Mathf.SmoothDamp(fov, (resetFov ? setFov : maxFov), ref fovVel, (resetFov ? returnFovTime : fovTime + 0.05f));

		if (!resetFov) return;
		if (maxFov > setFov) if (fov < setFov + 0.01f) fov = setFov;
		if (maxFov < setFov) if (fov > setFov - 0.01f) fov = setFov;
	}

	public void TiltCamera(bool reset, int i = 1, float extension = 0, float speed = 0)
	{
		if (maxCameraTilt == extension) return;

		tiltDirection = i;
		tiltTime = speed;
		resetTilt = reset;
		maxCameraTilt = extension;
	}

	public void ChangeFov(bool reset, float extension = 0, float speed = 0)
    {
		if (maxFov == setFov + extension) return;

		fovTime = speed;
		resetFov = reset;
		maxFov = setFov + extension;
	}
}
