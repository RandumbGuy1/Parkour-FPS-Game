using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    WeaponClass weaponType { get; }

    bool automatic { get; }
    float recoilForce { get; }

    bool OnAttack(Transform cam);
    bool SecondaryAction();
}

public enum WeaponClass
{
    Ranged,
    Melee,
}
