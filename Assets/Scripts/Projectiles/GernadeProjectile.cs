using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GernadeProjectile : MonoBehaviour, IProjectile
{
    [Header("Projectile Settings")]
    [SerializeField] private LayerMask ExplosionDetects;
    [SerializeField] private LayerMask CollidesWith;
    [SerializeField] private float explosionRadius;
    [SerializeField] private float explosionForce;
    [SerializeField] private float grenadeLifeTime;
    [SerializeField] private float maxCollisions;
    [Space(10)]
    [SerializeField] private string impactEffect;
    [SerializeField] private float impactDestroyTime;
    private bool exploded = false;
    private int collisionCount;
    private float elapsed = 0f;

    private float bulletDamage = 60f;
    private Transform shooter;

    [Header("Assignables")]
    [SerializeField] private PlayerHealth grenadeHealth;
    [SerializeField] private List<TrailRenderer> trails = new List<TrailRenderer>();
    private Rigidbody rb;

    public ProjectileType BulletType { get { return ProjectileType.Bullet; } }
    public float LifeTime { get { return grenadeLifeTime; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grenadeHealth.OnPlayerDamage += HandleDamage;
    }

    public void OnShoot(Transform shooter, RaycastHit target, Vector3 velocity, float bulletDamage, PlayerManager s = null, bool bulletClip = false)
    {
        if (trails.Count > 0) foreach (TrailRenderer trail in trails) trail.Clear();

        collisionCount = 0;
        elapsed = 0f;

        rb.isKinematic = false;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        exploded = false;

        this.shooter = shooter;
        this.bulletDamage = bulletDamage;

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

        StartCoroutine(LifeTimeTimer());
    }

    private void HandleDamage(float damage = 1f)
    {
        if (damage < 0) return;

        collisionCount++;
        if (collisionCount >= maxCollisions) Explode();
    }

    private IEnumerator LifeTimeTimer()
    {
        while (elapsed < grenadeLifeTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        Explode();
    }

    void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (CollidesWith != (CollidesWith | 1 << layer)) return;

        if (elapsed / grenadeLifeTime < 0.25f)
        {
            GernadeProjectile projectile = col.transform.GetComponent<GernadeProjectile>();
            if (projectile != null && projectile.CompareShooter(shooter)) return;
        }

        HandleDamage();
    }

    private void Explode()
    {
        if (exploded) return;

        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        rb.detectCollisions = false;

        exploded = true;
        gameObject.SetActive(false);

        Physics.SphereCast(transform.position, 1.1f, Vector3.zero, out var hit, 0f, ExplosionDetects);

        GameObject explosion = ObjectPooler.Instance.Spawn(impactEffect, transform.position + hit.normal * 0.2f, hit.normal != Vector3.zero ? Quaternion.LookRotation(hit.normal) : Quaternion.identity);
        explosion.GetComponent<Explosion>()?.Explode(shooter != null ? shooter.gameObject : null, CollidesWith, ForceMode.VelocityChange, transform.position - hit.normal * 0.6f, explosionRadius, explosionForce, 1.5f, bulletDamage);
    }

    public bool CompareShooter(Transform shooter)
    {
        if (this.shooter == null) return false;
        return shooter == this.shooter;
    }
}
