using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : Interactable
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupPositionTime;
    [SerializeField] private float pickupRotationSpeed;

    private Vector3 vel;

    [Header("Interaction Hint")]
    [SerializeField] private string description;

    private Transform weaponRot;
    private Transform weaponPos;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb.isKinematic)
        {
            Vector3 targetPos = weaponPos.position;

            if (transform.position != targetPos)
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref vel, pickupPositionTime);

            Quaternion targetRot = weaponRot.rotation;

            if (transform.rotation != targetRot)
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, pickupRotationSpeed * Time.deltaTime);
        }
    }

    public override string GetDescription()
    {
        if (!rb.isKinematic) return description;
        else return null;
    }

    public override void OnInteract()
    {
        transform.SetParent(weaponPos);
    }

    public void SetTransform(Transform weaponPos, Transform weaponRot)
    {
        this.weaponPos = weaponPos;
        this.weaponRot = weaponPos;
    }
}
