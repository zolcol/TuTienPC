using UnityEngine;

namespace TopDownGame.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);
    }
}
