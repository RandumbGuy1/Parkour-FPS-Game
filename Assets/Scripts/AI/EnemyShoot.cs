using UnityEngine;

public class EnemyShoot : MonoBehaviour, IWeapon
{
    public WeaponClass WeaponType { get { return WeaponClass.Ranged; } }

    public bool Automatic { get { return true; } }

    public float RecoilSmoothTime { get { return 0f; } }
    public float ReloadSmoothTime { get { return 0f; } }
    public ShakeData RecoilShakeData { get { return null; } }

    [Header("Shooting Settings")]
    [SerializeField] private LayerMask CollideAttack;
    [SerializeField] private float damagePerShot;
    [SerializeField] private float shootForce;
    [SerializeField] private float spread;
    [SerializeField] private float bulletsPerTap;
    [SerializeField] private float fireRate;
    private bool readyToShoot = true;

    [Header("Assignables")]
    [SerializeField] private Transform enemyPos;
    [SerializeField] private Transform target;
    private Rigidbody targetRb;

    void Awake() => targetRb = target.GetComponent<Rigidbody>();

    public bool OnAttack()
    {
        if (!readyToShoot) return false;

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

        return true;
    }

    public bool SecondaryAction() => true;
    private void ResetShot() => readyToShoot = true;
}
