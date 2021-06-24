﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileGun : Weapon
{
    [Header("Shooting Settings")]
    [SerializeField] private float shootForce;
    [SerializeField] private float attackRange;
    [SerializeField] private float spread;
    [SerializeField] private float fireRate;

    [Header("Reload Settings")]
    [SerializeField] private int magazineSize;
    [SerializeField] private int bulletsPerTap;
    [SerializeField] private float reloadTime;

    private int bulletsLeft;
    private bool readyToShoot = true;

    [Header("Collision")]
    [SerializeField] private LayerMask Environment;

    [Header("Assignables")]
    [SerializeField] private Transform attackPoint;

    void Start()
    {
        bulletsLeft = magazineSize;
    }

    public override bool OnAttack(Transform cam)
    {
        if (!readyToShoot) return false;

        if (bulletsLeft <= 0)
        {
            SecondaryAction();
            return false;
        }

        Vector3 targetPoint = Vector3.zero;
        Ray ray = cam.GetComponent<Camera>().ViewportPointToRay((Vector3) Vector2.one * 0.5f);

        if (Physics.Raycast(ray, out var hit, attackRange, Environment)) targetPoint = hit.point;
        else targetPoint = ray.GetPoint(attackRange);

        Vector3 dir = (targetPoint - attackPoint.position).normalized;

        for (int i = 0; i < bulletsPerTap; i++)
        {
            bulletsLeft--;

            Vector2 rand = Vector3.zero;
            rand.x = Random.Range(-1f, 1f) * spread * 0.01f;
            rand.y = Random.Range(-1f, 1f) * spread * 0.01f;

            Vector3 spreadDir = dir + (cam.right * rand.x) + (Vector3.up * rand.y);

            GameObject bullet = ObjectPooler.Instance.Spawn("Bullet", attackPoint.position, Quaternion.identity);     
            
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.AddForce(spreadDir * shootForce * 0.9f, ForceMode.Impulse);
        }

        if (readyToShoot)
        {
            readyToShoot = false;
            Invoke("ResetShot", 1 / fireRate);
        }

        return true;
    }

    public override void SecondaryAction()
    {
        StartCoroutine(Reload());
    }

    private IEnumerator Reload()
    {
        yield return new WaitForSeconds(reloadTime);

        bulletsLeft = magazineSize;
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }
}
