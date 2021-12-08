using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraFollow : MonoBehaviour
{
	private Vector3 wallRunRotation = Vector3.zero;
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

	private Vector2 rotation;
	private Vector2 smoothRotation;
	public Vector2 RotationDelta { get; private set; }

	[Header("Head Sway Settings")]
	[SerializeField] private float swayAmount;
	[SerializeField] private float swayFrequency;

	public Vector3 HeadSwayOffset { get; private set; } = Vector3.zero;
	private Vector3 finalSwayOffset = Vector3.zero;
	private float headSwayScroller = 0;

	private float smoothHeadTiltSway = 0f;
	private float smoothHeadTiltSwayVel = 0f;

	[Header("Assignables")]
	[SerializeField] private ScriptManager s;
	[SerializeField] private ParticleSystem sprintEffect;
	private Camera cam;

	void Awake()
	{
		cam = GetComponentInChildren<Camera>();
		setFov = fov;
	}

	void Update()
	{
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
		transform.position = s.playerHead.position;

		if (s.CameraLook.RotationDelta.sqrMagnitude < 5f && s.PlayerMovement.Magnitude < 1f && s.CameraShaker.Offset.sqrMagnitude < 0.01f)
		{
			Vector3 noiseOffset = Vector3.zero;

			headSwayScroller += Time.deltaTime * swayFrequency;

			noiseOffset.x = Mathf.PerlinNoise(headSwayScroller, 0f);
			noiseOffset.y = Mathf.PerlinNoise(headSwayScroller, 2f) * 0.8f;

			noiseOffset -= (Vector3) Vector2.one * 0.5f;
			HeadSwayOffset = noiseOffset;
		}

		float horizInput = (s.PlayerMovement.Grounded ? s.PlayerInput.InputVector.x : 0f);
		smoothHeadTiltSway = Mathf.SmoothDamp(smoothHeadTiltSway, horizInput * 1.25f, ref smoothHeadTiltSwayVel, 0.2f);

		finalSwayOffset = (HeadSwayOffset * swayAmount) + Vector3.forward * smoothHeadTiltSway;
	}

	void CalcRotation()
	{
		RotationDelta = s.PlayerInput.MouseInputVector * sensitivity * 0.02f;

		rotation.y += RotationDelta.y;
		rotation.x -= RotationDelta.x;

		rotation.x = Mathf.Clamp(rotation.x, -upClampAngle, downClampAngle);
	}

	void SmoothRotation()
	{
		smoothRotation.x = Mathf.Lerp(smoothRotation.x, rotation.x, rotateSmoothTime.x * Time.deltaTime);
		smoothRotation.y = Mathf.Lerp(smoothRotation.y, rotation.y,  rotateSmoothTime.y * Time.deltaTime);

		wallRunRotation.y += s.PlayerMovement.CalculateWallRunRotation(transform.rotation.y);
	}

	void ApplyRotation()
	{
		Quaternion newCamRot = Quaternion.Euler((Vector3) smoothRotation + wallRunRotation + finalSwayOffset + s.CameraShaker.Offset);
		Quaternion newPlayerRot = Quaternion.Euler(0, smoothRotation.y + wallRunRotation.y, 0);

		cam.transform.localRotation = newCamRot;
		s.orientation.transform.rotation = newPlayerRot;
	}

    #region Camera Effects
	private void CameraWallRunAutoTurn()
    {
		/*
		desiredWallRunRot += (s.PlayerMovement.CalculateWallRunRotation() * -tiltDirection * 0.5f);

		float targetRot = desiredWallRunRot - (wallRunRotation.z * 0.3f);

		if (wallRunRotation.y == targetRot) return;
		wallRunRotation.y = Mathf.SmoothDamp(wallRunRotation.y, targetRot, ref effectVel.z, 0.3f);

		if (Math.Abs(targetRot - wallRunRotation.y) < 0.01f) wallRunRotation.y = targetRot;
		*/
	}

	private void ChangeTilt()
	{
		if (wallRunRotation.z == targetCameraTilt) return;
		wallRunRotation.z = Mathf.SmoothDamp(wallRunRotation.z, targetCameraTilt * tiltDirection, ref effectVel.x, tiltTime);

		if (Math.Abs(targetCameraTilt - wallRunRotation.z) < 0.01f) wallRunRotation.z = targetCameraTilt;
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
		if (s.PlayerInput.Crouching) SetTilt(8f, 0.15f, 1);

		if (s.PlayerMovement.WallRunning)
		{
			SetTilt(15f, 0.15f, (s.PlayerMovement.IsWallRight ? 1 : -1));
			SetFov(15f, 0.2f);
		}
        else
        {
			if (!s.PlayerInput.Crouching) SetTilt(0, 0.2f);
			if (s.WeaponControls.Aiming) SetFov(-25, 0.2f);
			else SetFov(0, 0.3f);
		}
	}

	private void SpeedLines()
	{
		if (s.PlayerMovement.Magnitude >= 10f)
		{
			if (!sprintEffect.isPlaying) sprintEffect.Play();

			float rateOverLifeTime = Vector3.Angle(s.PlayerMovement.Velocity, cam.transform.forward) * 0.15f;
			rateOverLifeTime = Mathf.Clamp(rateOverLifeTime, 1f, 1000f);
			rateOverLifeTime = s.PlayerMovement.Magnitude * 3.5f / rateOverLifeTime;

			ParticleSystem.EmissionModule em = sprintEffect.emission;
			em.rateOverTime = (s.PlayerMovement.Grounded ? 0f : rateOverLifeTime);
		}
		else if (sprintEffect.isPlaying) sprintEffect.Stop();
	}
	#endregion
}
