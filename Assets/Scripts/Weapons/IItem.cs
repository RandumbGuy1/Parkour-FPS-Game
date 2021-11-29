using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItem
{
    float weight { get; }

    Vector3 defaultPos { get; }
    Vector3 defaultRot { get; }

    Vector3 aimPos { get; }
    Vector3 aimRot { get; }

    Sprite itemSprite { get; }

    string ReadData();
    void OnPickup();
    void OnDrop();
}
