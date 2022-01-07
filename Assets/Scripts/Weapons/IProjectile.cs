using UnityEngine;

public interface IProjectile
{
    ProjectileType BulletType { get; }
    float LifeTime { get; }

    void OnShoot(Transform shooter, RaycastHit target, Vector3 velocity, LayerMask CollidesWith, float bulletDamage, 
        ScriptManager s = null, bool bulletClip = false);
}

public enum ProjectileType
{
    Bullet,
    Grenade,
}
