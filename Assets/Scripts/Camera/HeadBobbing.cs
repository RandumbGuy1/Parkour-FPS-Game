using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HeadBobbing : MonoBehaviour
{
    [Header("View Bobbing")]
    public float bobSpeed;
    public float bobAmount;
    public float returnSmoothTime;

    private float waveSlice;
    private float timer;
    private float vel = 0;
    private float newPos;
    private bool bobbedLastFrame;

    [Header("Assignables")]
    public ScriptManager s;
    public Transform playerHead;

    void Update()
    {
        newPos = waveSlice;
        transform.position = playerHead.position + (Vector3.up * waveSlice);

        if (s.PlayerInput.moving && s.PlayerInput.grounded && !s.PlayerInput.crouching && !s.Effects.landed && s.rb.velocity.magnitude > 20f && s.cam.localPosition.y >= -0.1f && !bobbedLastFrame)
        {
            timer += bobSpeed * Time.smoothDeltaTime;
            waveSlice = Mathf.Sin(timer) * bobAmount;
        }
        else
        {
            timer = 0f;
            waveSlice = Mathf.SmoothDamp(waveSlice, 0, ref vel, returnSmoothTime);
            bobbedLastFrame = !InRange(waveSlice, -0.1f, 0.1f);
        }

        if (timer > Mathf.PI * 2) timer = 0f;
    }

    private bool InRange(float x, float min, float max)
    {
        return x >= min && x <= max;
    }
}
