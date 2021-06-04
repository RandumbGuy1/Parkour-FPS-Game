using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Equip Settings")]
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

        if (weapons.Count > 0)
        {
            if (s.PlayerInput.dropping) Drop();
            if (Input.GetKeyDown(KeyCode.Alpha1)) selectedWeapon = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2) && weapons.Count >= 2) selectedWeapon = 1;

            timer = s.PlayerInput.moving && s.PlayerInput.grounded && !s.PlayerInput.crouching && s.magnitude > 5f ? timer += Time.deltaTime : 0f;

            smoothBob = Vector3.SmoothDamp(smoothBob, CalculateBob() + (aiming ? aimPos : Vector3.zero), ref bobVel, bobSmoothTime);
            smoothSway = Vector3.SmoothDamp(smoothSway, CalculateSway() + (aiming ? aimRot : Vector3.zero), ref swayVel, swaySmoothTime);

            if (offsetPos != Vector3.zero) offsetPos = Vector3.SmoothDamp(offsetPos, Vector3.zero, ref switchPosVel, switchPosTime);
            if (offsetRot != Vector3.zero) offsetRot = Vector3.SmoothDamp(offsetRot, Vector3.zero, ref switchRotVel, switchRotTime);

            if (selectedWeapon != previousWeapon) SelectWeapon(true);
        }

        if (s.PlayerInput.rightClick) aiming = !aiming;
    }

    void LateUpdate()
    {
        Vector3 newPos = defaultPos + smoothBob + offsetPos;
        Quaternion newRot = Quaternion.Euler(defaultRot + smoothSway + offsetRot);

        weaponPos.localPosition = newPos;
        weaponPos.localRotation = newRot;
    }

    public void AddWeapon(GameObject obj)
    {
        weapons.Add(obj);
        obj.transform.SetParent(weaponPos);

        selectedWeapon = weapons.Count - 1;
        SelectWeapon(false);
    }

    private Vector3 CalculateBob()
    {
        Vector3 offset = Vector3.zero;

        if (weapons.Count > 0)
            if (timer > 0) offset += (Vector3.right * Mathf.Cos(timer * bobSpeed) * bobAmountHoriz) + (Vector3.up * Mathf.Sin(timer * bobSpeed * 2) * bobAmountVert);

        return offset;
    }

    private Vector3 CalculateSway()
    {
        Vector3 offset = Vector3.zero;

        if (weapons.Count > 0)
        {
            float sideStrafeRot = s.PlayerInput.input.x * swayAmount;
            float camDelta = s.CameraLook.rotationDelta.y * swayAmount * 1.3f;
            camDelta = Mathf.Clamp(camDelta, -60, 90);

            offset += (Vector3.up * (camDelta - sideStrafeRot));
        }

        return offset;
    }

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
        rb.AddForce(s.cam.transform.forward * throwForce * ((s.magnitude * 0.09f) + 1f), ForceMode.VelocityChange);

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
}
