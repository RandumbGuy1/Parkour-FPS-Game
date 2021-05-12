using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupObj : MonoBehaviour
{
    [Header("Grab detection")]
    public LayerMask Objects;
    public float grabRange;
    public float throwForce;
    public float grabRadius;
    public float objSpeed;
    public float maxGrabDistance;

    float setThrowForce;

    [Header("Assignables")]
    public Transform grabPos;
    private GameObject heldObj;
    private Rigidbody objRb;

    private ScriptManager s;

    bool grabbing;
    float storedDrag;
    float storedAngularDrag;
    Vector3 vel = Vector3.zero;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        setThrowForce = throwForce;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (Physics.SphereCast(s.cam.position, grabRadius, s.cam.forward, out hit, grabRange, Objects))
                StartCoroutine(Pickup(hit.transform.gameObject));
        }

        if (heldObj != null) CheckObject();
    }

    private void CheckObject()
    {
        if ((grabPos.position - heldObj.transform.position).sqrMagnitude > 0.01f)
            heldObj.transform.position = Vector3.SmoothDamp(heldObj.transform.position, grabPos.position, ref vel, objSpeed);

        objRb.velocity = Vector3.zero;

        if (Input.GetMouseButtonUp(0) || (grabPos.position - heldObj.transform.position).sqrMagnitude > maxGrabDistance * maxGrabDistance && grabbing) 
            Drop();
    }

    private IEnumerator Pickup(GameObject obj)
    {
        objRb = obj.transform.GetComponent<Rigidbody>();
        if (objRb == null) yield return null;

        if (objRb.mass <= 3f)
        {
            storedDrag = objRb.drag;
            storedAngularDrag = objRb.angularDrag;

            objRb.useGravity = false;
            objRb.drag = 0f;
            objRb.angularDrag = 1f;
            heldObj = obj;

            yield return new WaitForSeconds(0.5f);

            grabbing = true;
        }

        yield return null;
    }

    private void Drop()
    {
        objRb.useGravity = true;
        objRb.velocity = Vector3.zero;

        throwForce = setThrowForce + (s.velocity.magnitude * 0.3f);

        objRb.velocity = throwForce * (grabPos.position - heldObj.transform.position);
        objRb.drag = storedDrag;
        objRb.angularDrag = storedAngularDrag;

        objRb.transform.parent = null;
        objRb = null;
        heldObj = null;
        grabbing = false;
    }
}
