using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public enum InteractionType
    {
        WeaponPickup,
        Button,
    }

    public InteractionType type;

    public abstract string GetDescription();

    public abstract void OnInteract();
    public abstract void OnStartHover();
    public abstract void OnEndHover();
}
