using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletProjectile : MonoBehaviour, IProjectile
{
    [Header("Projectile Settings")]
    [SerializeField] private ProjectileType type;
    [SerializeField] private float bulletLifeTime;
    [SerializeField] private int maxCollisions;
    private int collisionCount = 0;

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
        playerVelocity = Mathf.Clamp(playerVelocity, 1f, 1.35f);

        transform.up = velocity;

        if (Vector3.Dot(shooter.PlayerMovement.Velocity, transform.up) < -0.1f) playerVelocity = 1f;

        rb.velocity = Vector3.zero;
        rb.AddForce(transform.up * distanceForce * shootForce * 25f * playerVelocity, ForceMode.Impulse);

        CancelInvoke("Explode");
        Invoke("Explode", bulletLifeTime);
    }

    void OnCollisionEnter(Collision col)
    {
        collisionCount++;
        if (collisionCount < maxCollisions) return;

        CancelInvoke("Explode");
        Invoke("Explode", 0.005f);

        ContactPoint contact = col.GetContact(0);
        ObjectPooler.Instance.SpawnParticle("LaserImpactFX", transform.position, Quaternion.LookRotation(contact.normal));
    }

    void Explode()
    {
        if (!gameObject.activeSelf) return;

        rb.detectCollisions = false;
        gameObject.SetActive(false);
    }
}
