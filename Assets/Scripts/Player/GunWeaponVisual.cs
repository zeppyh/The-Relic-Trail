using System.Collections;
using UnityEngine;

namespace RelicTrail.Player
{
    /// <summary>
    /// Procedural 3D firearm visual controller.
    /// Builds a sleek low-poly carbine/blaster model mounted in the player's right hand.
    /// Manages muzzle flash, recoil kickback animation, charging visuals, and procedural gunshot sound synthesis.
    /// </summary>
    [AddComponentMenu("Relic Trail/Player/Gun Weapon Visual")]
    public class GunWeaponVisual : MonoBehaviour
    {
        [Header("Weapon Transforms")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private GameObject gunRoot;

        [Header("Muzzle Flash FX")]
        [SerializeField] private GameObject muzzleFlashVisual;
        [SerializeField] private Light muzzleFlashLight;

        [Header("Super Charge FX")]
        [SerializeField] private GameObject chargeVisualObj;
        [SerializeField] private Light chargeLight;

        [Header("Procedural Audio Clips")]
        private AudioClip lightShotSound;
        private AudioClip heavyShotSound;
        private AudioClip chargeSound;

        [Header("Weapon Grip Placement")]
        [SerializeField] private Vector3 baseLocalPosition = new Vector3(0.015f, 0.035f, -0.095f);
        [SerializeField] private Vector3 baseLocalEulerAngles = new Vector3(4f, 192f, 0f);

        [Header("IK Foregrip Target")]
        [SerializeField] private Transform leftHandGripPoint;

        // Recoil state
        private Quaternion baseLocalRotation;
        private Vector3 currentRecoilOffset;
        private Quaternion currentRecoilRot = Quaternion.identity;
        private float recoilRecoverySpeed = 16f;

        public Transform MuzzlePoint => muzzlePoint;
        public Transform LeftHandGripPoint => leftHandGripPoint;
        public AudioClip LightShotSound => lightShotSound;
        public AudioClip HeavyShotSound => heavyShotSound;
        public AudioClip ChargeSound => chargeSound;

        private void Awake()
        {
            baseLocalRotation = Quaternion.Euler(baseLocalEulerAngles);
            BuildGunModel();
            GenerateGunAudio();
        }

        private void OnValidate()
        {
            baseLocalRotation = Quaternion.Euler(baseLocalEulerAngles);
            if (gunRoot != null)
            {
                gunRoot.transform.localPosition = baseLocalPosition;
                gunRoot.transform.localRotation = baseLocalRotation;
            }
        }

        private void Update()
        {
            // Smooth recoil recovery back to resting aim pose
            currentRecoilOffset = Vector3.Lerp(currentRecoilOffset, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
            currentRecoilRot = Quaternion.Slerp(currentRecoilRot, Quaternion.identity, Time.deltaTime * recoilRecoverySpeed);

            if (gunRoot != null)
            {
                gunRoot.transform.localPosition = baseLocalPosition + currentRecoilOffset;
                gunRoot.transform.localRotation = baseLocalRotation * currentRecoilRot;
            }
        }

        /// <summary>
        /// Builds the multi-part low-poly gun model.
        /// </summary>
        public void BuildGunModel()
        {
            if (gunRoot != null)
            {
                DestroyImmediate(gunRoot);
            }

            gunRoot = new GameObject("Gun_Model");
            gunRoot.transform.SetParent(transform, false);
            gunRoot.transform.localPosition = baseLocalPosition;
            gunRoot.transform.localRotation = baseLocalRotation;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Materials
            Material bodyMat = new Material(litShader);
            bodyMat.name = "Mat_GunBody";
            bodyMat.color = new Color(0.15f, 0.16f, 0.18f); // Dark gunmetal
            if (bodyMat.HasProperty("_Metallic")) bodyMat.SetFloat("_Metallic", 0.85f);
            if (bodyMat.HasProperty("_Smoothness")) bodyMat.SetFloat("_Smoothness", 0.65f);

            Material darkTrimMat = new Material(litShader);
            darkTrimMat.name = "Mat_GunTrim";
            darkTrimMat.color = new Color(0.08f, 0.08f, 0.09f); // Matte polymer
            if (darkTrimMat.HasProperty("_Metallic")) darkTrimMat.SetFloat("_Metallic", 0.2f);
            if (darkTrimMat.HasProperty("_Smoothness")) darkTrimMat.SetFloat("_Smoothness", 0.3f);

            Material goldMat = new Material(litShader);
            goldMat.name = "Mat_GunGold";
            goldMat.color = new Color(0.85f, 0.68f, 0.22f); // Brass/Gold accents
            if (goldMat.HasProperty("_Metallic")) goldMat.SetFloat("_Metallic", 0.9f);
            if (goldMat.HasProperty("_Smoothness")) goldMat.SetFloat("_Smoothness", 0.8f);

            Material holoMat = new Material(litShader);
            holoMat.name = "Mat_GunHolo";
            holoMat.color = new Color(0.2f, 0.9f, 1f); // Glowing cyan reticle
            if (holoMat.HasProperty("_EmissionColor"))
            {
                holoMat.EnableKeyword("_EMISSION");
                holoMat.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 1f) * 2.5f);
            }

            // 1. Receiver / Main Frame
            CreatePart(gunRoot.transform, "Receiver", PrimitiveType.Cube,
                new Vector3(0f, 0.02f, 0.08f),
                new Vector3(0.065f, 0.09f, 0.32f),
                Quaternion.identity, bodyMat);

            // 2. Barrel
            CreatePart(gunRoot.transform, "Barrel", PrimitiveType.Cylinder,
                new Vector3(0f, 0.045f, 0.34f),
                new Vector3(0.038f, 0.16f, 0.038f),
                Quaternion.Euler(90f, 0f, 0f), darkTrimMat);

            // 3. Muzzle Brake
            CreatePart(gunRoot.transform, "MuzzleBrake", PrimitiveType.Cylinder,
                new Vector3(0f, 0.045f, 0.51f),
                new Vector3(0.046f, 0.035f, 0.046f),
                Quaternion.Euler(90f, 0f, 0f), goldMat);

            // 4. Handguard / Lower Shroud
            CreatePart(gunRoot.transform, "Handguard", PrimitiveType.Cube,
                new Vector3(0f, 0.015f, 0.28f),
                new Vector3(0.055f, 0.05f, 0.18f),
                Quaternion.identity, darkTrimMat);

            // 5. Magazine Clip (angled slightly forward)
            CreatePart(gunRoot.transform, "Magazine", PrimitiveType.Cube,
                new Vector3(0f, -0.09f, 0.14f),
                new Vector3(0.042f, 0.14f, 0.065f),
                Quaternion.Euler(14f, 0f, 0f), darkTrimMat);

            // 6. Pistol Grip (angled backward into palm)
            CreatePart(gunRoot.transform, "Grip", PrimitiveType.Cube,
                new Vector3(0f, -0.075f, -0.04f),
                new Vector3(0.048f, 0.13f, 0.06f),
                Quaternion.Euler(-20f, 0f, 0f), darkTrimMat);

            // 7. Trigger Guard
            CreatePart(gunRoot.transform, "TriggerGuard", PrimitiveType.Cube,
                new Vector3(0f, -0.035f, 0.02f),
                new Vector3(0.025f, 0.035f, 0.055f),
                Quaternion.identity, goldMat);

            // 8. Compact Tactical Stock
            CreatePart(gunRoot.transform, "StockTube", PrimitiveType.Cylinder,
                new Vector3(0f, 0.03f, -0.14f),
                new Vector3(0.032f, 0.09f, 0.032f),
                Quaternion.Euler(90f, 0f, 0f), darkTrimMat);

            CreatePart(gunRoot.transform, "StockPad", PrimitiveType.Cube,
                new Vector3(0f, 0.01f, -0.23f),
                new Vector3(0.045f, 0.11f, 0.04f),
                Quaternion.identity, bodyMat);

            // 9. Top Rail & Reflex Sight Frame
            CreatePart(gunRoot.transform, "SightMount", PrimitiveType.Cube,
                new Vector3(0f, 0.078f, 0.07f),
                new Vector3(0.045f, 0.025f, 0.14f),
                Quaternion.identity, darkTrimMat);

            CreatePart(gunRoot.transform, "SightHood", PrimitiveType.Cube,
                new Vector3(0f, 0.105f, 0.07f),
                new Vector3(0.052f, 0.035f, 0.05f),
                Quaternion.identity, bodyMat);

            // 10. Glowing Holographic Sight Dot
            CreatePart(gunRoot.transform, "SightReticle", PrimitiveType.Cube,
                new Vector3(0f, 0.105f, 0.07f),
                new Vector3(0.015f, 0.015f, 0.015f),
                Quaternion.identity, holoMat);

            // 11. Muzzle Point
            GameObject muzzleObj = new GameObject("MuzzlePoint");
            muzzleObj.transform.SetParent(gunRoot.transform, false);
            muzzleObj.transform.localPosition = new Vector3(0f, 0.045f, 0.55f);
            muzzlePoint = muzzleObj.transform;

            // 12. Muzzle Flash Visual & Point Light
            muzzleFlashVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            muzzleFlashVisual.name = "FlashVisual";
            muzzleFlashVisual.transform.SetParent(muzzlePoint, false);
            muzzleFlashVisual.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            muzzleFlashVisual.transform.localScale = Vector3.one * 0.28f;
            var flashCol = muzzleFlashVisual.GetComponent<Collider>();
            if (flashCol != null) DestroyImmediate(flashCol);

            var flashRend = muzzleFlashVisual.GetComponent<Renderer>();
            if (flashRend != null)
            {
                Material fm = new Material(litShader);
                fm.color = new Color(1f, 0.88f, 0.35f);
                if (fm.HasProperty("_EmissionColor"))
                {
                    fm.EnableKeyword("_EMISSION");
                    fm.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f) * 4f);
                }
                flashRend.material = fm;
            }
            muzzleFlashVisual.SetActive(false);

            var lightObj = new GameObject("MuzzleLight");
            lightObj.transform.SetParent(muzzlePoint, false);
            lightObj.transform.localPosition = Vector3.zero;
            muzzleFlashLight = lightObj.AddComponent<Light>();
            muzzleFlashLight.type = LightType.Point;
            muzzleFlashLight.color = new Color(1f, 0.85f, 0.4f);
            muzzleFlashLight.range = 5f;
            muzzleFlashLight.intensity = 4f;
            muzzleFlashLight.enabled = false;

            // 13. Super Charged Visual Energy Sphere & Light
            chargeVisualObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chargeVisualObj.name = "ChargeVisual";
            chargeVisualObj.transform.SetParent(muzzlePoint, false);
            chargeVisualObj.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            chargeVisualObj.transform.localScale = Vector3.one * 0.15f;
            var chargeCol = chargeVisualObj.GetComponent<Collider>();
            if (chargeCol != null) DestroyImmediate(chargeCol);

            var chargeRend = chargeVisualObj.GetComponent<Renderer>();
            if (chargeRend != null)
            {
                Material cm = new Material(litShader);
                cm.color = new Color(0.25f, 0.9f, 1f, 0.95f);
                if (cm.HasProperty("_EmissionColor"))
                {
                    cm.EnableKeyword("_EMISSION");
                    cm.SetColor("_EmissionColor", new Color(0.2f, 0.85f, 1f) * 4.5f);
                }
                chargeRend.material = cm;
            }
            chargeVisualObj.SetActive(false);

            var clObj = new GameObject("ChargeLight");
            clObj.transform.SetParent(muzzlePoint, false);
            clObj.transform.localPosition = Vector3.zero;
            chargeLight = clObj.AddComponent<Light>();
            chargeLight.type = LightType.Point;
            chargeLight.color = new Color(0.2f, 0.85f, 1f);
            chargeLight.range = 4f;
            chargeLight.intensity = 3f;
            chargeLight.enabled = false;

            // 14. Left Hand Foregrip IK Target
            GameObject gripObj = new GameObject("LeftHandGripPoint");
            gripObj.transform.SetParent(gunRoot.transform, false);
            gripObj.transform.localPosition = new Vector3(-0.025f, -0.015f, 0.26f);
            gripObj.transform.localRotation = Quaternion.Euler(15f, -20f, -75f);
            leftHandGripPoint = gripObj.transform;
        }

        private GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRot;

            // Remove any colliders so weapon never blocks player or physics
            var col = part.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            var rend = part.GetComponent<Renderer>();
            if (rend != null && mat != null)
            {
                rend.material = mat;
            }
            return part;
        }

        /// <summary>
        /// Controls the glowing charge energy sphere during the Q Super Charged Attack windup.
        /// </summary>
        public void SetChargeEffect(bool active, float progress = 0f)
        {
            if (chargeVisualObj != null)
            {
                chargeVisualObj.SetActive(active);
                if (active)
                {
                    float scale = Mathf.Lerp(0.08f, 0.28f, progress);
                    scale += Random.Range(-0.015f, 0.015f); // energy jitter
                    chargeVisualObj.transform.localScale = Vector3.one * scale;
                }
            }
            if (chargeLight != null)
            {
                chargeLight.enabled = active;
                if (active)
                {
                    chargeLight.intensity = Mathf.Lerp(1.5f, 6.0f, progress);
                    chargeLight.range = Mathf.Lerp(2.5f, 5.5f, progress);
                }
            }
        }

        /// <summary>
        /// Triggers visual recoil kickback and muzzle flash.
        /// </summary>
        public void TriggerFireFeedback(bool isHeavy)
        {
            if (isHeavy)
            {
                currentRecoilOffset = new Vector3(0f, 0.04f, -0.15f);
                currentRecoilRot = Quaternion.Euler(-20f, 3f, 4f);
                recoilRecoverySpeed = 10f;
            }
            else
            {
                currentRecoilOffset = new Vector3(0f, 0.02f, -0.065f);
                currentRecoilRot = Quaternion.Euler(-9f, 2f, 1f);
                recoilRecoverySpeed = 16f;
            }

            StartCoroutine(FlashRoutine(isHeavy));
        }

        private IEnumerator FlashRoutine(bool isHeavy)
        {
            if (muzzleFlashVisual != null)
            {
                muzzleFlashVisual.transform.localScale = Vector3.one * (isHeavy ? 0.45f : 0.26f);
                muzzleFlashVisual.SetActive(true);
            }
            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.intensity = isHeavy ? 6f : 3.5f;
                muzzleFlashLight.range = isHeavy ? 7f : 4.5f;
                muzzleFlashLight.enabled = true;
            }

            yield return new WaitForSeconds(isHeavy ? 0.06f : 0.04f);

            if (muzzleFlashVisual != null) muzzleFlashVisual.SetActive(false);
            if (muzzleFlashLight != null) muzzleFlashLight.enabled = false;
        }

        /// <summary>
        /// Generates punchy procedural sound clips for firearms.
        /// </summary>
        private void GenerateGunAudio()
        {
            lightShotSound = CreateGunshotClip("GunShot_Light", 0.15f, 480f, 0.65f);
            heavyShotSound = CreateGunshotClip("GunShot_Heavy", 0.28f, 220f, 0.95f);
            chargeSound = CreateChargeSweepClip("GunCharge", 0.22f);
        }

        private AudioClip CreateGunshotClip(string name, float duration, float centerFreq, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Sharp attack envelope with exponential decay
                float envelope = Mathf.Exp(-progress * 14f);

                // Transient pop + noise crack + low frequency thump
                float noise = (Random.value * 2f - 1f) * 0.7f;
                float pop = Mathf.Sin(2f * Mathf.PI * centerFreq * Mathf.Exp(-progress * 8f) * t) * 0.5f;
                float sub = Mathf.Sin(2f * Mathf.PI * (centerFreq * 0.4f) * t) * 0.4f;

                data[i] = (noise + pop + sub) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateChargeSweepClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Upward frequency sweep (200Hz to 1200Hz)
                float freq = Mathf.Lerp(200f, 1200f, progress);
                float envelope = Mathf.Sin(progress * Mathf.PI);

                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
