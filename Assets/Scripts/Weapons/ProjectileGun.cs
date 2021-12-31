using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileGun : MonoBehaviour, IWeapon, IItem
{
    public ScriptManager Player { get { return s; } }
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
    [SerializeField] private float damagePerShot;
    [SerializeField] private float shootForce;
    [SerializeField] private float attackRange;
    [SerializeField] private float spread;
    [SerializeField] private float fireRate;
    [Space(10)]
    [SerializeField] private float lightIntensity;
    [SerializeField] private float lightReturnSpeed;
    private float desiredIntensity = 0f;

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
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Light muzzleLight;
    [SerializeField] private ShakeData recoilShake;
    private ScriptManager s;

    void Start()
    {
        bulletsLeft = magazineSize;
        ResetIntensity();
    }

    void OnEnable() => reloading = false;
    void OnDisable() => ResetIntensity();

    public bool OnAttack()
    {
        if (!readyToShoot || reloading) return false;
        if (bulletsLeft <= 0)
        {
            s.WeaponControls.AddReload(weaponReloadRotOffset);
            StartCoroutine(Reload());
            return false;
        }

        if (muzzleFlash != null) muzzleFlash.Play();
        if (muzzleLight != null) Flash();

        s.WeaponControls.AddRecoil(weaponRecoilPosOffset, weaponRecoilRotOffset, weaponRecoilForce, weaponRecoilAimMulti);

        Ray ray = s.cam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint = (Physics.Raycast(ray, out var hit, attackRange, Environment) ? hit.point : ray.GetPoint(attackRange));
        Vector3 dir = (targetPoint - attackPoint.position);

        for (int i = 0; i < bulletsPerTap; i++)
        {
            bulletsLeft--;

            Vector2 rand = Vector2.zero;
            rand.x = (Random.Range(-1f, 1f)) * spread * 0.003f;
            rand.y = (Random.Range(-1f, 1f)) * spread * 0.003f;

            Vector3 spreadDir = dir.normalized + (Vector3)rand;

            IProjectile bullet = ObjectPooler.Instance.Spawn("Bullet", attackPoint.position, Quaternion.identity).GetComponent<IProjectile>();
            bullet.OnShoot(s, hit, spreadDir, shootForce, damagePerShot);
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

    private void ResetIntensity()
    {
        desiredIntensity = 0f;
        if (muzzleLight != null) muzzleLight.intensity = 0f;
    }

    public void ItemUpdate()
    {
        if (desiredIntensity == 0f && muzzleLight.intensity == 0f) return;

        desiredIntensity = Mathf.Lerp(desiredIntensity, 0f, lightReturnSpeed * 0.5f * Time.deltaTime);
        muzzleLight.intensity = Mathf.Lerp(muzzleLight.intensity, desiredIntensity, lightReturnSpeed * Time.deltaTime);

        if (desiredIntensity < 0.01f)
        {
            desiredIntensity = 0f;
            muzzleLight.intensity = 0f;
        }
    }

    public void OnPickup(ScriptManager s)
    {
        this.s = s;
        ResetIntensity();
    }

    public void OnDrop()
    {
        StopAllCoroutines();
        reloading = false;

        ResetIntensity();
        s = null;
    }

    private void Flash() => desiredIntensity += lightIntensity;
    private void ResetShot() => readyToShoot = true;
    public string ReadData() => "<b> <color=white>" + (bulletsLeft / bulletsPerTap).ToString() + "</color></b>\n<color=grey> <size=10>" + (magazineSize / bulletsPerTap).ToString() + "</color> </size>";
    public string ReadName() => transform.name;
}
