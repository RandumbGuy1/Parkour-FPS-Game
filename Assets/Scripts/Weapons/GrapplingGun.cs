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
    [SerializeField] private float grappleDelay;
    private bool readyToGrapple;
    private Vector3 grapplePoint;
    private bool grappling;

    private int timesGrappled = 0;

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
    [SerializeField] private GrappleRope rope;
    [SerializeField] private Transform attackPoint;
    private ScriptManager s;

    void OnEnable()
    {
        CancelInvoke("ResetGrapple");
        Invoke("ResetGrapple", grappleDelay);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        grappling = false;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

    public bool OnAttack(ScriptManager s)
    {
        this.s = s;

        Ray ray = this.s.cam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out var hit, grappleRange, Grappleable)) return false;

        if (System.Math.Abs(Vector3.Dot(Vector3.up, hit.normal)) > 0.5f || !readyToGrapple) return false;
        this.s.WeaponControls.AddRecoil(weaponRecoilPosOffset, weaponRecoilRotOffset, weaponRecoilForce, weaponRecoilAimMulti);

        grapplePoint = hit.point;
        rope.DrawRope(attackPoint, grapplePoint);

        CancelInvoke("ResetGrapple");
        Invoke("ResetGrapple", grappleDelay);

        StopAllCoroutines();
        StartCoroutine(GrappleMovement(hit.normal));

        return true;
    }

    private IEnumerator GrappleMovement(Vector3 wallNormal)
    {
        float elapsed = 0f;

        grappling = true;
        readyToGrapple = false;
        timesGrappled++;

        s.rb.AddForce(wallNormal + Vector3.up * (s.PlayerInput.Jumping ? 0.1f : (s.PlayerMovement.Grounded ? 1.2f : 0.6f)) * initialGrapplePullForce, ForceMode.Impulse);

        while (s.PlayerInput.LeftHoldClick && !s.PlayerMovement.WallRunning && (!s.PlayerMovement.Grounded || elapsed < 0.1f) && !Physics.Linecast(attackPoint.position, grapplePoint + wallNormal, Grappleable))
        {
            Vector3 grappleToPlayer = (grapplePoint - s.transform.position);
            if (Vector3.Dot(wallNormal.normalized, grappleToPlayer.normalized) > 0.3f || grappleToPlayer.sqrMagnitude > (grappleRange + 5f) * (grappleRange + 5f)) break;

            Vector3 pullDir = grappleToPlayer.normalized * 1.3f + s.cam.forward * 0.5f + Vector3.up * Mathf.Clamp(-s.rb.velocity.y * 0.8f, 0.7f, 1.6f);
            s.rb.AddForce(pullDir * grapplePullForce, ForceMode.Acceleration);

            grappleToPlayer.y = 0f;
            s.rb.AddForce(pullDir.normalized * grappleHorizPullForce, ForceMode.Acceleration);

            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        grappling = false;
        rope.OnStopDraw();
    }

    void ResetGrapple() => readyToGrapple = true;

    public bool SecondaryAction(ScriptManager s) => true;
    public void OnPickup() => timesGrappled = 0;
    public void OnDrop()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);

        grappling = false;
        timesGrappled = 0;

        rope.OnStopDraw();
    }

    public void ItemUpdate() 
    {
        if (s == null || timesGrappled <= 0) return;

        Vector3 gunToGrapple = s.WeaponControls.WeaponPos.InverseTransformDirection(grapplePoint - transform.position);      
        Vector2 desiredPos = grappling ? (Vector2)gunToGrapple.normalized : Vector2.zero;
        Quaternion desiredRot = grappling ? Quaternion.LookRotation(gunToGrapple) : Quaternion.Euler(Vector3.zero);
        
        transform.localRotation = Quaternion.Slerp(transform.localRotation, desiredRot, 7.5f * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, desiredPos, 7.5f * Time.deltaTime);
    }

    public string ReadData() => " ";
    public string ReadName() => transform.name;
}
