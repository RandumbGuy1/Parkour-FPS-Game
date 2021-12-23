using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingGun : MonoBehaviour, IWeapon, IItem
{
    public ScriptManager Player { get { return s; } }
    public WeaponClass WeaponType { get { return type; } }
    public Sprite ItemSprite { get { return weaponSprite; } }

    public bool Automatic { get { return false; } }

    public float ReloadSmoothTime { get { return 0f; } }
    public float RecoilSmoothTime { get { return weaponRecoilSmoothTime; } }
    public ShakeData RecoilShakeData { get { return recoilShake; } }

    public Vector3 DefaultPos { get { return weaponDefaultPos; } }
    public Vector3 DefaultRot { get { return weaponDefaultRot; } }

    public Vector3 AimPos { get { return weaponAimPos; } }
    public Vector3 AimRot { get { return weaponAimRot; } }

    public float Weight { get { return weaponWeight; } }

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
    { get { return grappledToRigidbody != null ? 0 : (grappling && s != null ? Mathf.Clamp(-s.orientation.InverseTransformDirection(grapplePoint.position - transform.position).x * 0.5f, -maxTilt, maxTilt) : 0); } }

    public float GrappleFov
    { get { return grappledToRigidbody != null ? 0 : (grappling && s != null ? Mathf.Clamp(s.PlayerMovement.Magnitude * 0.17f, minFov, maxFov) : 0); } }

    [Header("Collision")]
    [SerializeField] private LayerMask Grappleable;
    [SerializeField] private LayerMask GrappleRopeIntersects;

    [Header("Recoil Settings")]
    [SerializeField] private float weaponRecoilForce;
    [SerializeField] private float weaponRecoilSmoothTime;
    [SerializeField] private Vector3 weaponRecoilPosOffset;
    [SerializeField] private Vector3 weaponRecoilRotOffset;
    [SerializeField] [Range(0f, 1f)] private float weaponRecoilAimMulti;

    [Header("Assignables")]
    [SerializeField] private ShakeData recoilShake;
    [SerializeField] private GrappleRope rope;
    [SerializeField] private Transform grapplePoint;
    private ScriptManager s;

    private Rigidbody grappledToRigidbody;
    private SpringJoint grappledToSpringJoint;

    void OnEnable()
    {
        CancelInvoke("ResetGrapple");
        Invoke(nameof(ResetGrapple), grappleDelay);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        ResetGun();
        rope.ResetRope();

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

    public bool OnAttack()
    {
        Ray ray = s.cam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out var hit, grappleRange, Grappleable)) return false;

        grappledToRigidbody = hit.collider.GetComponent<Rigidbody>();

        if (Mathf.Abs(Vector3.Dot(Vector3.up, hit.normal)) > 0.5f && grappledToRigidbody == null || !readyToGrapple) return false;
        
        s.WeaponControls.AddRecoil(weaponRecoilPosOffset, weaponRecoilRotOffset, weaponRecoilForce, weaponRecoilAimMulti);
        s.CameraShaker.ShakeOnce(5f, 5f, 1f, 10f, ShakeData.ShakeType.Perlin);

        grapplePoint.position = hit.point;
        grapplePoint.SetParent(hit.collider.transform);
        rope.DrawRope();

        CancelInvoke("ResetGrapple");
        Invoke(nameof(ResetGrapple), grappleDelay);

        grappling = true;
        readyToGrapple = false;
        timesGrappled++;

        s.PlayerMovement.ResetJumpSteps();
        s.CameraLook.SetGrapplingGun(this);

        StopAllCoroutines();
        if (grappledToRigidbody == null) StartCoroutine(GrappleMovement(hit.normal, s.PlayerMovement.WallRunning));
        else StartCoroutine(GrappledToRigidbodyMovement(hit.normal));

        return true;
    }

    private IEnumerator GrappledToRigidbodyMovement(Vector3 wallNormal)
    {
        float wallIntersectElapsed = 0f;

        s.rb.AddForce(wallNormal + (s.PlayerInput.Jumping ? 0.1f : (s.PlayerMovement.Grounded ? 1.2f : 0.8f)) * initialGrapplePullForce * Vector3.up, ForceMode.Impulse);

        grappledToSpringJoint = grappledToRigidbody.gameObject.AddComponent<SpringJoint>();
        grappledToSpringJoint.autoConfigureConnectedAnchor = false;

        grappledToSpringJoint.maxDistance = 10f;
        grappledToSpringJoint.minDistance = 0.3f;

        grappledToSpringJoint.spring = 20f;
        grappledToSpringJoint.damper = 0.5f;
        grappledToSpringJoint.massScale = 20f;

        while (s.PlayerInput.LeftHoldClick && wallIntersectElapsed < 0.1f)
        {
            grappledToSpringJoint.connectedAnchor = grapplePoint.position;

            Vector3 grappleToPlayer = (grapplePoint.position - s.transform.position);
            if (grappleToPlayer.sqrMagnitude > (grappleRange + 5f) * (grappleRange + 5f)) break;

            grappledToRigidbody.AddForce(-grappleToPlayer * 0.06f, ForceMode.VelocityChange);

            wallIntersectElapsed = (Physics.Linecast(transform.position, grapplePoint.position + wallNormal, GrappleRopeIntersects) ? wallIntersectElapsed += Time.fixedDeltaTime : 0f);
            yield return new WaitForFixedUpdate();
        }

        ResetGun();
    }

    private IEnumerator GrappleMovement(Vector3 wallNormal, bool wallRunning)
    {
        float groundElapsed = 0f;
        float wallIntersectElapsed = 0f;

        s.rb.AddForce(wallNormal + (s.PlayerInput.Jumping ? 0.1f : (s.PlayerMovement.Grounded ? 1.2f : 0.8f)) * initialGrapplePullForce * Vector3.up, ForceMode.Impulse);

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

            Vector3 grappleToPlayer = (grapplePoint.position - s.transform.position);
            if (grappleToPlayer.sqrMagnitude > (grappleRange + 5f) * (grappleRange + 5f)) break;

            s.rb.AddForce(Vector3.ClampMagnitude(grappleToPlayer * 0.1f, 1.55f) * grapplePullForce, ForceMode.Acceleration);
            s.rb.AddForce(0.5f * grappleCameraPullForce * (s.cam.forward.normalized + Vector3.up * 0.3f), ForceMode.Acceleration);
            
            s.rb.AddForce(grappleFallSpeedClamp * Mathf.Clamp(-s.rb.velocity.y * 0.08f, 0.5f, 1.5f) * Vector3.up, ForceMode.Acceleration);

            grappleToPlayer.y = 0f;
            Vector2 horizPlayerMovement = new Vector2(s.PlayerMovement.RelativeVel.x, s.PlayerMovement.RelativeVel.z);

            s.rb.AddForce(grappleToPlayer.normalized * (grappleHorizPullForce - horizPlayerMovement.magnitude * 0.55f), ForceMode.Acceleration);

            groundElapsed = (s.PlayerMovement.Grounded ? groundElapsed += Time.fixedDeltaTime : 0f);
            wallIntersectElapsed = (Physics.Linecast(transform.position, grapplePoint.position + wallNormal, GrappleRopeIntersects) ? wallIntersectElapsed += Time.fixedDeltaTime : 0f);

            yield return new WaitForFixedUpdate();
        }

        ResetGun();
    }

    public bool SecondaryAction() => true;
    public void OnPickup(ScriptManager s)
    {
        timesGrappled = 0;
        this.s = s;
    }

    public void OnDrop()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);

        ResetGun();

        timesGrappled = 0;
        s = null;
    }

    public void ItemUpdate() 
    {
        if (s == null || timesGrappled <= 0) return;

        /*
        grappleHits = Physics.SphereCastAll(transform.position, 15f, s.cam.forward, grappleRange / 2f, Grappleable); 
        idk = FindNearestPoint(new List<RaycastHit>(grappleHits));

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
        */

        Vector3 gunToGrapple = s.WeaponControls.WeaponPos.InverseTransformDirection(grapplePoint.position - transform.position);      
        Vector2 desiredPos = grappling ? (Vector2) gunToGrapple.normalized : Vector2.zero;
        Quaternion desiredRot = grappling ? Quaternion.LookRotation(gunToGrapple) : Quaternion.Euler(Vector3.zero);
        
        transform.localRotation = Quaternion.Slerp(transform.localRotation, desiredRot, 7f * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, desiredPos, 7f * Time.deltaTime);
    }

    public string ReadData() => " ";
    public string ReadName() => transform.name;

    void ResetGrapple() => readyToGrapple = true;

    void ResetGun()
    {
        grapplePoint.SetParent(transform);
        grappling = false;
        rope.OnStopDraw();

        Destroy(grappledToSpringJoint);
        grappledToRigidbody = null;
    }
}
