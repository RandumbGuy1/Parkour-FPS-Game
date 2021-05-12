using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();
    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
    }

    void Update()
    {
        if (s.PlayerInput.dropping) Drop();
    }

    public void AddWeapon(GameObject obj)
    {
        weapons.Add(obj);
        obj.GetComponent<Rigidbody>().isKinematic = true;
    }

    public void Drop()
    {
        Rigidbody rb = weapons[0].GetComponent<Rigidbody>();

        weapons[0].transform.SetParent(null);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;

        weapons[0].GetComponent<WeaponPickup>().pickedUp = false;
    }
}
