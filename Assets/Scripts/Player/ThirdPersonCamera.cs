using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RelicTrail.Enemies;

namespace RelicTrail.Player
{
    /// <summary>
    /// Third-person orbital over-the-shoulder camera controller inspired by PUBG and Fortnite.
    /// Features:
    /// - Over-the-shoulder framing at head level (character framed in foreground left, reticle clear ahead)
    /// - Right Mouse Button Aim Down Sights (ADS) smooth zoom, tight framing, and fov transition
    /// - Wall collision avoidance (spherecast prevents clipping through walls & terrain)
    /// - Lock-On tracking (Left Ctrl locks onto enemy, frames player and foe, draws reticle)
    /// - Subtle camera shake on heavy attacks / impacts
    /// </summary>
    [AddComponentMenu("Relic Trail/Player/Third Person Camera")]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target Follow")]
        [Tooltip("Transform the camera orbits around (e.g. the player). Automatically finds the active player if unassigned.")]
        [SerializeField] private Transform target;

        [Tooltip("Height offset above player origin (1.70m = head level for third-person shooting).")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.70f, 0f);

        [Tooltip("Horizontal shoulder offset (0.48m = over right shoulder, framing player in left foreground).")]
        [SerializeField] private float shoulderOffset = 0.48f;

        [Header("Camera Distance & Zoom")]
        [Tooltip("Default distance from the target (2.6m for responsive over-the-shoulder framing).")]
        [SerializeField] private float defaultDistance = 2.6f;

        [Tooltip("Minimum distance allowed when zooming or colliding.")]
        [SerializeField] private float minDistance = 0.9f;

        [Tooltip("Maximum distance allowed.")]
        [SerializeField] private float maxDistance = 5.0f;

        [Tooltip("Scroll sensitivity for zooming.")]
        [SerializeField] private float zoomSpeed = 2.5f;

        [Header("Aim Down Sights (ADS)")]
        [Tooltip("Camera distance while holding Right Mouse Button ADS.")]
        [SerializeField] private float adsDistance = 1.35f;

        [Tooltip("Horizontal shoulder offset during ADS.")]
        [SerializeField] private float adsShoulderOffset = 0.38f;

        [Tooltip("Height offset above player origin during ADS.")]
        [SerializeField] private Vector3 adsTargetOffset = new Vector3(0f, 1.68f, 0f);

        [Tooltip("Field of view in normal exploration mode.")]
        [SerializeField] private float normalFOV = 65f;

        [Tooltip("Field of view zoomed in during ADS.")]
        [SerializeField] private float adsFOV = 46f;

        [Tooltip("Speed of transition between normal exploration and ADS camera.")]
        [SerializeField] private float adsTransitionSpeed = 12f;

        [Header("3D Look & Sensitivity")]
        [Tooltip("Horizontal look sensitivity (yaw).")]
        [SerializeField] private float sensitivityX = 2.0f;

        [Tooltip("Vertical look sensitivity (pitch).")]
        [SerializeField] private float sensitivityY = 2.0f;

        [Tooltip("Invert vertical look direction.")]
        [SerializeField] private bool invertY = false;

        [Tooltip("Minimum vertical angle (looking down towards ground).")]
        [SerializeField] private float minPitch = -35f;

        [Tooltip("Maximum vertical angle (looking up towards sky).")]
        [SerializeField] private float maxPitch = 70f;

        [Tooltip("Initial pitch angle (degrees). Set in Inspector or read directly from Transform rotation.")]
        [SerializeField] private float initialPitch = 10f;

        [Tooltip("Smoothing time for camera position tracking. Set to 0 for crisp 1:1 follow without rubber-banding.")]
        [SerializeField] private float positionSmoothTime = 0.0f;

        [Tooltip("Smoothing time for rotation look (0 = instant responsive mouse look).")]
        [SerializeField] private float rotationSmoothTime = 0.0f;

        [Header("Obstacle Collision")]
        [Tooltip("Layers the camera should collide with to avoid clipping through walls and rocks.")]
        [SerializeField] private LayerMask collisionLayers = ~0;

        [Tooltip("Radius of the collision sphere cast.")]
        [SerializeField] private float collisionRadius = 0.15f;

        [Tooltip("Distance buffer from walls when collision is detected.")]
        [SerializeField] private float collisionOffset = 0.15f;

        [Header("Lock-On System")]
        [Tooltip("Currently locked-on enemy transform.")]
        [SerializeField] private Transform lockedTarget;

        [Tooltip("Maximum range to lock onto an enemy.")]
        [SerializeField] private float maxLockOnDistance = 24f;

        [Tooltip("Speed camera tracks locked enemy.")]
        [SerializeField] private float lockOnTrackingSpeed = 10f;

        // Components
        private PlayerInputHandler inputHandler;
        private Camera camComponent;

        // Runtime angles and positions
        private float targetYaw;
        private float targetPitch = 10f;
        private float currentYaw;
        private float currentPitch = 10f;
        private float yawVelocity;
        private float pitchVelocity;

        private float targetDistance;
        private float currentDistance;
        private float lastDefaultDistance;
        private Vector3 positionVelocity;

        // Dynamic ADS state
        private float currentFOV = 65f;
        private float currentShoulderOffset = 0.48f;
        private Vector3 currentTargetOffset = new Vector3(0f, 1.70f, 0f);

        // Camera Shake
        private float shakeDuration;
        private float shakeIntensity;
        private float shakeTimer;

        // Public Properties
        public Transform Target => target;
        public Transform LockedTarget => lockedTarget;
        public bool IsLockedOn => lockedTarget != null;
        public bool IsADS => inputHandler != null && inputHandler.IsAimingDownSights;

        private void Awake()
        {
            camComponent = GetComponent<Camera>();
            EnsurePerspectiveCamera();
            ValidateTarget();

            // Read starting pitch directly from Camera transform if rotated in Scene view or set in Inspector
            float rotX = transform.eulerAngles.x;
            if (rotX > 180f) rotX -= 360f;
            if (Mathf.Abs(rotX) > 0.05f)
            {
                targetPitch = currentPitch = initialPitch = Mathf.Clamp(rotX, minPitch, maxPitch);
            }
            else
            {
                targetPitch = currentPitch = initialPitch;
            }

            float rotY = transform.eulerAngles.y;
            if (rotY > 180f) rotY -= 360f;
            if (Mathf.Abs(rotY) > 0.05f)
            {
                targetYaw = currentYaw = rotY;
            }
        }

        private void OnEnable()
        {
            camComponent = GetComponent<Camera>();
            EnsurePerspectiveCamera();
            ValidateTarget();
            if (target != null)
            {
                SnapToTarget();
            }
        }

        private void Start()
        {
            camComponent = GetComponent<Camera>();
            EnsurePerspectiveCamera();
            ValidateTarget();

            if (target != null)
            {
                inputHandler = target.GetComponent<PlayerInputHandler>();
                SnapToTarget();
            }
            else
            {
                targetDistance = defaultDistance;
                currentDistance = defaultDistance;
                Vector3 angles = transform.eulerAngles;
                targetYaw = currentYaw = angles.y;
                targetPitch = currentPitch = initialPitch;
            }

            if (Application.isPlaying)
            {
                lastDefaultDistance = defaultDistance;
                LockCursor(true);
            }
        }

        /// <summary>
        /// Enforces standard 3D perspective projection.
        /// </summary>
        private void EnsurePerspectiveCamera()
        {
            if (camComponent == null) camComponent = GetComponent<Camera>();
            if (camComponent != null)
            {
                if (camComponent.orthographic) camComponent.orthographic = false;
            }
        }

        private bool IsTargetValid(Transform t)
        {
            if (t == null) return false;
            if (t.GetComponent<PlayerMovement>() != null) return true;
            if (t.CompareTag("Player") && t.GetComponent<CharacterController>() != null) return true;
            return false;
        }

        private void ValidateTarget()
        {
            // 1. Check if the world hierarchy player exists first (prevents binding to dummy pack assets)
            var worldRoot = GameObject.Find("--- THE LOST RELIC WORLD ---");
            if (worldRoot != null)
            {
                var p = worldRoot.transform.Find("Player");
                if (p != null)
                {
                    target = p;
                    inputHandler = target.GetComponent<PlayerInputHandler>();
                    return;
                }
            }

            if (IsTargetValid(target)) return;

            // 2. Search for any PlayerMovement in scene
            var movement = Object.FindAnyObjectByType<PlayerMovement>();
            if (movement != null)
            {
                target = movement.transform;
                inputHandler = target.GetComponent<PlayerInputHandler>();
                return;
            }

            // 3. Search for tagged Player with CharacterController
            var taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var go in taggedPlayers)
            {
                if (go.GetComponent<CharacterController>() != null || go.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase))
                {
                    target = go.transform;
                    inputHandler = target.GetComponent<PlayerInputHandler>();
                    return;
                }
            }

            if (taggedPlayers.Length > 0)
            {
                target = taggedPlayers[0].transform;
                inputHandler = target.GetComponent<PlayerInputHandler>();
            }
        }

        private void OnValidate()
        {
            minDistance = Mathf.Max(0.2f, minDistance);
            maxDistance = Mathf.Max(minDistance + 0.1f, maxDistance);
            defaultDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
            adsDistance = Mathf.Clamp(adsDistance, 0.2f, maxDistance);
        }

        public Vector3 TargetOffset
        {
            get => targetOffset;
            set => targetOffset = value;
        }

        public float ShoulderOffset
        {
            get => shoulderOffset;
            set => shoulderOffset = value;
        }

        public float DefaultDistance
        {
            get => defaultDistance;
            set
            {
                defaultDistance = value;
                targetDistance = value;
            }
        }

        public void SetCameraPositionOffsets(Vector3 newTargetOffset, float newShoulderOffset, float newDistance)
        {
            targetOffset = newTargetOffset;
            shoulderOffset = newShoulderOffset;
            defaultDistance = newDistance;
            targetDistance = newDistance;
        }

        public void ToggleShoulderSide()
        {
            // Toggle between Right (+0.48), Left (-0.48), and Centered (0)
            if (shoulderOffset > 0.1f)
            {
                shoulderOffset = -shoulderOffset;
            }
            else if (shoulderOffset < -0.1f)
            {
                shoulderOffset = 0f;
            }
            else
            {
                shoulderOffset = 0.48f;
            }
        }

        [ContextMenu("Snap Behind Player")]
        public void ContextSnap()
        {
            SnapToTarget();
        }

        [ContextMenu("Presets/Centered Behind Head (No Shoulder Offset)")]
        public void PresetCentered()
        {
            shoulderOffset = 0f;
            targetOffset = new Vector3(0f, 1.75f, 0f);
            defaultDistance = 2.8f;
            SnapToTarget();
        }

        [ContextMenu("Presets/Right Shoulder (PUBG Default)")]
        public void PresetRightShoulder()
        {
            shoulderOffset = 0.48f;
            targetOffset = new Vector3(0f, 1.70f, 0f);
            defaultDistance = 2.6f;
            SnapToTarget();
        }

        [ContextMenu("Presets/Left Shoulder")]
        public void PresetLeftShoulder()
        {
            shoulderOffset = -0.48f;
            targetOffset = new Vector3(0f, 1.70f, 0f);
            defaultDistance = 2.6f;
            SnapToTarget();
        }

        [ContextMenu("Presets/Wide Overview (Zoomed Out)")]
        public void PresetWideOverview()
        {
            shoulderOffset = 0.30f;
            targetOffset = new Vector3(0f, 2.0f, 0f);
            defaultDistance = 4.2f;
            maxDistance = 8.0f;
            SnapToTarget();
        }

        [ContextMenu("Presets/Close Over-Shoulder")]
        public void PresetClose()
        {
            shoulderOffset = 0.45f;
            targetOffset = new Vector3(0f, 1.65f, 0f);
            defaultDistance = 1.8f;
            SnapToTarget();
        }

        /// <summary>
        /// Snaps the camera immediately behind the player's back preserving current/initial pitch.
        /// </summary>
        public void SnapToTarget()
        {
            EnsurePerspectiveCamera();
            if (target == null) return;

            // Preserve current pitch rather than resetting to hardcoded 10 degrees
            if (Mathf.Abs(targetPitch) < 0.001f)
            {
                float rotX = transform.eulerAngles.x;
                if (rotX > 180f) rotX -= 360f;
                targetPitch = Mathf.Abs(rotX) > 0.05f ? Mathf.Clamp(rotX, minPitch, maxPitch) : initialPitch;
            }
            currentPitch = targetPitch;
            currentYaw = targetYaw = target.eulerAngles.y;

            currentFOV = normalFOV;
            currentShoulderOffset = shoulderOffset;
            currentTargetOffset = targetOffset;
            if (camComponent != null) camComponent.fieldOfView = normalFOV;

            targetDistance = defaultDistance;
            currentDistance = defaultDistance;

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 focusPoint = target.position + targetOffset;
            if (Mathf.Abs(shoulderOffset) > 0.001f)
            {
                focusPoint += rotation * Vector3.right * shoulderOffset;
            }

            transform.position = focusPoint + (rotation * Vector3.back * defaultDistance);
            transform.rotation = rotation;

            positionVelocity = Vector3.zero;
            yawVelocity = 0f;
            pitchVelocity = 0f;
        }

        private void Update()
        {
            EnsurePerspectiveCamera();

            if (!Application.isPlaying)
            {
                if (!IsTargetValid(target))
                {
                    ValidateTarget();
                }
                return;
            }

            HandleCursorToggle();

            if (!Mathf.Approximately(defaultDistance, lastDefaultDistance))
            {
                targetDistance = defaultDistance;
                lastDefaultDistance = defaultDistance;
            }

            if (!IsTargetValid(target))
            {
                ValidateTarget();
                if (IsTargetValid(target))
                {
                    SnapToTarget();
                }
            }

            // Lock-On toggle check
            if (inputHandler != null && inputHandler.LockOnTriggered)
            {
                ToggleLockOn();
                inputHandler.ConsumeLockOn();
            }

            // Toggle shoulder view side (V key)
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                ToggleShoulderSide();
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            if (RelicTrail.Core.GameManager.Instance != null && RelicTrail.Core.GameManager.Instance.CurrentGameState != RelicTrail.Core.GameState.Playing)
            {
                // Maintain proper position and framing behind player even when menus are active
                UpdateCameraTransform();
                return;
            }

            if (!IsTargetValid(target))
            {
                ValidateTarget();
                if (!IsTargetValid(target)) return;
                SnapToTarget();
            }

            // Validate active locked enemy
            UpdateLockOnState();

            // Look input / lock-on framing
            if (lockedTarget != null)
            {
                HandleLockOnFraming();
            }
            else
            {
                HandleLookInput();
            }

            UpdateCameraTransform();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && Application.isPlaying)
            {
                if (RelicTrail.Core.GameManager.Instance == null || RelicTrail.Core.GameManager.Instance.CurrentGameState == RelicTrail.Core.GameState.Playing)
                {
                    LockCursor(true);
                }
                else
                {
                    LockCursor(false);
                }
            }
        }

        private void HandleCursorToggle()
        {
            if (RelicTrail.Core.GameManager.Instance != null && RelicTrail.Core.GameManager.Instance.CurrentGameState != RelicTrail.Core.GameState.Playing)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
                LockCursor(shouldLock);
            }
            else if (Mouse.current != null && Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame ||
                    Mouse.current.rightButton.wasPressedThisFrame)
                {
                    LockCursor(true);
                }
            }
        }

        public void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleLookInput()
        {
            Vector2 look = Vector2.zero;

            if (inputHandler != null)
            {
                look = inputHandler.LookInput;
            }
            else if (Mouse.current != null)
            {
                look = Mouse.current.delta.ReadValue();
            }

            bool isADS = inputHandler != null && inputHandler.IsAimingDownSights;
            float fovRatio = (camComponent != null && normalFOV > 10f) ? (camComponent.fieldOfView / normalFOV) : 1f;
            float sensMultiplier = isADS ? Mathf.Clamp(fovRatio, 0.55f, 1f) : 1f;

            targetYaw += look.x * sensitivityX * 0.1f * sensMultiplier;
            float pitchDelta = look.y * sensitivityY * 0.1f * (invertY ? 1f : -1f) * sensMultiplier;
            targetPitch = Mathf.Clamp(targetPitch + pitchDelta, minPitch, maxPitch);

            // Zooming via scroll
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    targetDistance = Mathf.Clamp(targetDistance - (scroll * 0.005f * zoomSpeed), minDistance, maxDistance);
                }
            }
        }

        /// <summary>
        /// Automatically adjusts camera yaw and pitch so both player and locked enemy remain perfectly framed.
        /// </summary>
        private void HandleLockOnFraming()
        {
            if (target == null || lockedTarget == null) return;

            Vector3 playerHead = target.position + targetOffset;
            Vector3 enemyChest = lockedTarget.position + Vector3.up * 1.2f;
            Vector3 dirToEnemy = (enemyChest - playerHead).normalized;

            // Compute ideal yaw facing toward enemy
            float desiredYaw = Mathf.Atan2(dirToEnemy.x, dirToEnemy.z) * Mathf.Rad2Deg;

            // Compute ideal pitch framing both targets
            float distance = Vector3.Distance(playerHead, enemyChest);
            float heightDiff = enemyChest.y - playerHead.y;
            float desiredPitch = Mathf.Clamp(-Mathf.Atan2(heightDiff, Mathf.Max(2f, distance)) * Mathf.Rad2Deg + 12f, minPitch, maxPitch);

            // Smoothly steer targetYaw and targetPitch
            targetYaw = Mathf.LerpAngle(targetYaw, desiredYaw, Time.deltaTime * lockOnTrackingSpeed);
            targetPitch = Mathf.Lerp(targetPitch, desiredPitch, Time.deltaTime * lockOnTrackingSpeed);
        }

        /// <summary>
        /// Toggles lock-on mode. If unlocked, finds closest enemy in front of camera. If locked, unlocks or cycles target.
        /// </summary>
        public void ToggleLockOn()
        {
            if (lockedTarget != null)
            {
                // Disengage lock-on
                lockedTarget = null;
                return;
            }

            // Find best enemy to lock on to
            lockedTarget = FindBestLockOnTarget();
        }

        private Transform FindBestLockOnTarget()
        {
            if (target == null) return null;

            Vector3 origin = target.position + targetOffset;
            Vector3 camForward = transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
            Transform bestEnemy = null;
            float bestScore = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.IsDead) continue;

                Vector3 toEnemy = enemy.transform.position - origin;
                float dist = toEnemy.magnitude;
                if (dist > maxLockOnDistance) continue;

                // Check angle to camera forward
                toEnemy.y = 0f;
                float angle = Vector3.Angle(camForward, toEnemy.normalized);
                if (angle > 75f) continue; // Behind or too far to sides

                // Check line of sight
                Vector3 enemyCenter = enemy.transform.position + Vector3.up * 1.0f;
                if (Physics.Linecast(origin, enemyCenter, out RaycastHit hit, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform != enemy.transform && !hit.transform.IsChildOf(enemy.transform))
                    {
                        continue; // Blocked by wall
                    }
                }

                // Score by combination of distance and angle
                float score = dist + (angle * 0.4f);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestEnemy = enemy.transform;
                }
            }

            return bestEnemy;
        }

        private void UpdateLockOnState()
        {
            if (lockedTarget == null) return;

            // Check if enemy was destroyed or died
            if (lockedTarget.gameObject == null)
            {
                lockedTarget = null;
                return;
            }

            var eh = lockedTarget.GetComponent<EnemyHealth>();
            if (eh != null && eh.IsDead)
            {
                lockedTarget = null;
                return;
            }

            // Check distance
            float dist = Vector3.Distance(target.position, lockedTarget.position);
            if (dist > maxLockOnDistance + 6f)
            {
                lockedTarget = null;
                return;
            }
        }

        private void UpdateCameraTransform()
        {
            if (rotationSmoothTime > 0.001f)
            {
                currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
                currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);
            }
            else
            {
                currentYaw = targetYaw;
                currentPitch = targetPitch;
            }

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            bool isADS = inputHandler != null && inputHandler.IsAimingDownSights;

            // 1. Smoothly interpolate FOV between normal exploration and ADS
            float targetFOV = isADS ? adsFOV : normalFOV;
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * adsTransitionSpeed);
            if (camComponent != null)
            {
                camComponent.fieldOfView = currentFOV;
            }

            // 2. Smoothly interpolate shoulder offset and height target offset
            float targetShoulder = isADS ? adsShoulderOffset : shoulderOffset;
            currentShoulderOffset = Mathf.Lerp(currentShoulderOffset, targetShoulder, Time.deltaTime * adsTransitionSpeed);

            Vector3 targetHeadOffset = isADS ? adsTargetOffset : targetOffset;
            currentTargetOffset = Vector3.Lerp(currentTargetOffset, targetHeadOffset, Time.deltaTime * adsTransitionSpeed);

            // Focus point on the character with dynamic height & shoulder offsets
            Vector3 focusPoint = target.position + currentTargetOffset;
            if (Mathf.Abs(currentShoulderOffset) > 0.001f)
            {
                focusPoint += rotation * Vector3.right * currentShoulderOffset;
            }

            Vector3 direction = rotation * Vector3.back;
            float maxCastDist = isADS ? adsDistance : targetDistance;
            float desiredDistance = maxCastDist;

            // Obstacle collision prevention (spherecast)
            RaycastHit[] hits = Physics.SphereCastAll(focusPoint, collisionRadius, direction, maxCastDist, collisionLayers, QueryTriggerInteraction.Ignore);
            float closestValidDist = maxCastDist;

            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger) continue;
                if (hit.transform == target || hit.transform.IsChildOf(target)) continue;
                if (hit.collider.gameObject.name.Contains("Player") || hit.collider.gameObject.name.Contains("Coin") || hit.collider.gameObject.name.Contains("Relic") || hit.collider.gameObject.name.Contains("NPC") || hit.collider.gameObject.name.Contains("Guardian")) continue;
                try
                {
                    if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Coin") || hit.collider.CompareTag("Relic") || hit.collider.CompareTag("NPC")) continue;
                }
                catch { }

                if (hit.distance < closestValidDist)
                {
                    closestValidDist = hit.distance;
                }
            }

            if (closestValidDist < maxCastDist)
            {
                desiredDistance = Mathf.Clamp(closestValidDist - collisionOffset, minDistance, maxCastDist);
            }

            // Rapid zoom in to avoid clipping, smooth recovery out / transition to ADS distance
            if (desiredDistance < currentDistance)
            {
                currentDistance = Mathf.MoveTowards(currentDistance, desiredDistance, Time.deltaTime * 35f);
            }
            else
            {
                float lerpRate = isADS ? adsTransitionSpeed : 8f;
                currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * lerpRate);
            }

            Vector3 targetPosition = focusPoint + (direction * currentDistance);

            // Subtle camera shake on heavy impact
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float progress = Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, shakeDuration));
                targetPosition += Random.insideUnitSphere * (shakeIntensity * progress);
            }

            // Apply position
            if (positionSmoothTime > 0.001f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, positionSmoothTime);
            }
            else
            {
                transform.position = targetPosition;
            }

            transform.rotation = rotation;
        }

        /// <summary>
        /// Triggers a subtle camera shake (e.g. during heavy hits).
        /// </summary>
        public void TriggerShake(float duration = 0.18f, float intensity = 0.18f)
        {
            shakeDuration = duration;
            shakeIntensity = intensity;
            shakeTimer = duration;
        }

        /// <summary>
        /// Dynamically set the follow target at runtime.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                inputHandler = target.GetComponent<PlayerInputHandler>();
                SnapToTarget();
            }
        }

        private void OnGUI()
        {
            // Draw floating Lock-On target reticle indicator above the locked enemy
            if (lockedTarget == null || !Application.isPlaying) return;
            if (camComponent == null) camComponent = GetComponent<Camera>();
            if (camComponent == null) return;

            Vector3 enemyChest = lockedTarget.position + Vector3.up * 1.5f;
            Vector3 screenPoint = camComponent.WorldToScreenPoint(enemyChest);

            // Check if enemy is in front of camera
            if (screenPoint.z > 0.2f)
            {
                float x = screenPoint.x;
                float y = Screen.height - screenPoint.y;

                float size = 20f;
                Rect rect = new Rect(x - size * 0.5f, y - size * 0.5f, size, size);

                Color prevColor = GUI.color;
                GUI.color = new Color(1f, 0.85f, 0.2f, 0.9f); // Golden yellow diamond reticle

                // Draw 4 corners / diamond shape
                GUI.Box(new Rect(rect.x, rect.y + rect.height * 0.5f - 1f, rect.width, 2f), GUIContent.none);
                GUI.Box(new Rect(rect.x + rect.width * 0.5f - 1f, rect.y, 2f, rect.height), GUIContent.none);

                // Small center dot
                GUI.color = Color.white;
                GUI.Box(new Rect(x - 3f, y - 3f, 6f, 6f), GUIContent.none);

                GUI.color = prevColor;
            }
        }
    }
}