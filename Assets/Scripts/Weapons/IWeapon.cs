using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    WeaponClass weaponType { get; }

    bool automatic { get; }

    float recoilSmoothTime { get; }
    Vector3 recoilPosOffset { get; }
    Vector3 recoilRotOffset { get; }

    ShakeData recoilShakeData { get; }

    float reloadSmoothTime { get; }

    bool OnAttack(ScriptManager s);
    bool SecondaryAction(ScriptManager s);
}

public enum WeaponClass
{
    Ranged,
    Melee,
}
