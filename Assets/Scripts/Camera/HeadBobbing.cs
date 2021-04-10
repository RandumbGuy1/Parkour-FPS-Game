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
    private float newRot;

    [Header("Assignables")]
    public ScriptManager s;

    void Update()
    {
        newRot = waveSlice;
        transform.rotation = Quaternion.Euler(newRot, transform.rotation.y, transform.rotation.z);

        if (s.PlayerInput.moving && s.PlayerInput.grounded && !s.PlayerInput.crouching && !s.Effects.landed) 
        {
            timer += bobSpeed * Time.deltaTime;
            waveSlice = Math.Abs(Mathf.Sin(timer) * bobAmount) * -1f;
        }
        else
        {
            timer = 0f;
            waveSlice = Mathf.SmoothDamp(waveSlice, 0, ref vel, returnSmoothTime); 
        }

        if (timer > Mathf.PI * 2) timer = 0f;
    }
}
