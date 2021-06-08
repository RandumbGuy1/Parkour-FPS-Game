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
    [SerializeField] private float maxGrabDistance;

    private float storedDrag;
    private float storedAngularDrag;
    private float setThrowForce;

    [Header("Assignables")]
    [SerializeField] private Transform grabPos;
    private GameObject heldObj;
    private Rigidbody objRb;

    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        setThrowForce = throwForce;
    }

    void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (Physics.SphereCast(s.cam.position, grabRadius, s.cam.forward, out hit, grabRange, Objects))
                Pickup(hit.transform.gameObject);
        }

        if (heldObj != null) CheckObject();
    }

    private void CheckObject()
    {
        Vector3 followVel = (grabPos.position - heldObj.transform.position);
        followVel = Vector3.ClampMagnitude(followVel, 25f);

        objRb.AddForce(followVel * objSpeed * 15f, ForceMode.VelocityChange);

        if (Input.GetMouseButtonUp(0) || (grabPos.position - heldObj.transform.position).sqrMagnitude > maxGrabDistance * maxGrabDistance) 
            Drop();
    }

    private void Pickup(GameObject obj)
    {
        objRb = obj.transform.GetComponent<Rigidbody>();
        if (objRb == null) return;

        if (objRb.mass <= 3f)
        {
            storedDrag = objRb.drag;
            storedAngularDrag = objRb.angularDrag;

            objRb.useGravity = false;
            objRb.drag = 5f;
            objRb.angularDrag = 0.5f;
            heldObj = obj;
        }
    }

    private void Drop()
    {
        objRb.useGravity = true;
        objRb.velocity = Vector3.zero;

        throwForce = setThrowForce + (s.velocity.magnitude * 0.3f);

        objRb.velocity = throwForce * (grabPos.position - heldObj.transform.position);
        objRb.drag = storedDrag;
        objRb.angularDrag = storedAngularDrag;

        Vector3 rand = Vector3.zero;

        rand.x = Random.Range(-1f, 1f);
        rand.y = Random.Range(-1f, 1f);
        rand.z = Random.Range(-1f, 1f);

        objRb.AddTorque(rand.normalized * throwForce, ForceMode.VelocityChange);

        objRb.transform.parent = null;
        objRb = null;
        heldObj = null;
    }
}
