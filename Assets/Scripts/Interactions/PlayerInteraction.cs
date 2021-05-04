using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interactions")]
    public LayerMask Interactables;
    public float interactionRange;

    [Header("Assignables")]
    public GameObject textDisplay;
    public TextMeshProUGUI interactionText;

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
            Interactable obj = hit.transform.GetComponent<Interactable>();

            if (obj != null)
            {
                string text = obj.GetDiscription();
                found = text != null;

                if (found)
                {
                    textDisplay.SetActive(true);
                    interactionText.text = text;

                    if (s.PlayerInput.interacting) Interact(obj);
                }
            } 
        }

        if (!found && interactionText.text != " ")
        {
            textDisplay.SetActive(false);
            interactionText.text = " ";
        }
    }

    void Interact(Interactable obj)
    {
         switch (obj.type)
         {
            case Interactable.InteractionType.Button:
                 obj.OnInteract();
                 break;

            case Interactable.InteractionType.WeaponPickup:
                 obj.OnInteract();
                 break;
         }
    }
}
