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

    public abstract string GetDiscription();
    public abstract void OnInteract();
}
