﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponController : MonoBehaviour
{
    [Header("Equip Settings")]
    [SerializeField] private float throwForce;
    [SerializeField] private int selectedWeapon;
    [SerializeField] private int maxWeapons;
    [Space(10)]
    [SerializeField] private int aimFovOffset;
    public bool Aiming { get; private set; } = false;
    public bool HoldingWeapon { get; private set; } = false;
    public bool CanAttack { get; private set; } = true;

    public float AimFovOffset { get { return Aiming ? -aimFovOffset : 0; } }

    private IWeapon CurrentWeapon;
    private IItem CurrentItem; 

    private float timer = 0f;

    private Vector3 bobVel = Vector3.zero, swayVel = Vector3.zero, lookVel = Vector3.zero;
    private Vector3 smoothBob = Vector3.zero, smoothSway = Vector3.zero, smoothLookOffset = Vector3.zero;

    [Header("Idle Settings")]
    [SerializeField] private Vector3 defaultPos;
    [SerializeField] private Vector3 defaultRot;
    [Space(10)]
    [SerializeField] private Vector3 aimPos;
    [SerializeField] private Vector3 aimRot;

    private Vector3 desiredRecoilRot = Vector3.zero, desiredRecoilPos = Vector3.zero;
    private Vector3 recoilRot = Vector3.zero, recoilPos = Vector3.zero;
    private Vector3 recoilRotVel = Vector3.zero, recoilPosVel = Vector3.zero;

    private Vector3 reloadRot = Vector3.zero;
    private Vector3 reloadRotVel = Vector3.zero;

    [Header("Bob Settings")]
    [SerializeField] private float bobAmountHoriz;
    [SerializeField] private float bobAmountVert;
    [SerializeField] private float bobSpeed;
    [SerializeField] private float bobSmoothTime;

    [Header("Sway Settings")]
    [SerializeField] private float swayAmount;
    [SerializeField] private float swaySmoothTime;

    [Header("Weapon Switching Settings")]
    [SerializeField] private Vector3 switchPosOffset;
    [SerializeField] private Vector3 switchRotOffset;
    [Space(10)]
    [SerializeField] private float switchPosTime;
    [SerializeField] private float switchRotTime;
    [Space(10)]
    [SerializeField] private float slideTilt;

    private Vector3 switchOffsetPos = Vector3.zero, switchOffsetRot = Vector3.zero;
    private Vector3 switchPosVel = Vector3.zero, switchRotVel = Vector3.zero;

    private Vector3 smoothDefaultPosVel = Vector3.zero, smoothDefaultRotVel = Vector3.zero;
    private Vector3 smoothDefaultPos = Vector3.zero, smoothDefaultRot = Vector3.zero;

    [Header("Weapons Equipped")]
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();

    [Header("Assignables")]
    [SerializeField] private Transform weaponPos;
    [SerializeField] private Transform weaponEmptyGameObject;
    [Space(10)]
    [SerializeField] private TextMeshProUGUI weaponDataText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image itemArt;
    [Space(10)]
    [SerializeField] private GameObject weaponReticle;
    [SerializeField] private GameObject circleCursor;
    [Space(10)]
    [SerializeField] private ValueSlider slider;
    [SerializeField] private DynaimcReticle reticleEffects;
    private ScriptManager s;

    public Transform WeaponPos { get { return weaponPos; } }

    void Awake()
    {
        s = GetComponent<ScriptManager>();

        smoothDefaultPos = defaultPos;
        smoothDefaultRot = defaultRot;

        weaponDataText.gameObject.SetActive(false);

        weaponReticle.gameObject.SetActive(false);
        circleCursor.gameObject.SetActive(true);
    }

    void Update()
    {
        float previousWeapon = selectedWeapon;

        if (s.PlayerInput.MiddleClick && CanAttack)
        {
            Aiming = !Aiming;
            s.CameraLook.SetFov(Aiming ? -aimFovOffset : 0 + s.PlayerMovement.WallRunFovOffset, Aiming ? 0.2f : 0.3f);
        }
        
        if (weapons.Count > 0)
        {
            if (s.PlayerInput.Dropping) Drop();

            for (int i = 1; i < maxWeapons + 1; i++) if (Input.GetKeyDown(i.ToString()) && weapons.Count >= i) selectedWeapon = i - 1;

            ProcessWeaponInput();
            ProcessMovement();

            if (selectedWeapon != previousWeapon) SelectWeapon();

            if (CurrentItem != null)
            {
                weaponNameText.text = CurrentItem.ReadName();
                weaponDataText.text = CurrentItem.ReadData();
                itemArt.sprite = CurrentItem.itemSprite;

                CurrentItem.ItemUpdate();
            }
        }

        Vector3 newPos = smoothDefaultPos + smoothBob + smoothLookOffset + switchOffsetPos + recoilPos; 
        Quaternion newRot = Quaternion.Euler(smoothDefaultRot + smoothSway + switchOffsetRot + recoilRot + reloadRot - s.CameraShaker.Offset * 0.8f);

        weaponPos.localPosition = newPos;
        weaponPos.localRotation = newRot;

        HoldingWeapon = weapons.Count > 0;
    }

    private void ProcessWeaponInput()
    {
        if (CurrentWeapon == null) return;

        CanAttack = !s.PlayerMovement.Vaulting && switchOffsetPos.sqrMagnitude < 40f && switchOffsetRot.sqrMagnitude < 40f && reloadRot.sqrMagnitude < 40f && (CurrentWeapon.weaponType == WeaponClass.Melee ? recoilPos.sqrMagnitude < 50f && recoilRot.sqrMagnitude < 50f : true);

        if (CurrentWeapon.automatic ? s.PlayerInput.LeftHoldClick && CanAttack : s.PlayerInput.LeftClick && CanAttack) CurrentWeapon.OnAttack(s);
        if ((CurrentWeapon.weaponType == WeaponClass.Ranged ? s.PlayerInput.Reloading : s.PlayerInput.RightClick)) CurrentWeapon.SecondaryAction(s);
    }

    #region Weapon Actions
    public void AddRecoil(Vector3 recoilPosOffset, Vector3 recoilRotOffset, float amount = 0, float aimMulti = 1f)
    {
        switch (CurrentWeapon.weaponType)
        {
            case WeaponClass.Ranged:
                s.CameraShaker.ShakeOnce(CurrentWeapon.recoilShakeData, new Vector3(-0.9f, Random.Range(-0.1f, 0.1f), Random.Range(-0.3f, 0.3f)) * (amount * (0.1f * (Aiming ? aimMulti : 1f))));

                desiredRecoilPos = recoilPosOffset * (Aiming ? aimMulti : Random.Range(0.9f, 1.15f));
                desiredRecoilRot = recoilRotOffset * (Aiming ? aimMulti - 0.15f : Random.Range(0.9f, 1.15f));

                s.rb.AddForce(-s.cam.forward * amount * 0.25f, ForceMode.Impulse);

                if (Aiming) break;
                reticleEffects.AddReticleRecoil(amount * 3f);
                break;

            case WeaponClass.Melee:
                desiredRecoilPos = recoilPosOffset;
                desiredRecoilRot = recoilRotOffset;

                s.CameraShaker.ShakeOnce(CurrentWeapon.recoilShakeData, Vector3.left);
                slider.SetSliderCooldown(1f, 0.05f);
                break;
        }
    }

    public void AddReload(Vector3 reloadRotOffset)
    {
        Aiming = false;
        reloadRot = reloadRotOffset;
    }
    #endregion
    
    #region Inventory Management
    public void AddWeapon(GameObject obj)
    {
        weaponDataText.gameObject.SetActive(true);

        weaponReticle.gameObject.SetActive(true);
        circleCursor.gameObject.SetActive(false);

        CurrentWeapon = obj.GetComponent<IWeapon>();
        CurrentItem = obj.GetComponent<IItem>();

        obj.transform.SetParent(weaponPos, true);
        obj.transform.localScale = Vector3.one;

        if (weapons.Count >= maxWeapons)
        {
            Drop(true);
            weapons.Insert(selectedWeapon, obj);
            SelectWeapon(false);
        }
        else
        {
            weapons.Add(obj);

            selectedWeapon = weapons.Count - 1;
            SelectWeapon(false);
        }

        if (CurrentItem != null) CurrentItem.OnPickup();
    }

    private void SelectWeapon(bool switching = true)
    {
        if (switching)
        {
            switchOffsetPos = switchPosOffset;
            switchOffsetRot = switchRotOffset;

            reloadRot = Vector3.zero;
        }
        else Aiming = false;

        CurrentWeapon = weapons[selectedWeapon].GetComponent<IWeapon>();
        CurrentItem = weapons[selectedWeapon].GetComponent<IItem>();

        for (int i = 0; i < weapons.Count; i++) weapons[i].SetActive(i == selectedWeapon);
    }

    private void Drop(bool pickupDrop = false)
    {
        Rigidbody rb = weapons[selectedWeapon].gameObject.GetComponent<Rigidbody>();

        CurrentItem.OnDrop();

        rb.transform.SetParent(weaponEmptyGameObject);
        rb.gameObject.SetActive(true);
        rb.transform.localScale = Vector3.one;

        rb.isKinematic = false;
        rb.detectCollisions = true;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;

        rb.velocity = s.rb.velocity * 1f;
        rb.AddForce(s.cam.forward * throwForce + Vector3.up * 1.3f, ForceMode.Impulse);

        Vector3 rand = Vector3.zero;

        rand.x = Random.Range(-1f, 1f);
        rand.y = Random.Range(-1f, 1f);
        rand.z = Random.Range(-1f, 1f);

        rb.AddTorque(rand.normalized * throwForce * 3f, ForceMode.Impulse);
        weapons.RemoveAt(selectedWeapon);

        reloadRotVel = Vector3.zero;
        reloadRot = Vector3.zero;

        switchPosVel = Vector3.zero;
        switchRotVel = Vector3.zero;

        switchOffsetPos = Vector3.zero;
        switchOffsetRot = Vector3.zero;

        if (weapons.Count > 0 && !pickupDrop)
        {
            selectedWeapon = (selectedWeapon + 1 < weapons.Count ? selectedWeapon : weapons.Count - 1);
            SelectWeapon();
        }
        else if (weapons.Count == 0)
        {
            weaponReticle.gameObject.SetActive(false);
            circleCursor.gameObject.SetActive(true);

            weaponDataText.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Dynamic Weapon Movement
    private Vector3 CalculateBob()
    {
        float amp = (CurrentItem != null ? 1f / CurrentItem.weight : 1f) * (Aiming ? 0.15f : 1f);

        return (timer <= 0 ? Vector3.zero : (Vector3.right * Mathf.Cos(timer * bobSpeed) * bobAmountHoriz) + (Vector3.up * (Mathf.Sin(timer * bobSpeed * 2f)) * bobAmountVert)) * amp;
    }

    private Vector3 CalculateLookOffset()
    {
        float amp = (CurrentItem != null ? 1f / CurrentItem.weight : 1f) * (Aiming ? 0.2f : 1f);

        Vector2 camDelta = s.CameraLook.RotationDelta * 0.3f;
        camDelta.y = Mathf.Clamp(camDelta.y, -3f, 3f);
        camDelta.x = Mathf.Clamp(camDelta.x, -3f, 3f);

        float fallSpeed = s.PlayerMovement.Velocity.y * 0.023f;
        fallSpeed = Mathf.Clamp(fallSpeed, -0.5f, 0.5f);

        float strafeOffset = s.PlayerMovement.RelativeVel.x * 0.03f;
        strafeOffset = Mathf.Clamp(strafeOffset, -0.2f, 0.2f);

        return -new Vector3(camDelta.y + strafeOffset, camDelta.x + fallSpeed, 0f) * amp;
    }

    private Vector3 CalculateSway()
    {
         float amp = (CurrentItem != null ? 1f / CurrentItem.weight : 1f) * (Aiming ? 0.4f : 1f);

         Vector2 camDelta = (s.CameraLook.RotationDelta * swayAmount) * 0.5f;
         camDelta.y -= (s.PlayerInput.InputVector.x * swayAmount) * 1.5f;
         camDelta.y = Mathf.Clamp(camDelta.y, -100, 100);
         camDelta.x = Mathf.Clamp(camDelta.x, -60, 60);

         return (Vector3.up * camDelta.y + Vector3.right * camDelta.x * -1.4f) * amp;
    }

    private void CalculateSwitchOffset()
    {
        if (switchOffsetPos == Vector3.zero && switchOffsetRot == Vector3.zero) return;

        switchOffsetPos = Vector3.SmoothDamp(switchOffsetPos, Vector3.zero, ref switchPosVel, switchPosTime);
        switchOffsetRot = Vector3.SmoothDamp(switchOffsetRot, Vector3.zero, ref switchRotVel, switchRotTime);

        if (switchOffsetPos.sqrMagnitude < 0.001f && switchOffsetRot.sqrMagnitude < 0.001f)
        {
            switchOffsetPos = Vector3.zero;
            switchOffsetRot = Vector3.zero;
        }
    }

    private void CalculateReloadOffset()
    {
        if (CurrentWeapon == null || reloadRot == Vector3.zero) return;

        reloadRot = Vector3.SmoothDamp(reloadRot, Vector3.zero, ref reloadRotVel, CurrentWeapon.reloadSmoothTime);

        if (reloadRot.sqrMagnitude < 0.001f) reloadRot = Vector3.zero;
    }

    private void CalculateRecoilOffset()
    {
        if (CurrentWeapon == null || desiredRecoilPos == Vector3.zero && desiredRecoilRot == Vector3.zero) return;

        desiredRecoilPos = Vector3.Lerp(desiredRecoilPos, Vector3.zero, (CurrentWeapon.weaponType == WeaponClass.Melee ? 3f : 9f) * Time.deltaTime);
        desiredRecoilRot = Vector3.Lerp(desiredRecoilRot, Vector3.zero, (CurrentWeapon.weaponType == WeaponClass.Melee ? 3f : 9f) * Time.deltaTime);

        float smoothing = CurrentWeapon.recoilSmoothTime;
        recoilPos = Vector3.SmoothDamp(recoilPos, desiredRecoilPos, ref recoilPosVel, smoothing);
        recoilRot = Vector3.SmoothDamp(recoilRot, desiredRecoilRot, ref recoilRotVel, smoothing);
        
        if (desiredRecoilPos.sqrMagnitude < 0.001f && desiredRecoilRot.sqrMagnitude < 0.001f)
        {
            desiredRecoilPos = Vector3.zero;
            desiredRecoilRot = Vector3.zero;
        }
    }

    private void CalculateDefaultValues()
    {
        Vector3 targetDesiredPos = (CurrentItem != null ? CurrentItem.defaultPos : defaultPos);
        Vector3 targetDesiredRot = (CurrentItem != null ? CurrentItem.defaultRot : defaultRot);

        if (smoothDefaultPos == targetDesiredPos && smoothDefaultRot == targetDesiredRot) return;

        smoothDefaultPos = Vector3.SmoothDamp(smoothDefaultPos, targetDesiredPos, ref smoothDefaultPosVel, 0.55f);
        smoothDefaultRot = Vector3.SmoothDamp(smoothDefaultRot, targetDesiredRot, ref smoothDefaultRotVel, 0.55f);

        if ((smoothDefaultPos - targetDesiredPos).sqrMagnitude < 0.00001f && (smoothDefaultRot - targetDesiredRot).sqrMagnitude < 0.00001f)
        {
            smoothDefaultPos = targetDesiredPos;
            smoothDefaultRot = targetDesiredRot;
        }
    }

    private void ProcessMovement()
    {
        timer = (s.PlayerMovement.Grounded && s.PlayerMovement.CanCrouchWalk && s.PlayerMovement.Moving || s.PlayerMovement.WallRunning) && s.PlayerMovement.Magnitude > 0.5f ? timer += Time.deltaTime : 0f;

        smoothBob = Vector3.SmoothDamp(smoothBob, CalculateBob() - (Aiming ? s.CameraLook.HeadSwayOffset * 0.01f : s.CameraLook.HeadSwayOffset * 0.3f), ref bobVel, bobSmoothTime);
        smoothSway = Vector3.SmoothDamp(smoothSway, CalculateSway() + (Aiming ? (CurrentItem != null ? CurrentItem.aimRot : aimRot) : Vector3.zero) + (s.PlayerInput.Crouching ? Vector3.forward * slideTilt : Vector3.zero) - (Aiming ? s.CameraLook.HeadSwayOffset * 4f : s.CameraLook.HeadSwayOffset * 14f), ref swayVel, swaySmoothTime);
        smoothLookOffset = Vector3.SmoothDamp(smoothLookOffset, CalculateLookOffset() * (Aiming ? 0.5f : 1f) + (Aiming ? (CurrentItem != null ? CurrentItem.aimPos : aimPos) : Vector3.zero), ref lookVel, 0.2f);

        CalculateDefaultValues();
        CalculateRecoilOffset();
        CalculateSwitchOffset();
        CalculateReloadOffset();
    }
    #endregion
}
