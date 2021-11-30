using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletProjectile : MonoBehaviour, IProjectile
{
    [Header("Projectile Settings")]
    [SerializeField] private ProjectileType type;
    [SerializeField] private LayerMask Collides;
    [SerializeField] private float bulletLifeTime;
    [SerializeField] private string impactEffect;
    [SerializeField] private int maxCollisions;
    private int collisionCount = 0;

    private Vector3 impactPoint = Vector3.zero;

    private Rigidbody rb;
    private TrailRenderer tr;

    public ProjectileType bulletType { get { return type; } }
    public float lifeTime { get { return bulletLifeTime; } }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tr = GetComponent<TrailRenderer>();
    }

    public void OnShoot(ScriptManager shooter, Vector3 velocity, float shootForce)
    {
        tr.Clear();
        collisionCount = 0;
        rb.detectCollisions = true;

        float distanceForce = (velocity.magnitude * 0.1f);
        distanceForce = Mathf.Clamp(distanceForce, 0, 3f);

        float playerVelocity = shooter.PlayerMovement.Magnitude * 0.1f;
        playerVelocity = Mathf.Clamp(playerVelocity, 1f, 1.2f);

        transform.up = velocity;

        if (Vector3.Dot(shooter.PlayerMovement.Velocity, transform.up) < -0.1f) playerVelocity = 1f;

        rb.velocity = Vector3.zero;
        rb.AddForce(transform.up * distanceForce * shootForce * 25f * playerVelocity, ForceMode.Impulse);

        CancelInvoke("Explode");
        Invoke("Explode", bulletLifeTime);
    }

    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Collides != (Collides | 1 << layer)) return;

        collisionCount++;
        if (collisionCount < maxCollisions) return;

        CancelInvoke("Explode");
        Invoke("Explode", 0.005f);

        ContactPoint contact = col.GetContact(0);
        impactPoint = contact.point + contact.normal * 0.1f; 

        ObjectPooler.Instance.SpawnParticle(impactEffect, transform.position, Quaternion.LookRotation(contact.normal));
    }

    void Explode()
    {
        if (!gameObject.activeSelf) return;

        rb.detectCollisions = false;
        gameObject.SetActive(false);

        if (type != ProjectileType.Grenade) return;

        Collider[] enemiesInRadius = Physics.OverlapSphere(impactPoint, 5f, Collides);

        for (int i = 0; i < enemiesInRadius.Length; i++)
        {
            Rigidbody rb = enemiesInRadius[i].GetComponent<Rigidbody>();
            if (rb == null) continue;

            rb.AddExplosionForce(120f, impactPoint, 20f, 1.1f, ForceMode.VelocityChange);
        }
    }
}
