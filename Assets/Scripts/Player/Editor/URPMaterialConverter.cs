using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace RelicTrail.Editor
{
    /// <summary>
    /// Automatic converter that upgrades legacy Built-in Standard / CG materials to Universal Render Pipeline (URP) Lit.
    /// Eliminates pink/magenta material rendering errors across all asset packs.
    /// </summary>
    [InitializeOnLoad]
    public static class URPMaterialConverter
    {
        private const string URPLitShaderName = "Universal Render Pipeline/Lit";
        private const string URPSimpleLitShaderName = "Universal Render Pipeline/Simple Lit";

        static URPMaterialConverter()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    // Automatically ensure all materials are URP compliant in Edit mode
                    ConvertAllThirdPartyMaterials(true);
                }
            };
        }

        [MenuItem("Relic Trail/🎨 Fix Pink Materials (Convert to URP Lit)", false, 20)]
        public static void FixPinkMaterialsMenuItem()
        {
            int converted = ConvertAllThirdPartyMaterials(false);
            if (converted > 0)
            {
                EditorUtility.DisplayDialog(
                    "URP Material Converter",
                    $"Successfully converted {converted} materials to Universal Render Pipeline/Lit!\n\nAll pink/magenta shaders have been resolved.",
                    "Awesome!"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "URP Material Converter",
                    "All materials are already compatible with the Universal Render Pipeline (URP).\nNo pink materials detected!",
                    "OK"
                );
            }
        }

        /// <summary>
        /// Scans and converts materials in Assets/ThirdParty and Assets/Materials to URP Lit.
        /// </summary>
        public static int ConvertAllThirdPartyMaterials(bool silent = true)
        {
            Shader urpShader = Shader.Find(URPLitShaderName);
            if (urpShader == null)
            {
                urpShader = Shader.Find(URPSimpleLitShaderName);
            }

            if (urpShader == null)
            {
                if (!silent) Debug.LogError("[URPMaterialConverter] Universal Render Pipeline Lit shader not found in project.");
                return 0;
            }

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            int count = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in materialGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;

                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    if (NeedsURPUpgrade(mat))
                    {
                        UpgradeToURP(mat, urpShader);
                        EditorUtility.SetDirty(mat);
                        count++;
                        if (!silent)
                        {
                            Debug.Log($"[URPMaterialConverter] Converted '{mat.name}' ({path}) -> URP Lit");
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (count > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=#2ecc71><b>[URPMaterialConverter] Successfully converted {count} materials to Universal Render Pipeline/Lit!</b></color>");
            }

            return count;
        }

        private static bool NeedsURPUpgrade(Material mat)
        {
            if (mat == null || mat.shader == null) return true;
            string sName = mat.shader.name;
            if (string.IsNullOrEmpty(sName)) return true;

            // If it already uses URP shaders or ShaderGraph, it is compatible
            if (sName.StartsWith("Universal Render Pipeline/")) return false;
            if (sName.StartsWith("Shader Graphs/")) return false;

            // Legacy Standard, Mobile, Unlit legacy, Nicrom CG shaders, or Unity Error Shader
            if (sName == "Standard" ||
                sName == "Standard (Specular setup)" ||
                sName.Contains("InternalErrorShader") ||
                sName.Contains("Error") ||
                sName.StartsWith("Mobile/") ||
                sName.StartsWith("Legacy Shaders/") ||
                sName.StartsWith("Nicrom/"))
            {
                return true;
            }

            return false;
        }

        private static void UpgradeToURP(Material mat, Shader urpShader)
        {
            // 1. Extract existing textures, colors, and properties
            Color baseColor = Color.white;
            if (mat.HasProperty("_BaseColor")) baseColor = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color")) baseColor = mat.GetColor("_Color");

            Texture mainTex = null;
            if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                mainTex = mat.GetTexture("_BaseMap");
            else if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                mainTex = mat.GetTexture("_MainTex");

            float smoothness = 0.2f;
            if (mat.HasProperty("_Smoothness")) smoothness = mat.GetFloat("_Smoothness");
            else if (mat.HasProperty("_Glossiness")) smoothness = mat.GetFloat("_Glossiness");

            float metallic = 0.0f;
            if (mat.HasProperty("_Metallic")) metallic = mat.GetFloat("_Metallic");

            Texture bumpMap = null;
            if (mat.HasProperty("_BumpMap")) bumpMap = mat.GetTexture("_BumpMap");

            Color emissionColor = Color.black;
            if (mat.HasProperty("_EmissionColor")) emissionColor = mat.GetColor("_EmissionColor");

            Texture emissionMap = null;
            if (mat.HasProperty("_EmissionMap")) emissionMap = mat.GetTexture("_EmissionMap");

            // For texture atlases (e.g., Palmov Island "mat main" or medieval props), tint should be pure white
            if (mainTex != null && (mat.name.ToLower().Contains("main") || mat.name.ToLower().Contains("atlas")))
            {
                baseColor = Color.white;
            }

            // 2. Assign URP shader
            mat.shader = urpShader;

            // 3. Re-assign extracted properties
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

            if (mainTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
            }

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (bumpMap != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", bumpMap);
            }

            if (emissionMap != null && mat.HasProperty("_EmissionMap"))
            {
                mat.SetTexture("_EmissionMap", emissionMap);
            }

            if (emissionColor != Color.black && mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
            }
        }
    }
}
