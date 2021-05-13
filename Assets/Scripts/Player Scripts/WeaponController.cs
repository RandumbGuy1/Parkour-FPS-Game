using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Equip Settings")]
    [SerializeField] private float throwForce;
    [SerializeField] private float weaponSwitchDelay;
    [SerializeField] private int selectedWeapon;

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

        if (selectedWeapon != previousWeapon) SelectWeapon();
    }

    public void AddWeapon(GameObject obj)
    {
        weapons.Add(obj);
        obj.GetComponent<Rigidbody>().isKinematic = true;
        obj.GetComponent<WeaponPickup>().SetTransform(weaponPos, s.cam);

        selectedWeapon = weapons.Count - 1;
        SelectWeapon();
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

        float randX = Random.Range(-1f, 1f);
        float randY = Random.Range(-1f, 1f);
        float randZ = Random.Range(-1f, 1f);

        Vector3 dir = new Vector3(randX, randY, randZ);

        rb.AddTorque(dir * throwForce, ForceMode.VelocityChange);

        weapons[selectedWeapon].GetComponent<WeaponPickup>().pickedUp = false;
        weapons.RemoveAt(selectedWeapon);

        if (weapons.Count > 0)
        {
            selectedWeapon = weapons.Count - 1;
            SelectWeapon();
        }
    }
}
