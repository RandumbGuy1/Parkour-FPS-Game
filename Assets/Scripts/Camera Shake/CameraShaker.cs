using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    public Vector3 Offset { get; private set; }

    private bool canAddShakes = true;
    private List<ShakeEvent> shakeEvents = new List<ShakeEvent>();

    public void ShakeOnce(float magnitude, float frequency, float duration, float smoothness, ShakeData.ShakeType type, Vector3 initialKickback = default(Vector3))
    {
        if (!canAddShakes) return;

        ShakeData shakeData = ScriptableObject.CreateInstance<ShakeData>();
        shakeData.Intialize(magnitude, frequency, duration, smoothness, type);

        shakeEvents.Add(new ShakeEvent(shakeData, initialKickback));
    }

    public void ShakeOnce(ShakeData shakeData, Vector3 initialKickback = default(Vector3))
    {
        if (!canAddShakes) return;

        shakeEvents.Add(new ShakeEvent(shakeData, initialKickback));
    }

    public void DisableShakes() => canAddShakes = false;

    void LateUpdate()
    {
        Vector3 rotationOffset = Vector3.zero;

        if (shakeEvents.Count > 0)
        {
            for (int i = shakeEvents.Count - 1; i != -1; i--)
            {
                ShakeEvent shake = shakeEvents[i];

                if (shake.Finished)
                {
                    shakeEvents.RemoveAt(i);
                    continue;
                }

                shake.UpdateShake();
                rotationOffset += shake.Displacement;
            }
        }

        Offset = rotationOffset;
    }
}
