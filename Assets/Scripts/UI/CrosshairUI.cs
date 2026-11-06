using UnityEngine;
using UnityEngine.UI;
using RelicTrail.Combat;
using RelicTrail.Player;

namespace RelicTrail.UI
{
    /// <summary>
    /// Dynamic combat crosshair centered on screen.
    /// Provides aiming visual, turns red when locked onto enemies, and pulses on attack recoil.
    /// </summary>
    [AddComponentMenu("Relic Trail/UI/Crosshair UI")]
    public class CrosshairUI : MonoBehaviour
    {
        [Header("Crosshair Visuals")]
        [SerializeField] private Image centerDot;
        [SerializeField] private Image[] reticleTicks;
        [SerializeField] private RectTransform[] tickTransforms;

        [Header("Styling & Colors")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color enemyTargetColor = new Color(1f, 0.22f, 0.22f, 0.95f);
        [SerializeField] private float baseSpread = 8f;
        [SerializeField] private float adsSpread = 4f;
        [SerializeField] private float recoilSpread = 16f;
        [SerializeField] private float recoverySpeed = 18f;

        [Header("Raycast Aim Detection")]
        [SerializeField] private float maxDetectionDistance = 60f;
        [SerializeField] private LayerMask detectionLayers = ~0;

        private Camera aimCamera;
        private PlayerAttack playerAttack;
        private PlayerInputHandler playerInput;
        private float currentSpread;
        private bool isTargetingEnemy;

        private void Awake()
        {
            currentSpread = baseSpread;
            if (aimCamera == null) aimCamera = Camera.main;
        }

        private void Start()
        {
            FindPlayerAndCamera();
        }

        private void OnEnable()
        {
            FindPlayerAndCamera();
            if (playerAttack != null)
            {
                playerAttack.OnAttackFired += TriggerRecoil;
            }
        }

        private void OnDisable()
        {
            if (playerAttack != null)
            {
                playerAttack.OnAttackFired -= TriggerRecoil;
            }
        }

        private void FindPlayerAndCamera()
        {
            if (aimCamera == null) aimCamera = Camera.main;

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                if (playerInput == null) playerInput = player.GetComponent<PlayerInputHandler>();
                if (playerAttack == null)
                {
                    playerAttack = player.GetComponent<PlayerAttack>();
                    if (playerAttack != null)
                    {
                        playerAttack.OnAttackFired -= TriggerRecoil;
                        playerAttack.OnAttackFired += TriggerRecoil;
                    }
                }
            }
        }

        private void Update()
        {
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
                if (aimCamera == null) return;
            }

            CheckTargetUnderCrosshair();
            UpdateSpreadAndColor();
        }

        private void CheckTargetUnderCrosshair()
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            isTargetingEnemy = false;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDetectionDistance, detectionLayers, QueryTriggerInteraction.Ignore))
            {
                bool isEnemy = hit.collider.GetComponentInParent<IDamageable>() != null ||
                               hit.collider.GetComponent<IDamageable>() != null ||
                               hit.collider.gameObject.name.Contains("Enemy") ||
                               hit.collider.gameObject.name.Contains("Beast");
                if (!isEnemy)
                {
                    try { isEnemy = hit.collider.CompareTag("Enemy"); } catch { }
                }

                if (isEnemy)
                {
                    isTargetingEnemy = true;
                }
            }
        }

        private void UpdateSpreadAndColor()
        {
            // Smoothly recover from recoil towards normal or ADS spread
            float targetBaseSpread = (playerInput != null && playerInput.IsAimingDownSights) ? adsSpread : baseSpread;
            currentSpread = Mathf.MoveTowards(currentSpread, targetBaseSpread, recoverySpeed * Time.deltaTime);

            Color targetColor = isTargetingEnemy ? enemyTargetColor : normalColor;

            // Apply color to center dot
            if (centerDot != null)
            {
                centerDot.color = targetColor;
            }

            // Apply color and spread to 4 reticle ticks (Top, Bottom, Left, Right)
            if (reticleTicks != null)
            {
                for (int i = 0; i < reticleTicks.Length; i++)
                {
                    if (reticleTicks[i] != null) reticleTicks[i].color = targetColor;
                }
            }

            if (tickTransforms != null && tickTransforms.Length >= 4)
            {
                // 0: Top, 1: Bottom, 2: Left, 3: Right
                if (tickTransforms[0] != null) tickTransforms[0].anchoredPosition = new Vector2(0f, currentSpread);
                if (tickTransforms[1] != null) tickTransforms[1].anchoredPosition = new Vector2(0f, -currentSpread);
                if (tickTransforms[2] != null) tickTransforms[2].anchoredPosition = new Vector2(-currentSpread, 0f);
                if (tickTransforms[3] != null) tickTransforms[3].anchoredPosition = new Vector2(currentSpread, 0f);
            }
        }

        /// <summary>
        /// Expands crosshair ticks momentarily upon firing magic projectile.
        /// </summary>
        public void TriggerRecoil()
        {
            currentSpread = recoilSpread;
        }

        public void SetupComponents(Image dot, Image[] ticks, RectTransform[] tickRects)
        {
            centerDot = dot;
            reticleTicks = ticks;
            tickTransforms = tickRects;
        }

        /// <summary>
        /// Ensures a crosshair exists in the scene at runtime if one hasn't been built yet.
        /// </summary>
        public static CrosshairUI EnsureCrosshairExists()
        {
            var existing = Object.FindAnyObjectByType<CrosshairUI>();
            if (existing != null) return existing;

            // Find or create Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("Runtime_CrosshairCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            // Create Crosshair GameObject
            GameObject crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(canvasObj.transform, false);
            var rectCH = crosshairObj.AddComponent<RectTransform>();
            rectCH.anchorMin = rectCH.anchorMax = rectCH.pivot = new Vector2(0.5f, 0.5f);
            rectCH.anchoredPosition = Vector2.zero;
            rectCH.sizeDelta = new Vector2(32f, 32f);

            // Center Dot
            GameObject dotObj = new GameObject("CenterDot");
            dotObj.transform.SetParent(crosshairObj.transform, false);
            var rectDot = dotObj.AddComponent<RectTransform>();
            rectDot.anchorMin = rectDot.anchorMax = rectDot.pivot = new Vector2(0.5f, 0.5f);
            rectDot.anchoredPosition = Vector2.zero;
            rectDot.sizeDelta = new Vector2(4f, 4f);
            var dotImg = dotObj.AddComponent<Image>();
            dotImg.color = new Color(1f, 1f, 1f, 0.85f);

            // Top Tick
            GameObject topTick = new GameObject("Tick_Top");
            topTick.transform.SetParent(crosshairObj.transform, false);
            var rTop = topTick.AddComponent<RectTransform>();
            rTop.anchorMin = rTop.anchorMax = rTop.pivot = new Vector2(0.5f, 0.5f);
            rTop.anchoredPosition = new Vector2(0f, 8f);
            rTop.sizeDelta = new Vector2(2f, 7f);
            var imgTop = topTick.AddComponent<Image>();
            imgTop.color = new Color(1f, 1f, 1f, 0.85f);

            // Bottom Tick
            GameObject botTick = new GameObject("Tick_Bottom");
            botTick.transform.SetParent(crosshairObj.transform, false);
            var rBot = botTick.AddComponent<RectTransform>();
            rBot.anchorMin = rBot.anchorMax = rBot.pivot = new Vector2(0.5f, 0.5f);
            rBot.anchoredPosition = new Vector2(0f, -8f);
            rBot.sizeDelta = new Vector2(2f, 7f);
            var imgBot = botTick.AddComponent<Image>();
            imgBot.color = new Color(1f, 1f, 1f, 0.85f);

            // Left Tick
            GameObject leftTick = new GameObject("Tick_Left");
            leftTick.transform.SetParent(crosshairObj.transform, false);
            var rLeft = leftTick.AddComponent<RectTransform>();
            rLeft.anchorMin = rLeft.anchorMax = rLeft.pivot = new Vector2(0.5f, 0.5f);
            rLeft.anchoredPosition = new Vector2(-8f, 0f);
            rLeft.sizeDelta = new Vector2(7f, 2f);
            var imgLeft = leftTick.AddComponent<Image>();
            imgLeft.color = new Color(1f, 1f, 1f, 0.85f);

            // Right Tick
            GameObject rightTick = new GameObject("Tick_Right");
            rightTick.transform.SetParent(crosshairObj.transform, false);
            var rRight = rightTick.AddComponent<RectTransform>();
            rRight.anchorMin = rRight.anchorMax = rRight.pivot = new Vector2(0.5f, 0.5f);
            rRight.anchoredPosition = new Vector2(8f, 0f);
            rRight.sizeDelta = new Vector2(7f, 2f);
            var imgRight = rightTick.AddComponent<Image>();
            imgRight.color = new Color(1f, 1f, 1f, 0.85f);

            var comp = crosshairObj.AddComponent<CrosshairUI>();
            comp.SetupComponents(dotImg, new Image[] { imgTop, imgBot, imgLeft, imgRight }, new RectTransform[] { rTop, rBot, rLeft, rRight });
            return comp;
        }
    }
}
