using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Header("Active Shakes")]
    [SerializeField] private List<IShakeEvent> shakeEvents = new List<IShakeEvent>();

    public Vector3 Offset { get; private set; }
    private bool canAddShakes = true;

    public void ShakeOnce(IShakeEvent shakeEvent)
    {
        if (canAddShakes) shakeEvents.Add(shakeEvent);
    }

    public void DisableShakes() => canAddShakes = false;

    void LateUpdate()
    {
        if (shakeEvents.Count <= 0)
        {
            Offset = Vector3.zero;
            return;
        }

        Vector3 rotationOffset = Vector3.zero;

        for (int i = shakeEvents.Count - 1; i != -1; i--)
        {
            IShakeEvent shake = shakeEvents[i];

            if (shake.Finished)
            {
                shakeEvents.RemoveAt(i);
                continue;
            }

            shake.UpdateShake(Time.deltaTime);
            rotationOffset += shake.ShakeOffset;
        }

        Offset = rotationOffset;
    }
}
