﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HeadBobbing : MonoBehaviour
{
    [Header("View Bobbing")]
    public float bobSpeed;
    public float bobAmountHoriz;
    public float bobAmountVert;
    [Range(0f, 0.2f)] public float bobSmoothTime;

    private float timer;
    private bool shouldBob = false;
    private Vector3 vel = Vector3.zero;
    private Vector3 smoothOffset = Vector3.zero;

    [Header("Assignables")]
    public ScriptManager s;

    void Update()
    {
        shouldBob = s.PlayerInput.moving && s.PlayerInput.grounded && !s.PlayerInput.crouching && !s.CameraLandBob.landed && s.magnitude > 10f;
        timer = shouldBob ? timer + Time.deltaTime : 0f;

        smoothOffset = Vector3.SmoothDamp(smoothOffset, HeadBob(), ref vel, bobSmoothTime);
        Vector3 newPos = s.playerHead.position + smoothOffset;

        transform.position = newPos;
    }

    private Vector3 HeadBob()
    {
        Vector3 offset = Vector3.zero;

        if (timer > 0)
            offset = s.orientation.right * Mathf.Cos(timer * bobSpeed) * bobAmountHoriz + Vector3.up * Mathf.Sin(timer * bobSpeed * 2) * bobAmountVert;

        return offset;
    }
}
