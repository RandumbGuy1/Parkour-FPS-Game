using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItem
{
    float Weight { get; }

    Vector3 DefaultPos { get; }
    Vector3 DefaultRot { get; }

    Vector3 AimPos { get; }
    Vector3 AimRot { get; }

    Sprite ItemSprite { get; }
    PlayerManager Player { get; }

    string ReadData();
    string ReadName();

    void OnPickup(PlayerManager s);
    void OnDrop();
    void ItemUpdate();
}
