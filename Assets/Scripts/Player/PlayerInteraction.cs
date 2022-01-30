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
    [SerializeField] private float maxGrabDistance;

    private float storedDrag;
    private float storedAngularDrag;

    [Header("Assignables")]
    [SerializeField] private GameObject textDisplay;
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private Transform grabPos;
    private LineRenderer lr;

    private GameObject heldObj;
    private Rigidbody objRb;
    private ScriptManager s;

    private Interactable interactable;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
        lr = grabPos.GetComponent<LineRenderer>();

        lr.positionCount = 0;
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

    void LateUpdate() => DrawGrabLine();

    #region Interaction Detection
    private void CheckForInteractable()
    {
        if (Physics.SphereCast(s.cam.transform.position, interactionRadius, s.cam.transform.forward, out var hit, interactionRange, Interactables, QueryTriggerInteraction.Ignore))
        {
            GameObject currentleyLookingAt = hit.transform.gameObject;

            if (interactable == null)
            {
                Interactable interactableTemp = hit.transform.GetComponent<Interactable>();

                if (interactableTemp == null)
                {
                    if (Input.GetMouseButtonDown(0) && heldObj == null && !s.WeaponControls.HoldingWeapon) Pickup(hit.transform.gameObject);
                    return;
                }

                interactable = interactableTemp;
                interactable.OnStartHover();

            }
            else if (currentleyLookingAt != interactable.gameObject)
            {
                interactable.OnEndHover();
                interactable = null;

                textDisplay.SetActive(false);
                interactionText.text = " ";
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

            if (s.PlayerInput.Interacting) Interact(interactable);

        }
        else if (interactionText.text != " ")
        {
            textDisplay.SetActive(false);
            interactionText.text = " ";

            if (interactable == null) return;

            interactable.OnEndHover();
            interactable = null;
        }
    }
    #endregion

    #region Interaction Handling
    private void Interact(Interactable interactable)
    {
         switch (interactable.type)
         {
            case Interactable.InteractionType.Button:
                interactable.OnInteract();
                 break;

            case Interactable.InteractionType.WeaponPickup:
                s.WeaponControls.AddWeapon(interactable.gameObject);
                interactable.OnInteract();
                break;
         }
    }

    void DrawGrabLine()
    {
        if (heldObj == null) return;

        lr.SetPosition(0, grabPos.position);
        lr.SetPosition(1, heldObj.transform.position);

        float vel = (grabPos.position - heldObj.transform.position).sqrMagnitude * 0.007f;
        vel = Mathf.Clamp(vel, 0.02f, 0.15f);

        lr.startWidth = vel;
        lr.endWidth = vel;
    }

    private void CheckObject()
    {
        Vector3 followVel = (grabPos.position - heldObj.transform.position);
        followVel = Vector3.ClampMagnitude(followVel, 50f);

        if ((grabPos.position - heldObj.transform.position).sqrMagnitude < 20f) objRb.drag = 8f;
        else objRb.drag = 3f;

        objRb.AddForce(0.1f * objSpeed * followVel, ForceMode.VelocityChange);
    }
    #endregion

    void GrabInput()
    {
        if (heldObj == null) return;

        if (Input.GetMouseButtonUp(0) || (grabPos.position - heldObj.transform.position).sqrMagnitude > maxGrabDistance * maxGrabDistance || !heldObj.activeSelf) Drop();
    }

    private void Pickup(GameObject obj)
    {
        objRb = obj.transform.GetComponent<Rigidbody>();

        if (objRb != null)
        {
            storedDrag = objRb.drag;
            storedAngularDrag = objRb.angularDrag;

            objRb.useGravity = false;
            objRb.drag = 3f;
            objRb.angularDrag = 0.05f;
            heldObj = obj;
        }

        lr.positionCount = 2;
    }

    private void Drop()
    {
        if (heldObj == null && objRb == null) return;

        objRb.useGravity = true;

        objRb.drag = storedDrag;
        objRb.angularDrag = storedAngularDrag;

        Vector3 rand = Vector3.zero;

        rand.x = Random.Range(-1f, 1f);
        rand.y = Random.Range(-1f, 1f);
        rand.z = Random.Range(-1f, 1f);

        objRb.velocity *= throwForce * ((s.PlayerMovement.Magnitude * 0.01f) + 1f) * 2f;
        objRb.AddTorque(rand.normalized * throwForce, ForceMode.VelocityChange);

        heldObj = null;
        objRb.transform.parent = null;
        objRb = null;

        lr.positionCount = 0;
    }

    public void OnPlayerStateChanged(UnitState newState)
    {
        if (newState != UnitState.Dead) return;

        Drop();
        enabled = false;
    }
}
