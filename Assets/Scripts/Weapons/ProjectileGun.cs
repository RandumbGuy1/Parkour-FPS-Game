using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileGun : Weapon
{
    [Header("Shooting Settings")]
    [SerializeField] private float shootForce;
    [SerializeField] private float attackRange;
    [SerializeField] private float spread;
    [SerializeField] private float fireRate;

    [Header("Reload Settings")]
    [SerializeField] private int magazineSize;
    [SerializeField] private int bulletsPerTap;
    [SerializeField] private float reloadTime;

    private int bulletsLeft;
    private bool readyToShoot = true;
    private bool reloading;

    [Header("Collision")]
    [SerializeField] private LayerMask Environment;

    [Header("Assignables")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private ParticleSystem muzzleFlash;

    void Start() => bulletsLeft = magazineSize;

    void OnEnable() => reloading = false;

    public override bool OnAttack(Transform cam)
    {
        if (!readyToShoot || bulletsLeft <= 0 || reloading) return false;

        if (muzzleFlash != null) muzzleFlash.Play();

        Ray ray = cam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
  
        Vector3 targetPoint = (Physics.Raycast(ray, out var hit, attackRange, Environment) ? hit.point : ray.GetPoint(attackRange)); 
        Vector3 dir = (targetPoint - attackPoint.position);  

        for (int i = 0; i < bulletsPerTap; i++)
        {
            bulletsLeft--;

            Vector2 rand = Vector2.zero;
            rand.x = ((int) Random.Range(-1f, 1f)) * spread * 0.001f;
            rand.y = ((int) Random.Range(-1f, 1f)) * spread * 0.001f;

            Vector3 spreadDir = dir.normalized + (Vector3) rand;

            float distanceForce = (dir.magnitude * 0.1f);
            distanceForce = Mathf.Clamp(distanceForce, 0, 3f);

            GameObject bullet = ObjectPooler.Instance.Spawn("Bullet", attackPoint.position, Quaternion.identity);
            bullet.transform.up = spreadDir;

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.AddForce(bullet.transform.up * distanceForce * shootForce, ForceMode.Impulse);
        }

        if (readyToShoot)
        {
            readyToShoot = false;
            Invoke("ResetShot", 1 / fireRate);
        }

        return true;
    }

    public override bool SecondaryAction()
    {
        if (bulletsLeft >= magazineSize || reloading) return false;

        StartCoroutine(Reload());
        return true;
    }

    private IEnumerator Reload()
    {
        reloading = true;

        yield return new WaitForSeconds(reloadTime);

        bulletsLeft = magazineSize;
        reloading = false;
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }
}
