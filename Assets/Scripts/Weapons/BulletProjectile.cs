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

        float playerVelocity = 1f;

        if (shooter != null)
        {
            playerVelocity = shooter.PlayerMovement.Magnitude * 0.1f;
            playerVelocity = Mathf.Clamp(playerVelocity, 1f, 1.2f);

            if (Vector3.Dot(shooter.PlayerMovement.Velocity, transform.up) < -0.1f) playerVelocity = 1f;
        }

        transform.up = velocity;

        rb.velocity = Vector3.zero;
        rb.AddForce(25f * distanceForce * playerVelocity * shootForce * transform.up, ForceMode.Impulse);

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
        impactPoint = contact.point - contact.normal * 0.1f;

        ObjectPooler.Instance.SpawnParticle(impactEffect, transform.position, Quaternion.LookRotation(contact.normal));
    }

    void Explode()
    {
        if (!gameObject.activeSelf) return;

        rb.detectCollisions = false;
        gameObject.SetActive(false);

        if (type != ProjectileType.Grenade) return;

        Collider[] enemiesInRadius = Physics.OverlapSphere(impactPoint, 10f, Collides);

        for (int i = 0; i < enemiesInRadius.Length; i++)
        {
            ScriptManager s = enemiesInRadius[i].gameObject.GetComponent<ScriptManager>();
            Rigidbody rb;

            if (s == null) rb = enemiesInRadius[i].gameObject.GetComponent<Rigidbody>();
            else rb = s.rb;

            if (rb == null) continue;
            if (s != null)
            {
                s.PlayerMovement.ResetJumpSteps();
                s.CameraShaker.ShakeOnce(10f, 4f, 1.5f, 4f, ShakeData.ShakeType.Perlin);
            }

            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.5f, rb.velocity.z);
            rb.AddExplosionForce(55f, impactPoint, 15f, 1.5f, ForceMode.VelocityChange);
        }
    }
}
