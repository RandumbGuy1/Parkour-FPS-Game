using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItem
{
    float weight { get; }
    Sprite itemSprite { get; }

    string ReadData();
    bool OnUse();
}
