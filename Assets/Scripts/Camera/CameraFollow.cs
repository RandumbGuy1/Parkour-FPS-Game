﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraFollow : MonoBehaviour
{
	private Vector3 wallRunRotation = Vector3.zero;
	private float desiredWallRunRot = 0f;
	private int tiltDirection = 1;
	private float tiltTime = 0, targetCameraTilt = 0f;

	[Header("Fov")]
	[SerializeField] private float fov;

	private float targetFov = 0f, setFov, fovTime = 0;
	private Vector3 effectVel = Vector3.zero;

	[Header("Sensitivity")]
	[SerializeField] private float sensitivity;
	[SerializeField] private Vector2 rotateSmoothTime;

	[Header("Clamp Rotation")]
	[SerializeField] private float upClampAngle;
	[SerializeField] private float downClampAngle;

	private Vector2 mouse;
	private Vector2 rotation;
	private Vector2 smoothRotation;
	public Vector2 rotationDelta { get; private set; }

	[Header("Assignables")]
	[SerializeField] private ScriptManager s;
	[SerializeField] private ParticleSystem sprintEffect;
	private Camera cam;
	private bool fast = false;

	void Awake()
	{
		cam = GetComponentInChildren<Camera>();
		setFov = fov;
	}

	void Update()
	{
		mouse.y = Input.GetAxisRaw("Mouse X");
		mouse.x = Input.GetAxisRaw("Mouse Y");

		CameraEffects();
		SpeedLines();
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
		smoothRotation.x = Mathf.Lerp(smoothRotation.x, rotation.x, rotateSmoothTime.x * Time.deltaTime);
		smoothRotation.y = Mathf.Lerp(smoothRotation.y, rotation.y,  rotateSmoothTime.y * Time.deltaTime);

		CameraWallRunAutoTurn();
	}

	void ApplyRotation()
	{
		Quaternion newCamRot = Quaternion.Euler((Vector3) smoothRotation + wallRunRotation + s.CameraShaker.offset);
		Quaternion newPlayerRot = Quaternion.Euler(0, smoothRotation.y + desiredWallRunRot, 0);

		cam.transform.localRotation = newCamRot;
		s.orientation.transform.rotation = newPlayerRot;
	}

    #region Camera Effects
    private void ChangeTilt()
	{
		if (wallRunRotation.z == targetCameraTilt) return;
		wallRunRotation.z = Mathf.SmoothDamp(wallRunRotation.z, targetCameraTilt * tiltDirection, ref effectVel.x, tiltTime);

		if (Math.Abs(targetCameraTilt - wallRunRotation.z) < 0.01f) wallRunRotation.z = targetCameraTilt;
	}

	private void CameraWallRunAutoTurn()
    {
		desiredWallRunRot += (s.PlayerMovement.CalculateWallRunRotation() * -tiltDirection * 0.5f);

		float targetRot = desiredWallRunRot - (wallRunRotation.z * 0.2f);

		if (wallRunRotation.y == targetRot) return;
		wallRunRotation.y = Mathf.SmoothDamp(wallRunRotation.y, targetRot, ref effectVel.z, 0.3f);

		if (Math.Abs(targetRot - wallRunRotation.y) < 0.01f) wallRunRotation.y = targetRot;
	}

	private void ChangeFov()
	{
		if (fov == targetFov) return;
		fov = Mathf.SmoothDamp(fov, targetFov, ref effectVel.y, fovTime);

		if (Math.Abs(targetFov - fov) < 0.01f) fov = targetFov;
	}

	public void SetTilt(float target, float speed, int i = 1)
	{
		if (targetCameraTilt == target) return;

		tiltDirection = i;
		tiltTime = speed;
		targetCameraTilt = target;
	}

	public void SetFov(float extension = 0, float speed = 0)
	{
		if (targetFov == setFov + extension) return;

		fovTime = speed;
		targetFov = setFov + extension;
	}
	#endregion

	#region Process Camera Effects
	private void CameraEffects()
	{
		if (s.PlayerInput.crouching) SetTilt(8f, 0.15f, 1);

		if (s.PlayerMovement.wallRunning)
		{
			SetTilt(15f, 0.15f, (s.PlayerInput.isWallRight ? 1 : -1));
			SetFov(15f, 0.2f);
		}
        else
        {
			if (!s.PlayerInput.crouching) SetTilt(0, 0.2f);
			if (s.WeaponControls.aiming) SetFov(-25, 0.2f);
			else SetFov(0, 0.3f);
		}
	}

	private void SpeedLines()
	{
		if (s.PlayerMovement.magnitude >= 25f)
		{
			if (!fast)
            {
				sprintEffect.Play();
				fast = true;
			}
			
			var em = sprintEffect.emission;
			em.rateOverTime = s.PlayerMovement.magnitude;
		}
		else if (fast)
		{
			sprintEffect.Stop();
			fast = false;
		}
	}
	#endregion
}
