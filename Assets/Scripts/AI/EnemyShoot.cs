using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private LayerMask CollideAttack;
    [SerializeField] private LayerMask Obstruction;
    [SerializeField] private float damagePerShot;
    [SerializeField] private float shootForce;
    [SerializeField] private float spread;
    [SerializeField] private float bulletsPerTap;
    [SerializeField] private float fireRate;
    private bool readyToShoot = true;

    [Header("Assignables")]
    [SerializeField] private Transform enemyPos;

    public void OnAttack(Transform target)
    {
        if (!readyToShoot) return;
        if (Physics.Linecast(enemyPos.transform.position, target.position, Obstruction)) return;

        Rigidbody targetRb = target.GetComponent<Rigidbody>();

        Vector3 dir = (target.position - enemyPos.position);
        Vector3 attackPoint = enemyPos.position + dir.normalized * 2.5f;
        dir = target.position - attackPoint;

        for (int i = 0; i < bulletsPerTap; i++)
        {
            Vector2 rand = Vector2.zero;
            rand.x = (Random.Range(-1f, 1f)) * spread * 0.003f;
            rand.y = (Random.Range(-1f, 1f)) * spread * 0.003f;
            Vector3 spreadDir = dir.normalized + (Vector3)rand;

            IProjectile bullet = ObjectPooler.Instance.Spawn("Bullet", attackPoint, Quaternion.identity).GetComponent<IProjectile>();

            Physics.Raycast(attackPoint, dir, out var hit, dir.magnitude, CollideAttack);

            Vector3 finalVel = (spreadDir + targetRb.velocity * 0.001f) * shootForce;
            bullet.OnShoot(enemyPos, hit, finalVel, damagePerShot);
        }

        if (readyToShoot)
        {
            readyToShoot = false;
            Invoke(nameof(ResetShot), 1 / fireRate);
        }
    }

    private void ResetShot() => readyToShoot = true;
}
