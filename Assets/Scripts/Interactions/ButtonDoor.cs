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

    private bool opened = false;

    void Start()
    {
        originalDoorPos = door.position;
        dOffset = originalDoorPos;
        dOffset.y += doorOffset;

        originalButtonPos = transform.position;
        bOffset = originalButtonPos;
        bOffset.y -= buttonOffset;
    }

    public override string GetDescription()
    {
        if (!opened) return openHint;
        else return closeHint;
    }

    public override void OnInteract()
    {
        opened = !opened;
        StopAllCoroutines();
        StartCoroutine(OpenDoor(opened));
    }

    private IEnumerator OpenDoor(bool opened)
    {
        float elapsed = 0f;

        if (opened)
            while (elapsed < openSmoothTime)
            {
                door.position = Vector3.Lerp(door.position, dOffset, elapsed / openSmoothTime);
                transform.position = Vector3.Lerp(transform.position, bOffset, elapsed / openSmoothTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        else 
            while (elapsed < openSmoothTime)
            {
                door.position = Vector3.Lerp(door.position, originalDoorPos, elapsed / openSmoothTime);
                transform.position = Vector3.Lerp(transform.position, originalButtonPos, elapsed / openSmoothTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
    }
}
