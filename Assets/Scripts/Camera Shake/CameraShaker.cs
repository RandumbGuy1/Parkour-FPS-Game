using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    public Vector3 Offset { get; private set; }

    private List<ShakeEvent> shakeEvents = new List<ShakeEvent>();

    public void ShakeOnce(float magnitude, float frequency, float duration, float smoothness, ShakeData.ShakeType type, Vector3 initialKickback = default(Vector3))
    {
        AnimationCurve blendOverLifetime = new AnimationCurve(
        new Keyframe(0.0f, 0.0f, Mathf.Deg2Rad * 0.0f, Mathf.Deg2Rad * 720.0f),
        new Keyframe(0.2f, 1.0f),
        new Keyframe(1.0f, 0.0f));

        shakeEvents.Add(new ShakeEvent(magnitude, frequency, duration, smoothness, blendOverLifetime, type, initialKickback));
    }

    public void ShakeOnce(ShakeData sd, Vector3 initialKickback = default(Vector3))
    {
        shakeEvents.Add(new ShakeEvent(sd.magnitude, sd.frequency, sd.duration, sd.smoothness, sd.blendOverLifetime, sd.type, initialKickback));
    }

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
