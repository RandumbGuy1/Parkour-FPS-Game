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
    [SerializeField] private float recoilSmoothTime;
    [SerializeField] [Range(100f, 200f)] private float minSize;
    [SerializeField] [Range(200f, 300f)] private float maxSize;
    [Space(15)]
    [SerializeField] [Range(0f, 50f)] private float minAimSize;

    private float size;
    private float newSize;

    private float desiredRecoilSize = 0;
    private float smoothRecoilSize = 0;

    private float vel = 0f, recoilVel = 0f;

    private void OnDisable() => size = minSize;
    void LateUpdate() => Reticle();

    private void Reticle()
    {
        bool aiming = s.WeaponControls.aiming;

        newSize = Mathf.Pow((s.PlayerMovement.Magnitude + (s.CameraLook.RotationDelta.sqrMagnitude * 6f)) * 5f, 1.3f);
        newSize = Mathf.Clamp(newSize, (aiming ? minAimSize : minSize), (aiming ? minAimSize : maxSize));

        UpdateRecoilSize();

        size = Mathf.SmoothDamp(size, newSize, ref vel, smoothTime);
        reticle.sizeDelta = Vector2.one * size + Vector2.one * smoothRecoilSize;
    }

    public void AddReticleRecoil(float amount = 0) => desiredRecoilSize += amount;

    private void UpdateRecoilSize()
    {
        if (desiredRecoilSize <= 0f && smoothRecoilSize <= 0) return;

        desiredRecoilSize = Mathf.Lerp(desiredRecoilSize, 0f, 3f * Time.deltaTime);
        smoothRecoilSize = Mathf.SmoothDamp(smoothRecoilSize, desiredRecoilSize, ref recoilVel, recoilSmoothTime);

        if (desiredRecoilSize < 0.005f && smoothRecoilSize < 0.005f)
        {
            desiredRecoilSize = 0f;
            smoothRecoilSize = 0f;
        }
    }
}
