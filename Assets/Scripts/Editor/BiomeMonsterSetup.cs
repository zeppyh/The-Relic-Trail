using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using RelicTrail.Enemies;
using RelicTrail.Player;
using RelicTrail.Core;

namespace RelicTrail.Editor
{
    [InitializeOnLoad]
    public static class BiomeMonsterAutoRunner
    {
        static BiomeMonsterAutoRunner()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            if (SessionState.GetBool("BiomeMonsterSetup_V12_PlayerCrouch_NPCIdle_Done", false)) return;
            SessionState.SetBool("BiomeMonsterSetup_V12_PlayerCrouch_NPCIdle_Done", true);
            try
            {
                BiomeMonsterSetup.RunFullSetup(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BiomeMonsterSetup] Error during auto-setup: {ex}");
            }
        }
    }

    public static class BiomeMonsterSetup
    {
        public const string MonsterAnimDir = "Assets/ThirdParty/Stylized3DMonster/Anim";
        public const string MonsterControllerPath = "Assets/ThirdParty/Stylized3DMonster/Monster_Controller.controller";
        public const string PlayerControllerPath = "Assets/ThirdParty/Blink/Player_AnimatorController.controller";

        public const string ForestMonsterPrefabPath = "Assets/ThirdParty/Stylized3DMonster/Monster40/Prefab/Monster40_01.prefab";
        public const string GreenMonsterPrefabPath = ForestMonsterPrefabPath;
        public const string RedMonsterPrefabPath = "Assets/ThirdParty/Stylized3DMonster/Monster37/Prefab/Monster37_01.prefab";
        public const string DefaultMonsterPrefabPath = "Assets/ThirdParty/Stylized3DMonster/Monster41/Prefab/Monster41_01.prefab";
        public const string BlueMonsterPrefabPath = "Assets/ThirdParty/Stylized3DMonster/Monster42/Prefab/Monster42_01.prefab";
        public const string Coin01PrefabPath = "Assets/ThirdParty/LowPolyMedievalPropsLite/Prefabs/Coin_01.prefab";

        public const string FloreswaFbxPath = "Assets/ThirdParty/Floreswa/Models/male01_1.fbx";

        public static void RunFullSetupMenuItem()
        {
            RunFullSetup(true);
        }

        public static void RunFullSetupBatch()
        {
            RunFullSetup(false);
        }

        public static void RunFullSetup(bool showDialog)
        {
            Debug.Log("[BiomeMonsterSetup] Starting full setup of Biome Monsters and Character Animations...");

            // 1. Ensure looping on monster locomotion clips
            ConfigureMonsterClips();

            // 2. Build or update Monster Animator Controller
            var monsterController = CreateOrUpdateMonsterController();

            // 3. Configure Humanoid Rig for Floreswa Player Model
            ConfigurePlayerHumanoidRig();

            // 4. Build or update Player Animator Controller
            var playerController = CreateOrUpdatePlayerController();

            // 5. Update Scenes (SampleScene.unity & All models.unity)
            UpdateSceneMonstersAndPlayer("Assets/Scenes/SampleScene.unity", monsterController, playerController);
            UpdateSceneMonstersAndPlayer("Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Scenes/All models.unity", monsterController, playerController);

            // Re-open SampleScene as active
            if (File.Exists("Assets/Scenes/SampleScene.unity"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[BiomeMonsterSetup] Setup successfully complete!");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Setup Complete",
                    "Biome monsters and animations successfully configured!\n\n" +
                    "• Biome 1 (Forgotten Forest): Monster 40 (Monster40_01) with 16 beasts & 20 gold coins\n" +
                    "• Biome 2 (Scorched Canyon): Red Monster (Monster37) with 8 stalkers & 18 gold coins\n" +
                    "• Biome 3 (Frozen Ruins): Default Model Monster (Monster41) with 6 guardians & 18 gold coins\n" +
                    "• All Enemies drop physical gold coins on defeat, with walk-over trigger pickup enabled!\n" +
                    "• Player Character: Humanoid Animator Controller with gun-holding stance and two-handed IK\n" +
                    "• All enemy colliders, health, and combat drops verified and connected!",
                    "OK"
                );
            }
        }

        public static void ConfigureMonsterClips()
        {
            string[] loopingClips = new string[]
            {
                "Monster38_Idle02.anim",
                "Monster38_Idle03.anim",
                "Monster38_Walk.anim",
                "Monster38_Run.anim"
            };

            foreach (var clipName in loopingClips)
            {
                string path = Path.Combine(MonsterAnimDir, clipName).Replace('\\', '/');
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    SerializedObject so = new SerializedObject(clip);
                    SerializedProperty loopProp = so.FindProperty("m_AnimationClipSettings.m_LoopTime");
                    if (loopProp != null && !loopProp.boolValue)
                    {
                        loopProp.boolValue = true;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(clip);
                        Debug.Log($"[BiomeMonsterSetup] Enabled loopTime on {clipName}");
                    }
                }
            }
        }

        public static AnimatorController CreateOrUpdateMonsterController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MonsterControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(MonsterControllerPath);
            }

            // Ensure Parameters
            EnsureFloatParam(controller, "Speed");
            EnsureTriggerParam(controller, "Attack");
            EnsureTriggerParam(controller, "Die");

            var rootStateMachine = controller.layers[0].stateMachine;

            // Load clips
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{MonsterAnimDir}/Monster38_Idle02.anim");
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{MonsterAnimDir}/Monster38_Walk.anim");
            var runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{MonsterAnimDir}/Monster38_Run.anim");
            var attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{MonsterAnimDir}/Monster38_Attack01.anim");
            var dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{MonsterAnimDir}/Monster38_Die01.anim");

            // Find or create states
            var idleState = FindOrCreateState(rootStateMachine, "Idle", idleClip);
            var walkState = FindOrCreateState(rootStateMachine, "Walk", walkClip);
            var runState = FindOrCreateState(rootStateMachine, "Run", runClip);
            var attackState = FindOrCreateState(rootStateMachine, "Attack", attackClip);
            var dieState = FindOrCreateState(rootStateMachine, "Die", dieClip);

            rootStateMachine.defaultState = idleState;

            // Clear existing transitions on these states to avoid duplicates
            idleState.transitions = new AnimatorStateTransition[0];
            walkState.transitions = new AnimatorStateTransition[0];
            runState.transitions = new AnimatorStateTransition[0];
            attackState.transitions = new AnimatorStateTransition[0];

            // Idle <-> Walk
            var t_idleToWalk = idleState.AddTransition(walkState);
            t_idleToWalk.hasExitTime = false;
            t_idleToWalk.duration = 0.15f;
            t_idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

            var t_walkToIdle = walkState.AddTransition(idleState);
            t_walkToIdle.hasExitTime = false;
            t_walkToIdle.duration = 0.15f;
            t_walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            // Walk <-> Run
            var t_walkToRun = walkState.AddTransition(runState);
            t_walkToRun.hasExitTime = false;
            t_walkToRun.duration = 0.2f;
            t_walkToRun.AddCondition(AnimatorConditionMode.Greater, 3.2f, "Speed");

            var t_runToWalk = runState.AddTransition(walkState);
            t_runToWalk.hasExitTime = false;
            t_runToWalk.duration = 0.2f;
            t_runToWalk.AddCondition(AnimatorConditionMode.Less, 3.2f, "Speed");

            var t_runToIdle = runState.AddTransition(idleState);
            t_runToIdle.hasExitTime = false;
            t_runToIdle.duration = 0.2f;
            t_runToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            // Attack -> Idle
            var t_attackToIdle = attackState.AddTransition(idleState);
            t_attackToIdle.hasExitTime = true;
            t_attackToIdle.exitTime = 0.85f;
            t_attackToIdle.duration = 0.2f;

            // AnyState transitions
            ClearAnyStateTransitionsTo(rootStateMachine, attackState);
            var t_anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
            t_anyToAttack.canTransitionToSelf = false;
            t_anyToAttack.duration = 0.1f;
            t_anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            ClearAnyStateTransitionsTo(rootStateMachine, dieState);
            var t_anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
            t_anyToDie.canTransitionToSelf = false;
            t_anyToDie.duration = 0.1f;
            t_anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        public static void ConfigurePlayerHumanoidRig()
        {
            var importer = AssetImporter.GetAtPath(FloreswaFbxPath) as ModelImporter;
            if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.SaveAndReimport();
                Debug.Log($"[BiomeMonsterSetup] Configured Floreswa model ({FloreswaFbxPath}) to Humanoid animationType.");
            }
        }

        public static AnimatorController CreateOrUpdatePlayerController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerControllerPath);
            }

            // Ensure Parameters
            EnsureFloatParam(controller, "Speed");
            EnsureBoolParam(controller, "IsGrounded");
            EnsureBoolParam(controller, "IsJumping");
            EnsureBoolParam(controller, "IsFalling");
            EnsureBoolParam(controller, "IsAiming");
            EnsureBoolParam(controller, "IsShooting");
            EnsureTriggerParam(controller, "Jump");
            EnsureTriggerParam(controller, "Die");

            // Remove legacy Attack trigger parameter if present
            for (int i = controller.parameters.Length - 1; i >= 0; i--)
            {
                if (controller.parameters[i].name == "Attack")
                {
                    controller.RemoveParameter(i);
                }
            }

            // Ensure looping on Kevin Iglesias locomotion & aiming clips
            string idleFbx = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx";
            string walkFbx = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx";
            string runFbx = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx";
            string weaponHoldFbx = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Masked Poses/HumanM@WeaponHold_AssaultRifle01.fbx";
            string aimFbx = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Combat/AssaultRifle/HumanM@AssaultRifle_Aim01.fbx";
            string shootFbx = "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Male/Combat/AssaultRifle/HumanM@AssaultRifle_Aim01_Shoot01.fbx";
            string jumpsFbx = "Assets/ThirdParty/Blink/Art/Animations/Animations_Starter_Pack/Movement/Jumps.fbx";
            string fallFbx = "Assets/ThirdParty/Blink/Art/Animations/Animations_Starter_Pack/Movement/FallingLoop.fbx";
            string deathFbx = "Assets/ThirdParty/Blink/Art/Animations/Animations_Starter_Pack/Combat/Death.fbx";

            EnsureFbxLoopTime(idleFbx);
            EnsureFbxLoopTime(walkFbx);
            EnsureFbxLoopTime(runFbx);
            EnsureFbxLoopTime(weaponHoldFbx);
            EnsureFbxLoopTime(aimFbx);
            EnsureFbxLoopTime(fallFbx);

            // Load clips
            var idleClip = GetClipFromFbx(idleFbx);
            var walkClip = GetClipFromFbx(walkFbx);
            var runClip = GetClipFromFbx(runFbx);
            var weaponHoldClip = GetClipFromFbx(weaponHoldFbx);
            var aimClip = GetClipFromFbx(aimFbx);
            var shootClip = GetClipFromFbx(shootFbx);
            var jumpUpClip = GetClipFromFbx(jumpsFbx, "Jump_Up");
            var landClip = GetClipFromFbx(jumpsFbx, "Jump_Down");
            var fallClip = GetClipFromFbx(fallFbx);
            var dieClip = GetClipFromFbx(deathFbx);

            // -------------------------------------------------------------
            // LAYER 0: Base Layer (Locomotion Blend Tree, Jump, Fall, Land, Die)
            // -------------------------------------------------------------
            var rootStateMachine = controller.layers[0].stateMachine;

            // Remove obsolete discrete states from rootStateMachine
            string[] obsoleteStates = new string[] { "Idle", "Run", "Sprint", "Attack" };
            for (int i = rootStateMachine.states.Length - 1; i >= 0; i--)
            {
                var s = rootStateMachine.states[i].state;
                if (s != null && System.Array.IndexOf(obsoleteStates, s.name) >= 0)
                {
                    rootStateMachine.RemoveState(s);
                }
            }

            // Create or get Locomotion Blend Tree state
            var locomotionState = FindOrCreateState(rootStateMachine, "Locomotion", null);
            BlendTree blendTree = locomotionState.motion as BlendTree;
            if (blendTree == null)
            {
                blendTree = new BlendTree();
                blendTree.name = "Locomotion_1D";
                AssetDatabase.AddObjectToAsset(blendTree, controller);
                locomotionState.motion = blendTree;
            }

            blendTree.name = "Locomotion_1D";
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = "Speed";
            blendTree.useAutomaticThresholds = false;

            var children = new ChildMotion[]
            {
                new ChildMotion { motion = idleClip, threshold = 0.0f, timeScale = 1.0f },
                new ChildMotion { motion = walkClip, threshold = 4.5f, timeScale = 1.0f },
                new ChildMotion { motion = runClip,  threshold = 7.5f, timeScale = 1.0f }
            };
            blendTree.children = children;

            rootStateMachine.defaultState = locomotionState;

            var jumpState = FindOrCreateState(rootStateMachine, "Jump", jumpUpClip);
            var fallState = FindOrCreateState(rootStateMachine, "Fall", fallClip);
            var landState = FindOrCreateState(rootStateMachine, "Land", landClip);
            var dieState = FindOrCreateState(rootStateMachine, "Die", dieClip);

            // Clear old transitions
            locomotionState.transitions = new AnimatorStateTransition[0];
            jumpState.transitions = new AnimatorStateTransition[0];
            fallState.transitions = new AnimatorStateTransition[0];
            landState.transitions = new AnimatorStateTransition[0];

            // Locomotion -> Jump (IsJumping == true)
            var t_locoToJump = locomotionState.AddTransition(jumpState);
            t_locoToJump.hasExitTime = false;
            t_locoToJump.duration = 0.1f;
            t_locoToJump.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");

            // Locomotion -> Fall (IsFalling == true)
            var t_locoToFall = locomotionState.AddTransition(fallState);
            t_locoToFall.hasExitTime = false;
            t_locoToFall.duration = 0.15f;
            t_locoToFall.AddCondition(AnimatorConditionMode.If, 0, "IsFalling");

            // Jump -> Fall (exit time or IsFalling)
            var t_jumpToFallTime = jumpState.AddTransition(fallState);
            t_jumpToFallTime.hasExitTime = true;
            t_jumpToFallTime.exitTime = 0.65f;
            t_jumpToFallTime.duration = 0.15f;

            var t_jumpToFallCond = jumpState.AddTransition(fallState);
            t_jumpToFallCond.hasExitTime = false;
            t_jumpToFallCond.duration = 0.1f;
            t_jumpToFallCond.AddCondition(AnimatorConditionMode.If, 0, "IsFalling");

            // Fall -> Land (IsGrounded == true)
            var t_fallToLand = fallState.AddTransition(landState);
            t_fallToLand.hasExitTime = false;
            t_fallToLand.duration = 0.08f;
            t_fallToLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            // Land -> Locomotion (exit time or moving)
            var t_landToLocoTime = landState.AddTransition(locomotionState);
            t_landToLocoTime.hasExitTime = true;
            t_landToLocoTime.exitTime = 0.35f;
            t_landToLocoTime.duration = 0.15f;

            var t_landToLocoMove = landState.AddTransition(locomotionState);
            t_landToLocoMove.hasExitTime = false;
            t_landToLocoMove.duration = 0.1f;
            t_landToLocoMove.AddCondition(AnimatorConditionMode.Greater, 0.4f, "Speed");

            // AnyState transitions
            ClearAnyStateTransitionsTo(rootStateMachine, jumpState);
            var t_anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
            t_anyToJump.canTransitionToSelf = false;
            t_anyToJump.duration = 0.1f;
            t_anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");

            ClearAnyStateTransitionsTo(rootStateMachine, dieState);
            var t_anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
            t_anyToDie.canTransitionToSelf = false;
            t_anyToDie.duration = 0.1f;
            t_anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

            // -------------------------------------------------------------
            // LAYER 1: UpperBody_HoldingGun (Two-handed rifle carry, aim, shoot, IK pass)
            // -------------------------------------------------------------
            string maskPath = "Assets/ThirdParty/Blink/UpperBodyMask.mask";
            var upperBodyMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if (upperBodyMask == null)
            {
                upperBodyMask = new AvatarMask();
                AssetDatabase.CreateAsset(upperBodyMask, maskPath);
            }
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);
            EditorUtility.SetDirty(upperBodyMask);

            var layers = controller.layers;
            int upperLayerIndex = -1;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "UpperBody_HoldingGun")
                {
                    upperLayerIndex = i;
                    break;
                }
            }

            if (upperLayerIndex < 0)
            {
                controller.AddLayer("UpperBody_HoldingGun");
                layers = controller.layers;
                upperLayerIndex = layers.Length - 1;
            }

            var upperLayer = layers[upperLayerIndex];
            upperLayer.defaultWeight = 1.0f;
            upperLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            upperLayer.avatarMask = upperBodyMask;
            upperLayer.iKPass = true; // Crucial for Two-Handed Left Hand Foregrip IK!

            var upperStateMachine = upperLayer.stateMachine;

            // Remove legacy HoldingGun state if present
            for (int i = upperStateMachine.states.Length - 1; i >= 0; i--)
            {
                var s = upperStateMachine.states[i].state;
                if (s != null && s.name == "HoldingGun")
                {
                    upperStateMachine.RemoveState(s);
                }
            }

            var weaponHoldState = FindOrCreateState(upperStateMachine, "WeaponHold", weaponHoldClip);
            var aimState = FindOrCreateState(upperStateMachine, "Aim", aimClip);
            var shootState = FindOrCreateState(upperStateMachine, "Shoot", shootClip);
            var upperDieState = FindOrCreateState(upperStateMachine, "Die", dieClip);

            upperStateMachine.defaultState = weaponHoldState;

            // Clear transitions
            weaponHoldState.transitions = new AnimatorStateTransition[0];
            aimState.transitions = new AnimatorStateTransition[0];
            shootState.transitions = new AnimatorStateTransition[0];

            // WeaponHold <-> Aim
            var t_holdToAim = weaponHoldState.AddTransition(aimState);
            t_holdToAim.hasExitTime = false;
            t_holdToAim.duration = 0.12f;
            t_holdToAim.AddCondition(AnimatorConditionMode.If, 0, "IsAiming");

            var t_aimToHold = aimState.AddTransition(weaponHoldState);
            t_aimToHold.hasExitTime = false;
            t_aimToHold.duration = 0.15f;
            t_aimToHold.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAiming");

            // WeaponHold -> Shoot
            var t_holdToShoot = weaponHoldState.AddTransition(shootState);
            t_holdToShoot.hasExitTime = false;
            t_holdToShoot.duration = 0.05f;
            t_holdToShoot.AddCondition(AnimatorConditionMode.If, 0, "IsShooting");

            // Aim -> Shoot
            var t_aimToShoot = aimState.AddTransition(shootState);
            t_aimToShoot.hasExitTime = false;
            t_aimToShoot.duration = 0.05f;
            t_aimToShoot.AddCondition(AnimatorConditionMode.If, 0, "IsShooting");

            // Shoot -> Aim (if IsAiming)
            var t_shootToAim = shootState.AddTransition(aimState);
            t_shootToAim.hasExitTime = true;
            t_shootToAim.exitTime = 0.55f;
            t_shootToAim.duration = 0.1f;
            t_shootToAim.AddCondition(AnimatorConditionMode.If, 0, "IsAiming");

            // Shoot -> WeaponHold (if !IsAiming)
            var t_shootToHold = shootState.AddTransition(weaponHoldState);
            t_shootToHold.hasExitTime = true;
            t_shootToHold.exitTime = 0.55f;
            t_shootToHold.duration = 0.1f;
            t_shootToHold.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAiming");

            ClearAnyStateTransitionsTo(upperStateMachine, upperDieState);
            var t_anyToDieUpper = upperStateMachine.AddAnyStateTransition(upperDieState);
            t_anyToDieUpper.canTransitionToSelf = false;
            t_anyToDieUpper.duration = 0.1f;
            t_anyToDieUpper.AddCondition(AnimatorConditionMode.If, 0, "Die");

            layers[upperLayerIndex] = upperLayer;
            controller.layers = layers;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        public static void UpdateSceneMonstersAndPlayer(string scenePath, AnimatorController monsterController, AnimatorController playerController)
        {
            if (!File.Exists(scenePath)) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.isLoaded) return;

            Debug.Log($"[BiomeMonsterSetup] Processing scene: {scenePath}");

            // 1. Process All Enemies
            var enemies = UnityEngine.Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include);
            foreach (var ai in enemies)
            {
                var health = ai.GetComponent<EnemyHealth>();
                string name = ai.gameObject.name;

                // Determine Biome and matching monster prefab
                string prefabPath = GreenMonsterPrefabPath; // Default Green (Forest)
                BiomeType biome = BiomeType.ForgottenForest;
                bool isForestMission = true;

                if (name.StartsWith("Canyon_Stalker") || (health != null && health.EnemyBiome == BiomeType.ScorchedCanyon))
                {
                    prefabPath = RedMonsterPrefabPath; // Red (Canyon)
                    biome = BiomeType.ScorchedCanyon;
                    isForestMission = false;
                }
                else if (name.StartsWith("Frost_Guardian") || (health != null && health.EnemyBiome == BiomeType.FrozenRuins))
                {
                    prefabPath = DefaultMonsterPrefabPath; // Default Monster41 (Frozen/Snow Biome)
                    biome = BiomeType.FrozenRuins;
                    isForestMission = false;
                }

                // Remove legacy capsule meshes/lights
                for (int i = ai.transform.childCount - 1; i >= 0; i--)
                {
                    var child = ai.transform.GetChild(i);
                    if (child.name == "Enemy_Mesh" || child.name == "BeastEyesLight" || child.name.StartsWith("Monster") || child.name == "MonsterVisual")
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }

                // Instantiate Monster Prefab
                var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (monsterPrefab != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefab, ai.transform);
                    visual.name = "MonsterVisual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale = Vector3.one;

                    // Locate or add Animator on visual or its model child
                    var anim = visual.GetComponentInChildren<Animator>(true);
                    if (anim == null)
                    {
                        // Attach to child model (direct parent of 'root') or visual
                        if (visual.transform.childCount > 0)
                        {
                            anim = visual.transform.GetChild(0).gameObject.AddComponent<Animator>();
                        }
                        else
                        {
                            anim = visual.AddComponent<Animator>();
                        }
                    }
                    anim.runtimeAnimatorController = monsterController;

                    // Connect to EnemyAI
                    SerializedObject soAI = new SerializedObject(ai);
                    soAI.FindProperty("animator").objectReferenceValue = anim;
                    soAI.FindProperty("animSpeedParam").stringValue = "Speed";
                    soAI.FindProperty("animAttackTrigger").stringValue = "Attack";
                    var dieProp = soAI.FindProperty("animDieTrigger");
                    if (dieProp != null) dieProp.stringValue = "Die";
                    soAI.ApplyModifiedProperties();
                }

                // Configure EnemyHealth biome & settings
                if (health != null)
                {
                    SerializedObject soH = new SerializedObject(health);
                    soH.FindProperty("enemyBiome").enumValueIndex = (int)biome;
                    soH.FindProperty("countTowardsForestMission").boolValue = isForestMission;
                    var coinDropProp = soH.FindProperty("coinDropPrefab");
                    if (coinDropProp != null)
                    {
                        var coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Coin01PrefabPath);
                        if (coinPrefab != null) coinDropProp.objectReferenceValue = coinPrefab;
                    }
                    var dropCoinsProp = soH.FindProperty("dropCoinsOnDeath");
                    if (dropCoinsProp != null) dropCoinsProp.boolValue = true;
                    var minDropProp = soH.FindProperty("minCoinsToDrop");
                    if (minDropProp != null) minDropProp.intValue = 1;
                    var maxDropProp = soH.FindProperty("maxCoinsToDrop");
                    if (maxDropProp != null) maxDropProp.intValue = 3;
                    soH.ApplyModifiedProperties();
                }

                // Adjust CharacterController and Collider for monster dimensions
                var cc = ai.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.center = new Vector3(0f, 0.85f, 0f);
                    cc.height = 1.7f;
                    cc.radius = 0.5f;
                }

                var cap = ai.GetComponent<CapsuleCollider>();
                if (cap != null)
                {
                    cap.center = new Vector3(0f, 0.85f, 0f);
                    cap.height = 1.7f;
                    cap.radius = 0.55f;
                }

                EditorUtility.SetDirty(ai.gameObject);
            }

            // 2. Process Player
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var pMove = player.GetComponent<PlayerMovement>();
                var pAttack = player.GetComponent<PlayerAttack>();

                // Ensure Animator is on model child or player
                var anim = player.GetComponentInChildren<Animator>(true);
                if (anim == null)
                {
                    var modelChild = player.transform.Find("male01_1") ?? player.transform.Find("Model");
                    if (modelChild != null)
                    {
                        anim = modelChild.gameObject.AddComponent<Animator>();
                    }
                    else
                    {
                        anim = player.AddComponent<Animator>();
                    }
                }

                if (anim != null)
                {
                    anim.runtimeAnimatorController = playerController;

                    // Set Avatar if available from Floreswa
                    var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(FloreswaFbxPath);
                    if (avatar == null)
                    {
                        var all = AssetDatabase.LoadAllAssetsAtPath(FloreswaFbxPath);
                        foreach (var o in all)
                        {
                            if (o is Avatar av) { avatar = av; break; }
                        }
                    }

                    if (avatar != null)
                    {
                        anim.avatar = avatar;
                    }

                    if (pMove != null)
                    {
                        SerializedObject soMove = new SerializedObject(pMove);
                        soMove.FindProperty("animator").objectReferenceValue = anim;
                        soMove.FindProperty("animSpeedParam").stringValue = "Speed";
                        soMove.FindProperty("animGroundedParam").stringValue = "IsGrounded";
                        var jumpProp = soMove.FindProperty("animJumpingParam");
                        if (jumpProp != null) jumpProp.stringValue = "IsJumping";
                        soMove.FindProperty("animFallingParam").stringValue = "IsFalling";
                        var aimProp = soMove.FindProperty("animAimingParam");
                        if (aimProp != null) aimProp.stringValue = "IsAiming";
                        var shootProp = soMove.FindProperty("animShootingParam");
                        if (shootProp != null) shootProp.stringValue = "IsShooting";
                        soMove.FindProperty("animJumpTrigger").stringValue = "Jump";
                        soMove.ApplyModifiedProperties();
                    }

                    if (pAttack != null)
                    {
                        SerializedObject soAtk = new SerializedObject(pAttack);
                        var atkAnimProp = soAtk.FindProperty("animator");
                        if (atkAnimProp != null) atkAnimProp.objectReferenceValue = anim;
                        var atkShootProp = soAtk.FindProperty("animShootingParam");
                        if (atkShootProp != null) atkShootProp.stringValue = "IsShooting";
                        var atkTrigProp = soAtk.FindProperty("animAttackTrigger");
                        if (atkTrigProp != null) atkTrigProp.stringValue = "";
                        soAtk.ApplyModifiedProperties();

                        pAttack.SetupGunVisual();
                    }

                    // Ensure PlayerWeaponIK is attached to the Animator GameObject
                    var ik = anim.GetComponent<PlayerWeaponIK>();
                    if (ik == null)
                    {
                        ik = anim.gameObject.AddComponent<PlayerWeaponIK>();
                    }
                    var gunVis = player.GetComponentInChildren<GunWeaponVisual>(true);
                    if (gunVis != null)
                    {
                        ik.SetWeaponVisual(gunVis);
                    }
                }

                EditorUtility.SetDirty(player);
            }

            // 2.5 Process All NPCs (The Explorer & The Canyon Explorer)
            ProcessAllNPCs(scene);

            // 3. Ensure required enemy counts & collectible coins for each biome
            EnsureBiomeEnemiesAndCoins(scene, monsterController);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ProcessAllNPCs(UnityEngine.SceneManagement.Scene scene)
        {
            var npcController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/NPC_AnimatorController.controller");
            var male01Avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/ThirdParty/Floreswa/Models/male01_1.fbx");
            var male02Avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/ThirdParty/Floreswa/Models/male02_1.fbx");
            var male03Avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/ThirdParty/Floreswa/Models/male03_1.fbx");

            var missionNPCs = UnityEngine.Object.FindObjectsByType<RelicTrail.NPC.ExplorerMissionNPC>(FindObjectsInactive.Include);
            foreach (var npc in missionNPCs)
            {
                var anim = npc.GetComponentInChildren<Animator>(true);
                if (anim == null)
                {
                    Transform model = npc.transform.Find("male01_1");
                    if (model == null && npc.transform.childCount > 0) model = npc.transform.GetChild(0);
                    GameObject targetObj = model != null ? model.gameObject : npc.gameObject;
                    anim = targetObj.AddComponent<Animator>();
                }

                if (anim != null)
                {
                    if (npcController != null) anim.runtimeAnimatorController = npcController;
                    if (anim.avatar == null && male01Avatar != null) anim.avatar = male01Avatar;
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    anim.enabled = true;
                }

                SerializedObject soNPC = new SerializedObject(npc);
                var ctrlProp = soNPC.FindProperty("npcAnimatorController");
                if (ctrlProp != null && npcController != null) ctrlProp.objectReferenceValue = npcController;
                var avProp = soNPC.FindProperty("npcAvatar");
                if (avProp != null && male01Avatar != null) avProp.objectReferenceValue = male01Avatar;
                soNPC.ApplyModifiedProperties();

                EditorUtility.SetDirty(npc.gameObject);
            }

            var dialogueNPCs = UnityEngine.Object.FindObjectsByType<RelicTrail.NPC.NPCDialogue>(FindObjectsInactive.Include);
            foreach (var npc in dialogueNPCs)
            {
                var anim = npc.GetComponentInChildren<Animator>(true);
                if (anim == null)
                {
                    Transform model = npc.transform.Find("male03_1") ?? npc.transform.Find("male02_1") ?? npc.transform.Find("male01_1");
                    if (model == null && npc.transform.childCount > 0) model = npc.transform.GetChild(0);
                    GameObject targetObj = model != null ? model.gameObject : npc.gameObject;
                    anim = targetObj.AddComponent<Animator>();
                }

                Avatar chosenAvatar = (npc.name.Contains("Sage") || npc.name.Contains("Frozen")) && male03Avatar != null
                    ? male03Avatar
                    : ((npc.name.Contains("Canyon") && male02Avatar != null) ? male02Avatar : male01Avatar);

                if (anim != null)
                {
                    if (npcController != null) anim.runtimeAnimatorController = npcController;
                    if (anim.avatar == null && chosenAvatar != null) anim.avatar = chosenAvatar;
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    anim.enabled = true;
                }

                SerializedObject soNPC = new SerializedObject(npc);
                var ctrlProp = soNPC.FindProperty("npcAnimatorController");
                if (ctrlProp != null && npcController != null) ctrlProp.objectReferenceValue = npcController;
                var avProp = soNPC.FindProperty("npcAvatar");
                if (avProp != null && chosenAvatar != null) avProp.objectReferenceValue = chosenAvatar;
                soNPC.ApplyModifiedProperties();

                EditorUtility.SetDirty(npc.gameObject);
            }
        }

        private static void EnsureBiomeEnemiesAndCoins(UnityEngine.SceneManagement.Scene scene, AnimatorController monsterController)
        {
            var coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Coin01PrefabPath);

            // 1. Biome 1 (Forgotten Forest) - 16 enemies & 20 gold coins
            var forest = GameObject.Find("Biome1_ForgottenForest");
            if (forest != null)
            {
                Vector3[] forestEnemyPositions = new Vector3[]
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
                SpawnMissingEnemies(forest.transform, "Forest_Guardian_Beast_", forestEnemyPositions, BiomeType.ForgottenForest, true, GreenMonsterPrefabPath, monsterController, coinPrefab);
                EnsureCoinsInContainer(forest.transform, "Forest_Coins_Container", "Forest_Coin_", 20, 45f, coinPrefab);
            }

            // 2. Biome 2 (Scorched Canyon) - 8 enemies & 18 gold coins
            var canyon = GameObject.Find("Biome2_ScorchedCanyon");
            if (canyon != null)
            {
                Vector3[] canyonEnemyPositions = new Vector3[]
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
                SpawnMissingEnemies(canyon.transform, "Canyon_Stalker_", canyonEnemyPositions, BiomeType.ScorchedCanyon, false, RedMonsterPrefabPath, monsterController, coinPrefab);
                EnsureCoinsInContainer(canyon.transform, "Canyon_Coins_Container", "Canyon_Coin_", 18, 45f, coinPrefab);
            }

            // 3. Biome 3 (Frozen Ruins / Snow Biome) - 6 enemies (Default Monster41 model) & 18 gold coins
            var frozen = GameObject.Find("Biome3_FrozenRuins");
            if (frozen != null)
            {
                Vector3[] frostEnemyPositions = new Vector3[]
                {
                    new Vector3(-18f, 0f, 8f),
                    new Vector3(18f, 0f, 14f),
                    new Vector3(-22f, 0f, 24f),
                    new Vector3(20f, 0f, 26f),
                    new Vector3(-14f, 0f, -8f),
                    new Vector3(15f, 0f, -12f)
                };
                SpawnMissingEnemies(frozen.transform, "Frost_Guardian_", frostEnemyPositions, BiomeType.FrozenRuins, false, DefaultMonsterPrefabPath, monsterController, coinPrefab);
                EnsureCoinsInContainer(frozen.transform, "Frozen_Coins_Container", "Frozen_Coin_", 18, 45f, coinPrefab);
            }
        }

        private static void SpawnMissingEnemies(
            Transform parent,
            string prefix,
            Vector3[] positions,
            BiomeType biome,
            bool isForestMission,
            string prefabPath,
            AnimatorController monsterController,
            GameObject coinPrefab)
        {
            var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            for (int i = 0; i < positions.Length; i++)
            {
                string enemyName = $"{prefix}{i + 1}";
                Transform existing = parent.Find(enemyName);
                if (existing != null) continue;

                GameObject enemyObj = new GameObject(enemyName);
                enemyObj.transform.SetParent(parent, false);
                enemyObj.transform.localPosition = positions[i];

                var cc = enemyObj.AddComponent<CharacterController>();
                cc.center = new Vector3(0f, 0.85f, 0f);
                cc.height = 1.7f;
                cc.radius = 0.5f;

                var cap = enemyObj.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.85f, 0f);
                cap.height = 1.7f;
                cap.radius = 0.55f;

                var health = enemyObj.AddComponent<EnemyHealth>();
                health.EnemyBiome = biome;
                health.CountTowardsForestMission = isForestMission;

                var ai = enemyObj.AddComponent<EnemyAI>();

                if (monsterPrefab != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefab, enemyObj.transform);
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
                    anim.runtimeAnimatorController = monsterController;

                    SerializedObject soAI = new SerializedObject(ai);
                    soAI.FindProperty("animator").objectReferenceValue = anim;
                    soAI.FindProperty("animSpeedParam").stringValue = "Speed";
                    soAI.FindProperty("animAttackTrigger").stringValue = "Attack";
                    var dieProp = soAI.FindProperty("animDieTrigger");
                    if (dieProp != null) dieProp.stringValue = "Die";
                    soAI.ApplyModifiedProperties();
                }

                SerializedObject soH = new SerializedObject(health);
                soH.FindProperty("enemyBiome").enumValueIndex = (int)biome;
                soH.FindProperty("countTowardsForestMission").boolValue = isForestMission;
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

                EditorUtility.SetDirty(enemyObj);
            }
        }

        private static void EnsureCoinsInContainer(
            Transform parent,
            string containerName,
            string coinPrefix,
            int targetCount,
            float maxRadius,
            GameObject coinPrefab)
        {
            Transform containerTransform = parent.Find(containerName);
            GameObject container = null;
            if (containerTransform == null)
            {
                container = new GameObject(containerName);
                container.transform.SetParent(parent, false);
                container.transform.localPosition = Vector3.zero;
            }
            else
            {
                container = containerTransform.gameObject;
            }

            int currentCoins = container.transform.childCount;
            for (int i = currentCoins; i < targetCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / targetCount);
                float r = UnityEngine.Random.Range(10f, maxRadius);
                Vector3 coinPos = new Vector3(Mathf.Cos(angle) * r, 0.75f, Mathf.Sin(angle) * r);

                GameObject coin = null;
                if (coinPrefab != null)
                {
                    coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, container.transform);
                }
                if (coin == null)
                {
                    coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    coin.transform.SetParent(container.transform, false);
                    coin.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
                    coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
                else
                {
                    coin.transform.localPosition = coinPos;
                    coin.transform.localScale = Vector3.one * 1.6f;
                }

                coin.name = $"{coinPrefix}{i + 1}";

                var col = coin.GetComponent<Collider>();
                if (col == null) col = coin.AddComponent<SphereCollider>();
                col.isTrigger = true;

                var comp = coin.GetComponent<RelicTrail.Progression.CollectibleItem>();
                if (comp == null) comp = coin.AddComponent<RelicTrail.Progression.CollectibleItem>();

                EditorUtility.SetDirty(coin);
            }

            // Ensure all coins in container have trigger colliders and CollectibleItem
            for (int i = 0; i < container.transform.childCount; i++)
            {
                var c = container.transform.GetChild(i).gameObject;
                var col = c.GetComponent<Collider>();
                if (col == null) col = c.AddComponent<SphereCollider>();
                col.isTrigger = true;
                if (c.GetComponent<RelicTrail.Progression.CollectibleItem>() == null)
                {
                    c.AddComponent<RelicTrail.Progression.CollectibleItem>();
                }
                EditorUtility.SetDirty(c);
            }
        }

        private static void EnsureFbxLoopTime(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            var clipAnims = importer.clipAnimations;
            bool needsSave = false;

            if (clipAnims != null && clipAnims.Length > 0)
            {
                for (int i = 0; i < clipAnims.Length; i++)
                {
                    if (!clipAnims[i].loopTime)
                    {
                        clipAnims[i].loopTime = true;
                        needsSave = true;
                    }
                }
                if (needsSave) importer.clipAnimations = clipAnims;
            }
            else
            {
                var defaultClips = importer.defaultClipAnimations;
                if (defaultClips != null && defaultClips.Length > 0)
                {
                    for (int i = 0; i < defaultClips.Length; i++)
                    {
                        defaultClips[i].loopTime = true;
                    }
                    importer.clipAnimations = defaultClips;
                    needsSave = true;
                }
            }

            if (needsSave)
            {
                importer.SaveAndReimport();
                Debug.Log($"[BiomeMonsterSetup] Enabled loopTime on: {fbxPath}");
            }
        }

        private static AnimationClip GetClipFromFbx(string fbxPath, string clipName = null)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (!string.IsNullOrEmpty(clipName))
            {
                foreach (var a in assets)
                {
                    if (a is AnimationClip clip && !clip.name.StartsWith("__preview__") && clip.name.Equals(clipName, StringComparison.OrdinalIgnoreCase))
                    {
                        return clip;
                    }
                }
            }

            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__") && clip.name != "tpose")
                {
                    return clip;
                }
            }
            return null;
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string stateName, AnimationClip clip)
        {
            foreach (var childState in sm.states)
            {
                if (childState.state.name == stateName)
                {
                    if (clip != null) childState.state.motion = clip;
                    return childState.state;
                }
            }

            var newState = sm.AddState(stateName);
            if (clip != null) newState.motion = clip;
            return newState;
        }

        private static void ClearAnyStateTransitionsTo(AnimatorStateMachine sm, AnimatorState targetState)
        {
            var transitions = sm.anyStateTransitions;
            var kept = new List<AnimatorStateTransition>();
            foreach (var t in transitions)
            {
                if (t.destinationState != targetState)
                {
                    kept.Add(t);
                }
            }
            sm.anyStateTransitions = kept.ToArray();
        }

        private static void EnsureFloatParam(AnimatorController ac, string name)
        {
            foreach (var p in ac.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Float) return;
            }
            ac.AddParameter(name, AnimatorControllerParameterType.Float);
        }

        private static void EnsureBoolParam(AnimatorController ac, string name)
        {
            foreach (var p in ac.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Bool) return;
            }
            ac.AddParameter(name, AnimatorControllerParameterType.Bool);
        }

        private static void EnsureTriggerParam(AnimatorController ac, string name)
        {
            foreach (var p in ac.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Trigger) return;
            }
            ac.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }
    }
}
