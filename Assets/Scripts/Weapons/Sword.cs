using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Sword : MonoBehaviour, IItem, IWeapon
{
    public WeaponClass weaponType { get { return type; } }
    public Sprite itemSprite { get { return weaponSprite; } }

    public bool automatic { get { return weaponAutomatic; } }

    public Vector3 defaultPos { get { return weaponDefaultPos; } }
    public Vector3 defaultRot { get { return weaponDefaultRot; } }

    public Vector3 aimPos { get { return weaponAimPos; } }
    public Vector3 aimRot { get { return weaponAimRot; } }

    public float weight { get { return weaponWeight; } }

    public float recoilSmoothTime { get { return swingSmoothing; } }
    public Vector3 recoilPosOffset { get { return swingPosOffset; } }
    public Vector3 recoilRotOffset { get { return swingRotOffset; } }

    public ShakeData recoilShakeData { get { return recoilShake; } }

    public float recoilForce { get; }
    public float reloadSmoothTime { get; }
    public Vector3 reloadRotOffset { get; }

    [Header("Weapon Class")]
    [SerializeField] private WeaponClass type;
    [SerializeField] private bool weaponAutomatic;

    [Header("Weapon Artwork")]
    [SerializeField] private Sprite weaponSprite;

    [Header("Weapon Holding Settings")]
    [SerializeField] private Vector3 weaponDefaultPos;
    [SerializeField] private Vector3 weaponDefaultRot;
    [Space(10)]
    [SerializeField] private Vector3 weaponAimPos;
    [SerializeField] private Vector3 weaponAimRot;
    [Space(10)]
    [SerializeField] private float weaponWeight;

    [Header("Attack Settings")]
    [SerializeField] private float swingForce;
    [SerializeField] private float hitboxActive;
    [SerializeField] private LayerMask AttackLayer;

    [Header("Swing Settings")]
    [SerializeField] private float swingSmoothing;
    [SerializeField] private Vector3 swingPosOffset;
    [SerializeField] private Vector3 swingRotOffset;
    [SerializeField] private ShakeData recoilShake;

    [Header("Assignables")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private BoxCollider bc;
    private Transform cam;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bc = GetComponent<BoxCollider>();
    }

    public bool OnAttack(Transform cam)
    {
        this.cam = cam;

        CancelInvoke("RevertAttackHitBox");
        Invoke("AttackHitBox", 0.05f);

        return true;
    }

    private void AttackHitBox()
    {
        bc.isTrigger = true;
        rb.detectCollisions = true;

        Invoke("RevertAttackHitBox", hitboxActive);
    }

    private void RevertAttackHitBox()
    {
        bc.isTrigger = false;
        rb.detectCollisions = false;
    }

    void OnTriggerEnter(Collider col)
    {
        int layer = col.gameObject.layer;
        if (AttackLayer != (AttackLayer | 1 << layer)) return;

        Rigidbody rb = col.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 dirToLaunch = (col.transform.position - transform.position).normalized * 0.3f + Vector3.up * 2f * 0.3f + cam.forward;

            rb.AddForce(dirToLaunch * swingForce * 0.5f, ForceMode.Impulse);
            rb.AddTorque(dirToLaunch * swingForce * 0.5f, ForceMode.Impulse);
        }
    }
   
    public bool SecondaryAction()
    {
        return true;
    }

    public void OnDrop()
    {
        CancelInvoke("RevertAttackHitBox");

        bc.isTrigger = false;
        rb.detectCollisions = true;
    }

    public string ReadData() => " ";
}
