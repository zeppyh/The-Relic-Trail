using UnityEngine;

namespace RelicTrail.Player
{
    /// <summary>
    /// Elden Ring-inspired character movement controller.
    /// Features:
    /// - Camera-relative movement (walk, sprint, crouch)
    /// - Clean jump platforming and responsive gravity (Space key)
    /// - Lock-On strafe movement & orientation (facing locked enemy while orbiting/circling)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [AddComponentMenu("Relic Trail/Player/Player Movement")]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [Tooltip("Base walking speed in meters per second.")]
        [SerializeField] private float walkSpeed = 4.5f;

        [Tooltip("Sprint speed in meters per second.")]
        [SerializeField] private float sprintSpeed = 7.5f;

        [Tooltip("Crouch movement speed in meters per second.")]
        [SerializeField] private float crouchSpeed = 2.2f;

        [Tooltip("How fast the character reaches target speed.")]
        [SerializeField] private float acceleration = 12f;

        [Tooltip("How fast the character comes to a complete stop.")]
        [SerializeField] private float deceleration = 14f;

        [Header("Rotation & Camera")]
        [Tooltip("Time in seconds for character to smoothly rotate facing movement direction.")]
        [SerializeField] private float rotationSmoothTime = 0.10f;

        [Tooltip("Main camera transform for directional movement. If unassigned, automatically finds Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Jumping & Gravity")]
        [Tooltip("Maximum jump height in meters.")]
        [SerializeField] private float jumpHeight = 1.6f;

        [Tooltip("Downward gravity force.")]
        [SerializeField] private float gravity = -19.62f;

        [Tooltip("Terminal fall velocity.")]
        [SerializeField] private float terminalVelocity = -35f;

        [Tooltip("Small downward snap force applied when grounded.")]
        [SerializeField] private float groundedGravity = -3.5f;

        [Tooltip("Grace period (in seconds) where jump is still allowed right after walking off a ledge.")]
        [SerializeField] private float coyoteTime = 0.15f;

        [Tooltip("Buffer time (in seconds) to remember jump input right before touching the ground.")]
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Header("Ground Check")]
        [Tooltip("Offset from transform origin to check for ground.")]
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, 0.1f, 0f);

        [Tooltip("Radius of the ground check sphere.")]
        [SerializeField] private float groundCheckRadius = 0.28f;

        [Tooltip("Layers recognized as walkable ground.")]
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Animation Parameters")]
        [Tooltip("Optional Animator component.")]
        [SerializeField] private Animator animator;
        [SerializeField] private string animSpeedParam = "Speed";
        [SerializeField] private string animGroundedParam = "IsGrounded";
        [SerializeField] private string animJumpingParam = "IsJumping";
        [SerializeField] private string animFallingParam = "IsFalling";
        [SerializeField] private string animAimingParam = "IsAiming";
        [SerializeField] private string animShootingParam = "IsShooting";
        [SerializeField] private string animJumpTrigger = "Jump";

        // Components
        private CharacterController controller;
        private PlayerInputHandler input;
        private PlayerStamina stamina;
        private PlayerHealth health;
        private ThirdPersonCamera tpCamera;
        private PlayerAttack playerAttack;

        // Runtime Movement State
        private float currentSpeed;
        private float verticalVelocity;
        private float rotationVelocity;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool isGrounded;

        // Hash IDs for Animator parameters
        private int speedHash;
        private int groundedHash;
        private int jumpingHash;
        private int fallingHash;
        private int aimingHash;
        private int shootingHash;
        private int jumpHash;
        private bool hasAnimator;

        // Public Properties
        public bool IsGrounded => isGrounded;
        public float CurrentSpeed => currentSpeed;
        public bool IsMoving => currentSpeed > 0.1f && input.MoveInput.sqrMagnitude > 0.01f;
        public bool IsSprinting => input.IsSprinting && IsMoving;
        public bool IsDodging => false;
        public float VerticalVelocity => verticalVelocity;
        public Animator Animator => animator;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputHandler>();
            stamina = GetComponent<PlayerStamina>();
            health = GetComponent<PlayerHealth>();
            playerAttack = GetComponent<PlayerAttack>();

            FindCamera();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            SetupAnimationHashes();
        }

        private void Start()
        {
            SnapToGround();
            EnsureNaturalCharacterMaterials();
        }

        /// <summary>
        /// Guarantees that the Floreswa character model (male01_1) displays with authentic natural skin
        /// and blue shirt, preventing any reversed slots, blue skin, or missing shader artifacts.
        /// </summary>
        public void EnsureNaturalCharacterMaterials()
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null) return;

            if (smr.sharedMaterials != null && smr.sharedMaterials.Length == 11)
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
                if (urpShader == null) return;

                Material[] mats = new Material[11];
                for (int i = 0; i < 11; i++)
                {
                    mats[i] = (i < smr.sharedMaterials.Length && smr.sharedMaterials[i] != null)
                        ? new Material(smr.sharedMaterials[i])
                        : new Material(urpShader);
                    mats[i].shader = urpShader;
                }

                // Slot 0: Natural Caucasian/Human Skin Tone (Head, arms, neck, hands)
                Color skinColor = new Color(0.75f, 0.59f, 0.52f, 1f);
                mats[0].color = skinColor;
                if (mats[0].HasProperty("_BaseColor")) mats[0].SetColor("_BaseColor", skinColor);

                // Slot 1: Blue T-shirt (Iconic Floreswa polo shirt)
                Color blueShirt = new Color(0.41f, 0.49f, 0.77f, 1f);
                mats[1].color = blueShirt;
                if (mats[1].HasProperty("_BaseColor")) mats[1].SetColor("_BaseColor", blueShirt);

                // Slot 2: Pants (Charcoal Dark)
                Color pantsColor = new Color(0.24f, 0.22f, 0.20f, 1f);
                mats[2].color = pantsColor;
                if (mats[2].HasProperty("_BaseColor")) mats[2].SetColor("_BaseColor", pantsColor);

                // Slot 3: Shoes
                Color shoesColor = new Color(0.16f, 0.16f, 0.16f, 1f);
                mats[3].color = shoesColor;
                if (mats[3].HasProperty("_BaseColor")) mats[3].SetColor("_BaseColor", shoesColor);

                // Slot 4: Hair (Dark Brown)
                Color hairColor = new Color(0.25f, 0.23f, 0.22f, 1f);
                mats[4].color = hairColor;
                if (mats[4].HasProperty("_BaseColor")) mats[4].SetColor("_BaseColor", hairColor);

                // Slots 8, 9, 10: Clean-shaven facial hair zones match natural skin tone
                for (int f = 8; f <= 10; f++)
                {
                    mats[f].color = skinColor;
                    if (mats[f].HasProperty("_BaseColor")) mats[f].SetColor("_BaseColor", skinColor);
                }

                smr.materials = mats;
            }
        }

        /// <summary>
        /// Instantly snaps the player character controller and feet flush onto the ground floor.
        /// </summary>
        public void SnapToGround()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller == null) return;

            float bottomOffset = controller.center.y - (controller.height * 0.5f);
            Vector3 origin = transform.position + Vector3.up * 1.5f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 15f, groundLayers, QueryTriggerInteraction.Ignore))
            {
                bool ccEnabled = controller.enabled;
                controller.enabled = false;

                Vector3 pos = transform.position;
                pos.y = hit.point.y - bottomOffset;
                transform.position = pos;

                if (ccEnabled) controller.enabled = true;

                verticalVelocity = groundedGravity;
                isGrounded = true;
            }
        }

        private void FindCamera()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (tpCamera == null && cameraTransform != null)
            {
                tpCamera = cameraTransform.GetComponent<ThirdPersonCamera>();
            }
        }

        private void SetupAnimationHashes()
        {
            hasAnimator = animator != null;
            if (hasAnimator)
            {
                if (!string.IsNullOrEmpty(animSpeedParam)) speedHash = Animator.StringToHash(animSpeedParam);
                if (!string.IsNullOrEmpty(animGroundedParam)) groundedHash = Animator.StringToHash(animGroundedParam);
                if (!string.IsNullOrEmpty(animJumpingParam)) jumpingHash = Animator.StringToHash(animJumpingParam);
                if (!string.IsNullOrEmpty(animFallingParam)) fallingHash = Animator.StringToHash(animFallingParam);
                if (!string.IsNullOrEmpty(animAimingParam)) aimingHash = Animator.StringToHash(animAimingParam);
                if (!string.IsNullOrEmpty(animShootingParam)) shootingHash = Animator.StringToHash(animShootingParam);
                if (!string.IsNullOrEmpty(animJumpTrigger)) jumpHash = Animator.StringToHash(animJumpTrigger);
            }
        }

        private void Update()
        {
            if (RelicTrail.Core.GameManager.Instance != null && RelicTrail.Core.GameManager.Instance.CurrentGameState != RelicTrail.Core.GameState.Playing)
            {
                return;
            }

            if (controller == null) controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputHandler>();

            FindCamera();

            CheckGround();
            HandleJumpAndGravity();
            HandleHorizontalMovement();
            UpdateAnimator();
        }

        private void CheckGround()
        {
            if (verticalVelocity > 0.05f)
            {
                isGrounded = false;
                coyoteTimer = 0f;
                return;
            }

            bool grounded = controller != null && controller.isGrounded;

            // Extra downward raycast check for steps & slopes
            if (!grounded && controller != null)
            {
                float bottomY = controller.center.y - (controller.height * 0.5f);
                Vector3 rayOrigin = transform.position + new Vector3(0f, bottomY + 0.15f, 0f);
                float rayDistance = 0.30f;

                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayDistance, groundLayers, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (hit.transform != transform && !hit.transform.IsChildOf(transform) && !hit.collider.isTrigger)
                    {
                        grounded = true;
                        break;
                    }
                }
            }

            isGrounded = grounded;

            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
            }
        }

        private void HandleJumpAndGravity()
        {
            // Buffer jump input
            if (input.JumpTriggered)
            {
                jumpBufferTimer = jumpBufferTime;
                input.ConsumeJump();
            }
            else
            {
                jumpBufferTimer -= Time.deltaTime;
            }

            // Execute jump
            if (isGrounded && jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(gravity));
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                isGrounded = false;

                if (hasAnimator && jumpHash != 0)
                {
                    animator.SetTrigger(jumpHash);
                }
            }

            // Apply gravity
            if (isGrounded && verticalVelocity <= 0.0f)
            {
                verticalVelocity = groundedGravity;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
                if (verticalVelocity < terminalVelocity)
                {
                    verticalVelocity = terminalVelocity;
                }
            }
        }

        private void HandleHorizontalMovement()
        {
            Vector2 moveInput = input.MoveInput;
            bool hasInput = moveInput.sqrMagnitude > 0.01f;
            bool isLockedOn = tpCamera != null && tpCamera.IsLockedOn && tpCamera.LockedTarget != null;
            bool isAimingOrShooting = (input != null && input.IsAimingDownSights) || (playerAttack != null && playerAttack.IsAttacking);

            // Target speed determination
            float targetSpeed = 0f;
            if (hasInput)
            {
                if (input.IsCrouching) targetSpeed = crouchSpeed;
                else if (input.IsAimingDownSights) targetSpeed = walkSpeed * 0.8f;
                else if (input.IsSprinting) targetSpeed = sprintSpeed;
                else targetSpeed = walkSpeed;
            }

            // Rapid stopping when input released so character comes to a crisp stop rather than sliding
            if (!hasInput)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * 2.5f * Time.deltaTime);
                if (currentSpeed < 0.1f) currentSpeed = 0f;
            }
            else
            {
                float rate = (targetSpeed > currentSpeed) ? acceleration : deceleration;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
            }

            Vector3 moveDirection = Vector3.zero;

            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 camRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // 1. Lock-On Combat Strafing
            if (isLockedOn)
            {
                Transform enemy = tpCamera.LockedTarget;
                Vector3 toEnemy = enemy.position - transform.position;
                toEnemy.y = 0f;

                // Face locked enemy smoothly
                if (toEnemy.sqrMagnitude > 0.01f)
                {
                    Quaternion desiredRot = Quaternion.LookRotation(toEnemy.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * 18f);
                }

                // Move relative to enemy: Y = Advance/Retreat, X = Strafe Orbit
                if (hasInput)
                {
                    Vector3 forwardDir = toEnemy.normalized;
                    Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

                    moveDirection = (forwardDir * moveInput.y + rightDir * moveInput.x).normalized;
                }
            }
            // 2. Aim Down Sights (ADS) or Shooting Facing & Strafing (PUBG / Fortnite style)
            else if (isAimingOrShooting)
            {
                // Align character facing responsively with camera view direction
                if (camForward.sqrMagnitude > 0.01f)
                {
                    float targetAngle = Mathf.Atan2(camForward.x, camForward.z) * Mathf.Rad2Deg;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, 0.05f);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                }

                // Move relative to camera direction while maintaining forward-facing aim
                if (hasInput)
                {
                    moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
                }
            }
            // 3. Free Exploration Movement (Camera-Relative)
            else
            {
                if (hasInput)
                {
                    moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

                    float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                }
            }

            // Combine horizontal motion and vertical velocity
            Vector3 velocity = (moveDirection * currentSpeed) + (Vector3.up * verticalVelocity);
            controller.Move(velocity * Time.deltaTime);
        }

        private void UpdateAnimator()
        {
            if (!hasAnimator) return;

            // Measure actual horizontal velocity magnitude from the CharacterController
            Vector3 horizontalVelocity = controller != null ? controller.velocity : Vector3.zero;
            horizontalVelocity.y = 0f;
            float actualSpeed = horizontalVelocity.magnitude;

            bool hasInput = input != null && input.MoveInput.sqrMagnitude > 0.01f;

            if (speedHash != 0)
            {
                // When player releases input or is stopped, immediately drop Speed to 0 so character stops walking/sliding instantly
                if (!hasInput || actualSpeed < 0.08f)
                {
                    animator.SetFloat(speedHash, 0f);
                }
                else
                {
                    // Smoothly blend based on actual movement speed, clamped by current target speed
                    float blendSpeed = Mathf.Min(actualSpeed, currentSpeed);
                    animator.SetFloat(speedHash, blendSpeed, 0.05f, Time.deltaTime);
                }
            }

            if (groundedHash != 0)
            {
                animator.SetBool(groundedHash, isGrounded);
            }

            if (jumpingHash != 0)
            {
                animator.SetBool(jumpingHash, !isGrounded && verticalVelocity > 0.1f);
            }

            if (fallingHash != 0)
            {
                animator.SetBool(fallingHash, !isGrounded && verticalVelocity < -1.5f);
            }

            if (aimingHash != 0)
            {
                bool isAiming = (input != null && input.IsAimingDownSights) || (playerAttack != null && playerAttack.IsAttacking);
                animator.SetBool(aimingHash, isAiming);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckRadius);
        }
    }
}