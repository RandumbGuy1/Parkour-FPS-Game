using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonDoor : Interactable
{
    [Header("Door Interaction")]
    public Transform door;
    [Space(15)]
    public float openSmoothTime;
    public float doorOffset;
    public float buttonOffset;
    public bool useOnce;

    [Header("Interaction Hints")]
    public string openHint;
    public string closeHint;

    private Vector3 originalButtonPos;
    private Vector3 originalDoorPos;

    private Vector3 dOffset;
    private Vector3 bOffset;
    private float elapsed = 0f;

    private bool opened = false;

    void Start()
    {
        elapsed = openSmoothTime;

        originalDoorPos = door.position;
        dOffset = originalDoorPos;
        dOffset.y += doorOffset;

        originalButtonPos = transform.position;
        bOffset = originalButtonPos;
        bOffset.y -= buttonOffset;
    }

    void LateUpdate()
    {
        if (opened)
            if (elapsed < openSmoothTime)
            {
                door.position = Vector3.Lerp(door.position, dOffset, elapsed / openSmoothTime);
                transform.position = Vector3.Lerp(transform.position, bOffset, elapsed / openSmoothTime);
                elapsed += Time.deltaTime;
            }
        
        if (!opened)
            if (elapsed < openSmoothTime)
            {
                door.position = Vector3.Lerp(door.position, originalDoorPos, elapsed / openSmoothTime);
                transform.position = Vector3.Lerp(transform.position, originalButtonPos, elapsed / openSmoothTime);
                elapsed += Time.deltaTime;
            }
    }

    public override string GetDiscription()
    {
        if (!opened) return openHint;
        else return closeHint;
    }

    public override void OnInteract()
    {
        elapsed = 0f;
        opened = !opened;
    }
}
