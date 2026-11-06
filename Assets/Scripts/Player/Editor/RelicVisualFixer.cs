#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RelicTrail.Core;
using RelicTrail.Progression;

namespace RelicTrail.EditorTools
{
    [InitializeOnLoad]
    public static class RelicVisualFixer
    {
        private const string ForestGemPath = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/HeartGem.prefab";
        private const string CanyonGemPath = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/SphereGem.prefab";
        private const string FrozenGemPath = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/StarGem1.prefab";
        private const string RestoredRelicPath = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/DoubleDiamond.prefab";

        private const string ForestMatPath = "Assets/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Green.mat";
        private const string CanyonMatPath = "Assets/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Orange.mat";
        private const string FrozenMatPath = "Assets/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Teal.mat";
        private const string RestoredMatPath = "Assets/BTM_Assets/BTM_Items_Gems/Materials/BaseMaterial_Yellow.mat";

        static RelicVisualFixer()
        {
            EditorApplication.delayCall += RunAutoUpgrade;
        }

        [MenuItem("Relic Trail/Upgrade Relics to BTM Gems")]
        public static void RunAutoUpgrade()
        {
            if (Application.isPlaying) return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded) return;

            bool sceneModified = false;

            // 1. Upgrade all RelicFragment components in the scene
            var relics = Object.FindObjectsByType<RelicFragment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var relic in relics)
            {
                if (relic == null) continue;

                SerializedObject so = new SerializedObject(relic);
                var biomeProp = so.FindProperty("biome");
                BiomeType biome = (BiomeType)(biomeProp != null ? biomeProp.enumValueIndex : 0);

                string prefabPath = CanyonGemPath;
                string matPath = CanyonMatPath;
                if (biome == BiomeType.ForgottenForest)
                {
                    prefabPath = ForestGemPath;
                    matPath = ForestMatPath;
                }
                else if (biome == BiomeType.ScorchedCanyon)
                {
                    prefabPath = CanyonGemPath;
                    matPath = CanyonMatPath;
                }
                else if (biome == BiomeType.FrozenRuins)
                {
                    prefabPath = FrozenGemPath;
                    matPath = FrozenMatPath;
                }

                if (ApplyGemModelToObject(relic.gameObject, prefabPath, matPath, 0.75f))
                {
                    sceneModified = true;
                }
            }

            // 2. Upgrade FinalRelicAltar sockets and complete relic
            var altars = Object.FindObjectsByType<FinalRelicAltar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var altar in altars)
            {
                if (altar == null) continue;
                SerializedObject aso = new SerializedObject(altar);

                var s1 = aso.FindProperty("forestRelicSocket")?.objectReferenceValue as GameObject;
                var s2 = aso.FindProperty("canyonRelicSocket")?.objectReferenceValue as GameObject;
                var s3 = aso.FindProperty("frozenRelicSocket")?.objectReferenceValue as GameObject;
                var restored = aso.FindProperty("completeLostRelic")?.objectReferenceValue as GameObject;

                if (s1 != null && ApplyGemModelToObject(s1, ForestGemPath, ForestMatPath, 0.45f)) sceneModified = true;
                if (s2 != null && ApplyGemModelToObject(s2, CanyonGemPath, CanyonMatPath, 0.45f)) sceneModified = true;
                if (s3 != null && ApplyGemModelToObject(s3, FrozenGemPath, FrozenMatPath, 0.45f)) sceneModified = true;
                if (restored != null && ApplyGemModelToObject(restored, RestoredRelicPath, RestoredMatPath, 0.9f)) sceneModified = true;
            }

            if (sceneModified)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("<color=green>[RelicVisualFixer] Successfully upgraded all scene Relics & Altar Sockets to authentic BTM_Assets 3D Gems!</color>");
            }
        }

        private static bool ApplyGemModelToObject(GameObject parentObj, string prefabPath, string matPath, float scale)
        {
            if (parentObj == null) return false;

            bool changed = false;

            // Hide placeholder primitive sphere renderer on parent
            var mr = parentObj.GetComponent<MeshRenderer>();
            if (mr != null && mr.enabled)
            {
                Undo.RecordObject(mr, "Disable Primitive Renderer");
                mr.enabled = false;
                changed = true;
            }

            // Check if gem child already exists
            Transform existing = parentObj.transform.Find("Gem_Model");
            if (existing != null)
            {
                return changed;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ForestGemPath);
            }

            if (prefab != null)
            {
                GameObject gem = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parentObj.transform);
                if (gem == null) gem = Object.Instantiate(prefab, parentObj.transform);

                Undo.RegisterCreatedObjectUndo(gem, "Instantiate BTM Gem Model");
                gem.name = "Gem_Model";
                gem.transform.localPosition = Vector3.zero;
                gem.transform.localRotation = Quaternion.identity;
                gem.transform.localScale = Vector3.one * scale;

                // Remove child colliders
                foreach (var c in gem.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(c);
                }

                // Disable SimpleGemsAnim
                foreach (var anim in gem.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (anim != null && anim.GetType().Name == "SimpleGemsAnim")
                    {
                        anim.enabled = false;
                    }
                }

                // Apply material
                if (!string.IsNullOrEmpty(matPath))
                {
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat != null)
                    {
                        foreach (var r in gem.GetComponentsInChildren<Renderer>())
                        {
                            r.sharedMaterial = mat;
                        }
                    }
                }

                changed = true;
            }

            return changed;
        }
    }
}
#endif
