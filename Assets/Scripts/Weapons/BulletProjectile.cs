using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BulletProjectile : MonoBehaviour, IProjectile
{
    [Header("Projectile Settings")]
    [SerializeField] private new GameObject light;
    [SerializeField] private LayerMask CollidesWith;
    [SerializeField] private float explosionRadius;
    [SerializeField] private float explosionForce;
    [SerializeField] private float bulletLifeTime;
    [SerializeField] private string impactEffect;
    private bool exploded = false;
    private float damage = 0;

    [Header("Assignables")]
    [SerializeField] private List<TrailRenderer> trails = new List<TrailRenderer>();
    private Rigidbody rb;

    public ProjectileType BulletType { get { return ProjectileType.Bullet; } }
    public float LifeTime { get { return bulletLifeTime; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.detectCollisions = false;
    }

    public void OnShoot(ScriptManager shooter, RaycastHit target, Vector3 velocity, float shootForce, float bulletDamage)
    {
        foreach (TrailRenderer trail in trails) trail.Clear();
        light.SetActive(true);

        rb.isKinematic = false;

        exploded = false;
        damage = bulletDamage;

        float playerVelocity = 1f;

        if (shooter != null)
        {
            playerVelocity = shooter.PlayerMovement.Magnitude * 0.1f;
            playerVelocity = Mathf.Clamp(playerVelocity, 1f, 1.1f);

            if (Vector3.Dot(shooter.PlayerMovement.Velocity, transform.up) < -0.1f) playerVelocity = 1f;
        }

        transform.up = velocity;
        rb.velocity = Vector3.zero;

        rb.AddForce(40f * playerVelocity * shootForce * transform.up, ForceMode.Impulse);

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

        exploded = true;;

        Invoke(nameof(DeacivateLight), 0.03f);
        Invoke(nameof(DeactivateBullet), 0.2f);

        if (!collided) return;

        transform.position = hit.point + hit.normal * 0.1f;

        ObjectPooler.Instance.SpawnParticle(impactEffect, hit.point + hit.normal * 0.4f, hit.normal != Vector3.zero ? Quaternion.LookRotation(hit.normal) : Quaternion.identity);
        Collider[] enemiesInRadius = Physics.OverlapSphere(hit.point, explosionRadius, CollidesWith);

        for (int i = 0; i < enemiesInRadius.Length; i++)
        {
            ScriptManager s = enemiesInRadius[i].gameObject.GetComponent<ScriptManager>();
            Rigidbody rb = enemiesInRadius[i].gameObject.GetComponent<Rigidbody>();
            IDamagable damageable = enemiesInRadius[i].gameObject.GetComponent<IDamagable>();

            if (damageable != null) damageable.OnDamage(damage);

            if (s != null)
            {
                s.PlayerMovement.ResetJumpSteps();
                s.CameraShaker.ShakeOnce(10f, 4f, 1.5f, 4f, ShakeData.ShakeType.Perlin);
            }

            if (rb == null) continue;

            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.5f, rb.velocity.z);
            rb.AddExplosionForce(explosionForce, hit.point, explosionRadius * 1.5f, 0f, ForceMode.Impulse);
        }
    }

    void DeactivateBullet() => gameObject.SetActive(false);
    void DeacivateLight() => light.SetActive(false);
}
