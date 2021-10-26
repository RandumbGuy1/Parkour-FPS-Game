using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProjectile
{
    ProjectileType bulletType { get; }
    float lifeTime { get; }

    void OnShoot(ScriptManager shooter, Vector3 velocity, float shootForce);
}

public enum ProjectileType
{
    Bullet,
    Grenade,
}
