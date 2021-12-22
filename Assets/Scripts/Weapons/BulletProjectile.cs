using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletProjectile : MonoBehaviour, IProjectile
{
    [Header("Projectile Settings")]
    [SerializeField] private LayerMask CollidesWith;
    [SerializeField] private float explosionRadius;
    [SerializeField] private float explosionForce;
    [SerializeField] private float bulletLifeTime;
    [SerializeField] private string impactEffect;
    private bool exploded = false;

    [SerializeField] private List<TrailRenderer> trails = new List<TrailRenderer>();
    private Rigidbody rb;
    private MeshRenderer mr;

    public ProjectileType BulletType { get { return ProjectileType.Bullet; } }
    public float LifeTime { get { return bulletLifeTime; } }

    void Awake()
    {
        mr = GetComponentInChildren<MeshRenderer>();
        rb = GetComponent<Rigidbody>();
        rb.detectCollisions = false;
    }

    public void OnShoot(ScriptManager shooter, RaycastHit target, Vector3 velocity, float shootForce)
    {
        foreach (TrailRenderer trail in trails) trail.Clear();
        exploded = false;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        /*
        if (targetNormal != Vector3.zero)
        {
            if (Vector3.Dot((targetPoint - transform.position).normalized, -targetNormal.normalized) < 0.3f)
            {
                Explode(targetPoint, targetNormal, true);
                return;
            }
        }
        */
        //float distanceForce = (velocity.magnitude * 0.1f);
        //distanceForce = Mathf.Clamp(distanceForce, 0, 3f);
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
                Explode(hit.point, hit.normal, true);
                break;
            }

            bulletElapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    void Explode(Vector3 point = default, Vector3 normal = default, bool collided = false)
    {
        if (exploded) return;

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        rb.detectCollisions = false;
        rb.velocity = Vector3.zero;
        exploded = true;
        Invoke(nameof(DeactivateBullet), 0.15f);

        if (!collided) return;

        ObjectPooler.Instance.SpawnParticle(impactEffect, point + normal * 0.4f, normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity);
        Collider[] enemiesInRadius = Physics.OverlapSphere(point, explosionRadius, CollidesWith);

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
            rb.AddExplosionForce(explosionForce, point, explosionRadius * 1.5f, 0f, ForceMode.Impulse);
        }
    }

    void DeactivateBullet()
    {
        gameObject.SetActive(false);
    }
}
