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

    private Rigidbody rb;
    private TrailRenderer tr;

    public ProjectileType BulletType { get { return ProjectileType.Bullet; } }
    public float LifeTime { get { return bulletLifeTime; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tr = GetComponent<TrailRenderer>();

        rb.detectCollisions = false;
    }

    public void OnShoot(ScriptManager shooter, Vector3 shooterPos, Vector3 targetPoint, Vector3 targetNormal, Vector3 velocity, float shootForce)
    {
        tr.Clear();
        /*
        if (Vector3.Dot(velocity.normalized, shooter != null ? shooter.cam.forward : (transform.position - shooterPos).normalized) < 0.3f)
        {
            Explode(targetPoint, targetNormal, true);
            return;
        }
        */
        float distanceForce = (velocity.magnitude * 0.1f);
        distanceForce = Mathf.Clamp(distanceForce, 0, 3f);

        float playerVelocity = 1f;

        if (shooter != null)
        {
            playerVelocity = shooter.PlayerMovement.Magnitude * 0.1f;
            playerVelocity = Mathf.Clamp(playerVelocity, 1f, 1.1f);

            if (Vector3.Dot(shooter.PlayerMovement.Velocity, transform.up) < -0.1f) playerVelocity = 1f;
        }

        transform.up = velocity;
        rb.velocity = Vector3.zero;

        rb.AddForce(35f * distanceForce * playerVelocity * shootForce * transform.up, ForceMode.Impulse);

        StopAllCoroutines();
        StartCoroutine(CheckForBulletCollisions());
    }

    private IEnumerator CheckForBulletCollisions()
    {
        Vector3 lastPos = rb.position;
        float bulletElapsed = 0f;

        while (gameObject.activeSelf)
        {
            if (bulletElapsed > bulletLifeTime)
            {
                Explode();
                break;
            }

            Vector3 lastToNowPos = rb.position - lastPos;

            if (Physics.SphereCast(lastPos + lastToNowPos.normalized, 0.1f, lastToNowPos.normalized, out var hit, lastToNowPos.magnitude * 1.05f, CollidesWith))
            {
                Explode(hit.point, hit.normal, true);
                break;
            }

            bulletElapsed += Time.fixedDeltaTime;
            lastPos = rb.position;

            yield return new WaitForFixedUpdate();
        }
    }

    void Explode(Vector3 point = default, Vector3 normal = default, bool collided = false)
    {
        if (!gameObject.activeSelf) return;

        rb.detectCollisions = false;
        gameObject.SetActive(false);

        if (!collided) return;

        ObjectPooler.Instance.SpawnParticle(impactEffect, point + normal * 0.4f, Quaternion.LookRotation(normal));
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
}
