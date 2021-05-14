using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynaimcReticle : MonoBehaviour
{
    [Header("Assignables")]
    public RectTransform reticle;
    public ScriptManager s;

    [Header("Dynamic Reticle")]
    public float smoothTime;
    public float minSize;
    public float maxSize;
    private float camMag;
    private float size;
    private float newSize;
    private float vel = 0f;

    void LateUpdate()
    {
        Reticle();
    }

    private void Reticle()
    {
        newSize = Mathf.Pow((s.magnitude + s.CameraInput.camVel) * 5f, 1.3f);
        newSize = Mathf.Clamp(newSize, minSize, maxSize);

        size = Mathf.SmoothDamp(size, newSize, ref vel, smoothTime);
        reticle.sizeDelta = new Vector2(size, size);
    }
}
