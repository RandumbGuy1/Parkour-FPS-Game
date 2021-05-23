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

    private float size;
    private float newSize;

    void LateUpdate()
    {
        Reticle();
    }

    private void Reticle()
    {
        newSize = Mathf.Pow((s.magnitude + (s.CameraInput.camVel * 4f)) * 5f, 1.3f);
        newSize = Mathf.Clamp(newSize, minSize, maxSize);

        size = Mathf.Lerp(size, newSize, smoothTime * Time.deltaTime);
        reticle.sizeDelta = new Vector2(size, size);
    }
}
