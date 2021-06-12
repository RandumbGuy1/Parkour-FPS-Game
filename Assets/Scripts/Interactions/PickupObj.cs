using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupObj : MonoBehaviour
{
    [Header("Grab detection")]
    [SerializeField] private LayerMask Objects;
    [SerializeField] private float grabRange;
    [SerializeField] private float throwForce;
    [SerializeField] private float grabRadius;
    [SerializeField] private float objSpeed;
    [SerializeField] private float objSmoothing;
    [SerializeField] private float maxGrabDistance;

    private Vector3 smoothVel = Vector3.zero;
    private Vector3 vel = Vector3.zero;
    private float storedDrag;
    private float storedAngularDrag;

    [Header("Assignables")]
    [SerializeField] private Transform grabPos;
    private GameObject heldObj;
    private Rigidbody objRb;

    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && heldObj == null)
            if (Physics.SphereCast(s.cam.position, grabRadius, s.cam.forward, out var hit, grabRange, Objects)) Pickup(hit.transform.gameObject);

        if (heldObj == null) return;

        if (Input.GetMouseButtonUp(0) || (grabPos.position - heldObj.transform.position).sqrMagnitude > maxGrabDistance * maxGrabDistance)
            Drop();
    }

    void FixedUpdate()
    {
        if (heldObj != null) CheckObject();
    }

    private void CheckObject()
    {
        Vector3 followVel = (grabPos.position - heldObj.transform.position);
        followVel = Vector3.ClampMagnitude(followVel, 25f);

        smoothVel = Vector3.SmoothDamp(smoothVel, followVel, ref vel, objSmoothing);

        objRb.velocity = smoothVel * objSpeed;
    }

    private void Pickup(GameObject obj)
    {
        objRb = obj.transform.GetComponent<Rigidbody>();

        if (objRb != null)
        {
            storedDrag = objRb.drag;
            storedAngularDrag = objRb.angularDrag;

            objRb.useGravity = false;
            objRb.drag = 0f;
            objRb.angularDrag = 0.05f;
            heldObj = obj;
        }
    }

    private void Drop()
    {
        objRb.useGravity = true;

        objRb.drag = storedDrag;
        objRb.angularDrag = storedAngularDrag;

        Vector3 rand = Vector3.zero;

        rand.x = Random.Range(-1f, 1f);
        rand.y = Random.Range(-1f, 1f);
        rand.z = Random.Range(-1f, 1f);

        objRb.velocity *= throwForce * (s.PlayerMovement.magnitude * 0.01f + 1f) * 1.5f;
        objRb.AddTorque(rand.normalized * throwForce, ForceMode.VelocityChange);

        heldObj = null;
        objRb.transform.parent = null;
        objRb = null;
    }
}
