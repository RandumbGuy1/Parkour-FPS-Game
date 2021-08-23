using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonDoor : Interactable
{
    [Header("Door Interaction")]
    [SerializeField] private Transform door;
    [Space(15)]
    [SerializeField] private float openSmoothTime;
    [SerializeField] private float doorOffset;
    [SerializeField] private float buttonOffset;
    [SerializeField] private bool useOnce;

    [Header("Interaction Hints")]
    [SerializeField] private string openHint;
    [SerializeField] private string closeHint;

    private Outline outline;

    private Vector3 originalButtonPos;
    private Vector3 originalDoorPos;

    private Vector3 dOffset;
    private Vector3 bOffset;

    private bool opened = false;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline == null) return;

        outline.enabled = false;
    }

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
        return (opened ? closeHint : openHint);
    }

    public override void OnInteract()
    {
        opened = !opened;
        StopAllCoroutines();
        StartCoroutine(OpenDoor(opened));
    }

    public override void OnStartHover()
    {
        if (outline != null) outline.enabled = true;
    }

    public override void OnEndHover()
    {
        if (outline != null) outline.enabled = false;
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
