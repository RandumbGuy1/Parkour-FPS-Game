using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public enum InteractionType
    {
        pickup,
        button,
    }

    public InteractionType type;

    public abstract string GetDiscription();
    public abstract string OnInteract();
}
