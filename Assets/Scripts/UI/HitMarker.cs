using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HitMarker : MonoBehaviour
{
    [Header("Hitmarker Settings")]
    [SerializeField] private Color hitMarkerDefaultColor;
    [Space(10)]
    [SerializeField] private float minSize;
    [SerializeField] private float maxSize;
    [SerializeField] private float scaleSmoothTime;

    private float desiredSize = 0;
    private float newSize = 0;

    private float hitMarkerSmoothTime = 0f;
    private float hitMarkerElapsed = 0f;

    [Header("Assignables")]
    [SerializeField] private RectTransform hitMarker;
    private TextMeshProUGUI[] hitMarkers;

    void Awake() => hitMarkers = transform.GetComponentsInChildren<TextMeshProUGUI>();

    void Update()
    {
        UpdateHitMarkers();
        UpdateScale(Vector2.zero);
    }

    private void UpdateScale(Vector2 vel)
    {
        desiredSize = Mathf.Clamp(desiredSize, minSize, maxSize);
        desiredSize = Mathf.SmoothDamp(desiredSize, minSize, ref vel.x, scaleSmoothTime * 3f);
        newSize = Mathf.SmoothDamp(newSize, desiredSize, ref vel.y, scaleSmoothTime);

        hitMarker.sizeDelta = Vector2.one * newSize;
    }

    public void Flash(Vector3 pos, bool kill = false, float time = 0.4f)
    {
        gameObject.SetActive(true);
        hitMarkers[0].color = kill ? Color.red : Color.white;
        hitMarkerSmoothTime = time;
        hitMarkerElapsed = 0f;

        desiredSize += 50f;

        if (kill) ObjectPooler.Instance.Spawn("SparkFX", pos, Quaternion.identity);
    }

    private void UpdateHitMarkers()
    {
        if (hitMarkerElapsed >= hitMarkerSmoothTime)
        {
            desiredSize = minSize;
            hitMarkerElapsed = hitMarkerSmoothTime;
            gameObject.SetActive(false);
            return;
        }

        hitMarkers[0].color = Color.Lerp(hitMarkers[0].color, hitMarkerDefaultColor, Mathf.SmoothStep(0, 1, hitMarkerElapsed / hitMarkerSmoothTime));
        foreach (TextMeshProUGUI childHitMarker in hitMarkers) childHitMarker.color = hitMarkers[0].color;

        hitMarkerElapsed += Time.deltaTime;
    }
}
