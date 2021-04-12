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

    [Header("Assignables")]
    public ScriptManager s;
    public Transform playerHead;

    void Update()
    {
        newPos = waveSlice;
        transform.position = playerHead.position + (Vector3.up * waveSlice);

        if (s.PlayerInput.moving && s.PlayerInput.grounded && !s.PlayerInput.crouching && !s.Effects.landed && !s.PlayerInput.nearWall && s.cam.localPosition.y >= -0.1f) 
        {
            timer += bobSpeed * Time.deltaTime;
            waveSlice = Mathf.Sin(timer) * bobAmount;
        }
        else
        {
            timer = 0f;
            waveSlice = Mathf.SmoothDamp(waveSlice, 0, ref vel, returnSmoothTime); 
        }

        if (timer > Mathf.PI * 2) timer = 0f;
    }
}
