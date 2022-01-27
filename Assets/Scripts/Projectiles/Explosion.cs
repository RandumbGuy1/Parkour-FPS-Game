using UnityEngine;

public class Explosion : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private LayerMask CollidesWith;
    [SerializeField] private float explosionRadius;
    [SerializeField] private float explosionForce;
    [SerializeField] private float damage;
    [SerializeField] private bool explodeOnEnable;

    void OnEnable()
    {
        if (explodeOnEnable) Explode(gameObject, CollidesWith, ForceMode.Impulse, transform.position, explosionRadius, explosionForce, 1f, damage);
    }

    public void Explode(GameObject shooter, LayerMask CollidesWith, ForceMode forceMode, Vector3 point,
        float explosionRadius, float explosionForce, float upwardsModifier, float damage, bool applyForceToShooter = false)
    {
        Collider[] enemiesInRadius = Physics.OverlapSphere(point, explosionRadius, CollidesWith);

        for (int i = 0; i < enemiesInRadius.Length; i++)
        {
            Transform enemy = enemiesInRadius[i].transform;
            if (applyForceToShooter && shooter != null && enemy == shooter.transform) continue;

            Rigidbody rb = enemy.gameObject.GetComponent<Rigidbody>();
            enemy.GetComponent<ScriptManager>()?.PlayerMovement.ResetJumpSteps();
            if (shooter != null && enemy != shooter.transform) enemy.GetComponent<IDamagable>()?.OnDamage(damage, shooter.GetComponent<ScriptManager>());

            if (rb == null) continue;

            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.5f, rb.velocity.z);
            rb.AddExplosionForce(explosionForce, point, explosionRadius * 1.5f, upwardsModifier, forceMode);
            if (!rb.freezeRotation) rb.AddTorque(1.5f * explosionForce * (enemy.position - point), forceMode);
        }
    }
}
