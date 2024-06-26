﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Sword : MonoBehaviour, IItem, IWeapon
{
    public PlayerManager Player { get { return s; } }
    public WeaponClass WeaponType { get { return type; } }

    public bool Automatic { get { return weaponAutomatic; } }

    public float RecoilSmoothTime { get { return swingSmoothing; } }
    public float ReloadSmoothTime { get; }

    public ShakeData RecoilShakeData { get { return recoilShake; } }

    [Header("Weapon Class")]
    [SerializeField] private WeaponClass type;
    [SerializeField] private bool weaponAutomatic;

    [Header("Weapon Artwork")]
    [SerializeField] private ItemArtSettings spriteSettings;
    public ItemArtSettings SpriteSettings => spriteSettings;

    [Header("Weapon Holding Settings")]
    [SerializeField] private HoldingSettings swaySettings;
    public HoldingSettings SwaySettings => swaySettings;

    [Header("Attack Settings")]
    [SerializeField] private float damage;
    [SerializeField] private float swingForce;
    [SerializeField] private float hitboxActive;
    [SerializeField] private LayerMask AttackLayer;

    [Header("Swing Settings")]
    [SerializeField] private float swingSmoothing;
    [SerializeField] private Vector3 swingPosOffset;
    [SerializeField] private Vector3 swingRotOffset;
    [SerializeField] private ShakeData recoilShake;

    private Vector3 desiredSpinOffset;
    private Vector3 swingSpinOffset;
    private int timesAttacked = 0;

    [Header("Assignables")]
    [SerializeField] private Transform attackPoint;
    private Rigidbody rb;
    private BoxCollider bc;
    private PlayerManager s;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bc = GetComponent<BoxCollider>();
    }

    void OnDisable()
    {
        bc.isTrigger = false;
        StopAllCoroutines();
        StopAllCoroutines();
        rb.detectCollisions = false;
    }

    public bool OnAttack()
    {
        if (bc.isTrigger && rb.detectCollisions) return false;

        s.WeaponControls.AddRecoil(swingPosOffset, swingRotOffset);
        timesAttacked++;

        desiredSpinOffset = Vector3.up * 5f + Vector3.right * 2f;

        StopAllCoroutines();
        StartCoroutine(SetAttackHitBox());
        return true;
    }

    private IEnumerator SetAttackHitBox()
    {
        yield return new WaitForSeconds(0.01f);

        bc.isTrigger = true;
        rb.detectCollisions = true;

        yield return new WaitForSeconds(hitboxActive);

        bc.isTrigger = false;
        rb.detectCollisions = false;
    }

    void OnTriggerEnter(Collider col)
    {
        int layer = col.gameObject.layer;
        if (AttackLayer != (AttackLayer | 1 << layer)) return;

        if (s == null) return;
        if (col.gameObject == s.gameObject) return;

        Rigidbody rb = col.GetComponent<Rigidbody>();
        col.gameObject.GetComponent<IDamagable>()?.OnDamage(damage, s);

        if (rb != null)
        {
            Vector3 dirToLaunch = (col.transform.position - transform.position).normalized * 0.3f + 0.3f * 2f * Vector3.up + s.cam.transform.forward;
            rb.AddForce(dirToLaunch * swingForce, ForceMode.VelocityChange);
            if (!rb.freezeRotation) rb.AddTorque(dirToLaunch * swingForce, ForceMode.VelocityChange);
        }
    }

    public bool SecondaryAction() => true;
    public void OnPickup(PlayerManager s) => this.s = s;

    public void ItemUpdate() 
    {
        if (timesAttacked <= 0) return;

        desiredSpinOffset = Vector3.Lerp(desiredSpinOffset, Vector3.zero, 3f * Time.deltaTime);
        swingSpinOffset = Vector3.Lerp(swingSpinOffset, desiredSpinOffset, 8f * Time.deltaTime);

        transform.localPosition = swingSpinOffset;
    }

    public void OnDrop()
    {
        StopAllCoroutines();

        bc.isTrigger = false;
        rb.detectCollisions = true;
        timesAttacked = 0;

        s = null;
    }

    public string ReadData() => " ";
    public string ReadName() => transform.name;
}
