using System.Collections;
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
    [SerializeField] private string impactEffect;
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

    public void OnShoot(Transform shooter, RaycastHit target, Vector3 velocity, float bulletDamage, ScriptManager s = null, bool bulletClip = false)
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

        Invoke(nameof(DeacivateLight), 0.03f);
        Invoke(nameof(DeactivateBullet), 0.3f);

        if (!collided) return;

        transform.position = hit.point + hit.normal * 0.2f;

        ObjectPooler.Instance.SpawnParticle(impactEffect, hit.point + hit.normal * 0.4f, hit.normal != Vector3.zero ? Quaternion.LookRotation(hit.normal) : Quaternion.identity);
        Collider[] enemiesInRadius = Physics.OverlapSphere(hit.point, explosionRadius, CollidesWith);

        for (int i = 0; i < enemiesInRadius.Length; i++)
        {
            if (enemiesInRadius[i].transform == shooter.transform) continue;

            Rigidbody rb = enemiesInRadius[i].gameObject.GetComponent<Rigidbody>();

            enemiesInRadius[i].GetComponent<IDamagable>()?.OnDamage(bulletDamage);
            enemiesInRadius[i].GetComponent<ScriptManager>()?.PlayerMovement.ResetJumpSteps();

            if (rb == null) continue;

            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.5f, rb.velocity.z);
            rb.AddExplosionForce(explosionForce, hit.point, explosionRadius * 1.5f, 0f, ForceMode.Impulse);
        }
    }

    void DeactivateBullet() => gameObject.SetActive(false);
    void DeacivateLight() => light.SetActive(false);
}
