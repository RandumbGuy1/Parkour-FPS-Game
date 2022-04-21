using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingGun : MonoBehaviour, IWeapon, IItem
{
    public PlayerManager Player { get { return s; } }
    public WeaponClass WeaponType { get { return type; } }

    public bool Automatic { get { return false; } }

    public float ReloadSmoothTime { get { return 0f; } }
    public float RecoilSmoothTime { get { return weaponRecoilSmoothTime; } }
    public ShakeData RecoilShakeData { get { return recoilShake; } }

    [Header("Weapon Class")]
    [SerializeField] private WeaponClass type;

    [Header("Weapon Artwork")]
    [SerializeField] private ItemArtSettings spriteSettings;
    public ItemArtSettings SpriteSettings => spriteSettings;

    [Header("Weapon Holding Settings")]
    [SerializeField] private HoldingSettings swaySettings;
    public HoldingSettings SwaySettings => swaySettings;

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
    private PlayerManager s;

    private Rigidbody grappledToRigidbody;
    private SpringJoint grappledToSpringJoint;

    void OnEnable()
    {
        CancelInvoke("ResetGrapple");
        Invoke(nameof(ResetGrapple), grappleDelay);
    }

    void OnDisable()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);

        StopAllCoroutines();
        ResetGun();
        rope.ResetRope();
    }

    public bool OnAttack()
    {
        Ray ray = s.cam.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out var hit, grappleRange, Grappleable)) return false;

        grappledToRigidbody = hit.collider.GetComponent<Rigidbody>();

        if (Mathf.Abs(Vector3.Dot(Vector3.up, hit.normal)) > 0.5f && grappledToRigidbody == null || !readyToGrapple) return false;
        
        s.WeaponControls.AddRecoil(weaponRecoilPosOffset, weaponRecoilRotOffset, weaponRecoilForce, weaponRecoilAimMulti);
        s.CameraShaker.ShakeOnce(new PerlinShake(ShakeData.Create(3.5f, 5f, 1f, 9f)));

        grapplePoint.position = hit.point;
        grapplePoint.SetParent(hit.collider.transform);
        rope.DrawRope();

        CancelInvoke("ResetGrapple");
        Invoke(nameof(ResetGrapple), grappleDelay);

        grappling = true;
        readyToGrapple = false;
        timesGrappled++;

        s.PlayerMovement.Collision.ResetJumpSteps();
        s.CameraLook.SetGrapplingGun(this);

        StopAllCoroutines();
        if (grappledToRigidbody == null) StartCoroutine(GrappleMovement(hit.normal, s.PlayerMovement.WallRunning));
        else StartCoroutine(GrappledToRigidbodyMovement(hit.normal));

        return true;
    }

    private IEnumerator GrappledToRigidbodyMovement(Vector3 wallNormal)
    {
        float wallIntersectElapsed = 0f;

        s.rb.AddForce(wallNormal + (s.PlayerInput.Jumping ? 0.1f : (s.PlayerMovement.Collision.Grounded ? 0.2f : 0.7f)) * initialGrapplePullForce * Vector3.up, ForceMode.Impulse);

        grappledToSpringJoint = grappledToRigidbody.gameObject.AddComponent<SpringJoint>();
        grappledToSpringJoint.autoConfigureConnectedAnchor = false;

        grappledToSpringJoint.maxDistance = 3f;
        grappledToSpringJoint.minDistance = 0f;

        grappledToSpringJoint.spring = 50f;
        grappledToSpringJoint.damper = 10f;
        grappledToSpringJoint.massScale = 1.5f;

        while (s.PlayerInput.LeftHoldClick && wallIntersectElapsed < 0.3f)
        {
            grappledToSpringJoint.connectedAnchor = rope.transform.position + s.orientation.forward;

            Vector3 grappleToPlayer = (grapplePoint.position - s.transform.position);
            if (grappleToPlayer.sqrMagnitude > (grappleRange + 5f) * (grappleRange + 5f)) break;

            grappleToPlayer.y *= 0.4f;

            grappledToSpringJoint.damper = grappleToPlayer.sqrMagnitude > 16f ? 15f : 35f;
            grappledToRigidbody.AddForce(-grappleToPlayer * 0.01f, ForceMode.VelocityChange);

            wallIntersectElapsed = (Physics.Linecast(transform.position, grapplePoint.position + wallNormal, GrappleRopeIntersects) ? wallIntersectElapsed += Time.fixedDeltaTime : 0f);
            yield return new WaitForFixedUpdate();
        }

        ResetGun();
    }

    private IEnumerator GrappleMovement(Vector3 wallNormal, bool wallRunning)
    {
        float groundElapsed = 0f;
        float wallIntersectElapsed = 0f;

        s.rb.AddForce(wallNormal + (s.PlayerInput.Jumping ? 0.1f : (s.PlayerMovement.Collision.Grounded ? 1.2f : 0.9f)) * initialGrapplePullForce * Vector3.up, ForceMode.Impulse);

        if (wallRunning)
        {
            s.PlayerMovement.DetachFromWallRun();
            s.PlayerMovement.SetWallrunning(false);

            yield return new WaitForSeconds(0.05f);
        }

        s.PlayerMovement.Collision.ResetJumpSteps();
            
        while (s.PlayerInput.LeftHoldClick && !s.PlayerMovement.WallRunning && groundElapsed < 0.7f && wallIntersectElapsed < 0.1f)
        {
            s.CameraLook.SetTiltSmoothing(swaySmoothTime);
            s.CameraLook.SetFovSmoothing(fovSmoothTime);

            Vector3 grappleToPlayer = (grapplePoint.position - s.transform.position);
            if (grappleToPlayer.sqrMagnitude > (grappleRange + 5f) * (grappleRange + 5f)) break;

            s.rb.AddForce(Vector3.ClampMagnitude(grappleToPlayer * 0.1f, 1.55f) * grapplePullForce, ForceMode.Acceleration);
            s.rb.AddForce(0.5f * grappleCameraPullForce * (s.cam.transform.forward.normalized + Vector3.up * 0.3f), ForceMode.Acceleration);
            
            s.rb.AddForce(grappleFallSpeedClamp * Mathf.Clamp(-s.rb.velocity.y * 0.08f, 0.5f, 1.5f) * Vector3.up, ForceMode.Acceleration);

            grappleToPlayer.y = 0f;
            Vector2 horizPlayerMovement = new Vector2(s.PlayerMovement.RelativeVel.x, s.PlayerMovement.RelativeVel.z);

            s.rb.AddForce(grappleToPlayer.normalized * (grappleHorizPullForce - horizPlayerMovement.magnitude * 0.55f), ForceMode.Acceleration);

            groundElapsed = (s.PlayerMovement.Collision.Grounded ? groundElapsed += Time.fixedDeltaTime : 0f);
            wallIntersectElapsed = (Physics.Linecast(transform.position, grapplePoint.position + wallNormal, GrappleRopeIntersects) ? wallIntersectElapsed += Time.fixedDeltaTime : 0f);

            yield return new WaitForFixedUpdate();
        }

        ResetGun();
    }

    public bool SecondaryAction() => true;

    public void OnPickup(PlayerManager s)
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

        if (!grappling) grapplePoint.SetParent(transform);

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
        grappling = false;
        rope.OnStopDraw();

        Destroy(grappledToSpringJoint);

        if (grappledToRigidbody != null) grappledToRigidbody.AddForce(grappledToRigidbody.velocity * 0.2f, ForceMode.Impulse);
        grappledToRigidbody = null;
    }

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
}
