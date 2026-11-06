using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RelicTrail.Player;
using RelicTrail.Combat;

namespace RelicTrail.Enemies
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        TelegraphWindUp,
        AttackStrike,
        Recovery,
        Return,
        Dead
    }

    /// <summary>
    /// Elden Ring-inspired enemy AI controller.
    /// Operates smoothly using CharacterController physics movement (independent of baked NavMesh),
    /// eliminating blue NavMesh floor overlays in the Scene view while maintaining seamless enemy movement.
    /// Features:
    /// - Patrol around spawn area
    /// - Player detection and direct pursuit
    /// - Telegraphed attacks with visible 0.5s warning charge (dodgeable with i-frames)
    /// - Post-attack recovery opening for player counter-attacks
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    [AddComponentMenu("Relic Trail/Enemies/Enemy AI")]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRadius = 14f;
        [SerializeField] private float loseDistance = 22f;
        [SerializeField] private float fieldOfViewAngle = 140f;
        [SerializeField] private LayerMask obstacleLayers = ~0;

        [Header("Combat Settings")]
        [SerializeField] private float attackRange = 2.4f;
        [SerializeField] private float attackDamage = 18f;
        [SerializeField] private float telegraphDuration = 0.50f;
        [SerializeField] private float strikeDuration = 0.15f;
        [SerializeField] private float recoveryDuration = 0.75f;
        [SerializeField] private GameObject attackEffectPrefab;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip telegraphWarningSound;

        [Header("Movement Settings")]
        [SerializeField] private float patrolSpeed = 2.5f;
        [SerializeField] private float chaseSpeed = 5.0f;
        [SerializeField] private float patrolRadius = 10f;
        [SerializeField] private float waypointWaitTime = 2f;
        [SerializeField] private List<Transform> waypoints = new List<Transform>();

        [Header("Animation (Optional)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string animSpeedParam = "Speed";
        [SerializeField] private string animAttackTrigger = "Attack";
        [SerializeField] private string animDieTrigger = "Die";

        // Components
        private CharacterController controller;
        private NavMeshAgent agent;
        private EnemyHealth health;
        private Transform playerTarget;
        private PlayerHealth playerHealth;
        private Renderer[] enemyRenderers;
        private List<Color> cachedColors = new List<Color>();

        // State variables
        private EnemyState currentState = EnemyState.Patrol;
        private Vector3 spawnPosition;
        private Vector3 currentPatrolDestination;
        private int currentWaypointIndex;
        private float waitTimer;
        private bool hasLineOfSight;
        private bool isAttackingRoutine;
        private float verticalVelocity = -2f;
        private float currentMoveSpeed;

        public EnemyState CurrentState => currentState;

        public void ResetToSpawn()
        {
            currentState = EnemyState.Patrol;
            isAttackingRoutine = false;
            waitTimer = 0f;
            verticalVelocity = -2f;
            currentPatrolDestination = spawnPosition;
            RestoreTelegraphColor();
            PickNewPatrolDestination();
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
            spawnPosition = transform.position;
            currentPatrolDestination = spawnPosition;

            // Ensure a dedicated CapsuleCollider is present for reliable bullet & melee hit detection
            if (GetComponent<CapsuleCollider>() == null)
            {
                var cap = gameObject.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 1f, 0f);
                cap.height = 2f;
                cap.radius = 0.5f;
            }

            // Check if NavMesh is actually valid in the scene
            bool hasNavMesh = false;
            try
            {
                hasNavMesh = NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas);
            }
            catch { }

            if (agent != null)
            {
                if (!hasNavMesh || !agent.isOnNavMesh)
                {
                    agent.enabled = false;
                }
                else if (controller != null)
                {
                    controller.enabled = false;
                }
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            CacheVisuals();
        }

        private void CacheVisuals()
        {
            enemyRenderers = GetComponentsInChildren<Renderer>(true);
            cachedColors.Clear();
            foreach (var r in enemyRenderers)
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
                if (r.material != null && r.material.HasProperty("_BaseColor"))
                    cachedColors.Add(r.material.GetColor("_BaseColor"));
                else if (r.material != null && r.material.HasProperty("_Color"))
                    cachedColors.Add(r.material.GetColor("_Color"));
                else
                    cachedColors.Add(Color.white);
            }
        }

        private bool CanUseAgent => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

        private void Start()
        {
            FindPlayer();
            if (CanUseAgent) agent.speed = patrolSpeed;
            health.OnDefeated += HandleDefeated;
            PickNewPatrolDestination();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDefeated -= HandleDefeated;
            }
        }

        private void FindPlayer()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                playerHealth = playerObj.GetComponent<PlayerHealth>();
            }
        }

        private void Update()
        {
            if (currentState == EnemyState.Dead) return;

            if (playerTarget == null)
            {
                FindPlayer();
                if (playerTarget == null) return;
            }

            CheckLineOfSight();

            // Execute State Logic
            switch (currentState)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;
                case EnemyState.Chase:
                    UpdateChase();
                    break;
                case EnemyState.TelegraphWindUp:
                case EnemyState.AttackStrike:
                case EnemyState.Recovery:
                    // Handled inside coroutine
                    break;
                case EnemyState.Return:
                    UpdateReturn();
                    break;
            }

            UpdateAnimator();
        }

        private void CheckLineOfSight()
        {
            hasLineOfSight = false;
            float dist = Vector3.Distance(transform.position, playerTarget.position);

            if (dist <= detectionRadius)
            {
                Vector3 eyePos = transform.position + Vector3.up * 1.5f;
                Vector3 targetPos = playerTarget.position + Vector3.up * 1.0f;
                Vector3 dir = (targetPos - eyePos).normalized;

                float angle = Vector3.Angle(transform.forward, dir);
                if (angle <= fieldOfViewAngle * 0.5f || dist <= 3.5f) // Close proximity automatically alerts
                {
                    if (Physics.Raycast(eyePos, dir, out RaycastHit hit, dist, obstacleLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform == playerTarget || hit.transform.IsChildOf(playerTarget))
                        {
                            hasLineOfSight = true;
                        }
                    }
                    else
                    {
                        hasLineOfSight = true;
                    }
                }
            }
        }

        private void MoveTowardsDestination(Vector3 targetPos, float speed)
        {
            currentMoveSpeed = speed;

            if (CanUseAgent)
            {
                if (controller != null && controller.enabled) controller.enabled = false;
                agent.speed = speed;
                agent.SetDestination(targetPos);
                return;
            }

            // Direct CharacterController physics navigation
            if (agent != null && agent.enabled) agent.enabled = false;
            if (controller != null && !controller.enabled) controller.enabled = true;

            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;
            float dist = dir.magnitude;

            if (dist > 0.4f)
            {
                Vector3 moveDir = dir.normalized;
                Quaternion desiredRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * 8f);

                // Apply downward gravity to keep grounded safely without compounding velocity
                if (controller != null && controller.isGrounded)
                {
                    verticalVelocity = -2.0f;
                }
                else
                {
                    verticalVelocity = Mathf.Clamp(verticalVelocity - (19.62f * Time.deltaTime), -15f, 4f);
                }

                Vector3 velocity = (moveDir * speed) + (Vector3.up * verticalVelocity);

                if (controller != null && controller.enabled)
                {
                    controller.Move(velocity * Time.deltaTime);
                }
                else
                {
                    transform.position += moveDir * (speed * Time.deltaTime);
                }
            }
            else
            {
                currentMoveSpeed = 0f;
            }
        }

        private void StopMovement()
        {
            currentMoveSpeed = 0f;
            if (CanUseAgent)
            {
                agent.ResetPath();
            }
        }

        private void UpdatePatrol()
        {
            if (hasLineOfSight)
            {
                currentState = EnemyState.Chase;
                return;
            }

            if (waypoints.Count > 0)
            {
                Transform wp = waypoints[currentWaypointIndex];
                float dist = Vector3.Distance(transform.position, wp.position);
                if (dist <= 1.5f)
                {
                    StopMovement();
                    waitTimer += Time.deltaTime;
                    if (waitTimer >= waypointWaitTime)
                    {
                        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
                        waitTimer = 0f;
                    }
                }
                else
                {
                    MoveTowardsDestination(wp.position, patrolSpeed);
                }
            }
            else
            {
                // Roam around spawn position
                float dist = Vector3.Distance(transform.position, currentPatrolDestination);
                if (dist <= 1.5f)
                {
                    StopMovement();
                    waitTimer += Time.deltaTime;
                    if (waitTimer >= waypointWaitTime)
                    {
                        PickNewPatrolDestination();
                        waitTimer = 0f;
                    }
                }
                else
                {
                    MoveTowardsDestination(currentPatrolDestination, patrolSpeed);
                }
            }
        }

        private void PickNewPatrolDestination()
        {
            Vector2 randCircle = Random.insideUnitCircle * patrolRadius;
            currentPatrolDestination = spawnPosition + new Vector3(randCircle.x, 0f, randCircle.y);
        }

        private void UpdateChase()
        {
            float dist = Vector3.Distance(transform.position, playerTarget.position);

            if (dist > loseDistance || (playerHealth != null && playerHealth.IsDead))
            {
                currentState = EnemyState.Return;
                return;
            }

            MoveTowardsDestination(playerTarget.position, chaseSpeed);

            // In melee attack range
            if (dist <= attackRange && hasLineOfSight && !isAttackingRoutine)
            {
                StartCoroutine(TelegraphedAttackRoutine());
            }
        }

        /// <summary>
        /// Telegraphed Attack Routine:
        /// 1. Telegraph / Wind-Up (0.50s): Glows warning orange with audio telegraph; player has window to dodge roll!
        /// 2. Strike (0.15s): Active attack strike. If player dodged, i-frames negate damage!
        /// 3. Recovery (0.75s): Vulnerable recovery pause allowing player counter-attacks.
        /// </summary>
        private IEnumerator TelegraphedAttackRoutine()
        {
            isAttackingRoutine = true;
            currentState = EnemyState.TelegraphWindUp;

            StopMovement();

            // 1. WIND-UP TELEGRAPH PHASE
            if (telegraphWarningSound != null)
            {
                AudioSource.PlayClipAtPoint(telegraphWarningSound, transform.position, 0.75f);
            }

            SetTelegraphColor(new Color(1f, 0.4f, 0f, 1f));

            float t = 0f;
            while (t < telegraphDuration)
            {
                t += Time.deltaTime;
                // Face player continuously during early wind-up
                if (t < telegraphDuration * 0.7f && playerTarget != null)
                {
                    Vector3 lookDir = (playerTarget.position - transform.position).normalized;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
                    }
                }
                yield return null;
            }

            RestoreTelegraphColor();

            // 2. STRIKE ATTACK PHASE
            currentState = EnemyState.AttackStrike;

            if (animator != null && !string.IsNullOrEmpty(animAttackTrigger))
            {
                animator.SetTrigger(animAttackTrigger);
            }

            if (attackSound != null)
            {
                AudioSource.PlayClipAtPoint(attackSound, transform.position, 0.85f);
            }

            if (attackEffectPrefab != null)
            {
                Vector3 spawnPos = transform.position + transform.forward * 1.2f + Vector3.up * 1f;
                Instantiate(attackEffectPrefab, spawnPos, transform.rotation);
            }

            // Damage player if in strike range and cone
            if (playerHealth != null && !playerHealth.IsDead && playerTarget != null)
            {
                float dist = Vector3.Distance(transform.position, playerTarget.position);
                Vector3 toPlayer = (playerTarget.position - transform.position).normalized;
                toPlayer.y = 0f;
                float angle = Vector3.Angle(transform.forward, toPlayer);

                if (dist <= attackRange + 0.8f && angle <= 80f)
                {
                    // If player is in dodge roll, playerHealth.IsInvulnerable is true and damage is ignored!
                    playerHealth.TakeDamage(attackDamage, playerTarget.position + Vector3.up, toPlayer);
                }
            }

            yield return new WaitForSeconds(strikeDuration);

            // 3. RECOVERY PHASE
            currentState = EnemyState.Recovery;
            yield return new WaitForSeconds(recoveryDuration);

            isAttackingRoutine = false;
            currentState = EnemyState.Chase;
        }

        private void SetTelegraphColor(Color col)
        {
            if (enemyRenderers == null) return;
            foreach (var r in enemyRenderers)
            {
                if (r == null || r.material == null) continue;
                if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", col);
                else if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", col);
            }
        }

        private void RestoreTelegraphColor()
        {
            if (enemyRenderers == null) return;
            for (int i = 0; i < enemyRenderers.Length && i < cachedColors.Count; i++)
            {
                var r = enemyRenderers[i];
                if (r == null || r.material == null) continue;
                if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", cachedColors[i]);
                else if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", cachedColors[i]);
            }
        }

        private void UpdateReturn()
        {
            if (hasLineOfSight)
            {
                currentState = EnemyState.Chase;
                return;
            }

            float dist = Vector3.Distance(transform.position, spawnPosition);
            if (dist <= 1.5f)
            {
                StopMovement();
                currentState = EnemyState.Patrol;
            }
            else
            {
                MoveTowardsDestination(spawnPosition, patrolSpeed);
            }
        }

        private void HandleDefeated()
        {
            StopAllCoroutines();
            currentState = EnemyState.Dead;
            isAttackingRoutine = false;
            StopMovement();
            if (controller != null) controller.enabled = false;
            if (agent != null) agent.enabled = false;
            if (animator != null && !string.IsNullOrEmpty(animDieTrigger))
            {
                animator.SetTrigger(animDieTrigger);
            }
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;
            if (!string.IsNullOrEmpty(animSpeedParam))
            {
                float speed = CanUseAgent ? agent.velocity.magnitude : currentMoveSpeed;
                animator.SetFloat(animSpeedParam, speed, 0.1f, Time.deltaTime);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}