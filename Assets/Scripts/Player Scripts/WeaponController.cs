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
    public bool aiming { get; private set; } = false;

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

    [Header("Recoil Settings")]
    [SerializeField] private Vector3 recoilPosOffset;
    [SerializeField] private Vector3 recoilRotOffset;
    [Space(10)]
    [SerializeField] private float recoilSmoothTime;

    private Vector3 desiredRecoilRot = Vector3.zero, desiredRecoilPos = Vector3.zero;
    private Vector3 recoilRot = Vector3.zero, recoilPos = Vector3.zero;
    private Vector3 recoilRotVel = Vector3.zero, recoilPosVel = Vector3.zero;

    [Header("Reload Settings")]
    [SerializeField] private Vector3 reloadRotOffset;
    [Space(10)]
    [SerializeField] private float reloadSmoothTime;

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
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image itemArt;
    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();

        smoothDefaultPos = defaultPos;
        smoothDefaultRot = defaultRot;

        ammoText.gameObject.SetActive(false);
    }

    void Update()
    {
        float previousWeapon = selectedWeapon;

        if (s.PlayerInput.rightClick) aiming = !aiming;

        if (weapons.Count > 0)
        {
            if (s.PlayerInput.dropping) Drop();

            for (int i = 1; i < maxWeapons + 1; i++) if (Input.GetKeyDown(i.ToString()) && weapons.Count >= i) selectedWeapon = i - 1;

            ProcessMovement();
            ProcessWeaponInput();

            if (selectedWeapon != previousWeapon) SelectWeapon();

            if (CurrentItem != null)
            {
                ammoText.text = CurrentItem.ReadData();
                itemArt.sprite = CurrentItem.itemSprite;
            }
        }

        Vector3 newPos = smoothDefaultPos + smoothBob + smoothLookOffset + switchOffsetPos + recoilPos; 
        Quaternion newRot = Quaternion.Euler(smoothDefaultRot + smoothSway + switchOffsetRot + recoilRot + reloadRot);

        weaponPos.localPosition = newPos;
        weaponPos.localRotation = newRot;
    }

    #region Weapon Input
    private void ProcessWeaponInput()
    {
        if (CurrentWeapon == null) return;

        bool canAttack = !s.PlayerMovement.vaulting && switchOffsetPos.sqrMagnitude < 40f && switchOffsetRot.sqrMagnitude < 40f && reloadRot.sqrMagnitude < 40f;

        if (CurrentWeapon.automatic ? s.PlayerInput.leftHoldClick && canAttack : s.PlayerInput.leftClick && canAttack) Attack();
        if ((CurrentWeapon.weaponType == WeaponClass.Ranged ? s.PlayerInput.reloading : s.PlayerInput.rightClick)) if (CurrentWeapon.SecondaryAction()) reloadRot = reloadRotOffset;
    }
    #endregion

    #region Weapon Actions
    void Attack()
    {
        switch (CurrentWeapon.weaponType)
        {
            case WeaponClass.Ranged:
                if (CurrentWeapon.OnAttack(s.cam))
                {
                    s.CameraShaker.ShakeOnce(8f, 12.5f, 0.45f, 0.16f);

                    desiredRecoilPos = recoilPosOffset * (aiming ? Random.Range(0.6f, 0.8f) : Random.Range(0.9f, 1.1f)) * CurrentWeapon.recoilForce;
                    desiredRecoilRot = recoilRotOffset * (aiming ? Random.Range(0.3f, 0.5f) : Random.Range(0.9f, 1.1f)) * CurrentWeapon.recoilForce;

                    s.rb.AddForce(-s.cam.forward * CurrentWeapon.recoilForce * 0.25f, ForceMode.Impulse);
                }
                break;

            case WeaponClass.Melee:
                if (CurrentWeapon.OnAttack(s.cam)) s.CameraShaker.ShakeOnce(10f, 8f, 0.3f, 0.1f);
                break;
        }
    }
    #endregion

    #region Inventory Management
    public void AddWeapon(GameObject obj)
    {
        ammoText.gameObject.SetActive(true);

        CurrentWeapon = obj.GetComponent<IWeapon>();
        CurrentItem = obj.GetComponent<IItem>();

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

        obj.transform.SetParent(weaponPos, true);
        obj.transform.localScale = Vector3.one;
    }

    private void SelectWeapon(bool switching = true)
    {
        if (switching)
        {
            switchOffsetPos = switchPosOffset;
            switchOffsetRot = switchRotOffset;

            reloadRot = Vector3.zero;
        }

        CurrentWeapon = weapons[selectedWeapon].GetComponent<IWeapon>();
        CurrentItem = weapons[selectedWeapon].GetComponent<IItem>();

        for (int i = 0; i < weapons.Count; i++) weapons[i].SetActive(i == selectedWeapon);
    }

    private void Drop(bool pickupDrop = false)
    {
        Rigidbody rb = weapons[selectedWeapon].gameObject.GetComponent<Rigidbody>();

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

        if (weapons.Count > 0 && !pickupDrop)
        {
            selectedWeapon = (selectedWeapon + 1 < weapons.Count ? selectedWeapon : weapons.Count - 1);
            SelectWeapon();
        }
        else if (weapons.Count == 0) ammoText.gameObject.SetActive(false);
    }
    #endregion

    #region Dynamic Weapon Movement
    private Vector3 CalculateBob()
    {
        float amp = (CurrentItem != null ? 1f / CurrentItem.weight : 1f) * (aiming ? 0.6f : 1f);

        return (timer <= 0 ? Vector3.zero : (Vector3.right * Mathf.Cos(timer * bobSpeed) * bobAmountHoriz) + (Vector3.up * (Mathf.Sin(timer * bobSpeed * 2f)) * bobAmountVert)) * amp;
    }

    private Vector3 CalculateLookOffset()
    {
        float amp = (CurrentItem != null ? 1f / CurrentItem.weight : 1f) * (aiming ? 0.4f : 1f);

        Vector2 camDelta = s.CameraLook.rotationDelta * 0.3f;
        camDelta.y = Mathf.Clamp(camDelta.y, -3f, 3f);
        camDelta.x = Mathf.Clamp(camDelta.x, -3f, 3f);

        float fallSpeed = s.PlayerMovement.velocity.y * 0.02f;
        fallSpeed = Mathf.Clamp(fallSpeed, -0.5f, 0.5f);

        float strafeOffset = s.PlayerMovement.relativeVel.x * 0.03f;
        strafeOffset = Mathf.Clamp(strafeOffset, -0.2f, 0.2f);

        return -new Vector3(camDelta.y + strafeOffset, camDelta.x + fallSpeed, 0f) * amp;
    }

    private Vector3 CalculateSway()
    {
         float amp = (CurrentItem != null ? 1f / CurrentItem.weight : 1f) * (aiming ? 0.4f : 1f);

         Vector2 camDelta = (s.CameraLook.rotationDelta * swayAmount) * 0.5f;
         camDelta.y -= (s.PlayerInput.input.x * swayAmount) * 1.5f;
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
        if (reloadRot == Vector3.zero) return;

        reloadRot = Vector3.SmoothDamp(reloadRot, Vector3.zero, ref reloadRotVel, reloadSmoothTime);

        if (reloadRot.sqrMagnitude < 0.001f) reloadRot = Vector3.zero;
    }

    private void CalculateRecoilOffset()
    {
        if (desiredRecoilPos == Vector3.zero && desiredRecoilRot == Vector3.zero) return;

        desiredRecoilPos = Vector3.Lerp(desiredRecoilPos, Vector3.zero, 9f * Time.deltaTime);
        desiredRecoilRot = Vector3.Lerp(desiredRecoilRot, Vector3.zero, 9f * Time.deltaTime);

        recoilPos = Vector3.SmoothDamp(recoilPos, desiredRecoilPos, ref recoilPosVel, recoilSmoothTime);
        recoilRot = Vector3.SmoothDamp(recoilRot, desiredRecoilRot, ref recoilRotVel, recoilSmoothTime);
        
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
        timer = s.PlayerMovement.moving && s.PlayerMovement.grounded && s.PlayerMovement.canCrouchWalk && s.PlayerMovement.magnitude > 5f ? timer += Time.deltaTime : 0f;

        smoothBob = Vector3.SmoothDamp(smoothBob, CalculateBob(), ref bobVel, bobSmoothTime);
        smoothSway = Vector3.SmoothDamp(smoothSway, CalculateSway() + (aiming ? (CurrentItem != null ? CurrentItem.aimRot : aimRot) : Vector3.zero) + (s.PlayerInput.crouching ? Vector3.forward * slideTilt : Vector3.zero), ref swayVel, swaySmoothTime);
        smoothLookOffset = Vector3.SmoothDamp(smoothLookOffset, CalculateLookOffset() + (aiming ? (CurrentItem != null ? CurrentItem.aimPos : aimPos) : Vector3.zero), ref lookVel, 0.2f);

        CalculateDefaultValues();
        CalculateRecoilOffset();
        CalculateSwitchOffset();
        CalculateReloadOffset();
    }
    #endregion
}
