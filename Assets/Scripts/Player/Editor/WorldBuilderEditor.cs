using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using RelicTrail.Core;
using RelicTrail.Player;
using RelicTrail.Combat;
using RelicTrail.Enemies;
using RelicTrail.NPC;
using RelicTrail.Puzzles;
using RelicTrail.Progression;
using RelicTrail.UI;

namespace RelicTrail.Editor
{
    /// <summary>
    /// Master Editor Builder for 'The Lost Relic'.
    /// Generates all 3 Biomes (Forgotten Forest, Scorched Canyon, Frozen Ruins) and the Final Sanctuary,
    /// complete with NPCs, puzzles, 36 collectibles, portals, enemies, and full UI into the active scene.
    /// </summary>
    public static class WorldBuilderEditor
    {
        // Character Prefabs
        private const string Male01PrefabPath = "Assets/ThirdParty/Floreswa/Prefabs/male01_1.prefab";
        private const string Male02PrefabPath = "Assets/ThirdParty/Floreswa/Prefabs/male02_1.prefab";
        private const string Male03PrefabPath = "Assets/ThirdParty/Floreswa/Prefabs/male03_1.prefab";

        // Collectible Coins
        private const string Coin01PrefabPath = "Assets/ThirdParty/LowPolyMedievalPropsLite/Prefabs/Coin_01.prefab";
        private const string Coin02PrefabPath = "Assets/ThirdParty/LowPolyMedievalPropsLite/Prefabs/Coin_02.prefab";
        private const string Coin03PrefabPath = "Assets/ThirdParty/LowPolyMedievalPropsLite/Prefabs/Coin_03.prefab";

        // Forest Assets
        private const string FirTreeLargePath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Trees/Fir trees/fir tree large.prefab";
        private const string FirTreeMediumPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Trees/Fir trees/fir tree medium.prefab";
        private const string PineTreeLargePath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Trees/Fir trees/pine tree large.prefab";
        private const string TreeTrunkPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Trees/Tree trunks/tree trunk.prefab";
        private const string GrassPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Plants/Grass/grass.prefab";
        private const string Grass2Path = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Plants/Grass/grass 2.prefab";
        private const string BushPlantPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Plants/Autumn bush plants/autumn bush plant.prefab";
        private const string MushroomPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Plants/Mushrooms brown/mushroom brown.prefab";
        private const string StoneLargeGrayPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Stones/Stones large gray blue/stone large gray blue.prefab";
        private const string StoneSmallPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Stones/Stones small/stone small 2.prefab";
        private const string JapaneseTemplePath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Houses/japanes temple.prefab";
        private const string ToriiGatePath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/torii gate.prefab";
        private const string Cabin01Path = "Assets/ThirdParty/WoodsLifestyle/Prefabs/Cabins/Cabin_01.prefab";
        private const string CampFire01Path = "Assets/ThirdParty/WoodsLifestyle/Prefabs/Miscellaneous/CampFire_01.prefab";
        private const string LogStand01Path = "Assets/ThirdParty/WoodsLifestyle/Prefabs/Miscellaneous/LogStand_01.prefab";
        private const string SignPostPath = "Assets/ThirdParty/WoodsLifestyle/Prefabs/Miscellaneous/SignPost_Custom_01.prefab";

        // Canyon Assets
        private const string Cactus1Path = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Plants/Cacti/cactus.prefab";
        private const string Cactus2Path = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Plants/Cacti/cactus 2.prefab";
        private const string Cactus3Path = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Plants/Cacti/cactus 3.prefab";
        private const string PalmTreeLargePath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Trees/Palm trees/palm tree large.prefab";
        private const string PalmTreeBentPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Trees/Palm trees/palm tree bent.prefab";
        private const string DesertStoneSmallPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Stones/Desert stones/desert stone small.prefab";
        private const string DesertStoneSmall2Path = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Stones/Desert stones/desert stone small 2.prefab";
        private const string SphinxPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/sphinx.prefab";
        private const string WoodenBridgePath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/wooden bridge.prefab";
        private const string BarrelPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/Barrels/barrel.prefab";
        private const string BoxPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/Box/box.prefab";
        private const string CliffStraight1Path = "Assets/ThirdParty/BrokenVector/LowPolyCliffPack/Prefabs/Clifftile Straight 1.prefab";
        private const string CliffStraight2Path = "Assets/ThirdParty/BrokenVector/LowPolyCliffPack/Prefabs/Clifftile Straight 2.prefab";

        // Winter Assets
        private const string FirTreeWinterLargePath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Trees/Fir trees winter/fir tree winter large.prefab";
        private const string FirTreeWinterMediumPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Trees/Fir trees winter/fir tree winter medium.prefab";
        private const string FirTreeWinterSmallPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Trees/Fir trees winter/fir tree winter small.prefab";
        private const string StoneWinterSmallPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Stones/Stones winter small/stone winter small.prefab";
        private const string StoneWinterSmall2Path = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Stones/Stones winter small/stone winter small 2.prefab";
        private const string StonesGroupWinterPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Stones/stones group winter.prefab";
        private const string SkullMountainPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Mountains/skull mountain.prefab";
        private const string JapaneseLanternPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/japanese lantern.prefab";
        private const string ChestPath = "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/chest.prefab";
        private const string Cabin02Path = "Assets/ThirdParty/WoodsLifestyle/Prefabs/Cabins/Cabin_02.prefab";
        // Gem Relic Prefabs
        private const string ForestGemPrefabPath = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/HeartGem.prefab";
        private const string CanyonGemPrefabPath = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/SphereGem.prefab";
        private const string FrozenGemPrefabPath = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/StarGem1.prefab";

        // Sanctuary Assets
        private const string StairsPath = "Assets/ThirdParty/LowPolyMedievalPropsLite/Prefabs/Stairs_01.prefab";
        private const string FencePath = "Assets/ThirdParty/LowPolyMedievalPropsLite/Prefabs/Fence_01.prefab";
        private const string FXFirePath = "Assets/ThirdParty/LowPolyMedievalPropsLite/Prefabs/FX/FX_Fire_01.prefab";
        private const string StonePillarPath = "Assets/ThirdParty/LowPolyMedievalPropsLite/Prefabs/Stone_01.prefab";

        [MenuItem("Relic Trail/Generate Complete 'The Lost Relic' Game World", false, 1)]
        public static void GenerateCompleteGameWorld()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.isLoaded)
            {
                EditorUtility.DisplayDialog("Error", "Please open a scene first (e.g. SampleScene.unity).", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Build The Lost Relic World",
                "This will build all 3 Biomes (Forest, Canyon, Frozen Ruins), Final Sanctuary, NPCs, Puzzles, Collectibles, Portals, Player, and UI into the current scene.\n\nProceed?",
                "Yes, Build World", "Cancel"))
            {
                return;
            }

            ExecuteWorldGeneration(true);
        }

        [MenuItem("Relic Trail/Fix 3D Camera and Attach to Player", false, 2)]
        public static void FixCameraAndAttachToPlayer()
        {
            GameObject player = null;
            var worldRoot = GameObject.Find("--- THE LOST RELIC WORLD ---");
            if (worldRoot != null)
            {
                var p = worldRoot.transform.Find("Player");
                if (p != null) player = p.gameObject;
            }

            if (player == null)
            {
                var pm = Object.FindAnyObjectByType<PlayerMovement>();
                if (pm != null) player = pm.gameObject;
            }

            if (player == null)
            {
                player = GameObject.FindWithTag("Player");
            }

            if (player == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find Player GameObject in the active scene. Please generate the world first from Relic Trail menu.", "OK");
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
            }

            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 1000f;

            cam.transform.position = player.transform.position + new Vector3(0.35f, 1.65f + 0.3f, -3.2f);
            cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

            var tpCam = cam.GetComponent<ThirdPersonCamera>();
            if (tpCam == null) tpCam = cam.gameObject.AddComponent<ThirdPersonCamera>();

            SerializedObject so = new SerializedObject(tpCam);
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.FindProperty("defaultDistance").floatValue = 2.6f;
            so.FindProperty("minDistance").floatValue = 0.9f;
            so.FindProperty("maxDistance").floatValue = 5.0f;
            so.FindProperty("targetOffset").vector3Value = new Vector3(0f, 1.70f, 0f);
            so.FindProperty("shoulderOffset").floatValue = 0.48f;
            so.FindProperty("adsDistance").floatValue = 1.35f;
            so.FindProperty("adsShoulderOffset").floatValue = 0.38f;
            so.FindProperty("adsTargetOffset").vector3Value = new Vector3(0f, 1.68f, 0f);
            so.FindProperty("normalFOV").floatValue = 65f;
            so.FindProperty("adsFOV").floatValue = 46f;
            so.FindProperty("adsTransitionSpeed").floatValue = 12f;
            so.FindProperty("positionSmoothTime").floatValue = 0.0f;
            so.FindProperty("rotationSmoothTime").floatValue = 0.0f;
            so.ApplyModifiedProperties();

            tpCam.SnapToTarget();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = cam.gameObject;

            EditorUtility.DisplayDialog(
                "Elden Ring Camera Configured!",
                "Main Camera is now locked at Head Level with Elden Ring style action RPG framing!\n\n" +
                "• Target: " + player.name + "\n" +
                "• Height: 1.65m (Head Level)\n" +
                "• Shoulder Offset: 0.35m (Over-the-shoulder view)\n" +
                "• Distance: 3.2 meters (Action framing)\n" +
                "• Vertical Angle: 10°\n" +
                "• Lock-On: Middle Mouse Button\n" +
                "• Responsiveness: 1:1 Instant mouse look\n\n" +
                "Press PLAY (▶) to explore and battle!",
                "Great!"
            );
        }

        [MenuItem("Relic Trail/Remove Blue Floor Overlay (Clear NavMesh)", false, 3)]
        public static void ClearNavMeshOverlay()
        {
            try
            {
                UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            }
            catch { }

            string[] assetFiles = System.IO.Directory.GetFiles("Assets", "*NavMesh.asset*", System.IO.SearchOption.AllDirectories);
            foreach (var f in assetFiles)
            {
                try { System.IO.File.Delete(f); } catch { }
            }

            AssetDatabase.Refresh();
            SceneView.RepaintAll();

            EditorUtility.DisplayDialog(
                "Blue Floor Overlay Removed!",
                "The cyan/blue NavMesh overlay has been completely removed from the floor.\n\nEnemies now navigate directly using CharacterController physics.",
                "OK"
            );
        }

        public static void BuildWorldHeadless()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            ExecuteWorldGeneration(false);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldBuilder] Successfully generated and saved complete game world in SampleScene.unity!");
        }

        public static void BuildWindowsPlayer()
        {
            string buildPath = "Builds/Windows/TheLostRelic.exe";
            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = scenes;
            options.locationPathName = buildPath;
            options.target = BuildTarget.StandaloneWindows64;
            options.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("[Build] Windows Standalone Build SUCCEEDED!");
            }
            else
            {
                Debug.LogError($"[Build] Windows Standalone Build FAILED: {report.summary.totalErrors} errors.");
            }
        }

        public static void ExecuteWorldGeneration(bool isInteractive)
        {
            var activeScene = EditorSceneManager.GetActiveScene();

            // 0. Ensure all materials are upgraded to URP Lit (fixes any pink/magenta assets)
            URPMaterialConverter.ConvertAllThirdPartyMaterials(true);

            // 1. Clean existing generated objects
            var existingWorld = GameObject.Find("--- THE LOST RELIC WORLD ---");
            if (existingWorld != null)
            {
                Object.DestroyImmediate(existingWorld);
            }

            // Remove/disable conflicting stray Terrains from template scenes
            var oldTerrain = GameObject.Find("Terrain");
            if (oldTerrain != null && oldTerrain.transform.parent == null)
            {
                Object.DestroyImmediate(oldTerrain);
            }

            // Untag or remove any stray player objects from third-party scenes
            var strayPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var sp in strayPlayers)
            {
                sp.tag = "Untagged";
                var pm = sp.GetComponent<PlayerMovement>();
                if (pm != null) Object.DestroyImmediate(pm);
                var cc = sp.GetComponent<CharacterController>();
                if (cc != null) Object.DestroyImmediate(cc);
            }

            GameObject root = new GameObject("--- THE LOST RELIC WORLD ---");

            // 2. Build Core Managers
            GameObject managersObj = new GameObject("Managers");
            managersObj.transform.SetParent(root.transform);
            var gm = managersObj.AddComponent<GameManager>();
            var am = managersObj.AddComponent<AudioManager>();
            var dm = managersObj.AddComponent<DialogueManager>();
            var uiMgr = managersObj.AddComponent<GameUIManager>();

            // 3. Create Shared Materials
            Material forestGroundMat = CreateColorMaterial("Mat_ForestGround", new Color(0.22f, 0.42f, 0.18f), 0.2f);
            Material canyonGroundMat = CreateColorMaterial("Mat_CanyonGround", new Color(0.68f, 0.42f, 0.22f), 0.1f);
            Material frozenGroundMat = CreateColorMaterial("Mat_FrozenGround", new Color(0.85f, 0.92f, 0.98f), 0.4f);
            Material sanctuaryGroundMat = CreateColorMaterial("Mat_SanctuaryGround", new Color(0.25f, 0.25f, 0.32f), 0.5f);
            Material coinMat = CreateColorMaterial("Mat_CoinGold", new Color(1f, 0.85f, 0.15f), 0.8f, true);
            Material relicMat = CreateColorMaterial("Mat_RelicGem", new Color(0.2f, 0.9f, 1f), 0.9f, true);

            // Spawn points (Generous spacing for 110m x 110m biomes)
            GameObject spawnPointsObj = new GameObject("SpawnPoints");
            spawnPointsObj.transform.SetParent(root.transform);

            Transform forestSpawn = CreateSpawnPoint(spawnPointsObj.transform, "Spawn_Forest", new Vector3(0f, 0f, -30f));
            Transform canyonSpawn = CreateSpawnPoint(spawnPointsObj.transform, "Spawn_Canyon", new Vector3(250f, 0f, -30f));
            Transform frozenSpawn = CreateSpawnPoint(spawnPointsObj.transform, "Spawn_Frozen", new Vector3(500f, 0f, -30f));
            Transform sanctuarySpawn = CreateSpawnPoint(spawnPointsObj.transform, "Spawn_Sanctuary", new Vector3(250f, 0f, 222f));

            // 4. Build Player
            GameObject player = BuildPlayer(root.transform, forestSpawn.position);

            // 5. Build Portals
            GameObject portalsObj = new GameObject("Portals");
            portalsObj.transform.SetParent(root.transform);

            TeleportPortal portal1 = BuildPortal(portalsObj.transform, "Portal_ForestToCanyon", new Vector3(28f, 0f, 32f), canyonSpawn, BiomeType.ScorchedCanyon, "The Scorched Canyon");
            TeleportPortal portal2 = BuildPortal(portalsObj.transform, "Portal_CanyonToFrozen", new Vector3(278f, 0f, 32f), frozenSpawn, BiomeType.FrozenRuins, "The Frozen Ruins");
            TeleportPortal portal3 = BuildPortal(portalsObj.transform, "Portal_FrozenToSanctuary", new Vector3(528f, 0f, 32f), sanctuarySpawn, BiomeType.FinalSanctuary, "The Final Sanctuary");

            // 6. Build Biome 1: The Forgotten Forest (110m x 110m, High-Detail 3D Assets)
            BuildBiome1_Forest(root.transform, forestGroundMat, coinMat, relicMat, portal1);

            // 7. Build Biome 2: The Scorched Canyon (110m x 110m, High-Detail 3D Assets)
            BuildBiome2_Canyon(root.transform, canyonGroundMat, coinMat, relicMat, portal2);

            // 8. Build Biome 3: The Frozen Ruins (110m x 110m, High-Detail 3D Assets)
            BuildBiome3_Frozen(root.transform, frozenGroundMat, coinMat, relicMat, portal3);

            // 9. Build Final Sanctuary (70m x 70m Grand Altar Courtyard)
            BuildFinalSanctuary(root.transform, sanctuaryGroundMat, relicMat);

            // 10. Build Full Canvas UI
            BuildGameUI(root.transform, uiMgr, dm);

            // 11. Setup Camera
            SetupCamera(player);

            // 12. Clear NavMesh so the floor has no blue overlay in Scene View
            try
            {
                UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            }
            catch { }

            EditorSceneManager.MarkSceneDirty(activeScene);

            if (isInteractive)
            {
                EditorUtility.DisplayDialog(
                    "World Generation Complete!",
                    "The complete 3-Biome adventure world for 'The Lost Relic' has been generated:\n\n" +
                    "✓ Biome 1: Forgotten Forest (The Explorer NPC, 10-Enemy Combat Mission, 12 Coins, Relic 1, Portal 1)\n" +
                    "✓ Biome 2: Scorched Canyon (Canyon Explorer NPC, Pressure Plate Puzzle, 12 Coins, Relic 2, Portal 2)\n" +
                    "✓ Biome 3: Frozen Ruins (Frozen Sage NPC, Crystal Alignment Puzzle, 12 Coins, Relic 3, Final Portal)\n" +
                    "✓ Final Sanctuary: Central Altar with 3 Relic Sockets & Restoration Sequence\n" +
                    "✓ Full Gameplay HUD, Dialogue UI, Pause Menu, Game Over, and Victory Screens\n" +
                    "✓ Enemies in all biomes with NavMesh AI and combat\n\n" +
                    "Press PLAY to test the complete game!",
                    "Let's Play!"
                );
            }
        }

        private static GameObject InstantiateAssetPrefab(string assetPath, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale, bool ensureCollider = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                GameObject inst = null;
                try { inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); }
                catch { inst = null; }

                if (inst == null)
                {
                    inst = Object.Instantiate(prefab, parent);
                }

                if (inst != null)
                {
                    inst.transform.localPosition = localPos;
                    inst.transform.localRotation = localRot;
                    inst.transform.localScale = localScale;
                    if (ensureCollider && inst.GetComponentInChildren<Collider>() == null)
                    {
                        inst.AddComponent<BoxCollider>();
                    }
                    return inst;
                }
            }
            return null;
        }

        private static Material CreateColorMaterial(string name, Color color, float smoothness, bool emission = false)
        {
            Shader uRPShader = Shader.Find("Universal Render Pipeline/Lit");
            if (uRPShader == null) uRPShader = Shader.Find("Standard");

            Material mat = new Material(uRPShader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emission)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", color * 1.5f);
                    mat.EnableKeyword("_EMISSION");
                }
            }

            return mat;
        }

        private static Transform CreateSpawnPoint(Transform parent, string name, Vector3 pos)
        {
            GameObject sp = new GameObject(name);
            sp.transform.SetParent(parent);
            sp.transform.position = pos;
            return sp.transform;
        }

        private static GameObject BuildPlayer(Transform parent, Vector3 spawnPos)
        {
            var oldPlayer = GameObject.FindWithTag("Player");
            if (oldPlayer != null) Undo.DestroyObjectImmediate(oldPlayer);

            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetParent(parent);
            player.transform.position = spawnPos;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.radius = 0.32f;
            cc.skinWidth = 0.02f;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0f;

            // 1. Attach Floreswa model first so hand bones are available for weapon mounting
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Male01PrefabPath);
            if (modelPrefab != null)
            {
                GameObject modelInstance = null;
                try { modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, player.transform); }
                catch { modelInstance = null; }

                if (modelInstance == null)
                {
                    modelInstance = Object.Instantiate(modelPrefab, player.transform);
                }
                modelInstance.name = "Model";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;

                // Ensure URP Lit materials are assigned cleanly with natural skin and cloth colors (no pink)
                ApplyFloreswaURPMaterials(modelInstance);
            }
            else
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Visual";
                capsule.transform.SetParent(player.transform);
                capsule.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                Object.DestroyImmediate(capsule.GetComponent<Collider>());
            }

            // 2. Add gameplay & combat components
            player.AddComponent<PlayerInputHandler>();
            player.AddComponent<PlayerStamina>();
            var pm = player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerHealth>();
            var attack = player.AddComponent<PlayerAttack>();
            player.AddComponent<PlayerInteraction>();

            // 3. Mount 3D Gun weapon onto player's right hand
            attack.SetupGunVisual();
            pm.SnapToGround();
            return player;
        }

        private static void ApplyFloreswaURPMaterials(GameObject characterObj)
        {
            if (characterObj == null) return;

            var smrs = characterObj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var mrs = characterObj.GetComponentsInChildren<MeshRenderer>(true);

            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null) urpLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLitShader == null) urpLitShader = Shader.Find("Standard");

            Material skinMat = LoadOrCreateCharMat("skin", new Color(0.75f, 0.59f, 0.52f), urpLitShader);
            Material tshirtMat = LoadOrCreateCharMat("tshirt", new Color(0.41f, 0.49f, 0.77f), urpLitShader);
            Material pantsMat = LoadOrCreateCharMat("pants", new Color(0.24f, 0.22f, 0.20f), urpLitShader);
            Material shoesMat = LoadOrCreateCharMat("shoes", new Color(0.16f, 0.16f, 0.16f), urpLitShader);
            Material hairMat = LoadOrCreateCharMat("hair", new Color(0.25f, 0.23f, 0.22f), urpLitShader);
            Material eyesMat = LoadOrCreateCharMat("eyes", new Color(0.27f, 0.27f, 0.27f), urpLitShader);
            Material eyesWhiteMat = LoadOrCreateCharMat("eyes_white", new Color(0.85f, 0.85f, 0.85f), urpLitShader);
            Material mouthMat = LoadOrCreateCharMat("mouth", new Color(0.60f, 0.45f, 0.44f), urpLitShader);
            Material beardMat = LoadOrCreateCharMat("beard", new Color(0.75f, 0.59f, 0.52f), urpLitShader);
            Material mustacheMat = LoadOrCreateCharMat("mustache", new Color(0.75f, 0.59f, 0.52f), urpLitShader);
            Material goateeMat = LoadOrCreateCharMat("goatee", new Color(0.75f, 0.59f, 0.52f), urpLitShader);

            // Exact submesh order in male01_1 FBX:
            // 0: skin, 1: tshirt, 2: pants, 3: shoes, 4: hair, 5: eyes, 6: eyes_white, 7: mouth, 8: beard, 9: mustache, 10: goatee
            Material[] fullSet = new Material[]
            {
                skinMat, tshirtMat, pantsMat, shoesMat, hairMat, eyesMat, eyesWhiteMat, mouthMat, beardMat, mustacheMat, goateeMat
            };

            foreach (var smr in smrs)
            {
                if (smr.sharedMaterials != null && smr.sharedMaterials.Length == 11)
                {
                    smr.sharedMaterials = fullSet;
                }
                else
                {
                    Material[] cur = smr.sharedMaterials ?? new Material[0];
                    for (int i = 0; i < cur.Length; i++)
                    {
                        if (cur[i] == null || cur[i].shader == null || cur[i].shader.name.Contains("InternalErrorShader") || cur[i].shader.name.Contains("Standard"))
                        {
                            cur[i] = skinMat;
                        }
                    }
                    smr.sharedMaterials = cur;
                }
            }

            foreach (var mr in mrs)
            {
                Material[] cur = mr.sharedMaterials ?? new Material[0];
                for (int i = 0; i < cur.Length; i++)
                {
                    if (cur[i] == null || cur[i].shader == null || cur[i].shader.name.Contains("InternalErrorShader") || cur[i].shader.name.Contains("Standard"))
                    {
                        cur[i] = skinMat;
                    }
                }
                mr.sharedMaterials = cur;
            }
        }

        private static Material LoadOrCreateCharMat(string name, Color fallbackColor, Shader shader)
        {
            string path = $"Assets/ThirdParty/Floreswa/Models/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"));
                mat.name = name;
                mat.SetColor("_BaseColor", fallbackColor);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (shader != null && (mat.shader == null || mat.shader.name.Contains("InternalErrorShader") || mat.shader.name.Contains("Standard")))
                {
                    mat.shader = shader;
                }
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    if (c == Color.white || (c.r > 0.9f && c.g < 0.1f && c.b > 0.9f))
                    {
                        mat.SetColor("_BaseColor", fallbackColor);
                    }
                }
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static TeleportPortal BuildPortal(Transform parent, string name, Vector3 pos, Transform destSpawn, BiomeType destBiome, string destName)
        {
            GameObject portalObj = new GameObject(name);
            portalObj.transform.SetParent(parent);
            portalObj.transform.position = pos;

            string prefabPath = BiomePortalSetup.ForestPortalPrefabPath;
            Color portalColor = new Color(0.2f, 1.0f, 0.4f);

            if (name.Contains("Canyon"))
            {
                prefabPath = BiomePortalSetup.CanyonPortalPrefabPath;
                portalColor = new Color(1.0f, 0.55f, 0.1f);
            }
            else if (name.Contains("Frozen"))
            {
                prefabPath = BiomePortalSetup.FrozenPortalPrefabPath;
                portalColor = new Color(0.25f, 0.85f, 1.0f);
            }

            AudioClip humClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BiomePortalSetup.PortalHumPath);
            AudioClip teleportClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BiomePortalSetup.PortalTeleportPath);
            AudioClip activateClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BiomePortalSetup.PortalActivatePath);

            BiomePortalSetup.ConfigurePortalObject(portalObj, prefabPath, portalColor, humClip, teleportClip, activateClip);

            var portal = portalObj.GetComponent<TeleportPortal>();
            SerializedObject so = new SerializedObject(portal);
            so.FindProperty("destinationSpawnPoint").objectReferenceValue = destSpawn;
            so.FindProperty("destinationBiome").enumValueIndex = (int)destBiome;
            so.FindProperty("destinationName").stringValue = destName;
            so.ApplyModifiedProperties();

            return portal;
        }

        private static void BuildBiome1_Forest(Transform parent, Material groundMat, Material coinMat, Material relicMat, TeleportPortal portal)
        {
            GameObject forest = new GameObject("Biome1_ForgottenForest");
            forest.transform.SetParent(parent);
            forest.transform.position = new Vector3(0f, 0f, 0f);

            // Enlarged Ground Floor (110m x 110m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Forest_Floor";
            floor.transform.SetParent(forest.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(11f, 1f, 11f);
            floor.GetComponent<Renderer>().material = groundMat;

            // Solid Base Collider
            var boxCol = floor.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, -0.5f, 0f);
            boxCol.size = new Vector3(110f, 1f, 110f);

            // Ancient Japanese Temple & Torii Gate Sanctuary (North)
            InstantiateAssetPrefab(JapaneseTemplePath, forest.transform, new Vector3(0f, 0f, 44f), Quaternion.identity, Vector3.one * 1.5f, true);
            InstantiateAssetPrefab(ToriiGatePath, forest.transform, new Vector3(0f, 0f, 22f), Quaternion.identity, Vector3.one * 1.3f, true);

            // Explorer Camp (South)
            InstantiateAssetPrefab(Cabin01Path, forest.transform, new Vector3(9f, 0f, -22f), Quaternion.Euler(0f, -30f, 0f), Vector3.one * 1.3f, true);
            InstantiateAssetPrefab(CampFire01Path, forest.transform, new Vector3(4f, 0f, -23f), Quaternion.identity, Vector3.one * 1.2f);
            InstantiateAssetPrefab(LogStand01Path, forest.transform, new Vector3(13f, 0f, -19f), Quaternion.Euler(0f, 15f, 0f), Vector3.one);
            InstantiateAssetPrefab(SignPostPath, forest.transform, new Vector3(-3f, 0f, -25f), Quaternion.Euler(0f, 25f, 0f), Vector3.one);

            // NPC: The Explorer (Forest Combat Mission & Relic Broker)
            BuildExplorerNPC(forest.transform, "The Explorer", new Vector3(5f, 0f, -20f), Male01PrefabPath, portal);

            // 45+ 3D Trees, Foliage & Rocks across the 110m landscape
            for (int i = 0; i < 28; i++)
            {
                float angle = i * (Mathf.PI * 2f / 28f);
                float r = Random.Range(22f, 50f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                string treePrefab = (i % 3 == 0) ? FirTreeLargePath : ((i % 3 == 1) ? PineTreeLargePath : FirTreeMediumPath);
                InstantiateAssetPrefab(treePrefab, forest.transform, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1f, 1.4f), true);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector3 rockPos = new Vector3(Random.Range(-48f, 48f), 0f, Random.Range(-48f, 48f));
                if (rockPos.magnitude > 12f)
                {
                    string rockPrefab = (i % 2 == 0) ? StoneLargeGrayPath : StoneSmallPath;
                    InstantiateAssetPrefab(rockPrefab, forest.transform, rockPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1.2f, 1.8f), true);
                }
            }

            for (int i = 0; i < 22; i++)
            {
                Vector3 plantPos = new Vector3(Random.Range(-45f, 45f), 0f, Random.Range(-45f, 45f));
                if (plantPos.magnitude > 8f)
                {
                    string plantPrefab = (i % 3 == 0) ? BushPlantPath : ((i % 3 == 1) ? MushroomPath : GrassPath);
                    InstantiateAssetPrefab(plantPrefab, forest.transform, plantPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1f, 1.5f));
                }
            }

            // Ancient Forest Shrine Altar (North - atmospheric shrine without relic or puzzle, relic is granted by The Explorer)
            BuildForestShrine(forest.transform, new Vector3(0f, 0f, 32f));

            // 20 Authentic 3D Collectible Coins (Radius 12m to 46m)
            BuildCollectibleCoins(forest.transform, "Forest_Coin", new Vector3(0f, 0.6f, 0f), 20, 45f, coinMat, Coin01PrefabPath);

            // 16 Hostile Forest Beasts spread across clearings in the 110m landscape
            Vector3[] beastPositions = new Vector3[]
            {
                new Vector3(-18f, 0f, 8f),
                new Vector3(18f, 0f, 14f),
                new Vector3(-26f, 0f, -10f),
                new Vector3(25f, 0f, -6f),
                new Vector3(-12f, 0f, 22f),
                new Vector3(16f, 0f, 28f),
                new Vector3(-32f, 0f, 16f),
                new Vector3(30f, 0f, 10f),
                new Vector3(-10f, 0f, -14f),
                new Vector3(-24f, 0f, 36f),
                new Vector3(22f, 0f, 38f),
                new Vector3(0f, 0f, 10f),
                new Vector3(-15f, 0f, -6f),
                new Vector3(14f, 0f, -12f),
                new Vector3(-35f, 0f, 0f),
                new Vector3(35f, 0f, -18f)
            };

            for (int b = 0; b < beastPositions.Length; b++)
            {
                BuildEnemy(forest.transform, $"Forest_Guardian_Beast_{b + 1}", beastPositions[b]);
            }
        }

        private static void BuildBiome2_Canyon(Transform parent, Material groundMat, Material coinMat, Material relicMat, TeleportPortal portal)
        {
            GameObject canyon = new GameObject("Biome2_ScorchedCanyon");
            canyon.transform.SetParent(parent);
            canyon.transform.position = new Vector3(250f, 0f, 0f);

            // Enlarged Ground Floor (110m x 110m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Canyon_Floor";
            floor.transform.SetParent(canyon.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(11f, 1f, 11f);
            floor.GetComponent<Renderer>().material = groundMat;

            var boxCol = floor.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, -0.5f, 0f);
            boxCol.size = new Vector3(110f, 1f, 110f);

            // Monumental Ancient Chamber with Twin Sphinx Guardians (North)
            InstantiateAssetPrefab(SphinxPath, canyon.transform, new Vector3(-12f, 0f, 36f), Quaternion.Euler(0f, 45f, 0f), Vector3.one * 1.6f, true);
            InstantiateAssetPrefab(SphinxPath, canyon.transform, new Vector3(12f, 0f, 36f), Quaternion.Euler(0f, -45f, 0f), Vector3.one * 1.6f, true);

            // Canyon Explorer Camp (South)
            InstantiateAssetPrefab(CampFire01Path, canyon.transform, new Vector3(3f, 0f, -23f), Quaternion.identity, Vector3.one * 1.2f);
            InstantiateAssetPrefab(BarrelPath, canyon.transform, new Vector3(9f, 0f, -20f), Quaternion.identity, Vector3.one * 1.3f, true);
            InstantiateAssetPrefab(BoxPath, canyon.transform, new Vector3(10f, 0f, -22f), Quaternion.Euler(0f, 25f, 0f), Vector3.one * 1.3f, true);
            InstantiateAssetPrefab(WoodenBridgePath, canyon.transform, new Vector3(0f, 0.2f, -8f), Quaternion.identity, Vector3.one * 1.6f, true);

            // NPC: The Canyon Explorer
            BuildNPC(canyon.transform, "The Canyon Explorer", new Vector3(5f, 0f, -20f), Male02PrefabPath,
                new string[]
                {
                    "Phew! It's scorching out here. You made it past the forest!",
                    "The second relic fragment is sealed inside the ancient sandstone chamber ahead.",
                    "Step upon all three stone pressure plates before the entrance to unlock the chamber door!",
                    "Once all plates are pressed, the chamber opens to grant you the Canyon Relic."
                },
                new string[]
                {
                    "Step on all three pressure plates (Left, Center, and Right) before the entrance.",
                    "Once all three are pressed down, the stone door slides open to reveal the Canyon Relic!"
                }
            );

            // Perimeter Canyon Cliffs (Modular BrokenVector Cliff Tiles)
            for (int i = 0; i < 16; i++)
            {
                float x = (i % 2 == 0) ? -48f : 48f;
                float z = (i / 2) * 12f - 42f;
                string cliffPrefab = (i % 2 == 0) ? CliffStraight1Path : CliffStraight2Path;
                InstantiateAssetPrefab(cliffPrefab, canyon.transform, new Vector3(x, 0f, z), Quaternion.Euler(0f, (i % 2 == 0) ? 90f : -90f, 0f), Vector3.one * 2f, true);
            }

            // 25+ Desert Cacti
            for (int i = 0; i < 22; i++)
            {
                Vector3 cactusPos = new Vector3(Random.Range(-44f, 44f), 0f, Random.Range(-44f, 44f));
                if (cactusPos.magnitude > 9f)
                {
                    string cactusPrefab = (i % 3 == 0) ? Cactus1Path : ((i % 3 == 1) ? Cactus2Path : Cactus3Path);
                    InstantiateAssetPrefab(cactusPrefab, canyon.transform, cactusPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1.2f, 1.8f), true);
                }
            }

            // Palm Tree Oasis Grove
            for (int i = 0; i < 10; i++)
            {
                float angle = i * (Mathf.PI * 2f / 10f);
                Vector3 palmPos = new Vector3(-18f + Mathf.Cos(angle) * 14f, 0f, -6f + Mathf.Sin(angle) * 14f);
                string palmPrefab = (i % 2 == 0) ? PalmTreeLargePath : PalmTreeBentPath;
                InstantiateAssetPrefab(palmPrefab, canyon.transform, palmPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1.3f, 1.7f), true);
            }

            // Desert Stones & Boulders
            for (int i = 0; i < 20; i++)
            {
                Vector3 stonePos = new Vector3(Random.Range(-46f, 46f), 0f, Random.Range(-46f, 46f));
                if (stonePos.magnitude > 10f)
                {
                    string stonePrefab = (i % 2 == 0) ? DesertStoneSmallPath : DesertStoneSmall2Path;
                    InstantiateAssetPrefab(stonePrefab, canyon.transform, stonePos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1.5f, 2.2f), true);
                }
            }

            // Puzzle 2: Pressure Plate Puzzle (North)
            BuildPressurePlatePuzzle(canyon.transform, new Vector3(0f, 0f, 32f), groundMat, relicMat, portal);

            // 18 Authentic 3D Gold Coins
            BuildCollectibleCoins(canyon.transform, "Canyon_Coin", new Vector3(0f, 0.6f, 0f), 18, 45f, coinMat, Coin01PrefabPath);

            // 8 Hostile Canyon Stalkers (Red Monster)
            Vector3[] canyonStalkerPositions = new Vector3[]
            {
                new Vector3(-18f, 0f, 8f),
                new Vector3(18f, 0f, 14f),
                new Vector3(-24f, 0f, 22f),
                new Vector3(22f, 0f, 26f),
                new Vector3(-15f, 0f, -10f),
                new Vector3(16f, 0f, -8f),
                new Vector3(-28f, 0f, 0f),
                new Vector3(28f, 0f, 4f)
            };
            for (int i = 0; i < canyonStalkerPositions.Length; i++)
            {
                BuildEnemy(canyon.transform, $"Canyon_Stalker_{i + 1}", canyonStalkerPositions[i], BiomeType.ScorchedCanyon);
            }
        }

        private static void BuildBiome3_Frozen(Transform parent, Material groundMat, Material coinMat, Material relicMat, TeleportPortal portal)
        {
            GameObject frozen = new GameObject("Biome3_FrozenRuins");
            frozen.transform.SetParent(parent);
            frozen.transform.position = new Vector3(500f, 0f, 0f);

            // Enlarged Ground Floor (110m x 110m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Frozen_Floor";
            floor.transform.SetParent(frozen.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(11f, 1f, 11f);
            floor.GetComponent<Renderer>().material = groundMat;

            var boxCol = floor.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, -0.5f, 0f);
            boxCol.size = new Vector3(110f, 1f, 110f);

            // Monumental Skull Mountain Glacier Backdrop (North)
            InstantiateAssetPrefab(SkullMountainPath, frozen.transform, new Vector3(0f, 0f, 50f), Quaternion.identity, Vector3.one * 2f, true);

            // Frozen Sage Camp (South)
            InstantiateAssetPrefab(Cabin02Path, frozen.transform, new Vector3(9f, 0f, -22f), Quaternion.Euler(0f, -25f, 0f), Vector3.one * 1.3f, true);
            InstantiateAssetPrefab(CampFire01Path, frozen.transform, new Vector3(4f, 0f, -23f), Quaternion.identity, Vector3.one * 1.2f);
            InstantiateAssetPrefab(JapaneseLanternPath, frozen.transform, new Vector3(1f, 0f, -25f), Quaternion.identity, Vector3.one * 1.4f);
            InstantiateAssetPrefab(JapaneseLanternPath, frozen.transform, new Vector3(7f, 0f, -18f), Quaternion.identity, Vector3.one * 1.4f);
            InstantiateAssetPrefab(ChestPath, frozen.transform, new Vector3(12f, 0f, -20f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 1.2f, true);

            // NPC: The Frozen Sage
            BuildNPC(frozen.transform, "The Frozen Sage", new Vector3(5f, 0f, -20f), Male03PrefabPath,
                new string[]
                {
                    "Traveler... your perilous journey nears its culmination.",
                    "The third fragment lies trapped beneath eternal glacial ice at the central altar.",
                    "Focus all three celestial crystals toward the central altar to channel their energy!",
                    "Press [E] on each crystal to focus its beam directly onto the altar and shatter the ice barrier."
                },
                new string[]
                {
                    "Focus all three celestial crystals to direct their beams into the central altar.",
                    "Once all three converge, the ice barrier will shatter and release the Frozen Relic."
                }
            );

            // 35+ Winter Fir Trees
            for (int i = 0; i < 26; i++)
            {
                float angle = i * (Mathf.PI * 2f / 26f);
                float r = Random.Range(20f, 48f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                string winterTreePrefab = (i % 3 == 0) ? FirTreeWinterLargePath : ((i % 3 == 1) ? FirTreeWinterMediumPath : FirTreeWinterSmallPath);
                InstantiateAssetPrefab(winterTreePrefab, frozen.transform, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1f, 1.4f), true);
            }

            // Snow-covered Stones & Ice Formations
            for (int i = 0; i < 20; i++)
            {
                Vector3 stonePos = new Vector3(Random.Range(-46f, 46f), 0f, Random.Range(-46f, 46f));
                if (stonePos.magnitude > 10f)
                {
                    string stonePrefab = (i % 3 == 0) ? StonesGroupWinterPath : ((i % 3 == 1) ? StoneWinterSmallPath : StoneWinterSmall2Path);
                    InstantiateAssetPrefab(stonePrefab, frozen.transform, stonePos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1.3f, 2f), true);
                }
            }

            // Puzzle 3: Crystal Alignment Puzzle (North)
            BuildCrystalPuzzle(frozen.transform, new Vector3(0f, 0f, 32f), relicMat, portal);

            // 18 Authentic 3D Gold Coins
            BuildCollectibleCoins(frozen.transform, "Frozen_Coin", new Vector3(0f, 0.6f, 0f), 18, 45f, coinMat, Coin01PrefabPath);

            // 6 Hostile Frost Guardians (Default Monster Model - Monster41)
            Vector3[] frostGuardianPositions = new Vector3[]
            {
                new Vector3(-18f, 0f, 8f),
                new Vector3(18f, 0f, 14f),
                new Vector3(-22f, 0f, 24f),
                new Vector3(20f, 0f, 26f),
                new Vector3(-14f, 0f, -8f),
                new Vector3(15f, 0f, -12f)
            };
            for (int i = 0; i < frostGuardianPositions.Length; i++)
            {
                BuildEnemy(frozen.transform, $"Frost_Guardian_{i + 1}", frostGuardianPositions[i], BiomeType.FrozenRuins);
            }
        }

        private static void BuildFinalSanctuary(Transform parent, Material groundMat, Material relicMat)
        {
            GameObject sanctuary = new GameObject("FinalSanctuary");
            sanctuary.transform.SetParent(parent);
            sanctuary.transform.position = new Vector3(250f, 0f, 250f);

            // Grand Elevated Platform (70m x 70m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Sanctuary_Floor";
            floor.transform.SetParent(sanctuary.transform);
            floor.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            floor.transform.localScale = new Vector3(7f, 1f, 7f);
            floor.GetComponent<Renderer>().material = groundMat;

            var boxCol = floor.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, -0.5f, 0f);
            boxCol.size = new Vector3(70f, 1f, 70f);

            // Grand Entrance Stairs (South of Platform)
            InstantiateAssetPrefab(StairsPath, sanctuary.transform, new Vector3(0f, 0f, -32f), Quaternion.identity, new Vector3(2.5f, 1.2f, 2f), true);

            // Perimeter Fencing along Entrance
            InstantiateAssetPrefab(FencePath, sanctuary.transform, new Vector3(-8f, 0.5f, -32f), Quaternion.identity, Vector3.one * 1.4f, true);
            InstantiateAssetPrefab(FencePath, sanctuary.transform, new Vector3(8f, 0.5f, -32f), Quaternion.identity, Vector3.one * 1.4f, true);

            // 4 Ceremonial Torch Braziers with Real Fire FX around the central altar
            Vector3[] torchPositions = new Vector3[]
            {
                new Vector3(-6f, 0.5f, -6f),
                new Vector3(6f, 0.5f, -6f),
                new Vector3(-6f, 0.5f, 6f),
                new Vector3(6f, 0.5f, 6f)
            };

            for (int i = 0; i < 4; i++)
            {
                InstantiateAssetPrefab(StonePillarPath, sanctuary.transform, torchPositions[i], Quaternion.identity, Vector3.one * 1.4f, true);
                InstantiateAssetPrefab(FXFirePath, sanctuary.transform, torchPositions[i] + Vector3.up * 2f, Quaternion.identity, Vector3.one * 1.5f);
            }

            // Central Altar
            GameObject altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            altar.name = "Final_Relic_Altar";
            altar.transform.SetParent(sanctuary.transform);
            altar.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            altar.transform.localScale = new Vector3(5f, 0.6f, 5f);

            // Sockets — Use gem prefabs for each biome socket
            GameObject s1 = InstantiateAssetPrefab(ForestGemPrefabPath, altar.transform,
                new Vector3(-0.35f, 1.2f, 0f), Quaternion.identity, Vector3.one * 0.35f);
            if (s1 == null)
            {
                s1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s1.transform.SetParent(altar.transform);
                s1.transform.localPosition = new Vector3(-0.35f, 1.2f, 0f);
                s1.transform.localScale = Vector3.one * 0.35f;
                s1.GetComponent<Renderer>().material = relicMat;
            }
            s1.name = "Socket_Forest";

            GameObject s2 = InstantiateAssetPrefab(CanyonGemPrefabPath, altar.transform,
                new Vector3(0.35f, 1.2f, 0f), Quaternion.identity, Vector3.one * 0.35f);
            if (s2 == null)
            {
                s2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s2.transform.SetParent(altar.transform);
                s2.transform.localPosition = new Vector3(0.35f, 1.2f, 0f);
                s2.transform.localScale = Vector3.one * 0.35f;
                s2.GetComponent<Renderer>().material = relicMat;
            }
            s2.name = "Socket_Canyon";

            GameObject s3 = InstantiateAssetPrefab(FrozenGemPrefabPath, altar.transform,
                new Vector3(0f, 1.2f, 0.35f), Quaternion.identity, Vector3.one * 0.35f);
            if (s3 == null)
            {
                s3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s3.transform.SetParent(altar.transform);
                s3.transform.localPosition = new Vector3(0f, 1.2f, 0.35f);
                s3.transform.localScale = Vector3.one * 0.35f;
                s3.GetComponent<Renderer>().material = relicMat;
            }
            s3.name = "Socket_Frozen";

            // Restored Lost Relic — Use HeartGem as the complete restored relic
            GameObject restoredRelic = InstantiateAssetPrefab(ForestGemPrefabPath, altar.transform,
                new Vector3(0f, 2.5f, 0f), Quaternion.identity, Vector3.one * 0.9f);
            if (restoredRelic == null)
            {
                restoredRelic = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                restoredRelic.transform.SetParent(altar.transform);
                restoredRelic.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                restoredRelic.transform.localScale = Vector3.one * 0.9f;
                restoredRelic.GetComponent<Renderer>().material = relicMat;
            }
            restoredRelic.name = "Complete_Lost_Relic";

            // Divine Light
            GameObject lightObj = new GameObject("DivineLight");
            lightObj.transform.SetParent(altar.transform);
            lightObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            var light = lightObj.AddComponent<Light>();
            light.color = new Color(1f, 0.9f, 0.5f);
            light.range = 16f;
            light.intensity = 5f;

            var finalAltarComp = altar.AddComponent<FinalRelicAltar>();
            SerializedObject so = new SerializedObject(finalAltarComp);
            so.FindProperty("forestRelicSocket").objectReferenceValue = s1;
            so.FindProperty("canyonRelicSocket").objectReferenceValue = s2;
            so.FindProperty("frozenRelicSocket").objectReferenceValue = s3;
            so.FindProperty("completeLostRelic").objectReferenceValue = restoredRelic;
            so.FindProperty("floatingCenterPoint").objectReferenceValue = restoredRelic.transform;
            so.FindProperty("altarDivineLight").objectReferenceValue = light;
            so.ApplyModifiedProperties();
        }

        private static void BuildNPC(Transform parent, string name, Vector3 pos, string prefabPath, string[] intro, string[] repeat)
        {
            GameObject npc = new GameObject(name);
            npc.tag = "NPC";
            npc.transform.SetParent(parent);
            npc.transform.localPosition = pos;
            npc.transform.localRotation = Quaternion.Euler(0f, -140f, 0f);

            var col = npc.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.4f;

            var dialogue = npc.AddComponent<NPCDialogue>();

            SerializedObject so = new SerializedObject(dialogue);
            so.FindProperty("npcName").stringValue = name;

            SerializedProperty introProp = so.FindProperty("initialDialogue");
            introProp.ClearArray();
            for (int i = 0; i < intro.Length; i++)
            {
                introProp.InsertArrayElementAtIndex(i);
                introProp.GetArrayElementAtIndex(i).stringValue = intro[i];
            }

            SerializedProperty repeatProp = so.FindProperty("repeatDialogue");
            repeatProp.ClearArray();
            for (int i = 0; i < repeat.Length; i++)
            {
                repeatProp.InsertArrayElementAtIndex(i);
                repeatProp.GetArrayElementAtIndex(i).stringValue = repeat[i];
            }
            so.ApplyModifiedProperties();

            // Visual Model
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (modelPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, npc.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                ApplyFloreswaURPMaterials(model);
            }
            else
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.SetParent(npc.transform);
                capsule.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                Object.DestroyImmediate(capsule.GetComponent<Collider>());
            }
        }

        private static void BuildExplorerNPC(Transform parent, string name, Vector3 pos, string prefabPath, TeleportPortal portal)
        {
            GameObject npc = new GameObject(name);
            npc.tag = "NPC";
            npc.transform.SetParent(parent);
            npc.transform.localPosition = pos;
            npc.transform.localRotation = Quaternion.Euler(0f, -140f, 0f);

            var col = npc.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.4f;

            var missionNPC = npc.AddComponent<ExplorerMissionNPC>();
            missionNPC.SetLinkedPortal(portal);

            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (modelPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, npc.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                ApplyFloreswaURPMaterials(model);
            }
            else
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.SetParent(npc.transform);
                capsule.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                Object.DestroyImmediate(capsule.GetComponent<Collider>());
            }
        }

        private static void BuildForestShrine(Transform parent, Vector3 shrinePos)
        {
            GameObject shrineObj = new GameObject("Ancient_Forest_Shrine");
            shrineObj.transform.SetParent(parent);
            shrineObj.transform.localPosition = shrinePos;

            // Shrine Pedestal
            GameObject shrine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shrine.name = "Shrine_Altar";
            shrine.transform.SetParent(shrineObj.transform);
            shrine.transform.localPosition = Vector3.zero;
            shrine.transform.localScale = new Vector3(3f, 0.6f, 3f);

            // Torii ambient shrine glow
            var lightObj = new GameObject("Shrine_Lantern_Light");
            lightObj.transform.SetParent(shrineObj.transform);
            lightObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var sl = lightObj.AddComponent<Light>();
            sl.type = LightType.Point;
            sl.range = 8f;
            sl.intensity = 2.5f;
            sl.color = new Color(0.3f, 1f, 0.5f);
        }

        private static void BuildPressurePlatePuzzle(Transform parent, Vector3 centerPos, Material doorMat, Material relicMat, TeleportPortal portal)
        {
            GameObject puzzleObj = new GameObject("Puzzle_PressurePlates");
            puzzleObj.transform.SetParent(parent);
            puzzleObj.transform.localPosition = centerPos;

            // Sandstone Door
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Sandstone_Door";
            door.transform.SetParent(puzzleObj.transform);
            door.transform.localPosition = new Vector3(0f, 2f, 6f);
            door.transform.localScale = new Vector3(5f, 4f, 0.8f);
            door.GetComponent<Renderer>().material = doorMat;

            // Relic Fragment inside chamber — Canyon Gem Prefab
            GameObject relicObj = InstantiateAssetPrefab(CanyonGemPrefabPath, puzzleObj.transform,
                new Vector3(0f, 1.8f, 10f), Quaternion.identity, Vector3.one * 0.6f);
            if (relicObj == null)
            {
                relicObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                relicObj.transform.SetParent(puzzleObj.transform);
                relicObj.transform.localPosition = new Vector3(0f, 1.8f, 10f);
                relicObj.transform.localScale = Vector3.one * 0.6f;
                relicObj.GetComponent<Renderer>().material = relicMat;
            }
            relicObj.name = "Canyon_Relic_Fragment";
            if (relicObj.GetComponent<Collider>() == null)
            {
                var sc = relicObj.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.5f;
            }

            var relicComp = relicObj.AddComponent<RelicFragment>();
            SerializedObject rso = new SerializedObject(relicComp);
            rso.FindProperty("biome").enumValueIndex = (int)BiomeType.ScorchedCanyon;
            rso.FindProperty("relicName").stringValue = "Canyon Relic Fragment";
            rso.FindProperty("linkedPortal").objectReferenceValue = portal;
            rso.ApplyModifiedProperties();

            // 3 Pressure Plates (Left = 0, Center = 1, Right = 2)
            string[] plateNames = new string[] { "Left Plate (1st)", "Center Plate (3rd)", "Right Plate (2nd)" };
            Vector3[] plateOffsets = new Vector3[] { new Vector3(-3.5f, 0.05f, 0f), new Vector3(0f, 0.05f, 0f), new Vector3(3.5f, 0.05f, 0f) };

            var puzzleComp = puzzleObj.AddComponent<PressurePlatePuzzle>();
            SerializedObject pso = new SerializedObject(puzzleComp);
            pso.FindProperty("stoneDoor").objectReferenceValue = door;
            pso.FindProperty("relicFragmentObject").objectReferenceValue = relicObj;

            SerializedProperty platesList = pso.FindProperty("plates");
            platesList.ClearArray();

            for (int i = 0; i < 3; i++)
            {
                GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plate.name = $"Plate_{plateNames[i]}";
                plate.transform.SetParent(puzzleObj.transform);
                plate.transform.localPosition = plateOffsets[i];
                plate.transform.localScale = new Vector3(1.8f, 0.1f, 1.8f);
                var col = plate.GetComponent<Collider>();
                col.isTrigger = true;

                platesList.InsertArrayElementAtIndex(i);
                var element = platesList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("plateName").stringValue = plateNames[i];
                element.FindPropertyRelative("plateObject").objectReferenceValue = plate;
            }

            pso.ApplyModifiedProperties();
        }

        private static void BuildCrystalPuzzle(Transform parent, Vector3 centerPos, Material relicMat, TeleportPortal portal)
        {
            GameObject puzzleObj = new GameObject("Puzzle_CrystalAlignment");
            puzzleObj.transform.SetParent(parent);
            puzzleObj.transform.localPosition = centerPos;

            // Central Altar Receiver
            GameObject centralAltar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centralAltar.name = "Central_Crystal_Receiver";
            centralAltar.transform.SetParent(puzzleObj.transform);
            centralAltar.transform.localPosition = Vector3.zero;
            centralAltar.transform.localScale = new Vector3(2.5f, 0.8f, 2.5f);

            // Ice Barrier
            GameObject iceBarrier = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            iceBarrier.name = "Ice_Barrier";
            iceBarrier.transform.SetParent(puzzleObj.transform);
            iceBarrier.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            iceBarrier.transform.localScale = Vector3.one * 1.5f;

            // Frozen Relic — Frozen Gem Prefab
            GameObject relicObj = InstantiateAssetPrefab(FrozenGemPrefabPath, puzzleObj.transform,
                new Vector3(0f, 1.8f, 0f), Quaternion.identity, Vector3.one * 0.6f);
            if (relicObj == null)
            {
                relicObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                relicObj.transform.SetParent(puzzleObj.transform);
                relicObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);
                relicObj.transform.localScale = Vector3.one * 0.6f;
                relicObj.GetComponent<Renderer>().material = relicMat;
            }
            relicObj.name = "Frozen_Relic_Fragment";
            if (relicObj.GetComponent<Collider>() == null)
            {
                var sc = relicObj.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.5f;
            }

            var relicComp = relicObj.AddComponent<RelicFragment>();
            SerializedObject rso = new SerializedObject(relicComp);
            rso.FindProperty("biome").enumValueIndex = (int)BiomeType.FrozenRuins;
            rso.FindProperty("relicName").stringValue = "Frozen Relic Fragment";
            rso.FindProperty("linkedPortal").objectReferenceValue = portal;
            rso.ApplyModifiedProperties();

            // 3 Rotatable Crystals
            string[] crystalNames = new string[] { "North Crystal", "Southeast Crystal", "Southwest Crystal" };
            Vector3[] crystalOffsets = new Vector3[] { new Vector3(0f, 0f, 6f), new Vector3(5f, 0f, -4f), new Vector3(-5f, 0f, -4f) };
            int[] targetSteps = new int[] { 2, 0, 1 }; // Step required to aim at center

            var puzzleComp = puzzleObj.AddComponent<CrystalAlignmentPuzzle>();
            SerializedObject pso = new SerializedObject(puzzleComp);
            pso.FindProperty("centralAltar").objectReferenceValue = centralAltar.transform;
            pso.FindProperty("iceBarrier").objectReferenceValue = iceBarrier;
            pso.FindProperty("relicFragmentObject").objectReferenceValue = relicObj;

            SerializedProperty crystalsList = pso.FindProperty("crystals");
            crystalsList.ClearArray();

            for (int i = 0; i < 3; i++)
            {
                GameObject crysObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crysObj.name = $"Crystal_{crystalNames[i]}";
                crysObj.transform.SetParent(puzzleObj.transform);
                crysObj.transform.localPosition = crystalOffsets[i] + Vector3.up * 1f;
                crysObj.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f);

                var line = crysObj.AddComponent<LineRenderer>();
                line.startWidth = 0.1f;
                line.endWidth = 0.1f;
                line.positionCount = 2;
                line.enabled = false;

                crystalsList.InsertArrayElementAtIndex(i);
                var element = crystalsList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("crystalName").stringValue = crystalNames[i];
                element.FindPropertyRelative("crystalObject").objectReferenceValue = crysObj;
                element.FindPropertyRelative("beamRenderer").objectReferenceValue = line;
                element.FindPropertyRelative("targetStep").intValue = targetSteps[i];
            }

            pso.ApplyModifiedProperties();
        }

        private static void BuildCollectibleCoins(Transform parent, string prefix, Vector3 origin, int count, float radius, Material coinMat, string coinPrefabPath = null)
        {
            if (parent == null) return;
            GameObject container = new GameObject($"{prefix}s_Container");
            container.transform.SetParent(parent);
            container.transform.localPosition = Vector3.zero;

            var coinPrefab = !string.IsNullOrEmpty(coinPrefabPath) ? AssetDatabase.LoadAssetAtPath<GameObject>(coinPrefabPath) : null;

            for (int i = 0; i < count; i++)
            {
                float angle = i * (Mathf.PI * 2f / count);
                float r = Random.Range(10f, radius);
                Vector3 coinPos = origin + new Vector3(Mathf.Cos(angle) * r, 0.75f, Mathf.Sin(angle) * r);

                GameObject coin = null;
                if (coinPrefab != null)
                {
                    try
                    {
                        coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, container.transform);
                    }
                    catch
                    {
                        coin = null;
                    }

                    if (coin == null)
                    {
                        coin = Object.Instantiate(coinPrefab, container.transform);
                    }
                }

                if (coin == null)
                {
                    coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    coin.transform.SetParent(container.transform);
                    coin.transform.localPosition = coinPos;
                    coin.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
                    coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    if (coinMat != null) coin.GetComponent<Renderer>().material = coinMat;
                }
                else
                {
                    coin.transform.localPosition = coinPos;
                    coin.transform.localScale = Vector3.one * 1.6f;
                }

                coin.name = $"{prefix}_{i + 1}";

                var col = coin.GetComponent<Collider>();
                if (col == null) col = coin.AddComponent<SphereCollider>();
                col.isTrigger = true;

                var comp = coin.GetComponent<CollectibleItem>();
                if (comp == null) comp = coin.AddComponent<CollectibleItem>();

                try
                {
                    SerializedObject so = new SerializedObject(comp);
                    var rotProp = so.FindProperty("rotationSpeed");
                    if (rotProp != null) rotProp.floatValue = 110f;
                    var bobProp = so.FindProperty("bobAmplitude");
                    if (bobProp != null) bobProp.floatValue = 0.15f;
                    so.ApplyModifiedProperties();
                }
                catch { }
            }
        }

        private static void BuildEnemy(Transform parent, string name, Vector3 pos, BiomeType biome = BiomeType.ForgottenForest)
        {
            GameObject enemy = new GameObject(name);
            enemy.transform.SetParent(parent);
            enemy.transform.localPosition = pos;

            var cc = enemy.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.85f, 0f);
            cc.height = 1.7f;
            cc.radius = 0.5f;

            var cap = enemy.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.85f, 0f);
            cap.height = 1.7f;
            cap.radius = 0.55f;

            var health = enemy.AddComponent<EnemyHealth>();
            health.EnemyBiome = biome;
            health.CountTowardsForestMission = (biome == BiomeType.ForgottenForest);

            var ai = enemy.AddComponent<EnemyAI>();

            // Determine Biome Monster Prefab
            string prefabPath = BiomeMonsterSetup.GreenMonsterPrefabPath; // Green for Forgotten Forest
            if (biome == BiomeType.ScorchedCanyon)
            {
                prefabPath = BiomeMonsterSetup.RedMonsterPrefabPath; // Red for Scorched Canyon
            }
            else if (biome == BiomeType.FrozenRuins)
            {
                prefabPath = BiomeMonsterSetup.DefaultMonsterPrefabPath; // Default model Monster41 for Frozen Ruins
            }

            var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (monsterPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefab, enemy.transform);
                visual.name = "MonsterVisual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;

                var anim = visual.GetComponentInChildren<Animator>(true);
                if (anim == null)
                {
                    if (visual.transform.childCount > 0)
                        anim = visual.transform.GetChild(0).gameObject.AddComponent<Animator>();
                    else
                        anim = visual.AddComponent<Animator>();
                }

                var monsterController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BiomeMonsterSetup.MonsterControllerPath);
                if (monsterController != null)
                {
                    anim.runtimeAnimatorController = monsterController;
                }

                SerializedObject soAI = new SerializedObject(ai);
                soAI.FindProperty("animator").objectReferenceValue = anim;
                soAI.FindProperty("animSpeedParam").stringValue = "Speed";
                soAI.FindProperty("animAttackTrigger").stringValue = "Attack";
                var dieProp = soAI.FindProperty("animDieTrigger");
                if (dieProp != null) dieProp.stringValue = "Die";
                soAI.ApplyModifiedProperties();
            }

            // Configure EnemyHealth coin drops
            SerializedObject soH = new SerializedObject(health);
            var coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BiomeMonsterSetup.Coin01PrefabPath);
            if (coinPrefab != null)
            {
                var coinDropProp = soH.FindProperty("coinDropPrefab");
                if (coinDropProp != null) coinDropProp.objectReferenceValue = coinPrefab;
            }
            var dropCoinsProp = soH.FindProperty("dropCoinsOnDeath");
            if (dropCoinsProp != null) dropCoinsProp.boolValue = true;
            var minDropProp = soH.FindProperty("minCoinsToDrop");
            if (minDropProp != null) minDropProp.intValue = 1;
            var maxDropProp = soH.FindProperty("maxCoinsToDrop");
            if (maxDropProp != null) maxDropProp.intValue = 3;
            soH.ApplyModifiedProperties();
        }

        private static void BuildGameUI(Transform parent, GameUIManager uiMgr, DialogueManager dm)
        {
            // Canvas
            GameObject canvasObj = new GameObject("GameCanvas");
            canvasObj.transform.SetParent(parent);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem with InputSystemUIInputModule (Crucial for button clicks in New Input System)
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.transform.SetParent(parent);
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // HUD Root
            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(canvasObj.transform, false);

            // Health Slider & Text (Top-Left)
            GameObject healthObj = new GameObject("HealthSlider");
            healthObj.transform.SetParent(hud.transform, false);
            var rectH = healthObj.AddComponent<RectTransform>();
            rectH.anchorMin = new Vector2(0f, 1f);
            rectH.anchorMax = new Vector2(0f, 1f);
            rectH.pivot = new Vector2(0f, 1f);
            rectH.anchoredPosition = new Vector2(30f, -30f);
            rectH.sizeDelta = new Vector2(240f, 24f);

            var slider = healthObj.AddComponent<Slider>();

            GameObject hpTextObj = CreateTextObject(hud.transform, "HP: 100 / 100", defaultFont, 16, Color.white,
                new Vector2(30f, -60f), new Vector2(240f, 24f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

            // Stamina Slider & Text (Directly Below Health, Top-Left - Elden Ring Style)
            GameObject staminaObj = new GameObject("StaminaSlider");
            staminaObj.transform.SetParent(hud.transform, false);
            var rectS = staminaObj.AddComponent<RectTransform>();
            rectS.anchorMin = new Vector2(0f, 1f);
            rectS.anchorMax = new Vector2(0f, 1f);
            rectS.pivot = new Vector2(0f, 1f);
            rectS.anchoredPosition = new Vector2(30f, -88f);
            rectS.sizeDelta = new Vector2(240f, 18f);

            var staminaSlider = staminaObj.AddComponent<Slider>();

            GameObject spTextObj = CreateTextObject(hud.transform, "SP: 100 / 100", defaultFont, 14, new Color(0.4f, 0.95f, 0.4f),
                new Vector2(30f, -110f), new Vector2(240f, 20f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

            // Biome & Objective (Top-Center)
            GameObject biomeTextObj = CreateTextObject(hud.transform, "The Forgotten Forest", defaultFont, 24, new Color(0.95f, 0.85f, 0.3f),
                new Vector2(0f, -25f), new Vector2(500f, 36f), TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            GameObject objTextObj = CreateTextObject(hud.transform, "Objective: Explore the forest and find the Forest Guardian", defaultFont, 16, Color.white,
                new Vector2(0f, -65f), new Vector2(800f, 28f), TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            // Coins & Relics (Top-Right)
            GameObject coinTextObj = CreateTextObject(hud.transform, "Coins: 0", defaultFont, 18, Color.yellow,
                new Vector2(-30f, -25f), new Vector2(220f, 28f), TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

            GameObject relicTextObj = CreateTextObject(hud.transform, "Relic Fragments: 0 / 3", defaultFont, 18, Color.cyan,
                new Vector2(-30f, -55f), new Vector2(220f, 28f), TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

            // Combat Aiming Crosshair (Center of Screen)
            GameObject crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(hud.transform, false);
            var rectCH = crosshairObj.AddComponent<RectTransform>();
            rectCH.anchorMin = new Vector2(0.5f, 0.5f);
            rectCH.anchorMax = new Vector2(0.5f, 0.5f);
            rectCH.pivot = new Vector2(0.5f, 0.5f);
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

            var crosshairComp = crosshairObj.AddComponent<RelicTrail.UI.CrosshairUI>();
            crosshairComp.SetupComponents(dotImg, new Image[] { imgTop, imgBot, imgLeft, imgRight }, new RectTransform[] { rTop, rBot, rLeft, rRight });

            // Interaction Prompt (Bottom-Center)
            GameObject promptPanel = new GameObject("PromptPanel");
            promptPanel.transform.SetParent(hud.transform, false);
            var rectP = promptPanel.AddComponent<RectTransform>();
            rectP.anchorMin = new Vector2(0.5f, 0.22f);
            rectP.anchorMax = new Vector2(0.5f, 0.22f);
            rectP.pivot = new Vector2(0.5f, 0.5f);
            rectP.sizeDelta = new Vector2(500f, 40f);
            GameObject promptTextObj = CreateTextObject(promptPanel.transform, "Press [E] to Interact", defaultFont, 18, Color.white, Vector2.zero, new Vector2(500f, 40f), TextAnchor.MiddleCenter);

            // Dialogue Panel
            GameObject dialoguePanel = new GameObject("DialoguePanel");
            dialoguePanel.transform.SetParent(canvasObj.transform, false);
            var rectD = dialoguePanel.AddComponent<RectTransform>();
            rectD.anchorMin = new Vector2(0.5f, 0.15f);
            rectD.anchorMax = new Vector2(0.5f, 0.15f);
            rectD.sizeDelta = new Vector2(700f, 150f);
            var imgD = dialoguePanel.AddComponent<Image>();
            imgD.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            GameObject npcNameObj = CreateTextObject(dialoguePanel.transform, "NPC Name", defaultFont, 20, Color.yellow, new Vector2(20f, -15f), new Vector2(660f, 30f), TextAnchor.UpperLeft);
            GameObject diagTextObj = CreateTextObject(dialoguePanel.transform, "Dialogue text goes here...", defaultFont, 16, Color.white, new Vector2(20f, -50f), new Vector2(660f, 85f), TextAnchor.UpperLeft);

            // Main Menu Panel
            GameObject mainMenuPanel = CreateMenuPanel(canvasObj.transform, "MainMenuPanel", new Color(0.04f, 0.07f, 0.12f, 0.96f));
            CreateTextObject(mainMenuPanel.transform, "THE RELIC TRAIL", defaultFont, 40, new Color(1f, 0.85f, 0.2f), new Vector2(0f, 130f), new Vector2(600f, 60f), TextAnchor.MiddleCenter);
            CreateTextObject(mainMenuPanel.transform, "An Action-Adventure RPG", defaultFont, 18, new Color(0.6f, 0.9f, 1f), new Vector2(0f, 80f), new Vector2(600f, 30f), TextAnchor.MiddleCenter);
            Button playBtn = CreateButton(mainMenuPanel.transform, "PlayButton", "Play Game", defaultFont, new Vector2(0f, 15f), new Color(0.15f, 0.55f, 0.35f));
            Button instructionsBtn = CreateButton(mainMenuPanel.transform, "InstructionsButton", "Instructions", defaultFont, new Vector2(0f, -45f), new Color(0.2f, 0.4f, 0.65f));
            Button mmQuitBtn = CreateButton(mainMenuPanel.transform, "QuitButton", "Quit Game", defaultFont, new Vector2(0f, -105f), new Color(0.65f, 0.2f, 0.2f));

            // Instructions Modal
            GameObject instructionsPanel = CreateMenuPanel(mainMenuPanel.transform, "InstructionsPanel", new Color(0.06f, 0.09f, 0.14f, 0.98f));
            var rtInst = instructionsPanel.GetComponent<RectTransform>();
            if (rtInst != null)
            {
                rtInst.anchorMin = new Vector2(0.5f, 0.5f);
                rtInst.anchorMax = new Vector2(0.5f, 0.5f);
                rtInst.pivot = new Vector2(0.5f, 0.5f);
                rtInst.sizeDelta = new Vector2(740f, 520f);
                rtInst.anchoredPosition = Vector2.zero;
            }
            CreateTextObject(instructionsPanel.transform, "HOW TO PLAY & CONTROLS", defaultFont, 24, new Color(1f, 0.85f, 0.2f), new Vector2(0f, 215f), new Vector2(680f, 40f), TextAnchor.MiddleCenter);
            string instructionsGuide =
                "CONTROLS:\n" +
                "• W / A / S / D : Move Character\n" +
                "• Mouse Movement : Look & Aim Camera\n" +
                "• Left Mouse Button (LMB) : Shoot / Light Attack\n" +
                "• Right Mouse Button (RMB) : Aim Down Sights (ADS)\n" +
                "• Q Key : Super Charged Attack\n" +
                "• Left Ctrl : Lock-On / Unlock Enemy Target\n" +
                "• E Key : Interact / Talk to NPCs / Purchase Relics\n" +
                "• V Key : Toggle Shoulder Camera Side\n" +
                "• Esc Key : Pause / Resume Game\n\n" +
                "MISSION OBJECTIVE:\n" +
                "Explore each biome, defeat hostile beasts to fulfill NPC missions,\n" +
                "collect coins, restore ancient Relics, and unlock the portal to the Sanctuary!";
            CreateTextObject(instructionsPanel.transform, instructionsGuide, defaultFont, 16, Color.white, new Vector2(0f, 20f), new Vector2(680f, 320f), TextAnchor.MiddleCenter);
            Button closeInstBtn = CreateButton(instructionsPanel.transform, "CloseButton", "Close", defaultFont, new Vector2(0f, -210f), new Color(0.25f, 0.45f, 0.7f));
            instructionsPanel.SetActive(false);

            // Pause Menu Panel
            GameObject pausePanel = CreateMenuPanel(canvasObj.transform, "PauseMenuPanel", new Color(0f, 0f, 0f, 0.85f));
            CreateTextObject(pausePanel.transform, "GAME PAUSED", defaultFont, 28, Color.white, new Vector2(0f, 110f), new Vector2(300f, 40f), TextAnchor.MiddleCenter);
            Button resumeBtn = CreateButton(pausePanel.transform, "ResumeButton", "Resume", defaultFont, new Vector2(0f, 45f));
            Button restartBtn = CreateButton(pausePanel.transform, "RestartButton", "Restart", defaultFont, new Vector2(0f, -10f));
            Button pauseMmBtn = CreateButton(pausePanel.transform, "PauseMainMenuButton", "Main Menu", defaultFont, new Vector2(0f, -65f));
            Button quitBtn = CreateButton(pausePanel.transform, "QuitButton", "Quit", defaultFont, new Vector2(0f, -120f));

            // Game Over Panel
            GameObject gameOverPanel = CreateMenuPanel(canvasObj.transform, "GameOverPanel", new Color(0.3f, 0f, 0f, 0.92f));
            CreateTextObject(gameOverPanel.transform, "YOU WERE DEFEATED", defaultFont, 32, Color.red, new Vector2(0f, 90f), new Vector2(400f, 45f), TextAnchor.MiddleCenter);
            Button retryBtn = CreateButton(gameOverPanel.transform, "RetryButton", "Retry", defaultFont, new Vector2(0f, 25f));
            Button goMmBtn = CreateButton(gameOverPanel.transform, "GOMainMenuButton", "Main Menu", defaultFont, new Vector2(0f, -30f));
            Button goQuitBtn = CreateButton(gameOverPanel.transform, "GOQuitButton", "Quit", defaultFont, new Vector2(0f, -85f));

            // Victory Panel
            GameObject victoryPanel = CreateMenuPanel(canvasObj.transform, "VictoryPanel", new Color(0.05f, 0.2f, 0.1f, 0.95f));
            CreateTextObject(victoryPanel.transform, "THE LOST RELIC RESTORED!", defaultFont, 32, Color.yellow, new Vector2(0f, 105f), new Vector2(500f, 45f), TextAnchor.MiddleCenter);
            GameObject vicStatsObj = CreateTextObject(victoryPanel.transform, "All fragments united!", defaultFont, 18, Color.white, new Vector2(0f, 50f), new Vector2(500f, 40f), TextAnchor.MiddleCenter);
            Button vicRestartBtn = CreateButton(victoryPanel.transform, "VicRestartButton", "Play Again", defaultFont, new Vector2(0f, -10f));
            Button vicMmBtn = CreateButton(victoryPanel.transform, "VicMainMenuButton", "Main Menu", defaultFont, new Vector2(0f, -65f));
            Button vicQuitBtn = CreateButton(victoryPanel.transform, "VicQuitButton", "Exit Game", defaultFont, new Vector2(0f, -120f));

            // Hook into GameUIManager
            SerializedObject soUI = new SerializedObject(uiMgr);
            soUI.FindProperty("hudCanvas").objectReferenceValue = hud;
            soUI.FindProperty("healthBar").objectReferenceValue = slider;
            soUI.FindProperty("healthText").objectReferenceValue = hpTextObj.GetComponent<Text>();
            soUI.FindProperty("staminaBar").objectReferenceValue = staminaSlider;
            soUI.FindProperty("staminaText").objectReferenceValue = spTextObj.GetComponent<Text>();
            soUI.FindProperty("biomeNameText").objectReferenceValue = biomeTextObj.GetComponent<Text>();
            soUI.FindProperty("objectiveText").objectReferenceValue = objTextObj.GetComponent<Text>();
            soUI.FindProperty("coinCountText").objectReferenceValue = coinTextObj.GetComponent<Text>();
            soUI.FindProperty("relicProgressText").objectReferenceValue = relicTextObj.GetComponent<Text>();
            soUI.FindProperty("promptPanel").objectReferenceValue = promptPanel;
            soUI.FindProperty("promptText").objectReferenceValue = promptTextObj.GetComponent<Text>();

            soUI.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            soUI.FindProperty("playButton").objectReferenceValue = playBtn;
            soUI.FindProperty("instructionsButton").objectReferenceValue = instructionsBtn;
            soUI.FindProperty("mainMenuQuitButton").objectReferenceValue = mmQuitBtn;
            soUI.FindProperty("instructionsPanel").objectReferenceValue = instructionsPanel;
            soUI.FindProperty("closeInstructionsButton").objectReferenceValue = closeInstBtn;

            soUI.FindProperty("pauseMenuPanel").objectReferenceValue = pausePanel;
            soUI.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            soUI.FindProperty("pauseRestartButton").objectReferenceValue = restartBtn;
            soUI.FindProperty("pauseMainMenuButton").objectReferenceValue = pauseMmBtn;
            soUI.FindProperty("pauseQuitButton").objectReferenceValue = quitBtn;

            soUI.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            soUI.FindProperty("retryButton").objectReferenceValue = retryBtn;
            soUI.FindProperty("gameOverMainMenuButton").objectReferenceValue = goMmBtn;
            soUI.FindProperty("gameOverQuitButton").objectReferenceValue = goQuitBtn;

            soUI.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            soUI.FindProperty("victoryStatsText").objectReferenceValue = vicStatsObj.GetComponent<Text>();
            soUI.FindProperty("victoryRestartButton").objectReferenceValue = vicRestartBtn;
            soUI.FindProperty("victoryMainMenuButton").objectReferenceValue = vicMmBtn;
            soUI.FindProperty("victoryQuitButton").objectReferenceValue = vicQuitBtn;
            soUI.ApplyModifiedProperties();

            // Hook into DialogueManager
            SerializedObject soDM = new SerializedObject(dm);
            soDM.FindProperty("dialoguePanel").objectReferenceValue = dialoguePanel;
            soDM.FindProperty("npcNameText").objectReferenceValue = npcNameObj.GetComponent<Text>();
            soDM.FindProperty("dialogueText").objectReferenceValue = diagTextObj.GetComponent<Text>();
            soDM.ApplyModifiedProperties();
        }

        private static GameObject CreateMenuPanel(Transform parent, string name, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = bgColor;
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateTextObject(
            Transform parent,
            string text,
            Font font,
            int size,
            Color col,
            Vector2 pos,
            Vector2 sizeDelta,
            TextAnchor align = TextAnchor.UpperLeft,
            Vector2? anchorMin = null,
            Vector2? anchorMax = null,
            Vector2? pivot = null)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;
            var t = obj.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.color = col;
            t.alignment = align;
            return obj;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 pos, Color? btnColor = null)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200f, 44f);
            var img = btnObj.AddComponent<Image>();
            img.color = btnColor ?? new Color(0.2f, 0.4f, 0.6f);
            var btn = btnObj.AddComponent<Button>();

            CreateTextObject(btnObj.transform, label, font, 18, Color.white, Vector2.zero, new Vector2(200f, 44f), TextAnchor.MiddleCenter);
            return btn;
        }

        private static void SetupCamera(GameObject player)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
            }

            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 1000f;

            cam.transform.position = player.transform.position + new Vector3(0.35f, 1.65f + 0.3f, -3.2f);
            cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

            var tpCam = cam.GetComponent<ThirdPersonCamera>();
            if (tpCam == null) tpCam = cam.gameObject.AddComponent<ThirdPersonCamera>();

            SerializedObject so = new SerializedObject(tpCam);
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.FindProperty("defaultDistance").floatValue = 2.6f;
            so.FindProperty("minDistance").floatValue = 0.9f;
            so.FindProperty("maxDistance").floatValue = 5.0f;
            so.FindProperty("targetOffset").vector3Value = new Vector3(0f, 1.70f, 0f);
            so.FindProperty("shoulderOffset").floatValue = 0.48f;
            so.FindProperty("adsDistance").floatValue = 1.35f;
            so.FindProperty("adsShoulderOffset").floatValue = 0.38f;
            so.FindProperty("adsTargetOffset").vector3Value = new Vector3(0f, 1.68f, 0f);
            so.FindProperty("normalFOV").floatValue = 65f;
            so.FindProperty("adsFOV").floatValue = 46f;
            so.FindProperty("adsTransitionSpeed").floatValue = 12f;
            so.FindProperty("positionSmoothTime").floatValue = 0.0f;
            so.FindProperty("rotationSmoothTime").floatValue = 0.0f;
            so.ApplyModifiedProperties();

            tpCam.SnapToTarget();
        }
    }

    [InitializeOnLoad]
    public class LostRelicControlPanel : EditorWindow
    {
        static LostRelicControlPanel()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    ShowWindow();
                }
            };
        }

        [MenuItem("Relic Trail/Show Control Panel", false, 0)]
        [MenuItem("Window/The Lost Relic Controls", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<LostRelicControlPanel>("Lost Relic Controls");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(12);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("THE LOST RELIC — LEVEL CONTROLS", headerStyle);
            GUILayout.Space(12);

            GUI.backgroundColor = new Color(0.18f, 0.72f, 0.32f);
            if (GUILayout.Button("🌟 REBUILD EXPANDED 3D WORLD\n(110m Biomes, Rich 3D Trees, Temples, Cacti, Palms & Coins)", GUILayout.Height(55)))
            {
                WorldBuilderEditor.ExecuteWorldGeneration(true);
            }

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.2f, 0.55f, 0.95f);
            if (GUILayout.Button("🎥 FIX 3D CAMERA & ATTACH TO PLAYER\n(Perspective 60° FOV, Over Player Shoulders)", GUILayout.Height(45)))
            {
                WorldBuilderEditor.FixCameraAndAttachToPlayer();
            }

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.95f, 0.45f, 0.15f);
            if (GUILayout.Button("🎨 FIX PINK MATERIALS (Convert All Assets to URP Lit)", GUILayout.Height(45)))
            {
                URPMaterialConverter.ConvertAllThirdPartyMaterials(false);
            }

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.35f, 0.75f, 0.95f);
            if (GUILayout.Button("🧹 REMOVE BLUE FLOOR OVERLAY (Clear NavMesh)", GUILayout.Height(45)))
            {
                WorldBuilderEditor.ClearNavMeshOverlay();
            }

            GUILayout.Space(15);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox(
                "• Click 'REBUILD EXPANDED 3D WORLD' to populate the enlarged 110x110m biomes with rich 3D assets from your packs (Fir & Pine trees, Torii Gate, Japanese Temple, Sphinx, Cacti, Palms, Winter pines, Camp cabins, and authentic 3D Coins).\n" +
                "• Click 'FIX 3D CAMERA' to lock the standard 3D camera right behind the player.\n" +
                "• Press PLAY (▶) to explore!",
                MessageType.Info
            );
        }
    }
}
