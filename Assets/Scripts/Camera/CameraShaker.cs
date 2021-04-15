using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [HideInInspector] public Vector3 offset = Vector3.zero;
    public bool isShaking = false;

    public void ShakeOnce(float magnitude, float frequency, float rise, float run, float pauseDuration)
    {
        StartCoroutine(Shake(magnitude, frequency, rise, run, pauseDuration));
    }

    IEnumerator Shake(float magitude, float frequency, float rise, float run, float pauseDuration)
    {
        float vel = 0f;

        float trama = 0.1f;
        float elapsedPause = 0f;
        bool induceTrama = true;

        float scroller = 0f;
        float seed = Random.value;

        while (trama > 0.01f)
        {
            isShaking = true;

            if (induceTrama)
            {
                trama = Mathf.SmoothDamp(trama, 1f, ref vel, rise);

                if (trama >= 0.9f)
                {
                    trama = 1f;
                    elapsedPause += Time.deltaTime;
                    if (elapsedPause >= pauseDuration) induceTrama = false;
                }
            }

            if (!induceTrama)
            {
                trama = Mathf.SmoothDamp(trama, 0f, ref vel, run);
                run -= Time.deltaTime * run;
            }

            scroller += Time.deltaTime * frequency;

            offset.x = Mathf.PerlinNoise(seed, scroller);
            offset.y = Mathf.PerlinNoise(seed + 1f, scroller);
            offset.z = Mathf.PerlinNoise(seed + 2f, scroller);

            offset -= Vector3.one * 0.5f;
            offset *= magitude * trama;

            yield return null;
        }

        offset = Vector3.zero;
        isShaking = false;
    }
}
