using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : Interactable
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupPositionTime;
    [SerializeField] private float pickupRotationSpeed;

    private Vector3 vel;
    public bool pickedUp = false;

    [Header("Interaction Hint")]
    [SerializeField] private string description;

    [Header("Assignables")]
    [SerializeField] private Transform weaponPos;
    [SerializeField] private Transform cam;

    void Update()
    {
        if (pickedUp)
        {
            Vector3 targetPos = weaponPos.position;

            if (transform.position != targetPos)
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref vel, pickupPositionTime);

            Quaternion targetRot = cam.rotation;

            if (transform.rotation != targetRot)
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, pickupRotationSpeed * Time.deltaTime);
        }
    }

    public override string GetDescription()
    {
        if (!pickedUp) return description;
        else return null;
    }

    public override void OnInteract()
    {
        transform.SetParent(weaponPos);
        pickedUp = true;
    }
}
