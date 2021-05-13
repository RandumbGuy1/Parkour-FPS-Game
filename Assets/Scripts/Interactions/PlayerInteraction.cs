using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interactions")]
    [SerializeField] private LayerMask Interactables;
    [SerializeField] private float interactionRange;

    [Header("Assignables")]
    [SerializeField] private GameObject textDisplay;
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private WeaponController weaponController;

    private ScriptManager s;

    void Awake()
    {
        s = GetComponent<ScriptManager>();
    }

    void Update()
    {
        CheckForInteractable();
    }

    void CheckForInteractable()
    {
        RaycastHit hit;
        bool found = false;

        if (Physics.Raycast(s.cam.position, s.cam.forward, out hit, interactionRange, Interactables))
        {
            Interactable interactable = hit.transform.GetComponent<Interactable>();

            if (interactable != null)
            {
                string text = interactable.GetDescription();
                found = text != null;

                if (found)
                {
                    GameObject obj = hit.transform.gameObject;

                    textDisplay.SetActive(true);
                    interactionText.text = text;

                    if (s.PlayerInput.interacting) Interact(interactable, obj);
                }
            } 
        }

        if (!found && interactionText.text != " ")
        {
            textDisplay.SetActive(false);
            interactionText.text = " ";
        }
    }

    void Interact(Interactable interactable, GameObject obj)
    {
         switch (interactable.type)
         {
            case Interactable.InteractionType.Button:
                interactable.OnInteract();
                 break;

            case Interactable.InteractionType.WeaponPickup:
                weaponController.AddWeapon(obj);
                interactable.OnInteract();
                break;
         }
    }
}
