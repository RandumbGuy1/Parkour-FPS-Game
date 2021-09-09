using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ValueSlider : MonoBehaviour
{
    private float fillSmoothTime = 0;
    private float desiredFill = 0;
    private Vector2 fillVel = Vector2.zero;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        CalculateSlideFill();
    }

    private void CalculateSlideFill()
    {
        if (desiredFill <= 0f) return;

        desiredFill = Mathf.SmoothDamp(desiredFill, 0, ref fillVel.y, fillSmoothTime * 2f);
        slider.value = Mathf.SmoothDamp(slider.value, desiredFill, ref fillVel.x, fillSmoothTime);

        if (desiredFill < 0.01f)
        {
            desiredFill = 0f;
            gameObject.SetActive(false);
        }
    }

    public void SetSliderCooldown(float ratio, float smoothTime)
    {
        gameObject.SetActive(true);

        desiredFill = ratio;
        fillSmoothTime = smoothTime;
    }
}
