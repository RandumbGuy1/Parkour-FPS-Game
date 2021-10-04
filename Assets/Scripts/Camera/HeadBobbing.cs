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
    [Range(0f, 0.5f)] [SerializeField] private float bobSmoothTime;

    private float timer;
    private Vector3 vel = Vector3.zero;
    private Vector3 smoothOffset = Vector3.zero;

    [Header("Step Settings")]
    [SerializeField] private float stepSmoothTime;

    [HideInInspector] 
    public Vector3 vaultDesync = Vector3.zero;
    private Vector3 vaultVel = Vector3.zero;

    [Header("Assignables")]
    [SerializeField] private ScriptManager s;

    void LateUpdate()
    {
        timer = s.PlayerMovement.Moving && s.PlayerMovement.Grounded && s.PlayerMovement.CanCrouchWalk && s.PlayerMovement.Magnitude > 0.5f ? timer + Time.deltaTime : 0f;

        smoothOffset = Vector3.SmoothDamp(smoothOffset, HeadBob(), ref vel, bobSmoothTime);
        Vector3 newPos = s.playerHead.position + smoothOffset + vaultDesync;

        transform.position = newPos;
    }

    private Vector3 HeadBob()
    {
        if (timer <= 0) return Vector3.zero;

        return s.orientation.right * Mathf.Cos(timer * bobSpeed) * bobAmountHoriz + Vector3.up * Math.Abs(Mathf.Sin(timer * bobSpeed)) * bobAmountVert;
    }
    
    public void StepUp(Vector3 offset)
    {
        vaultDesync = Vector3.zero + offset;

        StopAllCoroutines();
        StartCoroutine(StepOffset());
    }

    private IEnumerator StepOffset()
    {
        Vector3 vel = Vector3.zero;

        while (vaultDesync != Vector3.zero)
        {
            vaultDesync = Vector3.SmoothDamp(vaultDesync, Vector3.zero, ref vel, stepSmoothTime);
            yield return null;
        }

        vaultDesync = Vector3.zero;
    }
}
