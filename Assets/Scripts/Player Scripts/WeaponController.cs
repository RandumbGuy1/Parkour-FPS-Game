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
    private bool shouldBobGun = false;

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

    [Header("Weapons Equipped")]
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();

    [Header("Assignables")]
    [SerializeField] private Transform weaponPos;

    private ScriptManager s;

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

            if (Input.GetKeyDown(KeyCode.Alpha1))
                selectedWeapon = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2) && weapons.Count >= 2)
                selectedWeapon = 1;
        }

        if (s.PlayerInput.rightClick) aiming = !aiming;

        if (selectedWeapon != previousWeapon) SelectWeapon();
    }

    void LateUpdate()
    {
        shouldBobGun = s.PlayerInput.moving && s.PlayerInput.grounded && !s.PlayerInput.crouching && s.magnitude > 10f;

        if (!shouldBobGun) timer = 0f;
        else timer += Time.deltaTime;

        smoothBob = Vector3.SmoothDamp(smoothBob, CalculateBob() + (aiming ? aimPos : Vector3.zero), ref bobVel, bobSmoothTime);
        smoothSway = Vector3.SmoothDamp(smoothSway, CalculateSway() + (aiming ? aimRot : Vector3.zero), ref swayVel, swaySmoothTime);

        Vector3 newPos = defaultPos + smoothBob;
        Quaternion newRot = Quaternion.Euler(defaultRot + smoothSway);

        weaponPos.localPosition = newPos;
        weaponPos.localRotation = newRot;
    }

    public void AddWeapon(GameObject obj)
    {
        weapons.Add(obj);
        obj.transform.SetParent(weaponPos);
 
        selectedWeapon = weapons.Count - 1;
        SelectWeapon();
    }

    private Vector3 CalculateBob()
    {
        Vector3 offset = Vector3.zero;

        if (weapons.Count > 0)
        {
            if (timer > 0)
            {
                float horizOffset = 0f;
                float vertOffset = 0f;

                horizOffset = Mathf.Cos(timer * bobSpeed) * bobAmountHoriz;
                vertOffset = Mathf.Sin(timer * bobSpeed * 2) * bobAmountVert;

                offset += weaponPos.forward * horizOffset + Vector3.up * vertOffset;
            }
        }

        return offset;
    }

    private Vector3 CalculateSway()
    {
        Vector3 offset = Vector3.zero;

        if (weapons.Count > 0)
        {
            float strafeRot = s.PlayerInput.input.x * swayAmount * 1.8f;
            float camRot = s.CameraInput.rotationDelta.y * swayAmount;
            camRot = Mathf.Clamp(camRot, -60, 90);

            offset += (Vector3.up * (camRot - strafeRot));
        }

        return offset;
    }

    private void SelectWeapon()
    {
        for (int i = 0; i < weapons.Count; i++) 
            weapons[i].SetActive(i == selectedWeapon);
    }

    private void Drop()
    {
        Rigidbody rb = weapons[selectedWeapon].GetComponent<Rigidbody>();

        weapons[selectedWeapon].transform.SetParent(null);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.AddForce(s.cam.transform.forward * throwForce * ((s.magnitude * 0.1f) + 1f), ForceMode.VelocityChange);

        Vector3 rand = Vector3.zero;

        rand.x = Random.Range(-1f, 1f);
        rand.y = Random.Range(-1f, 1f);
        rand.z = Random.Range(-1f, 1f);

        rb.AddTorque(rand * throwForce, ForceMode.VelocityChange);

        weapons[selectedWeapon].SetActive(true);
        weapons.RemoveAt(selectedWeapon);

        if (weapons.Count > 0)
        {
            selectedWeapon = weapons.Count - 1;
            SelectWeapon();
        }
    }
}
