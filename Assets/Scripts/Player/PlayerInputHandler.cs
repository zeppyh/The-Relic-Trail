using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicTrail.Player
{
    /// <summary>
    /// Reads movement, look, jump, sprint, crouch, and interaction inputs using Unity's Input System.
    /// Supports binding to an InputActionAsset, PlayerInput component, or automatic keyboard/mouse/gamepad fallback.
    /// </summary>
    [AddComponentMenu("Relic Trail/Player/Player Input Handler")]
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input Action Configuration")]
        [Tooltip("Optional reference to InputSystem_Actions asset. If left null, binds automatically via PlayerInput component or direct device input.")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Action Map & Names")]
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string crouchActionName = "Crouch";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string attackActionName = "Attack";
        [SerializeField] private string heavyAttackActionName = "HeavyAttack";
        [SerializeField] private string lockOnActionName = "LockOn";
        [SerializeField] private string adsActionName = "AimDownSights";

        // Cached Action References
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction interactAction;
        private InputAction attackAction;
        private InputAction heavyAttackAction;
        private InputAction lockOnAction;
        private InputAction adsAction;

        // Current Input States
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpTriggered { get; private set; }
        public bool DodgeTriggered => false;
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool InteractTriggered { get; private set; }
        public bool LightAttackTriggered { get; private set; }
        public bool SuperChargedAttackTriggered { get; private set; }
        public bool IsAimingDownSights { get; private set; }
        public bool LockOnTriggered { get; private set; }

        // Backwards compatibility
        public bool AttackTriggered => LightAttackTriggered;
        public bool HeavyAttackTriggered => SuperChargedAttackTriggered;
        public bool IsADS => IsAimingDownSights;

        private void Awake()
        {
            InitializeInputActions();
        }

        private void OnEnable()
        {
            EnableActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        private void InitializeInputActions()
        {
            // 1. Try assigned InputActionAsset
            if (inputActions != null)
            {
                var map = inputActions.FindActionMap(actionMapName);
                if (map != null)
                {
                    BindActionsFromMap(map);
                    return;
                }
            }

            // 2. Try PlayerInput component on this GameObject
            if (TryGetComponent<PlayerInput>(out var playerInput) && playerInput.actions != null)
            {
                var map = playerInput.actions.FindActionMap(actionMapName);
                if (map != null)
                {
                    BindActionsFromMap(map);
                    return;
                }
            }
        }

        private void BindActionsFromMap(InputActionMap map)
        {
            moveAction = map.FindAction(moveActionName);
            lookAction = map.FindAction(lookActionName);
            jumpAction = map.FindAction(jumpActionName);
            sprintAction = map.FindAction(sprintActionName);
            crouchAction = map.FindAction(crouchActionName);
            interactAction = map.FindAction(interactActionName);
            attackAction = map.FindAction(attackActionName);
            heavyAttackAction = map.FindAction(heavyAttackActionName);
            lockOnAction = map.FindAction(lockOnActionName);
            adsAction = map.FindAction(adsActionName);
        }

        private void EnableActions()
        {
            moveAction?.Enable();
            lookAction?.Enable();
            jumpAction?.Enable();
            sprintAction?.Enable();
            crouchAction?.Enable();
            interactAction?.Enable();
            attackAction?.Enable();
            heavyAttackAction?.Enable();
            lockOnAction?.Enable();
            adsAction?.Enable();
        }

        private void DisableActions()
        {
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
            sprintAction?.Disable();
            crouchAction?.Disable();
            interactAction?.Disable();
            attackAction?.Disable();
            heavyAttackAction?.Disable();
            lockOnAction?.Disable();
            adsAction?.Disable();
        }

        private void Update()
        {
            ReadInputs();
        }

        private void ReadInputs()
        {
            if (RelicTrail.Core.GameManager.Instance != null && RelicTrail.Core.GameManager.Instance.CurrentGameState != RelicTrail.Core.GameState.Playing)
            {
                MoveInput = Vector2.zero;
                LookInput = Vector2.zero;
                JumpTriggered = false;
                IsSprinting = false;
                IsCrouching = false;
                InteractTriggered = false;
                LightAttackTriggered = false;
                SuperChargedAttackTriggered = false;
                IsAimingDownSights = false;
                LockOnTriggered = false;
                return;
            }

            // If Input System action map is bound, read from it
            if (moveAction != null)
            {
                MoveInput = moveAction.ReadValue<Vector2>();
                LookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
                JumpTriggered = jumpAction != null && jumpAction.WasPressedThisFrame();
                IsSprinting = sprintAction != null && sprintAction.IsPressed();
                IsCrouching = crouchAction != null && crouchAction.IsPressed();
                InteractTriggered = (interactAction != null && (interactAction.WasPressedThisFrame() || interactAction.WasPerformedThisFrame())) ||
                                    (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                                    (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
                LightAttackTriggered = attackAction != null && attackAction.WasPressedThisFrame();
                SuperChargedAttackTriggered = (heavyAttackAction != null && heavyAttackAction.WasPressedThisFrame()) ||
                                              (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame);
                IsAimingDownSights = (adsAction != null && adsAction.IsPressed()) ||
                                     (Mouse.current != null && Mouse.current.rightButton.isPressed);
                LockOnTriggered = (lockOnAction != null && lockOnAction.WasPressedThisFrame()) ||
                                  (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasPressedThisFrame);
                return;
            }

            // Zero-configuration fallback to keyboard, mouse, and gamepad
            ReadDeviceFallback();
        }

        private void ReadDeviceFallback()
        {
            Vector2 move = Vector2.zero;
            Vector2 look = Vector2.zero;
            bool jump = false;
            bool sprint = false;
            bool crouch = false;
            bool interact = false;
            bool lightAttack = false;
            bool superCharged = false;
            bool isADS = false;
            bool lockOn = false;

            // Keyboard input
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;

                // Space key: Jump
                jump = Keyboard.current.spaceKey.wasPressedThisFrame;
                sprint = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
                crouch = Keyboard.current.cKey.isPressed;
                interact = Keyboard.current.eKey.wasPressedThisFrame;

                // Q: Super Charged Attack
                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    superCharged = true;
                }

                // Left Ctrl: Lock on to enemy / release lock-on
                if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
                {
                    lockOn = true;
                }
            }

            // Mouse look & attacks & ADS
            if (Mouse.current != null)
            {
                look = Mouse.current.delta.ReadValue();

                // Check if mouse is over UI buttons
                bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                                       UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

                if (!isPointerOverUI)
                {
                    // Left Mouse Button: Shoot / light attack
                    lightAttack = Mouse.current.leftButton.wasPressedThisFrame;

                    // Right Mouse Button (Hold): Aim Down Sights (ADS)
                    isADS = Mouse.current.rightButton.isPressed;
                }
            }

            // Gamepad input
            if (Gamepad.current != null)
            {
                Vector2 stickMove = Gamepad.current.leftStick.ReadValue();
                if (stickMove.sqrMagnitude > 0.04f) move = stickMove;

                Vector2 stickLook = Gamepad.current.rightStick.ReadValue();
                if (stickLook.sqrMagnitude > 0.04f) look = stickLook * 10f;

                if (Gamepad.current.buttonSouth.wasPressedThisFrame) jump = true;
                if (Gamepad.current.leftStickButton.isPressed) sprint = true;
                if (Gamepad.current.buttonWest.wasPressedThisFrame) interact = true;
                if (Gamepad.current.rightShoulder.wasPressedThisFrame) lightAttack = true;
                if (Gamepad.current.leftTrigger.ReadValue() > 0.3f) isADS = true;
                if (Gamepad.current.buttonNorth.wasPressedThisFrame) superCharged = true;
                if (Gamepad.current.rightStickButton.wasPressedThisFrame) lockOn = true;
            }

            MoveInput = Vector2.ClampMagnitude(move, 1f);
            LookInput = look;
            JumpTriggered = jump;
            IsSprinting = sprint;
            IsCrouching = crouch;
            InteractTriggered = interact;
            LightAttackTriggered = lightAttack;
            SuperChargedAttackTriggered = superCharged;
            IsAimingDownSights = isADS;
            LockOnTriggered = lockOn;
        }

        public void ConsumeJump()
        {
            JumpTriggered = false;
        }

        public void ConsumeDodge()
        {
        }

        public void ConsumeInteract()
        {
            InteractTriggered = false;
        }

        public void ConsumeLightAttack()
        {
            LightAttackTriggered = false;
        }

        public void ConsumeSuperChargedAttack()
        {
            SuperChargedAttackTriggered = false;
        }

        public void ConsumeHeavyAttack()
        {
            SuperChargedAttackTriggered = false;
        }

        public void ConsumeAttack()
        {
            LightAttackTriggered = false;
        }

        public void ConsumeLockOn()
        {
            LockOnTriggered = false;
        }
    }
}
