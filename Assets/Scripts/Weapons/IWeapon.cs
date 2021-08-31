using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    WeaponClass weaponType { get; }

    bool automatic { get; }

    float recoilSmoothTime { get; }
    float recoilForce { get; }
    Vector3 recoilPosOffset { get; }
    Vector3 recoilRotOffset { get; }

    Vector3 reloadRotOffset { get; }
    float reloadSmoothTime { get; }

    bool OnAttack(Transform cam);
    bool SecondaryAction();
}

public enum WeaponClass
{
    Ranged,
    Melee,
}
