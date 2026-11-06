using UnityEngine;

namespace RelicTrail.Combat
{
    /// <summary>
    /// Interface for all entities that can receive damage.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);
        bool IsDead { get; }
    }
}
