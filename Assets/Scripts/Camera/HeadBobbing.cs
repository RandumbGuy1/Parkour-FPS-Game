using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HeadBobbing : MonoBehaviour
{
    [Header("View Bobbing")]
    public float bobSpeed;
    public float bobAmountHoriz;
    public float bobAmountVert;
    public float bobSmoothTime;

    private float timer;
    private bool shouldBob = false;
    private Vector3 vel = Vector3.zero;

    private Vector3 newPos;
    private Vector3 smoothOffset = Vector3.zero;

    [Header("Assignables")]
    public ScriptManager s;
    public Transform playerHead;
    public Transform orientation;

    void Update()
    {
        shouldBob = s.PlayerInput.moving && s.PlayerInput.grounded && !s.PlayerInput.crouching && !s.Effects.landed && s.rb.velocity.magnitude > 10f;

        if (!shouldBob) timer = 0f;
        else timer += Time.deltaTime;

        smoothOffset = Vector3.SmoothDamp(smoothOffset, HeadBob(timer), ref vel, bobSmoothTime);
        newPos = playerHead.position + smoothOffset;

        transform.position = newPos;
    }

    private Vector3 HeadBob(float t)
    {
        float horizOffset = 0f;
        float vertOffset = 0f;
        Vector3 offset = Vector3.zero;

        if (t > 0)
        {
            horizOffset = Mathf.Cos(t * bobSpeed) * bobAmountHoriz;
            vertOffset = Mathf.Sin(t * bobSpeed * 2) * bobAmountVert;

            offset = orientation.right * horizOffset + Vector3.up * vertOffset;
        }

        return offset;
    }
}
