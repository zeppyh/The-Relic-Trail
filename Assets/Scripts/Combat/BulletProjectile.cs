using System.Collections.Generic;
using UnityEngine;

namespace RelicTrail.Combat
{
    /// <summary>
    /// High-velocity continuous-collision bullet projectile fired from the player's gun.
    /// Uses raycast sweep per frame to prevent tunneling through enemies or obstacles.
    /// Features glowing tracer visuals, trail renderer, impact sparks, and audio.
    /// </summary>
    [AddComponentMenu("Relic Trail/Combat/Bullet Projectile")]
    public class BulletProjectile : MonoBehaviour
    {
        [Header("Projectile Properties")]
        [SerializeField] private float speed = 65f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float maxLifetime = 2.5f;
        [SerializeField] private bool isHeavy = false;
        [SerializeField] private LayerMask hitLayers = ~0;

        private GameObject shooter;
        private float aliveTimer;
        private bool hasHit;
        private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        public void Initialize(GameObject instigator, float bulletDamage, bool heavyShot, LayerMask layers)
        {
            shooter = instigator;
            damage = bulletDamage;
            isHeavy = heavyShot;
            hitLayers = layers;
            speed = heavyShot ? 85f : 65f;
        }

        private void Update()
        {
            if (hasHit) return;

            aliveTimer += Time.deltaTime;
            if (aliveTimer >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Continuous spherecast sweep to prevent tunneling or grazing misses
            float stepDistance = speed * Time.deltaTime;
            Vector3 rayDir = transform.forward;
            Vector3 currentPos = transform.position;

            RaycastHit[] hits = Physics.SphereCastAll(currentPos, 0.25f, rayDir, stepDistance, hitLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                hits = Physics.RaycastAll(currentPos, rayDir, stepDistance, hitLayers, QueryTriggerInteraction.Ignore);
            }
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // Ignore self and children of shooter
                if (shooter != null && (hit.collider.gameObject == shooter || hit.collider.transform.IsChildOf(shooter.transform)))
                {
                    continue;
                }

                // Ignore other projectiles
                if (hit.collider.GetComponent<BulletProjectile>() != null || hit.collider.GetComponent<MagicProjectile>() != null)
                {
                    continue;
                }

                OnImpact(hit);
                return;
            }

            transform.position += rayDir * stepDistance;
        }

        private void OnImpact(RaycastHit hit)
        {
            hasHit = true;
            transform.position = hit.point;

            // Damage target
            var damageable = hit.collider.GetComponentInParent<IDamageable>() ?? hit.collider.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead && !hitTargets.Contains(damageable))
            {
                hitTargets.Add(damageable);
                damageable.TakeDamage(damage, hit.point, transform.forward);
            }

            // Spawn dynamic impact spark burst
            SpawnImpactSparks(hit.point, hit.normal);

            Destroy(gameObject, 0.05f);
        }

        private void SpawnImpactSparks(Vector3 point, Vector3 normal)
        {
            GameObject sparkObj = new GameObject("BulletImpactSparks");
            sparkObj.transform.position = point;
            if (normal.sqrMagnitude > 0.001f)
            {
                sparkObj.transform.rotation = Quaternion.LookRotation(normal);
            }

            // Visual impact spark core
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.transform.SetParent(sparkObj.transform);
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = Vector3.one * (isHeavy ? 0.35f : 0.20f);
            var col = core.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = core.GetComponent<Renderer>();
            if (rend != null)
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                Color sparkColor = isHeavy ? new Color(1f, 0.45f, 0.1f) : new Color(1f, 0.9f, 0.35f);
                m.color = sparkColor;
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", sparkColor * 3f);
                }
                rend.material = m;
            }

            // Temporary impact light flash
            var lightObj = new GameObject("ImpactLight");
            lightObj.transform.SetParent(sparkObj.transform);
            lightObj.transform.localPosition = normal * 0.1f;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = isHeavy ? new Color(1f, 0.5f, 0.15f) : new Color(1f, 0.95f, 0.5f);
            light.range = isHeavy ? 5f : 3.2f;
            light.intensity = isHeavy ? 4f : 2.5f;

            Destroy(sparkObj, isHeavy ? 0.25f : 0.18f);
        }

        /// <summary>
        /// Instantiates a fully configured procedural bullet tracer in the scene.
        /// </summary>
        public static BulletProjectile Spawn(Vector3 position, Vector3 direction, float damage, bool isHeavy, GameObject shooter, LayerMask layers)
        {
            GameObject bulletObj = new GameObject(isHeavy ? "HeavyBulletTracer" : "LightBulletTracer");
            bulletObj.transform.position = position;
            bulletObj.transform.rotation = Quaternion.LookRotation(direction.normalized);

            // Glowing bullet core
            GameObject meshObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            meshObj.name = "TracerMesh";
            meshObj.transform.SetParent(bulletObj.transform);
            meshObj.transform.localPosition = Vector3.zero;
            meshObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            meshObj.transform.localScale = isHeavy ? new Vector3(0.09f, 0.28f, 0.09f) : new Vector3(0.05f, 0.18f, 0.05f);

            var col = meshObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = meshObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                Color tracerColor = isHeavy ? new Color(1f, 0.5f, 0.1f) : new Color(1f, 0.9f, 0.35f);
                m.color = tracerColor;
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", tracerColor * 3.5f);
                }
                rend.material = m;
            }

            // Trail Renderer for streak behind bullet
            var trail = bulletObj.AddComponent<TrailRenderer>();
            trail.time = isHeavy ? 0.18f : 0.10f;
            trail.startWidth = isHeavy ? 0.12f : 0.06f;
            trail.endWidth = 0.01f;
            trail.material = rend != null ? rend.material : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;

            // Point light attached to projectile
            var lightObj = new GameObject("BulletLight");
            lightObj.transform.SetParent(bulletObj.transform);
            lightObj.transform.localPosition = Vector3.zero;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = isHeavy ? new Color(1f, 0.55f, 0.1f) : new Color(1f, 0.95f, 0.5f);
            light.range = isHeavy ? 3.5f : 2.0f;
            light.intensity = 2f;

            var proj = bulletObj.AddComponent<BulletProjectile>();
            proj.Initialize(shooter, damage, isHeavy, layers);
            return proj;
        }
    }
}
