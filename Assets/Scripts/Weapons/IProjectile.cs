using UnityEngine;

public interface IProjectile
{
    ProjectileType BulletType { get; }
    float LifeTime { get; }

    void OnShoot(ScriptManager shooter, RaycastHit target, Vector3 velocity, float shootForce);
}

public enum ProjectileType
{
    Bullet,
    Grenade,
}
