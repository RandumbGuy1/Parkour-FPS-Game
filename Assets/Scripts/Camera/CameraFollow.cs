using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraFollow : MonoBehaviour
{
	private Vector3 wallRunRotation = Vector3.zero;
	private float tiltTime = 0.2f, targetCameraTilt = 0f;

	private GrapplingGun gp = null;

	[Header("Fov")]
	[SerializeField] private float fov;

	[Header("Camera States")]
	[SerializeField] private CameraState state;
	private Vector3 specateOffset = Vector3.zero;
	private Vector3 lastHeadPos = Vector3.zero;

	public enum CameraState
    {
		FPS,
		Spectate
    }

	private float targetFov = 0f, setFov, fovTime = 0.2f;
	private Vector3 effectVel = Vector3.zero;

	[Header("Sensitivity")]
	[SerializeField] private float sensitivity;
	[SerializeField] private float aimSensitivity;
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

	[Header("Assignables")]
	[SerializeField] private ScriptManager s;
	[SerializeField] private ParticleSystem sprintEffect;
	private Camera cam;

	void Awake()
	{
		cam = GetComponentInChildren<Camera>();
		setFov = fov;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		wallRunRotation.z = 0f;
		cam.fieldOfView = setFov;
		targetFov = setFov;
	}

	void Update()
	{
		if (state == CameraState.Spectate) return;

		SpeedLines();

		float inputX = (s.PlayerMovement.Grounded ? s.PlayerInput.InputVector.x * 1.25f : 0f);
		if (inputX != 0f && (gp != null ? gp.GrappleTilt : 0) == 0) SetTiltSmoothing(0.2f);

		targetCameraTilt = s.PlayerMovement.WallRunTiltOffset + s.PlayerMovement.SlideTiltOffset + (gp != null ? gp.GrappleTilt : 0) + (s.PlayerMovement.Grounded ? s.PlayerInput.InputVector.x * 1.25f : 0f);
		targetFov = setFov + s.PlayerMovement.WallRunFovOffset + s.WeaponControls.AimFovOffset + (gp != null ? gp.GrappleFov : 0);

		ChangeTilt();
		ChangeFov();
	}

	void LateUpdate()
	{
		CalcRotation();
		SmoothRotation();
		ApplyRotation();
		IdleCameraSway();

		cam.fieldOfView = fov;
		UpdateCameraPosition();
	}

	void UpdateCameraPosition()
	{
		switch (state)
		{
			case CameraState.FPS:
				transform.position = s.playerHead.position;
				break;

			case CameraState.Spectate:
				Vector2 input = s.PlayerInput.InputVector.normalized;
				specateOffset += cam.transform.TransformDirection(new Vector3(input.x, 0f, input.y));
				transform.position = Vector3.Lerp(transform.position, lastHeadPos + specateOffset, 3f * Time.deltaTime);
				break;
		}
	}

	void CalcRotation()
	{
		RotationDelta = 0.02f * (s.WeaponControls.Aiming ? aimSensitivity : sensitivity) * s.PlayerInput.MouseInputVector;

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
		Quaternion newCamRot = Quaternion.Euler((Vector3) smoothRotation + wallRunRotation + finalSwayOffset + s.CameraShaker.Offset + s.CameraHeadBob.ViewBobOffset * 3.5f);
		Quaternion newPlayerRot = Quaternion.Euler(0, smoothRotation.y + wallRunRotation.y, 0);

		cam.transform.localRotation = newCamRot;
		s.orientation.transform.rotation = newPlayerRot;
	}

	#region Camera Effects
	private void IdleCameraSway()
    {
		if (!s.WeaponControls.Aiming && s.PlayerMovement.Grounded && s.PlayerMovement.Magnitude < 5f)
		{
			Vector3 noiseOffset = Vector3.zero;

			headSwayScroller += Time.deltaTime * swayFrequency;

			noiseOffset.x = Mathf.PerlinNoise(headSwayScroller, 0f);
			noiseOffset.y = Mathf.PerlinNoise(headSwayScroller, 2f) * 0.8f;

			noiseOffset -= (Vector3)Vector2.one * 0.5f;
			HeadSwayOffset = Vector3.Slerp(HeadSwayOffset, noiseOffset, 15f * Time.deltaTime);
		}

		finalSwayOffset = HeadSwayOffset * swayAmount;
	}

	private void ChangeTilt()
	{
		if (wallRunRotation.z == targetCameraTilt) return;
		wallRunRotation.z = Mathf.SmoothDamp(wallRunRotation.z, targetCameraTilt, ref effectVel.x, tiltTime);

		if (Math.Abs(targetCameraTilt - wallRunRotation.z) < 0.05f) wallRunRotation.z = targetCameraTilt;
	}

	private void ChangeFov()
	{
		if (fov == targetFov) return;
		fov = Mathf.SmoothDamp(fov, targetFov, ref effectVel.y, fovTime);

		if (Math.Abs(targetFov - fov) < 0.05f) fov = targetFov;
	}

	public void SetTiltSmoothing(float speed = 0) => tiltTime = speed;
	public void SetFovSmoothing(float speed = 0) => fovTime = speed;

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

	public void SetGrapplingGun(GrapplingGun gp) => this.gp = gp;

	public void OnPlayerStateChanged(PlayerState newState)
	{
		if (newState != PlayerState.Dead) return;

		state = CameraState.Spectate;
		lastHeadPos = s.playerHead.position - s.orientation.forward * 15f;

		s.CameraShaker.ShakeOnce(35f, 6f, 1.5f, 10f, ShakeData.ShakeType.Perlin);
		s.CameraShaker.DisableShakes();

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}	
}
