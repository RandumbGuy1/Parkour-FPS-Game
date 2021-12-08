using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleRope : MonoBehaviour
{
    [Header("Rope Settings")]
    [SerializeField] private LineRenderer lr;
    [SerializeField] private int quality;
    [SerializeField] private float damper;
    [SerializeField] private float strength;
    [SerializeField] private float velocity;
    [SerializeField] private float wobbleCount;
    [SerializeField] private float waveCount;
    [SerializeField] private float waveHeight;
    [SerializeField] private AnimationCurve blendOverLifeTime = new AnimationCurve(
      new Keyframe(0.0f, 0.0f, Mathf.Deg2Rad * 0.0f, Mathf.Deg2Rad * 720.0f),
      new Keyframe(0.2f, 1.0f),
      new Keyframe(1.0f, 0.0f));

    private float springValue;
    private float sprintTarget;
    private float springVelocity;

    private Transform gunTip;
    private Vector3 grapplePoint;
    private bool grappling = false;

    public void DrawRope(Transform gunTip, Vector3 grapplePoint)
    {
        lr.positionCount = 2;

        this.gunTip = gunTip;
        this.grapplePoint = grapplePoint;
        grappling = true;
    }

    public void OnStopDraw()
    {
        springVelocity = 0f;
        springValue = 0f;
        grappling = false;

        if (lr.positionCount > 0) lr.positionCount = 0;
    }

    public void Update()
    {
        if (!grappling) return;

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, grapplePoint);
    }

    /*
    private IEnumerator AnimateRope(Vector3 gunTip, Vector3 grapplePoint)
    {
        Vector2 angle = LookAtAngle(grapplePoint - gunTip);

        float percent = 0f;
        while (percent <= 1f)
        {
            percent += Time.deltaTime * velocity;
            SetPoints(grapplePoint, gunTip, percent, angle);
            yield return null;
        }

        SetPoints(grapplePoint, gunTip, 1f, angle);
    }

    private void SetPoints(Vector3 targetPos, Vector3 gunTipPos, float percent, Vector2 angle)
    {
        Vector3 ropeEnd = Vector3.Lerp(gunTipPos, targetPos, percent);
        float length = Vector3.Distance(gunTipPos, ropeEnd);

        for (int i = 0; i < quality + 1; i++)
        {
            Vector3 ropePos = Vector3.zero;

            ropePos.x = (float)i / (quality + 1) * length;

            float reversePercent = i - percent;
            float amplitude = Mathf.Sin(reversePercent * wobbleCount * Mathf.PI);
            
            ropePos.y = Mathf.Sin(waveCount * i / (quality + 1) * 2f * Mathf.PI * reversePercent) * amplitude;

            Vector3 finalPos = RotatePoint((gunTipPos + ropePos), gunTipPos, angle);
        }
    }

    private Vector3 RotatePoint(Vector3 point, Vector3 pivot, Vector2 angle)
    {
        Vector3 dir = point - pivot;
        dir = Quaternion.Euler(0, angle.y, angle.x) * dir;
        return dir + pivot;
    }

    private Vector2 LookAtAngle(Vector3 targetPos)
    {
        return new Vector2(Mathf.Atan2(targetPos.y, targetPos.x), Mathf.Atan2(targetPos.y, targetPos.z));
    }

    private void DrawRope()
    {
        UpdateSpring(Time.smoothDeltaTime);

        Vector3 gunTipPos = gunTip.position;
        Vector3 grapplePoint = this.grapplePoint;

        Vector3 up = Quaternion.LookRotation((grapplePoint - gunTipPos).normalized) * Vector3.up;
        grapplingPosition = Vector3.Lerp(grapplingPosition, grapplePoint, Time.deltaTime * 12f);

        for (var i = 0; i < quality + 1; i++)
        {
            var delta = i / (float)quality;
            var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * springValue * blendOverLifeTime.Evaluate(delta);

            lr.SetPosition(i, Vector3.Lerp(gunTipPos, grapplePoint, delta) + offset);
        }
    }

    private void UpdateSpring(float deltaTime)
    {
        float direction = sprintTarget - springValue >= 0 ? 1f : -1f;
        float force = Mathf.Abs(sprintTarget - springValue) * strength;
        springVelocity += (force * direction - springVelocity * damper) * deltaTime;
        springValue += springVelocity * deltaTime;
    }
    */
}
