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
    [SerializeField] private float grappleCameraPullForce;
    [SerializeField] private float grappleHorizPullForce;
    [SerializeField] private float grappleFallSpeedClamp;
    [SerializeField] private float initialGrapplePullForce;
    [SerializeField] private float grappleRange;
    [SerializeField] private float grappleDelay;
    private Vector3 grapplePoint;
    private RaycastHit[] grappleHits;

    private bool readyToGrapple;
    private bool grappling;
    private int timesGrappled = 0;

    [Header("Grapple Sway Settings")]
    [SerializeField] private float swaySmoothTime;
    [SerializeField] private float maxTilt;

    [Header("Grapple Fov Settings")]
    [SerializeField] private float fovSmoothTime;
    [SerializeField] private float maxFov;
    [SerializeField] private float minFov;

    public float GrappleTilt 
    { get { return (grappling && s != null ? Mathf.Clamp(-s.orientation.InverseTransformDirection(grapplePoint - transform.position).x * 0.5f, -maxTilt, maxTilt) : 0); } }

    public float GrappleFov
    { get { return (grappling && s != null ? Mathf.Clamp(s.PlayerMovement.Magnitude * 0.17f, minFov, maxFov) : 0); } }

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
    private ScriptManager s;

    void OnEnable()
    {
        CancelInvoke("ResetGrapple");
        Invoke("ResetGrapple", grappleDelay);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        ResetGun();
        rope.ResetRope();

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

    public bool OnAttack(ScriptManager s)
    {
        this.s = s;

        Ray ray = s.cam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out var hit, grappleRange, Grappleable)) return false;

        if (System.Math.Abs(Vector3.Dot(Vector3.up, hit.normal)) > 0.5f || !readyToGrapple) return false;
        s.WeaponControls.AddRecoil(weaponRecoilPosOffset, weaponRecoilRotOffset, weaponRecoilForce, weaponRecoilAimMulti);
       
        grapplePoint = hit.point;
        rope.DrawRope(grapplePoint);

        CancelInvoke("ResetGrapple");
        Invoke("ResetGrapple", grappleDelay);

        StopAllCoroutines();
        StartCoroutine(GrappleMovement(hit.normal, s.PlayerMovement.WallRunning));

        return true;
    }

    private IEnumerator GrappleMovement(Vector3 wallNormal, bool wallRunning)
    {
        float groundElapsed = 0f;
        float wallIntersectElapsed = 0f;

        grappling = true;
        readyToGrapple = false;
        timesGrappled++;

        s.rb.AddForce(wallNormal + Vector3.up * (s.PlayerInput.Jumping ? 0.1f : (s.PlayerMovement.Grounded ? 1.2f : 0.8f)) * initialGrapplePullForce, ForceMode.Impulse);
        s.CameraLook.SetGrapplingGun(this);

        if (wallRunning)
        {
            s.PlayerMovement.DetachFromWallRun();
            s.PlayerMovement.SetWallrunning(false);

            yield return new WaitForSeconds(0.05f);
        }

        s.PlayerMovement.ResetJumpSteps();
            
        while (s.PlayerInput.LeftHoldClick && !s.PlayerMovement.WallRunning && groundElapsed < 0.7f && wallIntersectElapsed < 0.1f)
        {
            s.CameraLook.SetTiltSmoothing(swaySmoothTime);
            s.CameraLook.SetFovSmoothing(fovSmoothTime);

            Vector3 grappleToPlayer = (grapplePoint - s.transform.position);
            if (grappleToPlayer.sqrMagnitude > (grappleRange + 5f) * (grappleRange + 5f)) break;

            s.rb.AddForce(Vector3.ClampMagnitude(grappleToPlayer * 0.1f, 1.5f) * grapplePullForce, ForceMode.Acceleration);
            s.rb.AddForce(0.5f * grappleCameraPullForce * (s.orientation.forward + s.cam.forward * 0.5f).normalized, ForceMode.Acceleration);
            s.rb.AddForce(grappleFallSpeedClamp * Mathf.Clamp(-s.rb.velocity.y * 0.8f, 0.75f, 1.3f) * Vector3.up, ForceMode.Acceleration);

            grappleToPlayer.y = 0f;
            Vector2 horizPlayerMovement = new Vector2(s.PlayerMovement.RelativeVel.x, s.PlayerMovement.RelativeVel.z);

            s.rb.AddForce(grappleToPlayer.normalized * (grappleHorizPullForce - horizPlayerMovement.magnitude * 0.4f), ForceMode.Acceleration);

            groundElapsed = (s.PlayerMovement.Grounded ? groundElapsed += Time.fixedDeltaTime : 0f);
            wallIntersectElapsed = (Physics.Linecast(transform.position, grapplePoint + wallNormal, Grappleable) ? wallIntersectElapsed += Time.fixedDeltaTime : 0f);

            yield return new WaitForFixedUpdate();
        }

        ResetGun();
    }

    public bool SecondaryAction(ScriptManager s) => true;
    public void OnPickup() => timesGrappled = 0;
    public void OnDrop()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);

        ResetGun();

        timesGrappled = 0;
    }

    public void ItemUpdate() 
    {
        if (s == null || timesGrappled <= 0) return;

        /*
        grappleHits = Physics.SphereCastAll(transform.position, 15f, s.cam.forward, grappleRange / 2f, Grappleable); 
        idk = FindNearestPoint(new List<RaycastHit>(grappleHits));
        */

        Vector3 gunToGrapple = s.WeaponControls.WeaponPos.InverseTransformDirection(grapplePoint - transform.position);      
        Vector2 desiredPos = grappling ? (Vector2) gunToGrapple.normalized : Vector2.zero;
        Quaternion desiredRot = grappling ? Quaternion.LookRotation(gunToGrapple) : Quaternion.Euler(Vector3.zero);
        
        transform.localRotation = Quaternion.Slerp(transform.localRotation, desiredRot, 7.5f * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, desiredPos, 7.5f * Time.deltaTime);
    }

    private Vector3 FindNearestPoint(List<RaycastHit> hits)
    {
        Vector3 result = Vector3.zero;

        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i].distance < float.PositiveInfinity && System.Math.Abs(hits[i].normal.y) < 0.4f && !Physics.Linecast(hits[i].point, transform.position))
            {
                Vector3 closestTo = s.cam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).GetPoint(hits[i].distance);
                result = hits[i].collider.ClosestPoint(closestTo);
            }
        }

        return result;
    }

    public string ReadData() => " ";
    public string ReadName() => transform.name;

    void ResetGrapple() => readyToGrapple = true;

    void ResetGun()
    {
        grappling = false;
        rope.OnStopDraw();
    }
}
