using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HeadBobbing : MonoBehaviour
{
    [Header("View Bob Settings")]
    [SerializeField] private float bobSpeed;
    [SerializeField] private float bobAmountHoriz;
    [SerializeField] private float bobAmountVert;
    [Range(0f, 0.2f)] [SerializeField] private float bobSmoothTime;

    private float timer;
    private Vector3 vel = Vector3.zero;
    private Vector3 smoothOffset = Vector3.zero;

    [Header("Assignables")]
    [SerializeField] private ScriptManager s;

    void Update()
    {
        timer = s.PlayerInput.moving && s.PlayerInput.grounded && !s.PlayerInput.crouching && !s.CameraLandBob.landed && s.PlayerMovement.magnitude > 5f ? timer + Time.deltaTime : 0f;

        smoothOffset = Vector3.SmoothDamp(smoothOffset, HeadBob(), ref vel, bobSmoothTime);
        Vector3 newPos = s.playerHead.position + smoothOffset;

        transform.position = newPos;
    }

    private Vector3 HeadBob()
    {
        Vector3 offset = Vector3.zero;
        if (timer > 0) offset = s.orientation.right * Mathf.Cos(timer * bobSpeed) * bobAmountHoriz + Vector3.up * Mathf.Sin(timer * bobSpeed * 2) * bobAmountVert;
        return offset;
    }
}
