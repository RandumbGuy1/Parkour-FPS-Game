using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IShakeEvent
{
    ShakeData ShakeData { get; }

    Vector3 ShakeOffset { get; }
    bool Finished { get; }

    void UpdateShake(float deltaTime);
}
