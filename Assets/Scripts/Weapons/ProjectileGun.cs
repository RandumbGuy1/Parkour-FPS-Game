using System.Collections;
using UnityEngine;

public class ProjectileGun : MonoBehaviour, IWeapon, IItem
{
    public PlayerManager Player { get { return s; } }
    public WeaponClass WeaponType { get { return type; } }
    public Sprite ItemSprite { get { return weaponSprite; } }

    public bool Automatic { get { return weaponAutomatic; } }

    public float ReloadSmoothTime { get { return reloadTime; } }
    public float RecoilSmoothTime { get { return weaponRecoilSmoothTime; } }
    public ShakeData RecoilShakeData { get { return recoilShake; } }

    public Vector3 DefaultPos { get { return weaponDefaultPos; } }
    public Vector3 DefaultRot { get { return weaponDefaultRot; } }

    public Vector3 AimPos { get { return weaponAimPos; } }
    public Vector3 AimRot { get { return weaponAimRot; } }

    public float Weight { get { return weaponWeight; } }

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
    [SerializeField] private string projectile;
    [SerializeField] private float damagePerShot;
    [SerializeField] private float shootForce;
    [SerializeField] private float attackRange;
    [SerializeField] private float spread;
    [SerializeField] private float fireRate;

    [Header("Recoil Settings")]
    [SerializeField] private float weaponRecoilForce;
    [SerializeField] private float weaponRecoilSmoothTime;
    [SerializeField] private Vector3 weaponRecoilPosOffset;
    [SerializeField] private Vector3 weaponRecoilRotOffset;
    [SerializeField] [Range(0f, 1f)] private float weaponRecoilAimMulti;

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
    [SerializeField] private PlayParticleOnEnable muzzleFlash;
    [SerializeField] private ShakeData recoilShake;
    private PlayerManager s;
    private BoxCollider col;

    void Start()
    {
        col = GetComponent<BoxCollider>();

        bulletsLeft = magazineSize;
    }

    void OnEnable() => reloading = false;

    public bool OnAttack()
    {
        if (!readyToShoot || reloading) return false;
        if (bulletsLeft <= 0)
        {
            s.WeaponControls.AddReload(weaponReloadRotOffset);
            StartCoroutine(Reload());
            return false;
        }

        if (muzzleFlash != null) muzzleFlash.PlayParticle();

        if (s != null) s.WeaponControls.AddRecoil(weaponRecoilPosOffset, weaponRecoilRotOffset, weaponRecoilForce, weaponRecoilAimMulti);

        Ray ray = s.cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint = (Physics.Raycast(ray, out var hit, attackRange, Environment) && hit.collider.gameObject != s.gameObject ? hit.point : ray.GetPoint(attackRange));
        Vector3 bulletDir = targetPoint - attackPoint.position;

        for (int i = 0; i < bulletsPerTap; i++)
        {
            bulletsLeft--;

            Vector3 rand = Vector3.zero;
            rand += Random.Range(-1f, 1f) * 0.003f * spread * s.cam.transform.right;
            rand += Random.Range(-1f, 1f) * 0.003f * spread * s.cam.transform.up;

            Vector3 spreadDir = bulletDir.normalized + rand * (s.WeaponControls.Aiming ? 0.3f : 1f);

            IProjectile bullet = ObjectPooler.Instance.Spawn(projectile, attackPoint.position, Quaternion.identity).GetComponent<IProjectile>();
            bullet.OnShoot(s.transform, hit, spreadDir * shootForce, damagePerShot, s, hit.collider != null && col.bounds.Intersects(hit.collider.bounds));
        }

        if (readyToShoot)
        {
            readyToShoot = false;
            Invoke(nameof(ResetShot), 1 / fireRate);
        }

        return true;
    }

    public bool SecondaryAction()
    {
        if (bulletsLeft >= magazineSize || reloading) return false;

        s.WeaponControls.AddReload(weaponReloadRotOffset);
        StartCoroutine(Reload());
        return true;
    }

    private IEnumerator Reload()
    {
        reloading = true;

        yield return new WaitForSeconds(reloadTime * 2.5f);

        bulletsLeft = magazineSize;
        reloading = false;
    }

    public void ItemUpdate() { }

    public void OnPickup(PlayerManager s) => this.s = s;

    public void OnDrop()
    {
        StopAllCoroutines();
        reloading = false;
        s = null;
    }

    private void ResetShot() => readyToShoot = true;
    public string ReadData() => "<b> <color=white>" + (bulletsLeft / bulletsPerTap).ToString() + "</color></b>\n<color=grey> <size=10>" + (magazineSize / bulletsPerTap).ToString() + "</color> </size>";
    public string ReadName() => transform.name;
}
