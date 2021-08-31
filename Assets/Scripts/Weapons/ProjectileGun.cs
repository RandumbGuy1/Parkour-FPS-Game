using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileGun : MonoBehaviour, IWeapon, IItem
{
    public WeaponClass weaponType { get { return type; } }
    public Sprite itemSprite { get { return weaponSprite; } }

    public bool automatic { get { return weaponAutomatic; } }

    public float recoilForce { get { return weaponRecoilForce; } }
    public float recoilSmoothTime { get { return weaponRecoilSmoothTime; } }
    public Vector3 recoilPosOffset { get { return weaponRecoilPosOffset; } }
    public Vector3 recoilRotOffset { get { return weaponRecoilRotOffset; } }

    public Vector3 defaultPos { get { return weaponDefaultPos; } }
    public Vector3 defaultRot { get { return weaponDefaultRot; } }

    public Vector3 aimPos { get { return weaponAimPos; } }
    public Vector3 aimRot { get { return weaponAimRot; } }

    public float weight { get { return weaponWeight; } }

    public Vector3 reloadRotOffset { get { return weaponReloadRotOffset; } }
    public float reloadSmoothTime { get { return reloadTime; } }

    [Header("Weapon Class")]
    [SerializeField] private WeaponClass type;
    [SerializeField] private bool weaponAutomatic;

    [Header("Weapon Artwork")]
    [SerializeField] private Sprite weaponSprite;

    [Header("Weapon Holding Settings")]
    [SerializeField] private Vector3 weaponDefaultPos;
    [SerializeField] private Vector3 weaponDefaultRot;
    [Space(10)]
    [SerializeField] private Vector3 weaponAimPos;
    [SerializeField] private Vector3 weaponAimRot;
    [Space(10)]
    [SerializeField] private float weaponWeight;

    [Header("Shooting Settings")]
    [SerializeField] private float shootForce;
    [SerializeField] private float attackRange;
    [SerializeField] private float spread;
    [SerializeField] private float fireRate;

    [Header("Recoil Settings")]
    [SerializeField] private float weaponRecoilForce;
    [SerializeField] private float weaponRecoilSmoothTime;
    [SerializeField] private Vector3 weaponRecoilPosOffset;
    [SerializeField] private Vector3 weaponRecoilRotOffset;

    [Header("Reload Settings")]
    [SerializeField] private int magazineSize;
    [SerializeField] private int bulletsPerTap;
    [SerializeField] private float reloadTime;
    [SerializeField] private Vector3 weaponReloadRotOffset;

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

    public bool OnAttack(Transform cam)
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
            rand.x = (Random.Range(-1f, 1f)) * spread * 0.003f;
            rand.y = (Random.Range(-1f, 1f)) * spread * 0.003f;

            Vector3 spreadDir = dir.normalized + (Vector3)rand;

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

    public bool SecondaryAction()
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

    private void ResetShot() => readyToShoot = true;

    public string ReadData() => (bulletsLeft / bulletsPerTap).ToString() + " / " + (magazineSize / bulletsPerTap).ToString();

    public bool OnUse() => true;
}
