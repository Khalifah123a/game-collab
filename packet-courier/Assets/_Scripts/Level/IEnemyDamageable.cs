using UnityEngine;

public interface IEnemyDamageable
{
    Transform HitTransform { get; }
    bool IsDead { get; }
    void TakeHit(int damage);
}
