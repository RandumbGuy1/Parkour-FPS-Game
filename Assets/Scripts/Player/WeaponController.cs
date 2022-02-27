using System.Collections;
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
    public bool HoldingWeapon { get; private set; } = false;
    public bool CanAttack { get; private set; } = true;
    public bool Firing { get; private set; } = false;

    public float AimFovOffset { get { return Aiming ? -aimFovOffset : 0; } }
    public bool Aiming { get; private set; } = false;

    private IWeapon CurrentWeapon;
    private IItem CurrentItem; 

    private Vector3 bobVel = Vector3.zero, swayVel = Vector3.zero, idleVel = Vector3.zero;
    private Vector3 smoothBob = Vector3.zero, smoothSway = Vector3.zero, idleLookOffset = Vector3.zero;

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
    [Space(10)]
    [SerializeField] private float idleSwayAmount;
    [SerializeField] private float idleSwayFrequency;
    [SerializeField] private float idleSwaySmoothTime;
    private float idleSwayTimer = 0;

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
    [SerializeField] private CameraShaker weaponShaker;
    [SerializeField] private Transform weaponPos;
    [SerializeField] private Transform weaponEmptyGameObject;
    [Space(10)]
    [SerializeField] private TextMeshProUGUI weaponDataText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image itemArt;
    [Space(10)]
    [SerializeField] private HitMarker hitMarker;
    [SerializeField] private GameObject weaponReticle;
    [SerializeField] private GameObject circleCursor;
    [Space(10)]
    [SerializeField] private ValueSlider slider;
    [SerializeField] private DynaimcReticle reticleEffects;

    private PlayerManager s;
    public HitMarker HitMarker { get { return hitMarker; } }
    public Transform WeaponPos { get { return weaponPos; } }

    void Awake()
    {
        s = GetComponent<PlayerManager>();

        weaponDataText.gameObject.SetActive(false);

        weaponReticle.SetActive(false);
        circleCursor.SetActive(true);
    }

    void Update()
    {
        float previousWeapon = selectedWeapon;

        if (s.PlayerInput.MiddleClick && CanAttack)
        {
            Aiming = !Aiming;
            s.CameraLook.SetFovSmoothing(0.2f);
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
                itemArt.sprite = CurrentItem.ItemSprite;

                CurrentItem.ItemUpdate();
            }
        }

        Vector3 newPos = smoothDefaultPos + smoothBob + idleLookOffset * 0.01f + switchOffsetPos + recoilPos; 
        Quaternion newRot = Quaternion.Euler(smoothDefaultRot + ConverToEuler(smoothBob * 15f) + smoothSway + idleLookOffset + switchOffsetRot + recoilRot + reloadRot - s.CameraShaker.Offset * 0.8f);

        weaponPos.localPosition = newPos;
        weaponPos.localRotation = newRot;

        HoldingWeapon = weapons.Count > 0;
    }

    private void ProcessWeaponInput()
    {
        if (CurrentWeapon == null) return;

        CanAttack = !s.PlayerMovement.Vaulting && switchOffsetPos.sqrMagnitude < 40f && switchOffsetRot.sqrMagnitude < 40f && reloadRot.sqrMagnitude < 40f && (CurrentWeapon.WeaponType != WeaponClass.Melee || recoilPos.sqrMagnitude < 50f && recoilRot.sqrMagnitude < 50f);
        Firing = (CurrentWeapon.Automatic ? s.PlayerInput.LeftHoldClick : s.PlayerInput.LeftClick) && CanAttack;

        if (Firing) CurrentWeapon.OnAttack();
        if (CurrentWeapon.WeaponType == WeaponClass.Ranged ? s.PlayerInput.Reloading : s.PlayerInput.RightClick) CurrentWeapon.SecondaryAction();
    }

    #region Weapon Actions
    public void AddRecoil(Vector3 recoilPosOffset, Vector3 recoilRotOffset, float amount = 0, float aimMulti = 1f)
    {
        switch (CurrentWeapon.WeaponType)
        {
            case WeaponClass.Ranged:
                s.CameraShaker.ShakeOnce(new KickbackShake(CurrentWeapon.RecoilShakeData, new Vector3(-0.9f, Random.Range(-0.1f, 0.1f), Random.Range(-0.3f, 0.3f)) * (amount * (0.1f * (Aiming ? aimMulti : 1f)))));
                s.CameraShaker.ShakeOnce(new PerlinShake(ShakeData.Create(4.5f * aimMulti, 9.5f, 0.6f, 8f)));

                desiredRecoilPos = recoilPosOffset * (Aiming ? aimMulti : Random.Range(0.9f, 1.15f));
                desiredRecoilRot = recoilRotOffset * (Aiming ? aimMulti - 0.15f : Random.Range(0.9f, 1.15f));

                s.rb.AddForce(0.25f * amount * -s.cam.transform.forward, ForceMode.Impulse);

                if (!Aiming) reticleEffects.AddReticleRecoil(amount * 3f);

                break;

            case WeaponClass.Melee:
                desiredRecoilPos = recoilPosOffset;
                desiredRecoilRot = recoilRotOffset;

                s.CameraShaker.ShakeOnce(new KickbackShake(CurrentWeapon.RecoilShakeData, Vector3.left));
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
        weaponReticle.SetActive(true);
        circleCursor.SetActive(false);

        obj.transform.SetParent(weaponPos, true);
        obj.transform.localScale = Vector3.one;

        if (weapons.Count >= maxWeapons)
        {
            Drop(true);
            weapons.Insert(selectedWeapon, obj);
        }
        else
        {
            weapons.Add(obj);
            selectedWeapon = weapons.Count - 1;
        }

        SelectWeapon(false);

        CurrentWeapon = weapons[selectedWeapon].GetComponent<IWeapon>();
        CurrentItem = weapons[selectedWeapon].GetComponent<IItem>();

        if (CurrentItem != null) CurrentItem.OnPickup(s);
    }

    private void SelectWeapon(bool switching = true)
    {
        ResetMovementValues();

        if (switching)
        {
            switchOffsetPos = switchPosOffset;
            switchOffsetRot = switchRotOffset;

            CurrentWeapon = weapons[selectedWeapon].GetComponent<IWeapon>();
            CurrentItem = weapons[selectedWeapon].GetComponent<IItem>();
        }
        else Aiming = false;

        for (int i = 0; i < weapons.Count; i++) weapons[i].SetActive(i == selectedWeapon);

        reticleEffects.ResetReticle();
    }

    private void Drop(bool pickupDrop = false, bool all = false)
    {
        Rigidbody rb = weapons[selectedWeapon].GetComponent<Rigidbody>();

        CurrentItem.OnDrop();

        rb.transform.SetParent(weaponEmptyGameObject);
        rb.gameObject.SetActive(true);
        rb.transform.localScale = Vector3.one;

        rb.isKinematic = false;
        rb.detectCollisions = true;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;

        rb.velocity = s.rb.velocity * 1f;
        rb.AddForce(s.cam.transform.forward * throwForce + Vector3.up * 1.3f, ForceMode.Impulse);

        Vector3 rand = Vector3.zero;

        rand.x = Random.Range(-1f, 1f);
        rand.y = Random.Range(-1f, 1f);
        rand.z = Random.Range(-1f, 1f);

        rb.AddTorque(3f * throwForce * rand.normalized, ForceMode.Impulse);
        if (all) rb.AddExplosionForce(15f, s.BottomCapsuleSphereOrigin + (Vector3.down * 0.5f) + Random.insideUnitSphere, 15f, 1f, ForceMode.Impulse);
        
        weapons.RemoveAt(selectedWeapon);

        ResetMovementValues();

        if (weapons.Count > 0 && !pickupDrop)
        {
            selectedWeapon = (selectedWeapon + 1 < weapons.Count ? selectedWeapon : weapons.Count - 1);
            SelectWeapon();

            if (all) Drop(false, true);
        }
        else if (weapons.Count == 0)
        {
            weaponReticle.SetActive(false);
            circleCursor.SetActive(true);
            weaponDataText.gameObject.SetActive(false);

            Firing = false;
        }
    }
    #endregion

    #region Dynamic Weapon Movement
    private void ResetMovementValues()
    {
        reloadRot = Vector3.zero;
        reloadRotVel = Vector3.zero;

        switchOffsetPos = Vector3.zero;
        switchOffsetRot = Vector3.zero;
        switchPosVel = Vector3.zero;
        switchRotVel = Vector3.zero;

        recoilPos = Vector3.zero;
        recoilRot = Vector3.zero;
        recoilPosVel = Vector3.zero;
        recoilRotVel = Vector3.zero;
    }

    private Vector3 CalculateBob(float timer, float amp) => (timer <= 0 ? Vector3.zero : (bobAmountHoriz * Mathf.Cos(timer * bobSpeed) * Vector3.right) + ((Mathf.Sin(timer * bobSpeed * 2f)) * bobAmountVert * Vector3.up)) * amp;
    private Vector3 ConverToEuler(Vector3 rotation) => new Vector3(-rotation.y, -rotation.x, rotation.z);

    private Vector3 CalculateMoveOffset(float amp)
    {
        Vector3 moveOffset = s.PlayerMovement.RelativeVel * 0.02f;
        moveOffset.x += s.CameraLook.RotationDelta.x * 0.08f;
        moveOffset.x = Mathf.Clamp(moveOffset.x, -0.25f, 0.25f);
        moveOffset.y = Mathf.Clamp(moveOffset.y, -0.8f, 0.8f);
        moveOffset.z = Mathf.Clamp(moveOffset.z, -0.05f, 0.05f);

        return -moveOffset * amp;
    }

    private Vector3 CalculateSway(float amp)
    {
        Vector2 swayOffset = 0.35f * swayAmount * s.CameraLook.RotationDelta;
        swayOffset.y += s.PlayerMovement.RelativeVel.x * 0.25f;
        swayOffset.x *= -1f;
        swayOffset = Vector3.ClampMagnitude(swayOffset, 60f);

        return (Aiming ? 0.25f : 1f) * amp * swayOffset;
    }

    private Vector3 CalculateIdleSway(float amp) => (Aiming ? 0.15f : 1f) * amp * idleSwayAmount * (0.5f * idleSwayAmount * LissajousCurve(idleSwayTimer));
    private Vector3 LissajousCurve(float Time) => new Vector3(Mathf.Sin(Time), 1f * Mathf.Sin(2f * Time + Mathf.PI));

    private void CalculateSwitchOffset()
    {
        if (switchOffsetPos == Vector3.zero && switchOffsetRot == Vector3.zero) return;

        switchOffsetPos = Vector3.SmoothDamp(switchOffsetPos, Vector3.zero, ref switchPosVel, switchPosTime);
        switchOffsetRot = Vector3.SmoothDamp(switchOffsetRot, Vector3.zero, ref switchRotVel, switchRotTime);

        if (switchOffsetPos.sqrMagnitude < 0.00001f && switchOffsetRot.sqrMagnitude < 0.00001f)
        {
            switchOffsetPos = Vector3.zero;
            switchOffsetRot = Vector3.zero;
        }
    }

    private void CalculateReloadOffset()
    {
        if (CurrentWeapon == null || reloadRot == Vector3.zero) return;

        reloadRot = Vector3.SmoothDamp(reloadRot, Vector3.zero, ref reloadRotVel, CurrentWeapon.ReloadSmoothTime);

        if (reloadRot.sqrMagnitude < 0.001f) reloadRot = Vector3.zero;
    }

    private void CalculateRecoilOffset()
    {
        if (CurrentWeapon == null || desiredRecoilPos == Vector3.zero && desiredRecoilRot == Vector3.zero) return;

        desiredRecoilPos = Vector3.Lerp(desiredRecoilPos, Vector3.zero, (CurrentWeapon.WeaponType == WeaponClass.Melee ? 3f : 9f) * Time.deltaTime);
        desiredRecoilRot = Vector3.Lerp(desiredRecoilRot, Vector3.zero, (CurrentWeapon.WeaponType == WeaponClass.Melee ? 3f : 9f) * Time.deltaTime);

        float smoothing = CurrentWeapon.RecoilSmoothTime;
        recoilPos = Vector3.SmoothDamp(recoilPos, desiredRecoilPos, ref recoilPosVel, smoothing);
        recoilRot = Vector3.SmoothDamp(recoilRot, desiredRecoilRot, ref recoilRotVel, smoothing);
        
        if (desiredRecoilPos.sqrMagnitude < 0.00001f && desiredRecoilRot.sqrMagnitude < 0.00001f)
        {
            desiredRecoilPos = Vector3.zero;
            desiredRecoilRot = Vector3.zero;
        }
    }

    private void CalculateDefaultValues()
    {
        Vector3 targetDesiredPos = CurrentItem.DefaultPos + (Aiming ? CurrentItem.AimPos : Vector3.zero);
        Vector3 targetDesiredRot = CurrentItem.DefaultRot + (Aiming ? CurrentItem.AimRot : Vector3.zero);

        if (smoothDefaultPos == targetDesiredPos && smoothDefaultRot == targetDesiredRot) return;

        smoothDefaultPos = Vector3.SmoothDamp(smoothDefaultPos, targetDesiredPos, ref smoothDefaultPosVel, 0.15f);
        smoothDefaultRot = Vector3.SmoothDamp(smoothDefaultRot, targetDesiredRot, ref smoothDefaultRotVel, 0.15f);

        float mag = (smoothDefaultPos - targetDesiredPos).sqrMagnitude + (smoothDefaultRot - targetDesiredRot).sqrMagnitude;

        if (mag < 0.00002f)
        {
            smoothDefaultPos = targetDesiredPos;
            smoothDefaultRot = targetDesiredRot;
        }
    }

    private void ProcessMovement()
    {
        if (CurrentItem == null) return;

        idleSwayTimer += Time.deltaTime * idleSwayFrequency;

        float timer = s.CameraHeadBob.BobTimer;
        float amp = 1f / CurrentItem.Weight;
        float bobAimMulti = Aiming ? 0.08f : 1f;

        Vector3 targetPos = CalculateBob(timer, amp) + CalculateMoveOffset(amp);
        smoothBob = Vector3.SmoothDamp(smoothBob,targetPos * bobAimMulti, ref bobVel, bobSmoothTime);

        Vector3 targetSway = CalculateSway(amp) + (3f * s.PlayerMovement.SlideTiltOffset * Vector3.forward);
        smoothSway = Vector3.SmoothDamp(smoothSway, targetSway, ref swayVel, swaySmoothTime);
        idleLookOffset = Vector3.SmoothDamp(idleLookOffset, CalculateIdleSway(amp), ref idleVel, idleSwaySmoothTime);

        CalculateDefaultValues();
        CalculateRecoilOffset();
        CalculateSwitchOffset();
        CalculateReloadOffset();
    }
    #endregion

    public void OnPlayerStateChanged(UnitState newState)
    {
        if (newState != UnitState.Dead) return;

        if (HoldingWeapon) Drop(false, true);

        Aiming = false;
        enabled = false;
    }
}
