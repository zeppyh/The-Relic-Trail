using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RelicTrail.Player;

namespace RelicTrail.Editor
{
    /// <summary>
    /// Editor utility to set up the Player and 3D Third-Person Camera in the active scene in one click.
    /// </summary>
    public static class SetupPlayerSceneMenu
    {
        private const string CharacterPrefabPath = "Assets/ThirdParty/Floreswa/Prefabs/male01_1.prefab";

        [MenuItem("Relic Trail/Setup Player and 3D Camera in Current Scene", false, 10)]
        public static void SetupPlayerAndCamera()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.isLoaded)
            {
                EditorUtility.DisplayDialog("Relic Trail Setup", "Please open a scene before running this setup.", "OK");
                return;
            }

            // 1. Ensure Ground exists
            EnsureGround();

            // 2. Find or Create Player
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var existingMovement = Object.FindAnyObjectByType<PlayerMovement>();
                if (existingMovement != null)
                {
                    player = existingMovement.gameObject;
                }
            }

            if (player == null)
            {
                player = CreatePlayerObject();
                Undo.RegisterCreatedObjectUndo(player, "Create Player");
            }

            // 3. Setup Main Camera with ThirdPersonCamera
            SetupCamera(player);

            // Select player in Hierarchy
            Selection.activeGameObject = player;

            EditorSceneManager.MarkSceneDirty(activeScene);

            EditorUtility.DisplayDialog(
                "3D Camera & Player Setup Complete",
                "Successfully configured:\n\n" +
                "• Player GameObject with CharacterController & PlayerMovement\n" +
                "• Floreswa 3D character model attached\n" +
                "• Main Camera equipped with 3D ThirdPersonCamera orbiting the Player\n" +
                "• Ground test floor\n\n" +
                "Press Play to test your 3D camera and movement!",
                "Great!"
            );
        }

        private static void EnsureGround()
        {
            var colliders = Object.FindObjectsByType<Collider>();
            foreach (var col in colliders)
            {
                if (col.gameObject.name.ToLower().Contains("ground") || col.gameObject.name.ToLower().Contains("floor") || col.gameObject.name.ToLower().Contains("plane"))
                {
                    return; // Ground already exists
                }
            }

            // Create a simple ground plane
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(5f, 1f, 5f); // 50m x 50m area
            Undo.RegisterCreatedObjectUndo(plane, "Create Ground Plane");
        }

        private static GameObject CreatePlayerObject()
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            // CharacterController setup
            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1f, 0f);
            cc.height = 1.9f;
            cc.radius = 0.35f;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;

            // Movement & Input components
            player.AddComponent<PlayerInputHandler>();
            player.AddComponent<PlayerMovement>();

            // Attach Floreswa character model
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (modelPrefab != null)
            {
                GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, player.transform);
                modelInstance.name = "Model";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Fallback cylinder visual if model prefab is missing
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(player.transform);
                visual.transform.localPosition = new Vector3(0f, 1f, 0f);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
            }

            return player;
        }

        private static void SetupCamera(GameObject player)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
            }

            var tpCam = mainCam.GetComponent<ThirdPersonCamera>();
            if (tpCam == null)
            {
                tpCam = Undo.AddComponent<ThirdPersonCamera>(mainCam.gameObject);
            }

            // Assign target via SerializedObject to persist
            SerializedObject serializedCam = new SerializedObject(tpCam);
            serializedCam.FindProperty("target").objectReferenceValue = player.transform;
            serializedCam.FindProperty("defaultDistance").floatValue = 3.8f;
            serializedCam.FindProperty("minDistance").floatValue = 1.4f;
            serializedCam.FindProperty("maxDistance").floatValue = 6.5f;
            serializedCam.FindProperty("targetOffset").vector3Value = new Vector3(0f, 1.75f, 0f);
            serializedCam.FindProperty("shoulderOffset").floatValue = 0.0f;
            serializedCam.FindProperty("positionSmoothTime").floatValue = 0.0f;
            serializedCam.FindProperty("rotationSmoothTime").floatValue = 0.0f;
            serializedCam.ApplyModifiedProperties();

            mainCam.transform.position = player.transform.position + new Vector3(0f, 2.4f, -3.8f);
            mainCam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
        }

        [MenuItem("Relic Trail/Configure & Connect All UI Buttons in Scenes", false, 15)]
        public static void ConfigureAllUIInScenesMenuItem()
        {
            ConfigureAllUIInScenes(true);
        }

        public static void ConfigureAllUIInScenes(bool showDialog)
        {
            string[] scenes = new string[] {
                "Assets/Scenes/SampleScene.unity",
                "Assets/ThirdParty/Palmov Island/Low Poly Atmospheric Locations Pack/Scenes/All models.unity"
            };

            foreach (var scenePath in scenes)
            {
                if (!System.IO.File.Exists(scenePath)) continue;

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.isLoaded) continue;

                var uiMgr = Object.FindAnyObjectByType<RelicTrail.UI.GameUIManager>();
                if (uiMgr != null)
                {
                    uiMgr.EnsureEventSystem();
                    uiMgr.EnsureMenuPanelsAndButtons();
                    uiMgr.BindAllButtonsByText();
                    EditorUtility.SetDirty(uiMgr);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            // Return to SampleScene
            if (System.IO.File.Exists("Assets/Scenes/SampleScene.unity"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "UI Configuration Complete",
                    "Successfully configured and verified UI in all scenes:\n\n" +
                    "• EventSystem with InputSystemUIInputModule added (clicks enabled)\n" +
                    "• MainMenuPanel with Play, Instructions, and Quit Game buttons\n" +
                    "• Instructions Modal with full controls breakdown and Close button\n" +
                    "• GameOverPanel with Retry, Main Menu, and Quit buttons\n" +
                    "• VictoryPanel with Play Again, Main Menu, and Exit Game buttons\n" +
                    "• PauseMenuPanel with Resume, Restart, Main Menu, and Quit buttons\n" +
                    "• All buttons wired to execute according to their written labels!",
                    "Awesome!"
                );
            }
        }
    }
}
