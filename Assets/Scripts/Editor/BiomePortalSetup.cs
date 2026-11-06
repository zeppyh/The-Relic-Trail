using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RelicTrail.Core;
using RelicTrail.Progression;

namespace RelicTrail.Editor
{
    [InitializeOnLoad]
    public static class BiomePortalAutoRunner
    {
        static BiomePortalAutoRunner()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            if (SessionState.GetBool("BiomePortalSetup_V1_Done", false)) return;
            SessionState.SetBool("BiomePortalSetup_V1_Done", true);
            try
            {
                BiomePortalSetup.RunFullPortalSetup(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BiomePortalSetup] Error during auto-setup: {ex}");
            }
        }
    }

    public static class BiomePortalSetup
    {
        public const string ForestPortalPrefabPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalEight.prefab";
        public const string CanyonPortalPrefabPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalTen.prefab";
        public const string FrozenPortalPrefabPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalSix.prefab";

        public const string PortalHumPath = "Assets/Audio/Portals/portal_hum.wav";
        public const string PortalTeleportPath = "Assets/Audio/Portals/portal_teleport.wav";
        public const string PortalActivatePath = "Assets/Audio/Portals/portal_activate.wav";

        public static void RunFullPortalSetupMenuItem()
        {
            RunFullPortalSetup(true);
        }

        public static void RunFullPortalSetupBatch()
        {
            RunFullPortalSetup(false);
        }

        public static void RunFullPortalSetup(bool showDialog)
        {
            Debug.Log("[BiomePortalSetup] Starting setup of Biome Portals with The Portal Collection assets...");

            AudioClip humClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PortalHumPath);
            AudioClip teleportClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PortalTeleportPath);
            AudioClip activateClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PortalActivatePath);

            string[] scenesToUpdate = new string[]
            {
                "Assets/Scenes/SampleScene.unity",
                "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Scenes/All models.unity"
            };

            foreach (var scenePath in scenesToUpdate)
            {
                if (!File.Exists(scenePath)) continue;

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool modified = false;

                // 1. Forest Portal (Portal_ForestToCanyon) -> Forest theme (PortalEight, Green)
                var p1 = GameObject.Find("Portal_ForestToCanyon");
                if (p1 != null)
                {
                    ConfigurePortalObject(
                        p1,
                        ForestPortalPrefabPath,
                        new Color(0.2f, 1.0f, 0.4f), // Emerald green
                        humClip, teleportClip, activateClip
                    );
                    modified = true;
                }

                // 2. Canyon Portal (Portal_CanyonToFrozen) -> Scorched Canyon theme (PortalTen, Orange)
                var p2 = GameObject.Find("Portal_CanyonToFrozen");
                if (p2 != null)
                {
                    ConfigurePortalObject(
                        p2,
                        CanyonPortalPrefabPath,
                        new Color(1.0f, 0.55f, 0.1f), // Fiery canyon orange
                        humClip, teleportClip, activateClip
                    );
                    modified = true;
                }

                // 3. Frozen Portal (Portal_FrozenToSanctuary) -> Frozen Ruins theme (PortalSix, Cyan-Blue)
                var p3 = GameObject.Find("Portal_FrozenToSanctuary");
                if (p3 != null)
                {
                    ConfigurePortalObject(
                        p3,
                        FrozenPortalPrefabPath,
                        new Color(0.25f, 0.85f, 1.0f), // Glacial icy cyan-blue
                        humClip, teleportClip, activateClip
                    );
                    modified = true;
                }

                if (modified)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[BiomePortalSetup] Saved updated portals in scene: {scenePath}");
                }
            }

            // Restore SampleScene as active
            if (File.Exists("Assets/Scenes/SampleScene.unity"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[BiomePortalSetup] Biome Portals setup complete!");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Portals Setup Complete",
                    "Biome Portals successfully upgraded with The Portal Collection!\n\n" +
                    "✓ Biome 1 (Forgotten Forest): PortalEight with overgrown stone arch & glowing emerald green vortex\n" +
                    "✓ Biome 2 (Scorched Canyon): PortalTen with canyon sandstone arch & blazing orange vortex\n" +
                    "✓ Biome 3 (Frozen Ruins): PortalSix with ancient frost stone arch & glacial blue vortex\n" +
                    "✓ 3D Spatial Audio: Looping ambient portal hum, activation energy surge, and teleport transit whoosh\n" +
                    "✓ Dynamic Animation: Continuous vortex swirling & breathing scale pulse, rhythmic light glow, and walk-in trigger!",
                    "OK"
                );
            }
        }

        public static void ConfigurePortalObject(
            GameObject portalObj,
            string prefabPath,
            Color biomeColor,
            AudioClip humClip,
            AudioClip teleportClip,
            AudioClip activateClip)
        {
            if (portalObj == null) return;

            // 1. Remove obsolete primitive children (PortalArch, old Vortex)
            for (int i = portalObj.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = portalObj.transform.GetChild(i);
                if (child.name == "PortalArch" || child.name == "Vortex" || child.name.StartsWith("PortalModel"))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            // 2. Instantiate new visual portal model from The Portal Collection
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject visualInstance = null;
            if (modelPrefab != null)
            {
                visualInstance = PrefabUtility.InstantiatePrefab(modelPrefab, portalObj.transform) as GameObject;
                if (visualInstance != null)
                {
                    visualInstance.name = "PortalModel";
                    visualInstance.transform.localPosition = Vector3.zero;
                    visualInstance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // Face approaching player along Z axis
                    visualInstance.transform.localScale = Vector3.one;
                }
            }
            else
            {
                Debug.LogWarning($"[BiomePortalSetup] Could not load portal prefab at: {prefabPath}");
            }

            // 3. Locate vortex mesh child inside the visual model
            GameObject vortexObj = null;
            Transform vortexTransform = null;

            if (visualInstance != null)
            {
                // Look for Cylinder vortex child (e.g. Cylinder.014 in PortalEight, Cylinder.011 in PortalTen/Six)
                var meshRenderers = visualInstance.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in meshRenderers)
                {
                    // Check if material is one of the vortex materials
                    if (mr.sharedMaterial != null && mr.sharedMaterial.name.StartsWith("Portal") &&
                        (mr.sharedMaterial.name.Contains("Green") || mr.sharedMaterial.name.Contains("Orange") ||
                         mr.sharedMaterial.name.Contains("Blue") || mr.sharedMaterial.name.Contains("Red") ||
                         mr.sharedMaterial.name.Contains("White") || mr.sharedMaterial.name.Contains("Violet")))
                    {
                        vortexObj = mr.gameObject;
                        vortexTransform = mr.transform;
                        break;
                    }
                }

                // Fallback: look by name
                if (vortexObj == null)
                {
                    foreach (Transform c in visualInstance.transform)
                    {
                        if (c.name.StartsWith("Cylinder") && c.name != visualInstance.transform.GetChild(0).name)
                        {
                            vortexObj = c.gameObject;
                            vortexTransform = c;
                            break;
                        }
                    }
                }
            }

            // 4. Configure or add PortalLight
            Transform lightTr = portalObj.transform.Find("PortalLight");
            GameObject lightObj;
            Light portalLight;
            if (lightTr != null)
            {
                lightObj = lightTr.gameObject;
                portalLight = lightObj.GetComponent<Light>();
                if (portalLight == null) portalLight = lightObj.AddComponent<Light>();
            }
            else
            {
                lightObj = new GameObject("PortalLight");
                lightObj.transform.SetParent(portalObj.transform);
                portalLight = lightObj.AddComponent<Light>();
            }

            lightObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            portalLight.type = LightType.Point;
            portalLight.color = biomeColor;
            portalLight.range = 8.5f;
            portalLight.intensity = 3.5f;

            // 5. Configure or add Biome Particle System
            Transform particlesTr = portalObj.transform.Find("PortalParticles");
            GameObject particlesObj;
            ParticleSystem ps;
            if (particlesTr != null)
            {
                particlesObj = particlesTr.gameObject;
                ps = particlesObj.GetComponent<ParticleSystem>();
                if (ps == null) ps = particlesObj.AddComponent<ParticleSystem>();
            }
            else
            {
                particlesObj = new GameObject("PortalParticles");
                particlesObj.transform.SetParent(portalObj.transform);
                ps = particlesObj.AddComponent<ParticleSystem>();
            }

            particlesObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            particlesObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var main = ps.main;
            main.startLifetime = 2.8f;
            main.startSpeed = 0.6f;
            main.startSize = 0.12f;
            main.startColor = new ParticleSystem.MinMaxGradient(biomeColor, new Color(biomeColor.r * 1.2f, biomeColor.g * 1.2f, biomeColor.b * 1.2f, 0.8f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.1f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.z = new ParticleSystem.MinMaxCurve(0.4f, 1.0f); // drifts upward relative to -90 deg rotation

            // 6. Configure 3D spatial AudioSource
            var audioSource = portalObj.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = portalObj.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 2.5f;
            audioSource.maxDistance = 25.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.volume = 0.7f;
            if (humClip != null) audioSource.clip = humClip;

            // 7. Configure BoxCollider trigger for walk-in interaction
            var boxCol = portalObj.GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = portalObj.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.center = new Vector3(0f, 1.5f, 0f);
            boxCol.size = new Vector3(3f, 3.5f, 2.5f);

            // 8. Configure TeleportPortal component
            var portalComp = portalObj.GetComponent<TeleportPortal>();
            if (portalComp == null) portalComp = portalObj.AddComponent<TeleportPortal>();

            SerializedObject so = new SerializedObject(portalComp);
            so.FindProperty("activeVfx").objectReferenceValue = vortexObj;
            so.FindProperty("portalLight").objectReferenceValue = portalLight;
            so.FindProperty("portalAudioSource").objectReferenceValue = audioSource;
            so.FindProperty("teleportSound").objectReferenceValue = teleportClip;
            so.FindProperty("activationSound").objectReferenceValue = activateClip;
            so.FindProperty("ambientHumSound").objectReferenceValue = humClip;
            so.FindProperty("vortexTransform").objectReferenceValue = vortexTransform;
            so.FindProperty("portalParticles").objectReferenceValue = ps;
            so.FindProperty("spinSpeed").floatValue = 25f;
            so.FindProperty("pulseSpeed").floatValue = 2f;
            so.FindProperty("pulseAmount").floatValue = 0.04f;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(portalObj);
            EditorUtility.SetDirty(portalComp);
        }
    }
}
