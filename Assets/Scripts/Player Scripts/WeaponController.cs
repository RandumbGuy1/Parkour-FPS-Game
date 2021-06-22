using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Equip Settings")]
    [SerializeField] private Weapon CurrentWeapon;
    [SerializeField] private float throwForce;
    [SerializeField] private int selectedWeapon;
    public bool aiming = false;

    private Vector3 bobVel = Vector3.zero;
    private Vector3 swayVel = Vector3.zero;

    private float timer = 0f;
    private Vector3 smoothBob = Vector3.zero;
    private Vector3 smoothSway = Vector3.zero;

    [Header("Bob Settings")]
    [SerializeField] private Vector3 defaultPos;
    [SerializeField] private Vector3 aimPos;
    [SerializeField] private float bobAmountHoriz;
    [SerializeField] private float bobAmountVert;
    [SerializeField] private float bobSpeed;
    [SerializeField] private float bobSmoothTime;

    [Header("Sway Settings")]
    [SerializeField] private Vector3 defaultRot;
    [SerializeField] private Vector3 aimRot;
    [SerializeField] private float swayAmount;
    [SerializeField] private float swaySmoothTime;

    [Header("WeaponSwitching Settings")]
    [SerializeField] private Vector3 switchPosOffset;
    [SerializeField] private Vector3 switchRotOffset;
    [SerializeField] private float switchPosTime;
    [SerializeField] private float switchRotTime;

    private Vector3 offsetPos = Vector3.zero;
    private Vector3 offsetRot = Vector3.zero;
    private Vector3 switchPosVel = Vector3.zero;
    private Vector3 switchRotVel = Vector3.zero;

    [Header("Weapons Equipped")]
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();

    [Header("Assignables")]
    [SerializeField] private Transform weaponPos;

    private ScriptManager s;
    private Transform selectedTransform;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
    }

    void Update()
    {
        float previousWeapon = selectedWeapon;

        if (s.PlayerInput.rightClick) aiming = !aiming;

        if (weapons.Count > 0)
        {
            if (s.PlayerInput.dropping) Drop();
            if (Input.GetKeyDown(KeyCode.Alpha1)) selectedWeapon = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2) && weapons.Count >= 2) selectedWeapon = 1;

            ProcessWeaponInput();
            ProcessMovement();

            if (selectedWeapon != previousWeapon)
            {
                CurrentWeapon = weapons[selectedWeapon].GetComponent<Weapon>();
                SelectWeapon(true);
            }
        }

        Vector3 newPos = defaultPos + smoothBob + offsetPos;
        Quaternion newRot = Quaternion.Euler(defaultRot + smoothSway + offsetRot);

        weaponPos.localPosition = newPos;
        weaponPos.localRotation = newRot;
    }

    private void ProcessWeaponInput()
    {
        if (CurrentWeapon == null) return;

        if ((CurrentWeapon.weaponType == Weapon.WeaponClass.Ranged ? s.PlayerInput.reloading : s.PlayerInput.rightClick)) CurrentWeapon.SecondaryAction();
        if ((CurrentWeapon.attackType == Weapon.AttackType.Automatic ? s.PlayerInput.leftHoldClick : s.PlayerInput.leftClick)) CurrentWeapon.OnAttack();
    }

    public void AddWeapon(GameObject obj)
    {
        CurrentWeapon = obj.GetComponent<Weapon>();

        weapons.Add(obj);
        obj.transform.SetParent(weaponPos);

        selectedWeapon = weapons.Count - 1;
        SelectWeapon(false);
    }

    #region Inventory Management
    private void SelectWeapon(bool switching)
    {
        if (switching)
        {
            offsetPos = switchPosOffset;
            offsetRot = switchRotOffset;
        }

        for (int i = 0; i < weapons.Count; i++)
            weapons[i].SetActive(i == selectedWeapon);
    }

    private void Drop()
    {
        Rigidbody rb = weapons[selectedWeapon].GetComponent<Rigidbody>();

        weapons[selectedWeapon].transform.SetParent(null);

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.AddForce(s.cam.transform.forward * throwForce * ((s.PlayerMovement.magnitude * 0.09f) + 1f), ForceMode.VelocityChange);

        Vector3 rand = Vector3.zero;

        rand.x = Random.Range(-1f, 1f);
        rand.y = Random.Range(-1f, 1f);
        rand.z = Random.Range(-1f, 1f);

        rb.AddTorque(rand.normalized * throwForce, ForceMode.VelocityChange);

        weapons[selectedWeapon].SetActive(true);
        weapons.RemoveAt(selectedWeapon);

        if (weapons.Count > 0)
        {
            selectedWeapon = weapons.Count - 1;
            SelectWeapon(true);
        }
    }
    #endregion

    #region Dynamic Weapon Movement
    private Vector3 CalculateBob()
    {
        Vector3 offset = Vector3.zero;

        if (weapons.Count > 0)
            if (timer > 0) offset += (Vector3.right * Mathf.Cos(timer * bobSpeed) * bobAmountHoriz) + (Vector3.up * Mathf.Sin(timer * bobSpeed * 2) * bobAmountVert);

        Vector2 camDelta = s.CameraLook.rotationDelta * 0.3f;
        camDelta.y = Mathf.Clamp(camDelta.y, -3f, 3f);
        camDelta.x = Mathf.Clamp(camDelta.x, -3f, 3f);

        float fallSpeed = s.PlayerMovement.velocity.y * 0.03f;
        fallSpeed = Mathf.Clamp(fallSpeed, -1f, 1f);

        offset -= new Vector3(camDelta.y + (s.PlayerInput.input.x * 0.4f), camDelta.x + fallSpeed, 0f);

        return offset;
    }

    private Vector3 CalculateSway()
    {
        Vector3 offset = Vector3.zero;

        if (weapons.Count > 0)
        {
            Vector2 camDelta = s.CameraLook.rotationDelta * swayAmount * 0.8f;
            camDelta.y -= s.PlayerInput.input.x * swayAmount * 1.4f;
            camDelta.y = Mathf.Clamp(camDelta.y, -100, 100);
            camDelta.x = Mathf.Clamp(camDelta.x, -60, 60);

            offset += Vector3.up * camDelta.y + Vector3.right * camDelta.x * -1.4f;
        }

        return offset;
    }

    private void ProcessMovement()
    {
        timer = s.PlayerMovement.moving && s.PlayerInput.grounded && !s.PlayerInput.crouching && s.PlayerMovement.magnitude > 5f ? timer += Time.deltaTime : 0f;

        smoothBob = Vector3.SmoothDamp(smoothBob, CalculateBob() + (aiming ? aimPos : Vector3.zero), ref bobVel, bobSmoothTime);
        smoothSway = Vector3.SmoothDamp(smoothSway, CalculateSway() + (aiming ? aimRot : Vector3.zero), ref swayVel, swaySmoothTime);

        if (offsetPos != Vector3.zero) offsetPos = Vector3.SmoothDamp(offsetPos, Vector3.zero, ref switchPosVel, switchPosTime);
        if (offsetRot != Vector3.zero) offsetRot = Vector3.SmoothDamp(offsetRot, Vector3.zero, ref switchRotVel, switchRotTime);
    }
    #endregion
}
