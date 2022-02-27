using UnityEngine;

public class DynaimcReticle : MonoBehaviour
{
    [Header("Assignables")]
    [SerializeField] private RectTransform reticle;
    [SerializeField] private PlayerManager s;

    [Header("Dynamic Reticle")]
    [SerializeField] private float resizeSmoothTime;
    [SerializeField] private float recoilSmoothTime;
    [SerializeField] private float minSize;
    [SerializeField] private float maxSize;
    [Space(15)]
    [SerializeField] [Range(0f, 50f)] private float aimSize;

    private float size;
    private float desiredSize;

    private float desiredRecoilSize = 0;
    private float smoothRecoilSize = 0;

    private float vel = 0f, recoilVel = 0f;

    private void OnEnable() => ResetReticle();
    void LateUpdate() => Reticle();

    private void Reticle()
    {
        bool aiming = s.WeaponControls.Aiming;

        desiredSize = Mathf.Pow((s.PlayerMovement.Magnitude * 0.7f + (s.CameraLook.RotationDelta.sqrMagnitude * 10f)) * 5f, 1.2f);
        desiredSize = Mathf.Clamp(desiredSize, (aiming ? aimSize : minSize), (aiming ? aimSize : maxSize));

        UpdateRecoilSize();

        size = Mathf.SmoothDamp(size, desiredSize, ref vel, resizeSmoothTime);
        reticle.sizeDelta = Vector2.one * size + Vector2.one * smoothRecoilSize;
    }

    public void AddReticleRecoil(float amount = 0) => desiredRecoilSize += amount * 0.5f;
    public void ResetReticle() => size = aimSize;

    private void UpdateRecoilSize()
    {
        if (desiredRecoilSize <= 0f && smoothRecoilSize <= 0) return;

        desiredRecoilSize = Mathf.Lerp(desiredRecoilSize, 0f, 7f * Time.deltaTime);
        smoothRecoilSize = Mathf.SmoothDamp(smoothRecoilSize, desiredRecoilSize, ref recoilVel, recoilSmoothTime);

        if (desiredRecoilSize < 0.005f && smoothRecoilSize < 0.005f)
        {
            desiredRecoilSize = 0f;
            smoothRecoilSize = 0f;
        }
    }
}
