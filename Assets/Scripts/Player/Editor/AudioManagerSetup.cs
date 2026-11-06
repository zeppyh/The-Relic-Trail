#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RelicTrail.Core;

namespace RelicTrail.EditorTools
{
    [InitializeOnLoad]
    public static class AudioManagerSetup
    {
        private const string CoinPickupPath = "Assets/Audio/SFX/coin_pickup.wav";
        private const string RelicPickupPath = "Assets/Audio/SFX/relic_pickup.wav";
        private const string MagicShootPath = "Assets/Audio/Overwatch - Kill Sound Effect.mp3";
        private const string MagicHitPath = "Assets/Audio/SFX/magic_hit.wav";
        private const string PlayerHurtPath = "Assets/Audio/Something being hit - Sound Effect.mp3";
        private const string EnemyDefeatPath = "Assets/Audio/Elimination.mp3";
        private const string PortalTeleportPath = "Assets/Audio/Portals/portal_teleport.wav";
        private const string PuzzleChimePath = "Assets/Audio/SFX/puzzle_chime.wav";
        private const string ButtonClickPath = "Assets/Audio/Button Plate Click (Minecraft Sound) - Sound Effect (Free).mp3";

        private const string ForestFootstepPath = "Assets/Audio/SFX/footstep_forest.wav";
        private const string CanyonFootstepPath = "Assets/Audio/Sound Effect - Walking in Sand.mp3";
        private const string FrozenFootstepPath = "Assets/Audio/SFX/footstep_frozen.wav";
        private const string SanctuaryFootstepPath = "Assets/Audio/SFX/footstep_sanctuary.wav";

        private const string ForestMusicPath = "Assets/Audio/Peaceful Swiss Forest Sounds – Birds and Nature in 4K & STEREO (15s).mp3";
        private const string CanyonMusicPath = "Assets/Audio/Windy Canyon SOUND Effect.mp3";
        private const string FrozenMusicPath = "Assets/Audio/Freeze Airy - Free Sound Effect.mp3";
        private const string SanctuaryMusicPath = "Assets/Audio/Huge Hidden Passages sound effects example.mp3";
        private const string VictoryMusicPath = "Assets/Audio/Victory Sound Effect.mp3";

        static AudioManagerSetup()
        {
            EditorApplication.delayCall += RunAudioSetup;
        }

        [MenuItem("Relic Trail/Auto-Fill Audio Manager Clips")]
        public static void RunAudioSetup()
        {
            if (Application.isPlaying) return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded) return;

            var audioManager = Object.FindAnyObjectByType<AudioManager>();
            if (audioManager == null) return;

            GameObject mgrObj = audioManager.gameObject;
            SerializedObject so = new SerializedObject(audioManager);
            bool modified = false;

            // 1. Music Tracks
            if (AssignClipProperty(so, "forestMusic", ForestMusicPath)) modified = true;
            if (AssignClipProperty(so, "canyonMusic", CanyonMusicPath)) modified = true;
            if (AssignClipProperty(so, "frozenMusic", FrozenMusicPath)) modified = true;
            if (AssignClipProperty(so, "sanctuaryMusic", SanctuaryMusicPath)) modified = true;
            if (AssignClipProperty(so, "victoryMusic", VictoryMusicPath)) modified = true;

            // 2. Shared SFX
            if (AssignClipProperty(so, "coinPickupSfx", CoinPickupPath)) modified = true;
            if (AssignClipProperty(so, "relicPickupSfx", RelicPickupPath)) modified = true;
            if (AssignClipProperty(so, "magicShootSfx", MagicShootPath)) modified = true;
            if (AssignClipProperty(so, "magicHitSfx", MagicHitPath)) modified = true;
            if (AssignClipProperty(so, "playerHurtSfx", PlayerHurtPath)) modified = true;
            if (AssignClipProperty(so, "enemyDefeatSfx", EnemyDefeatPath)) modified = true;
            if (AssignClipProperty(so, "portalTeleportSfx", PortalTeleportPath)) modified = true;
            if (AssignClipProperty(so, "puzzleChimeSfx", PuzzleChimePath)) modified = true;
            if (AssignClipProperty(so, "buttonClickSfx", ButtonClickPath)) modified = true;

            // 3. Biome Footsteps
            if (AssignClipProperty(so, "forestFootstepSfx", ForestFootstepPath)) modified = true;
            if (AssignClipProperty(so, "canyonFootstepSfx", CanyonFootstepPath)) modified = true;
            if (AssignClipProperty(so, "frozenFootstepSfx", FrozenFootstepPath)) modified = true;
            if (AssignClipProperty(so, "sanctuaryFootstepSfx", SanctuaryFootstepPath)) modified = true;

            // 4. AudioSources: Ensure AudioSource components exist on AudioManager GameObject
            AudioSource[] sources = mgrObj.GetComponents<AudioSource>();
            AudioSource srcA = sources.Length > 0 ? sources[0] : Undo.AddComponent<AudioSource>(mgrObj);
            AudioSource srcB = sources.Length > 1 ? sources[1] : Undo.AddComponent<AudioSource>(mgrObj);
            AudioSource srcSfx = sources.Length > 2 ? sources[2] : Undo.AddComponent<AudioSource>(mgrObj);
            AudioSource srcStep = sources.Length > 3 ? sources[3] : Undo.AddComponent<AudioSource>(mgrObj);

            srcA.loop = true;
            srcA.playOnAwake = false;
            srcA.volume = 0.3f;

            srcB.loop = true;
            srcB.playOnAwake = false;
            srcB.volume = 0f;

            srcSfx.loop = false;
            srcSfx.playOnAwake = false;
            srcSfx.volume = 0.9f;

            srcStep.loop = false;
            srcStep.playOnAwake = false;
            srcStep.volume = 0.45f;

            var propA = so.FindProperty("musicSourceA");
            var propB = so.FindProperty("musicSourceB");
            var propSfx = so.FindProperty("sfxSource");
            var propStep = so.FindProperty("footstepSource");

            if (propA != null && propA.objectReferenceValue != srcA) { propA.objectReferenceValue = srcA; modified = true; }
            if (propB != null && propB.objectReferenceValue != srcB) { propB.objectReferenceValue = srcB; modified = true; }
            if (propSfx != null && propSfx.objectReferenceValue != srcSfx) { propSfx.objectReferenceValue = srcSfx; modified = true; }
            if (propStep != null && propStep.objectReferenceValue != srcStep) { propStep.objectReferenceValue = srcStep; modified = true; }

            if (modified)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(audioManager);
                EditorUtility.SetDirty(mgrObj);
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("<color=green>[AudioManagerSetup] Successfully filled all missing audio clips and wired AudioSources in AudioManager!</color>");
            }
        }

        private static bool AssignClipProperty(SerializedObject so, string propertyName, string assetPath)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null) return false;

            if (prop.objectReferenceValue == null)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null)
                {
                    prop.objectReferenceValue = clip;
                    return true;
                }
            }
            return false;
        }
    }
}
#endif
