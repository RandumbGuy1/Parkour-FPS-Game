using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private LayerMask Interactables;
    [SerializeField] private float interactionRange;
    [SerializeField] private float interactionRadius;

    [Header("Grab Settings")]
    [SerializeField] private float throwForce;
    [SerializeField] private float objSpeed;
    [SerializeField] private float objSmoothing;
    [SerializeField] private float maxGrabDistance;

    private Vector3 smoothVel = Vector3.zero;
    private Vector3 vel = Vector3.zero;
    private float storedDrag;
    private float storedAngularDrag;

    [Header("Assignables")]
    [SerializeField] private GameObject textDisplay;
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private Transform grabPos;

    private GameObject heldObj;
    private Rigidbody objRb;
    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
    }

    void FixedUpdate()
    {
        if (heldObj != null) CheckObject();
    }

    void Update()
    {
        CheckForInteractable();
        GrabInput();
    }

    void GrabInput()
    {
        if (heldObj == null) return;
        if (Input.GetMouseButtonUp(0) || (grabPos.position - heldObj.transform.position).sqrMagnitude > maxGrabDistance * maxGrabDistance) Drop();
    }

    void CheckForInteractable()
    {
        if (Physics.SphereCast(s.cam.position, interactionRadius, s.cam.forward, out var hit, interactionRange, Interactables))
        {
            Interactable interactable = hit.transform.GetComponent<Interactable>();
            GameObject obj = hit.transform.gameObject;

            if (interactable == null)
            {
                if (Input.GetMouseButtonDown(0) && heldObj == null) Pickup(obj);
                return;
            }

            string text = interactable.GetDescription();
            if (text == null)
            {
                textDisplay.SetActive(false);
                interactionText.text = " ";
                return;
            }

            textDisplay.SetActive(true);
            interactionText.text = text;

            if (s.PlayerInput.interacting) Interact(interactable, obj);
        }
        else if (interactionText.text != " ")
        {
            textDisplay.SetActive(false);
            interactionText.text = " ";
        }
    }

    private void Interact(Interactable interactable, GameObject obj)
    {
         switch (interactable.type)
         {
            case Interactable.InteractionType.Button:
                interactable.OnInteract();
                 break;

            case Interactable.InteractionType.WeaponPickup:
                s.WeaponControls.AddWeapon(obj);
                interactable.OnInteract();
                break;
         }
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
