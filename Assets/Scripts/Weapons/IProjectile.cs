using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProjectile
{
    ProjectileType BulletType { get; }
    float LifeTime { get; }

    void OnShoot(ScriptManager shooter, Vector3 targetPoint, Vector3 targetNormal, Vector3 velocity, float shootForce);
}

public enum ProjectileType
{
    Bullet,
    Grenade,
}
