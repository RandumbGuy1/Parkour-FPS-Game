﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BulletProjectile : MonoBehaviour, IProjectile
{
    [Header("Projectile Settings")]
    [SerializeField] private LayerMask CollidesWith;
    [SerializeField] private float explosionRadius;
    [SerializeField] private float explosionForce;
    [SerializeField] private float bulletLifeTime;
    [Space(10)]
    [SerializeField] private string impactEffect;
    [SerializeField] private float impactDestroyTime;
    private bool exploded = false;

    private float bulletDamage;
    private Transform shooter;

    [Header("Assignables")]
    [SerializeField] private MeshRenderer bulletGfx;
    [SerializeField] private new GameObject light;
    [SerializeField] private List<TrailRenderer> trails = new List<TrailRenderer>();
    private Rigidbody rb;

    public ProjectileType BulletType { get { return ProjectileType.Bullet; } }
    public float LifeTime { get { return bulletLifeTime; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.detectCollisions = false;
    }

    public void OnShoot(Transform shooter, RaycastHit target, Vector3 velocity, float bulletDamage, PlayerManager s = null, bool bulletClip = false)
    {
        foreach (TrailRenderer trail in trails) trail.Clear();

        bulletGfx.enabled = true;
        light.SetActive(true);

        rb.isKinematic = false;
        exploded = false;

        this.shooter = shooter;
        this.bulletDamage = bulletDamage;

        if (bulletClip)
        {
            Explode(target, true);
            return;
        }

        transform.up = velocity;
        rb.velocity = Vector3.zero;

        if (s != null)
        {
            float playerVelocity = s.PlayerMovement.Magnitude * 0.1f;
            playerVelocity = Mathf.Clamp(playerVelocity, 1f, 1.1f);

            if (Vector3.Dot(s.PlayerMovement.Velocity, transform.up) < -0.1f) playerVelocity = 1f;

            velocity *= playerVelocity;
        }

        rb.AddForce(40f * velocity, ForceMode.Impulse);

        StopAllCoroutines();
        StartCoroutine(CheckForBulletCollisions());
    }

    private IEnumerator CheckForBulletCollisions()
    {
        float bulletElapsed = 0f;

        while (gameObject.activeSelf)
        {
            if (bulletElapsed > bulletLifeTime)
            {
                Explode();
                break;
            }

            Vector3 fixedVelocity = rb.velocity * Time.fixedDeltaTime;
            if (Physics.SphereCast(rb.position - fixedVelocity, 0.1f, fixedVelocity.normalized, out var hit, fixedVelocity.magnitude, CollidesWith))
            {
                Explode(hit, true);
                break;
            }

            bulletElapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    void Explode(RaycastHit hit = default, bool collided = false)
    {
        if (exploded) return;

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        exploded = true;
        bulletGfx.enabled = false;
        light.SetActive(false);

        Invoke(nameof(DeactivateBullet), impactDestroyTime);

        if (!collided) return;

        transform.position = hit.point + hit.normal * 0.2f;

        GameObject explosion = ObjectPooler.Instance.Spawn(impactEffect, hit.point + hit.normal * 0.4f, hit.normal != Vector3.zero ? Quaternion.LookRotation(hit.normal) : Quaternion.identity);
        explosion.GetComponent<Explosion>()?.Explode(shooter.gameObject, CollidesWith, ForceMode.Impulse, hit.point, explosionRadius, explosionForce, 0.1f, bulletDamage);
    }

    void DeactivateBullet() => gameObject.SetActive(false);
}
