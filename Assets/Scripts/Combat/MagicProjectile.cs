using UnityEngine;

namespace RelicTrail.Combat
{
    /// <summary>
    /// Magical projectile fired by the player. Deals damage to IDamageable targets on impact.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Relic Trail/Combat/Magic Projectile")]
    public class MagicProjectile : MonoBehaviour
    {
        [Header("Projectile Properties")]
        [SerializeField] private float speed = 24f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float lifeTime = 3.5f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private AudioClip hitSound;

        private GameObject instigator;
        private bool hasHit;

        public void Initialize(GameObject shooter, float customDamage = -1f)
        {
            instigator = shooter;
            if (customDamage > 0f)
            {
                damage = customDamage;
            }
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            if (hasHit) return;
            transform.Translate(Vector3.forward * (speed * Time.deltaTime), Space.Self);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // Ignore shooter and other projectiles
            if (instigator != null && (other.gameObject == instigator || other.transform.IsChildOf(instigator.transform)))
            {
                return;
            }

            if (other.GetComponent<MagicProjectile>() != null) return;
            if (other.isTrigger && other.GetComponent<IDamageable>() == null) return;

            // Check layer
            if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

            hasHit = true;
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitDirection = transform.forward;

            // Check for damageable entity
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage, hitPoint, hitDirection);
            }
            else if (other.GetComponentInParent<IDamageable>() is IDamageable parentDamageable)
            {
                parentDamageable.TakeDamage(damage, hitPoint, hitDirection);
            }

            // Spawn hit particle
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitDirection));
            }

            // Play audio if available
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, hitPoint, 0.8f);
            }

            Destroy(gameObject);
        }
    }
}
