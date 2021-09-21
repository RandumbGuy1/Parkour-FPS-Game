using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeManager : MonoBehaviour
{
    [Header("Receivers")]
    [SerializeField] private List<CameraShaker> shakeRecievers = new List<CameraShaker>();

    public static ShakeManager Instance;

    void Awake()
    {
        if (Instance == this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShakeAll(float magnitude, float frequency, float duration, float smoothness, ShakeData.ShakeType type, Vector3 shakeSource = default(Vector3))
    {
        for (int i = 0; i < shakeRecievers.Count; i++)
        {
            if (shakeSource != default(Vector3))
            {
                float distance = Vector3.Distance(shakeRecievers[i].transform.position, shakeSource);

                distance = Mathf.Clamp(distance, 1.8f, 20f);
                distance -= 1f;
                distance = 1f - (distance / 20f);
                distance = 1f - Mathf.Pow(1f - distance, 2.7f);

                magnitude *= distance;
            }

            shakeRecievers[i].ShakeOnce(magnitude, frequency, duration, smoothness, type);
        }
    }

    public void ShakeAll(ShakeData sd, Vector3 shakeSource = default(Vector3))
    {
        for (int i = 0; i < shakeRecievers.Count; i++)
        {
            if (shakeSource != default(Vector3))
            {
                float distance = Vector3.Distance(shakeRecievers[i].transform.position, shakeSource);

                distance = Mathf.Clamp(distance, 1.8f, 20f);
                distance -= 1f;
                distance = 1f - (distance / 20f);
                distance = 1f - Mathf.Pow(1f - distance, 2.7f);

                sd.magnitude *= distance;
            }

            shakeRecievers[i].ShakeOnce(sd);
        }
    }
}
