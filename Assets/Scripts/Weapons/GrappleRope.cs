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

    private float springTarget = 0;
    private float springVelocity = 0;
    private float springValue = 0;

    private Vector3 grapplePoint;
    private Vector3 reachedGrapplePoint = Vector3.zero;

    private Vector3 returnVel = Vector3.zero;
    private bool grappling = false;

    private void Start()
    {
        lr.positionCount = 0;
    }

    public void DrawRope(Vector3 grapplePoint)
    {
        lr.positionCount = vertexCount;
        springVelocity = velocity;

        this.grapplePoint = grapplePoint;

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

        Vector3 targetPoint = (grappling ? grapplePoint : transform.position);
        if ((targetPoint - reachedGrapplePoint).sqrMagnitude < 0.4f && !grappling)
        {
            lr.positionCount = 0;
            return;
        }

        reachedGrapplePoint = Vector3.SmoothDamp(reachedGrapplePoint, targetPoint, ref returnVel, grappling ? 0.15f : (targetPoint - reachedGrapplePoint).sqrMagnitude < 9f ? 0.005f : 0.02f);

        UpdateSpring(Time.smoothDeltaTime);
        Vector3 up = Quaternion.LookRotation((grapplePoint - transform.position).normalized) * Vector3.up;

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
