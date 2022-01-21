using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeManager : MonoBehaviour
{
    [Header("Receivers")]
    [SerializeField] private List<CameraShaker> shakeRecievers = new List<CameraShaker>();
    public static ShakeManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShakeAll(IShakeEvent shakeEvent, Vector3 shakeSource)
    {
        for (int i = 0; i < shakeRecievers.Count; i++)
        { 
            shakeEvent.ShakeData.Magnitude *= CalculateDistanceBasedMagnitude(shakeRecievers[i].transform.position, shakeSource);
            shakeRecievers[i].ShakeOnce(shakeEvent);
        }
    }

    public void ShakeAll(IShakeEvent shakeEvent)
    {
        for (int i = 0; i < shakeRecievers.Count; i++) shakeRecievers[i].ShakeOnce(shakeEvent);
    }

    private float CalculateDistanceBasedMagnitude(Vector3 a, Vector3 b)
    {
        float distance = (Vector3.Distance(a, b) * 0.5f) - 5f;
        distance = Mathf.Clamp(distance, 1f, 15f);
        distance = Mathf.Clamp(1f / distance, 0.3f, 1.25f);

        return distance;
    }
}
