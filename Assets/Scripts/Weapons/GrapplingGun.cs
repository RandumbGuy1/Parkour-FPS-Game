using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingGun : MonoBehaviour, IWeapon, IItem
{
    public WeaponClass weaponType { get { return type; } }
    public Sprite itemSprite { get { return weaponSprite; } }

    public bool automatic { get { return false; } }

    public float reloadSmoothTime { get { return 0f; } }
    public float recoilSmoothTime { get { return weaponRecoilSmoothTime; } }
    public ShakeData recoilShakeData { get { return recoilShake; } }

    public Vector3 defaultPos { get { return weaponDefaultPos; } }
    public Vector3 defaultRot { get { return weaponDefaultRot; } }

    public Vector3 aimPos { get { return weaponAimPos; } }
    public Vector3 aimRot { get { return weaponAimRot; } }

    public float weight { get { return weaponWeight; } }

    [Header("Weapon Class")]
    [SerializeField] private WeaponClass type;

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

    [Header("Grapple Shooting")]
    [SerializeField] private float grapplePullForce;
    [SerializeField] private float grappleHorizPullForce;
    [SerializeField] private float initialGrapplePullForce;
    [SerializeField] private float grappleRange;

    [Header("Collision")]
    [SerializeField] private LayerMask Grappleable;

    [Header("Recoil Settings")]
    [SerializeField] private float weaponRecoilForce;
    [SerializeField] private float weaponRecoilSmoothTime;
    [SerializeField] private Vector3 weaponRecoilPosOffset;
    [SerializeField] private Vector3 weaponRecoilRotOffset;
    [SerializeField] [Range(0f, 1f)] private float weaponRecoilAimMulti;

    [Header("Assignables")]
    [SerializeField] private ShakeData recoilShake;
    private ScriptManager s;

    Vector3 idk;
    Vector3 idk2;
    Vector3 idk3;

    void OnDisable()
    {
        StopAllCoroutines();

        idk = Vector3.zero;
        idk2 = Vector3.zero;
        idk3 = Vector3.zero;
    }

    public bool OnAttack(ScriptManager s)
    {
        this.s = s;
        this.s.WeaponControls.AddRecoil(weaponRecoilPosOffset, weaponRecoilRotOffset, weaponRecoilForce, weaponRecoilAimMulti);

        Ray ray = this.s.cam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out var hit, grappleRange, Grappleable)) return false;

        Vector3 targetPoint = hit.point;

        if (System.Math.Abs(Vector3.Dot(Vector3.up, hit.normal)) > 0.5f) return false;

        idk = targetPoint;
        idk2 = hit.normal;

        StopAllCoroutines();
        StartCoroutine(GrappleMovement(targetPoint, hit.normal));

        return true;
    }

    private IEnumerator GrappleMovement(Vector3 grapplePoint, Vector3 wallNormal)
    {
        s.rb.AddForce(((grapplePoint - s.transform.position).normalized + wallNormal + Vector3.up * 0.4f) * initialGrapplePullForce, ForceMode.Impulse);

        while (!s.PlayerInput.LeftReleaseClick && !s.PlayerMovement.WallRunning)
        {
            idk3 = (grapplePoint - s.transform.position);

            Vector3 grappleToPlayer = (grapplePoint - s.transform.position);
            if (Vector3.Dot(wallNormal.normalized, grappleToPlayer.normalized) > 0.3f || grappleToPlayer.sqrMagnitude > (grappleRange + 5f) * (grappleRange + 5f)) break;

            Vector3 pullDir = grappleToPlayer.normalized * 1.3f + s.cam.forward * 0.5f + Vector3.up * Mathf.Clamp(-s.rb.velocity.y * 0.8f, 0.7f, 1.6f);
            s.rb.AddForce(pullDir * grapplePullForce, ForceMode.Acceleration);

            grappleToPlayer.y = 0f;
            s.rb.AddForce(pullDir.normalized * grappleHorizPullForce, ForceMode.Acceleration);

            yield return new WaitForFixedUpdate();
        }

        idk = Vector3.zero;
        idk2 = Vector3.zero;
        idk3 = Vector3.zero;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(idk, 0.5f);

        Debug.DrawRay(idk, idk2 * 3f, Color.red);
        Debug.DrawRay(idk - idk3, idk3, Color.green);
    }

    public bool SecondaryAction(ScriptManager s) => true;

    public void OnPickup() { }
    public void OnDrop() { }

    public string ReadData() => " ";
}
