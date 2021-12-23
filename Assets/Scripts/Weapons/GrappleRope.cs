using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleRope : MonoBehaviour
{
    [Header("Rope Settings")]
    [SerializeField] private LineRenderer lr;
    [SerializeField] private int vertexCount;
    [Space(10)]
    [SerializeField] private float strength;
    [SerializeField] private float damper;
    [SerializeField] private float waveHeight;
    [SerializeField] private float waveCount;
    [SerializeField] private float velocity;

    [Header("Assignables")]
    [SerializeField] private Transform grapplePoint;

    private float springTarget = 0;
    private float springVelocity = 0;
    private float springValue = 0;

    private Vector3 reachedGrapplePoint = Vector3.zero;
    private Vector3 returnVel = Vector3.zero;
    private bool grappling = false;

    private void Start() => lr.positionCount = 0;

    public void DrawRope()
    {
        lr.positionCount = vertexCount;
        springVelocity = velocity;

        grappling = true;
    }

    public void OnStopDraw()
    {
        grappling = false;
        springVelocity = 0;
    }

    public void ResetRope() => lr.positionCount = 0;

    void LateUpdate()
    {
        if (lr.positionCount <= 0)
        {
            reachedGrapplePoint = transform.position;
            return;
        }

        Vector3 targetPoint = (grappling ? grapplePoint.position : transform.position);
        if ((targetPoint - reachedGrapplePoint).sqrMagnitude < 0.4f && !grappling)
        {
            lr.positionCount = 0;
            return;
        }

        bool closeToGrapplePoint = (targetPoint - reachedGrapplePoint).sqrMagnitude < 10f;

        reachedGrapplePoint = Vector3.SmoothDamp(reachedGrapplePoint, targetPoint, ref returnVel, grappling ? (closeToGrapplePoint ? 0.01f : 0.15f) : (closeToGrapplePoint ? 0.005f : 0.02f));

        UpdateSpring(Time.smoothDeltaTime);
        Vector3 up = Quaternion.LookRotation((grapplePoint.position - transform.position).normalized) * Vector3.up;

        for (int i = 0; i < vertexCount; i++)
        {
            var delta = i / (float) vertexCount;
            var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * springValue;
            lr.SetPosition(i, Vector3.Lerp(transform.position, reachedGrapplePoint, delta) + offset);
        }
    }

    private void UpdateSpring(float deltaTime)
    {
        float direction = springTarget - springValue >= 0 ? 1f : -1f;
        float force = Mathf.Abs(springTarget - springValue) * strength * 50;
        springVelocity += (force * direction - springVelocity * damper) * deltaTime;
        springValue += springVelocity * deltaTime;
    }
}
