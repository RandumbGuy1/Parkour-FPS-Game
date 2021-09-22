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
                magnitude *= CalculateDistanceBasedMagnitude(shakeRecievers[i].transform.position, shakeSource);

            shakeRecievers[i].ShakeOnce(magnitude, frequency, duration, smoothness, type);
        }
    }

    public void ShakeAll(ShakeData sd, Vector3 shakeSource = default(Vector3))
    {
        for (int i = 0; i < shakeRecievers.Count; i++)
        { 
            if (shakeSource != default(Vector3)) 
                sd.magnitude *= CalculateDistanceBasedMagnitude(shakeRecievers[i].transform.position, shakeSource);

            shakeRecievers[i].ShakeOnce(sd);
        }
    }

    private float CalculateDistanceBasedMagnitude(Vector3 a, Vector3 b)
    {
        float distance = Vector3.Distance(a, b) * 0.4f;

        distance = Mathf.Clamp(distance, 0f, 20f);
        distance = 1f - (distance / 20f);
        distance = Mathf.Clamp(distance, 0.2f, 1f);

        print(distance);
        return distance;
    }
}
