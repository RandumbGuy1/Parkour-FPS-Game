using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BulletProjectile : MonoBehaviour, IProjectile
{
    [Header("Projectile Settings")]
    [SerializeField] private float explosionRadius;
    [SerializeField] private float explosionForce;
    [SerializeField] private float bulletLifeTime;
    [SerializeField] private string impactEffect;
    private bool exploded = false;

    [Header("Assignables")]
    [SerializeField] private GameObject bulletGfx;
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

    public void OnShoot(Transform shooter, RaycastHit target, Vector3 velocity, LayerMask CollidesWith, float bulletDamage, ScriptManager s = null, bool bulletClip = false)
    {
        foreach (TrailRenderer trail in trails) trail.Clear();
        light.SetActive(true);

        rb.isKinematic = false;
        exploded = false;

        if (bulletClip)
        {
            Explode(CollidesWith, target, true, bulletDamage, 0f);
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
        StartCoroutine(CheckForBulletCollisions(bulletDamage, CollidesWith));
    }

    private IEnumerator CheckForBulletCollisions(float damage, LayerMask CollidesWith)
    {
        float bulletElapsed = 0f;

        while (gameObject.activeSelf)
        {
            if (bulletElapsed > bulletLifeTime)
            {
                Explode(CollidesWith);
                break;
            }

            Vector3 fixedVelocity = rb.velocity * Time.fixedDeltaTime;
            if (Physics.SphereCast(rb.position - fixedVelocity, 0.1f, fixedVelocity.normalized, out var hit, fixedVelocity.magnitude, CollidesWith))
            {
                Explode(CollidesWith, hit, true, damage);
                break;
            }

            bulletElapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    void Explode(LayerMask CollidesWith, RaycastHit hit = default, bool collided = false, float damage = 0f, float destroyDelay = 0.2f)
    {
        if (exploded) return;

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        exploded = true;;

        Invoke(nameof(DeacivateLight), 0.03f);
        Invoke(nameof(DeactivateBullet), destroyDelay);

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
