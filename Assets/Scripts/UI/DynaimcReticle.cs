using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynaimcReticle : MonoBehaviour
{
    [Header("Assignables")]
    [SerializeField] private RectTransform reticle;
    [SerializeField] private ScriptManager s;

    [Header("Dynamic Reticle")]
    [SerializeField] private float smoothTime;
    [SerializeField] [Range(100f, 200f)] private float minSize;
    [SerializeField] [Range(200f, 300f)] private float maxSize;
    [Space(15)]
    [SerializeField] [Range(0f, 50f)] private float minAimSize;

    private float size;
    private float newSize;

    void LateUpdate()
    {
        Reticle();
    }

    private void Reticle()
    {
        newSize = Mathf.Pow((s.magnitude + (s.CameraInput.camVel * 6f)) * 5f, 1.3f);
        if (!s.WeaponControls.aiming) newSize = Mathf.Clamp(newSize, minSize, maxSize);
        else newSize = Mathf.Clamp(newSize, minAimSize, minAimSize);

        size = Mathf.Lerp(size, newSize, smoothTime * Time.deltaTime);
        reticle.sizeDelta = new Vector2(size, size);
    }
}
