using UnityEngine;

public interface IEnemy
{
    PlayerHealth EnemyHealth { get; }

    void EnemyUpdate();
}
