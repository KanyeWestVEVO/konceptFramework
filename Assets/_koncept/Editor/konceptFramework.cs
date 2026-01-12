using koncept.SceneManagement;
using koncept.UIManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace koncept.Tools
{
    /// <summary>
    /// Editor window that allows configuration of the koncept Framework.
    /// </summary>
    public class konceptFramework : EditorWindow
    {
        // The root of growth
        public koncept app;
        static konceptFramework _konceptGameObject;

        // App Creation Properties
        string newAppName = "Hello World";

        // Tool States & Properties
        [SerializeField] Manager[] attachedManagers;
        [SerializeField] List<string> associatedUIs = new List<string>();
        [SerializeField] List<string> associatedScenes = new List<string>();
        Vector2 scrollPos;
        SerializedObject serializedObject;
        SerializedProperty _value;
        public enum ManagementType { Manager, Scene, UI }
        ManagementType selectedManagerType = ManagementType.Manager;

        public ManagementType SelectedManagerType
        {
            get
            {
                return selectedManagerType;
            }
            set
            {
                selectedManagerType = value;

                switch (selectedManagerType)
                {
                    case ManagementType.Manager:
                        uiRemovalConfirm = false;
                        break;
                    case ManagementType.Scene:
                        uiRemovalConfirm = false;
                        managerRemovalConfirm = false;
                        break;
                    case ManagementType.UI:
                        managerRemovalConfirm = false;
                        break;
                }
            }
        }

        static bool compileNeeded = false;
        bool buildWaiting = false;
        bool uiBuildWaiting = false;
        bool sceneBuildWaiting = false;

        private void Awake()
        {
            if (_konceptGameObject == null)
            {
                _konceptGameObject = this;
            }
            else
            {
                Destroy(this);
            }
        }

        [MenuItem("koncept/Framework")]
        static void Init()
        {
            konceptFramework window = GetWindow<konceptFramework>("koncept Framework");

            if (window == null)
                return;

            window.minSize = new Vector2(512, 200);
            window.autoRepaintOnSceneChange = true;

            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += SelectionChanged;
            SelectedManagerType = ManagementType.Manager;

            // Link the Serialized Property to the variable
            try
            {
                serializedObject = new SerializedObject(_konceptGameObject);
                _value = serializedObject.FindProperty("value");
            }
            catch
            {
                // Do fucking nothing
            }
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= SelectionChanged;
        }

        private void OnInspectorUpdate()
        {
            if (!compileNeeded)
            {
                compileNeeded = EditorApplication.isCompiling;

                if (!EditorApplication.isCompiling)
                {
                    if (buildWaiting)
                    {
                        SelectedManagerType = ManagementType.Manager;
                        AddManagerToPrefab(filterManagerName);
                        buildWaiting = false;
                    }

                    if (uiBuildWaiting)
                    {
                        SelectedManagerType = ManagementType.UI;
                        CreateUIPrefab(filterUIName);
                        uiBuildWaiting = false;
                    }

                    if (sceneBuildWaiting)
                    {
                        SelectedManagerType = ManagementType.Scene;
                        ApplyFrameworkPrefabToNewSceneRoot();
                        sceneBuildWaiting = false;
                    }
                }
            }

            // Check which Managers are attached to this koncept Framework App
            if (app != null)
            {
                attachedManagers = FindObjectsByType<Manager>(FindObjectsSortMode.None);
                app.SetManagers(attachedManagers);

                UpdateAssociatedUIs(Application.dataPath + "/" + app.appName + "/Prefabs/UI/");
                UpdateAssociatedScenes(Application.dataPath + "/" + app.appName + "/Scenes/");

                if (!Helpers.IsStringBlank(selectedUI))
                {
                    try
                    {
                        string selectedUIPath = Application.dataPath + "/" + app.appName + "/Prefabs/UIData/" + selectedUI + "/";
                        DirectoryInfo dir = new DirectoryInfo(selectedUIPath);
                        FileInfo[] info = dir.GetFiles("*.*");
                        List<string> uiDataNames = new List<string>();

                        foreach (FileInfo f in info)
                        {
                            string fileName = f.ToString().Split('\\').Last();

                            if (!fileName.Contains(".meta"))
                            {
                                fileName = fileName.Substring(0, fileName.Length - 6);
                                uiDataNames.Add(fileName);
                            }
                        }

                        UIDatas = uiDataNames.ToArray();
                    }
                    catch
                    {
                        // Do fucking nothing
                    }
                }

                // Update the EditorWindow even when it is not in focus
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                serializedObject.Update();
            }

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box((Texture)Resources.Load("konceptFrameworkLogo", typeof(Texture)));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            if (compileNeeded)
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label("Waiting on Editor Recompile - PLEASE DO NOT CLOSE THIS WINDOW!\n\nWait a couple of seconds or click anywhere within the Projects window to force recompile.", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;
                return;
            }

            // Check if koncept Framework exists in the currently opened scene
            app = FindFirstObjectByType<koncept>();

            if (app == null)
            {
                // If koncept Framework is not found, allow the user to create and initialize a new koncept App
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.red;
                GUILayout.Label("koncept Framework not detected in this scene!", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;

                GUILayout.Label("\nIf you have not yet created a koncept Application, you can do so using the form below.", EditorStyles.wordWrappedLabel);
                newAppName = EditorGUILayout.TextField("App Name: ", newAppName);

                if (GUILayout.Button("Generate New koncept App & Directories"))
                {
                    CreateNewkonceptApp(newAppName);
                }
            }
            else
            {
                // Manage the state when a koncept Framework application is found
                GUILayout.Label("koncept Framework Application - " + app.appName, EditorStyles.largeLabel);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Locate Framework Instance"))
                {
                    EditorGUIUtility.PingObject(app);
                    Selection.SetActiveObjectWithContext(app, app);
                }

                if (GUILayout.Button("Open " + app.appName + " Framework Prefab"))
                {
                    string prefabVariantPath = "Assets/" + app.appName + "/Resources/_koncept-" + app.appName + ".prefab";
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath(prefabVariantPath, typeof(GameObject)));
                }

                GUILayout.EndHorizontal();

                if (app != null)
                {
                    if (GUILayout.Button("Save Instance Changes to Framework Prefab"))
                    {
                        PrefabUtility.ApplyPrefabInstance(app.gameObject, InteractionMode.AutomatedAction);
                    }
                }

                DrawUILine(Color.gray);

                SelectedManagerType = (ManagementType)EditorGUILayout.EnumPopup("Filter by Module: ", SelectedManagerType);

                DrawUILine(Color.gray);

                switch (SelectedManagerType)
                {
                    case ManagementType.Manager:
                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                        if (attachedManagers == null || attachedManagers.Length <= 0)
                        {
                            Color cachedColor = GUI.contentColor;
                            GUI.contentColor = Color.red;
                            GUILayout.Label("No Manager(s) found in this scene!", EditorStyles.wordWrappedLabel);
                            GUI.contentColor = cachedColor;
                        }
                        else
                        {
                            HandleManagers();
                        }

                        EditorGUILayout.EndScrollView();
                        break;
                    case ManagementType.Scene:
                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                        if (app.GetManager<SceneManager>() == null)
                        {
                            Color cachedColor = GUI.contentColor;
                            GUI.contentColor = Color.red;
                            GUILayout.Label("koncept.SceneManagement.SceneManager not found on this app's Framework Prefab!", EditorStyles.wordWrappedLabel);
                            GUI.contentColor = cachedColor;
                        }
                        else
                        {
                            HandleScenes();
                        }

                        EditorGUILayout.EndScrollView();
                        break;
                    case ManagementType.UI:
                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                        if (app.GetManager<UIManager>() == null)
                        {
                            Color cachedColor = GUI.contentColor;
                            GUI.contentColor = Color.red;
                            GUILayout.Label("koncept.UI.UIManager not found on this app's Framework Prefab!", EditorStyles.wordWrappedLabel);
                            GUI.contentColor = cachedColor;
                        }
                        else
                        {
                            HandleUI();
                        }

                        EditorGUILayout.EndScrollView();
                        break;
                }

                if (serializedObject != null && serializedObject.targetObject != null)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        #region Manager Configuration

        string filterManagerName = "NewManager";

        void HandleManagers()
        {
            // Show all managers assigned to this app
            GUILayout.Label(string.Format("Attached Managers [{0}]", attachedManagers.Length), EditorStyles.largeLabel);

            if (attachedManagers.Length > 0)
            {
                for (int i = attachedManagers.Length - 1; i >= 0; i--)
                {
                    Manager m = attachedManagers[i];

                    if (m != null)
                    {
                        string managerName = m.GetType().ToString().Split('.').Last();

                        if (GUILayout.Button(managerName))
                        {
                            EditorGUIUtility.PingObject(m);
                            Selection.SetActiveObjectWithContext(m, m);
                            filterManagerName = managerName;
                        }
                    }
                }
            }
            else
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.red;
                GUILayout.Label("No Managers detected on Framework Prefab!", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;
            }

            if (GUILayout.Button("Add New Manager [+]"))
            {
                filterManagerName = "NewManager";
            }

            DrawUILine(Color.gray);

            // Handle manager creation and management
            filterManagerName = EditorGUILayout.TextField("Filter by Manager Name: ", filterManagerName);
            filterManagerName = Helpers.RemoveSpaces(filterManagerName);

            string managerScriptPath = Application.dataPath + "/" + app.appName + "/Scripts/Managers/" + filterManagerName + ".cs";
            if (Helpers.IsStringBlank(filterManagerName))
            {
                GUILayout.Label("Type into the field above to get more information about Managers.", EditorStyles.wordWrappedMiniLabel);
            }
            else if (filterManagerName == "SceneManager" || filterManagerName == "UIManager")
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label(filterManagerName + " is protected as part of koncept namespace.  Try another Manager name.", EditorStyles.wordWrappedMiniLabel);
                GUI.contentColor = cachedColor;
            }
            else if (GameObject.Find(filterManagerName) != null)
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.green;
                GUILayout.Label(filterManagerName + " component found in scene and attached to Framework Prefab.", EditorStyles.wordWrappedMiniLabel);
                GUI.contentColor = cachedColor;
            }
            else if (!File.Exists(managerScriptPath))
            {
                GUILayout.Label(filterManagerName + " can be used as a new Manager name.", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Create " + filterManagerName + ".cs [+]"))
                {
                    CreateNewManager(filterManagerName);
                }
            }
            else if (File.Exists(managerScriptPath))
            {
                GUILayout.Label(filterManagerName + " script was found but is not currently attached to this app's framework.", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Add " + filterManagerName + " to Framework Prefab [+]"))
                {
                    AddManagerToPrefab(filterManagerName);
                }

                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                if (filterManagerName != "UIManager" && filterManagerName != "SceneManager")
                {
                    if (!managerScriptRemovalConfirm)
                    {
                        if (GUILayout.Button("Delete " + filterManagerName + ".cs from Unity Project [-]"))
                        {
                            managerScriptRemovalConfirm = true;
                        }
                    }
                    else if (managerScriptRemovalConfirm)
                    {
                        if (GUILayout.Button("Click again to confirm deletion"))
                        {
                            string managerScriptPathToDelete = Application.dataPath + "/" + app.appName + "/Scripts/Managers/" + filterManagerName + ".cs";
                            File.Delete(managerScriptPathToDelete);
                            File.Delete(managerScriptPathToDelete + ".meta");

                            Debug.Log("koncept.CodeGen - File deleted: " + managerScriptPathToDelete);
                            AssetDatabase.Refresh();
                            compileNeeded = true;
                        }
                    }
                }
                GUI.contentColor = cachedColor;
            }

            DrawUILine(Color.gray);

            // Handle active selection
            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Manager>() != null)
            {
                HandleManagerComponentsSelection();
                Debug.ClearDeveloperConsole();
            }
            else
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label("Select a koncept Manager above to access its properties.", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;
            }
        }

        string filterSceneName = "NewScene";
        string selectedScene;

        private void HandleScenes()
        {
            // Show all scenes assigned to this app
            GUILayout.Label(string.Format("Associated Scenes [{0}]", associatedScenes.Count), EditorStyles.largeLabel);

            if (associatedScenes.Count > 0)
            {
                for (int i = associatedScenes.Count - 1; i >= 0; i--)
                {
                    string s = associatedScenes[i];

                    if (s != null)
                    {

                        if (GUILayout.Button(s))
                        {
                            filterSceneName = s;
                            selectedScene = s;
                        }
                    }
                }

                if (GUILayout.Button("Add New Scene [+]"))
                {
                    filterSceneName = "NewScene";
                }
            }
            else
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label("No Scenes have been created for this app yet.  You can start generating some below!", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;
            }

            DrawUILine(Color.gray);

            // Handle scene creation and management
            filterSceneName = EditorGUILayout.TextField("Scene Name: ", filterSceneName);

            string uiScenePath = Application.dataPath + "/" + app.appName + "/Scenes/" + filterSceneName + ".unity";
            if (Helpers.IsStringBlank(filterSceneName))
            {
                GUILayout.Label("Type into the field above to get more information about Scenes.", EditorStyles.wordWrappedMiniLabel);
            }
            if (File.Exists(uiScenePath))
            {
                Color cachedColor = GUI.contentColor;

                if (filterSceneName == "_koncept-" + app.appName)
                {
                    GUI.contentColor = Color.yellow;
                    GUILayout.Label(filterSceneName + " is protected as part of koncept namespace.  Try another Scene name.", EditorStyles.wordWrappedMiniLabel);
                    GUI.contentColor = cachedColor;
                }
                else
                {
                    GUI.contentColor = Color.green;
                    GUILayout.Label(filterSceneName + " already exists within the app's Framework.", EditorStyles.wordWrappedMiniLabel);
                    GUI.contentColor = cachedColor;

                    GUI.contentColor = Color.yellow;
                    if (!sceneRemovalConfirm)
                    {
                        if (GUILayout.Button("Delete " + filterSceneName + " from Unity Project [-]"))
                        {
                            sceneRemovalConfirm = true;
                        }
                    }
                    else if (sceneRemovalConfirm)
                    {
                        if (GUILayout.Button("Click again to confirm deletion"))
                        {
                            string sceneToDelete = Application.dataPath + "/" + app.appName + "/Scenes/" + filterSceneName + ".unity";
                            File.Delete(sceneToDelete);
                            File.Delete(sceneToDelete + ".meta");

                            Debug.Log("koncept.CodeGen - Scene deleted at " + sceneToDelete);
                            AssetDatabase.Refresh();
                            sceneRemovalConfirm = false;

                            UpdateAssociatedScenes(Application.dataPath + "/" + app.appName + "/Scenes/");
                            UpdateSceneBuildSettings();
                        }
                    }
                    GUI.contentColor = cachedColor;
                }
            }
            else if (!File.Exists(uiScenePath))
            {
                GUILayout.Label(filterSceneName + " can be created as a new Scene.", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Create " + filterSceneName + " Scene [+]"))
                {
                    CreateScene(uiScenePath);
                }
            }

            DrawUILine(Color.gray);

            // Handle active selection
            if (!Helpers.IsStringBlank(selectedScene))
            {
                HandleSceneSelection(selectedScene);
            }
            else
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label(associatedScenes.Count > 0 ? "Select a Scene above to access its properties." : "No Scenes to configure.", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;
            }

        }

        string filterUIName = "NewUI";
        string selectedUI;

        private void HandleUI()
        {
            // Show all UIs associated with this app
            GUILayout.Label(string.Format("Associated UIs [{0}]", associatedUIs.Count), EditorStyles.largeLabel);

            if (associatedUIs.Count > 0)
            {
                for (int i = associatedUIs.Count - 1; i >= 0; i--)
                {
                    string u = associatedUIs[i];

                    if (u != null)
                    {

                        if (GUILayout.Button(u))
                        {
                            selectedUI = u;
                            filterUIName = u;
                        }
                    }
                }

                if (GUILayout.Button("Add New UI [+]"))
                {
                    filterUIName = "NewUI";
                }
            }
            else
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label("No UIs have been created for this app yet.  You can start generating some below!", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;
            }

            DrawUILine(Color.gray);

            // Handle UIData/UI/Prefab creation and management
            filterUIName = EditorGUILayout.TextField("Filter by UI Name: ", filterUIName);
            filterUIName = Helpers.RemoveSpaces(filterUIName);

            string uiScriptPath = Application.dataPath + "/" + app.appName + "/Scripts/UI/" + filterUIName + ".cs";
            string uiPrefabPath = Application.dataPath + "/" + app.appName + "/Prefabs/UI/" + filterUIName + ".prefab";
            if (Helpers.IsStringBlank(filterUIName))
            {
                GUILayout.Label("Type into the field above to get more information about UIs.", EditorStyles.wordWrappedMiniLabel);
            }
            else if (File.Exists(uiScriptPath))
            {
                if (!File.Exists(uiPrefabPath))
                {
                    GUILayout.Label(filterUIName + " script was found but a Prefab could not be found that associates with it.", EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("Create " + filterUIName + " Prefab [+]"))
                    {
                        CreateUIPrefab(filterUIName);
                    }
                }
                else
                {
                    Color cachedColor = GUI.contentColor;
                    GUI.contentColor = Color.green;
                    GUILayout.Label(filterUIName + " is already associated with this app's Framework.", EditorStyles.wordWrappedMiniLabel);
                    GUI.contentColor = cachedColor;

                    if (GUILayout.Button("Open " + filterUIName + " Prefab"))
                    {
                        string prefabPath = "Assets/" + app.appName + "/Prefabs/UI/" + filterUIName + ".prefab";
                        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)));
                    }

                    if (GUILayout.Button("Edit " + filterUIName + ".cs [#]"))
                    {
                        Helpers.OpenComponentInVisualStudioIDE(filterUIName, app.appName);
                    }

                    GUI.contentColor = Color.yellow;
                    if (!uiRemovalConfirm)
                    {
                        if (GUILayout.Button("Delete " + filterUIName + " from Unity Project [-]"))
                        {
                            uiRemovalConfirm = true;
                        }
                    }
                    else if (uiRemovalConfirm)
                    {
                        if (GUILayout.Button("Click again to confirm deletion"))
                        {
                            string uiScriptToDelete = Application.dataPath + "/" + app.appName + "/Scripts/UI/" + filterUIName + ".cs";
                            string uiSOToDelete = Application.dataPath + "/" + app.appName + "/Scripts/UI/ScriptableObjects/" + filterUIName + "Data.cs";
                            string uiPrefabToDelete = Application.dataPath + "/" + app.appName + "/Prefabs/UI/" + filterUIName + ".prefab";
                            File.Delete(uiPrefabToDelete);
                            File.Delete(uiPrefabToDelete + ".meta");
                            File.Delete(uiScriptToDelete);
                            File.Delete(uiScriptToDelete + ".meta");
                            File.Delete(uiSOToDelete);
                            File.Delete(uiSOToDelete + ".meta");
                            FileUtil.DeleteFileOrDirectory("Assets/" + app.appName + "/Prefabs/UIData/" + filterUIName + "/");
                            FileUtil.DeleteFileOrDirectory("Assets/" + app.appName + "/Prefabs/UIData/" + filterUIName + ".meta");

                            Debug.Log("koncept.CodeGen - Files deleted associated with " + uiScriptToDelete);
                            AssetDatabase.Refresh();
                            compileNeeded = true;
                        }
                    }
                    GUI.contentColor = cachedColor;
                }
            }
            else if (!File.Exists(filterUIName))
            {
                GUILayout.Label(filterUIName + " can be used as a new UI name.", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Create " + filterUIName + " [+]"))
                {
                    CreateNewUIPackage(filterUIName);
                }
            }

            DrawUILine(Color.gray);

            // Handle active selection
            string uiPath = Application.dataPath + "/" + app.appName + "/Prefabs/UI/" + selectedUI + ".prefab";
            if (!Helpers.IsStringBlank(selectedUI) && File.Exists(uiPath))
            {
                HandleUISelection(selectedUI);
            }
            else
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label(associatedUIs.Count > 0 ? "Select a UI above to access its properties." : "No UIs to configure.", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;
            }
        }

        #endregion

        #region CodeGen

        /// <summary>
        /// Creates an empty koncept App with the given name and generates a standard folder structure for it.
        /// </summary>
        /// <param name="appName">The name of the koncept App to create.</param>
        void CreateNewkonceptApp(string appName)
        {
            appName = Helpers.RemoveSpaces(appName);

            if (Helpers.IsStringBlank(appName))
            {
                Debug.LogWarning("koncept.CodeGen - Field 'App Name' is required.");
                return;
            }

            string appPath = Application.dataPath + "/" + appName + "/";
            if (Helpers.DoesDirectoryExist(appPath))
            {
                Debug.LogError("koncept.CodeGen - App directory already exists for " + appName + " at " + appPath + ".");
                return;
            }

            AssetDatabase.CreateFolder("Assets", appName);
            AssetDatabase.CreateFolder("Assets\\" + appName, "Resources");
            AssetDatabase.CreateFolder("Assets\\" + appName, "Scenes");
            AssetDatabase.CreateFolder("Assets\\" + appName, "Scripts");
            AssetDatabase.CreateFolder("Assets\\" + appName + "\\Scripts", "Managers");
            AssetDatabase.CreateFolder("Assets\\" + appName + "\\Scripts", "UI");
            AssetDatabase.CreateFolder("Assets\\" + appName + "\\Scripts\\UI", "ScriptableObjects");
            AssetDatabase.CreateFolder("Assets\\" + appName, "Prefabs");
            AssetDatabase.CreateFolder("Assets\\" + appName + "\\Prefabs", "UI");
            AssetDatabase.CreateFolder("Assets\\" + appName + "\\Prefabs", "UIData");
            AssetDatabase.CopyAsset("Assets/_koncept/Resources/Staging.unity", "Assets/" + appName + "/Scenes/_koncept-" + appName + ".unity");

            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene("Assets/" + appName + "/Scenes/_koncept-" + appName + ".unity");
            GameObject singletonPrefab = PrefabUtility.InstantiatePrefab(Resources.Load("_koncept")) as GameObject;
            singletonPrefab.GetComponent<koncept>().appName = appName;
            GameObject appVariant = PrefabUtility.SaveAsPrefabAsset(singletonPrefab, "Assets/" + appName + "/Resources/_koncept-" + appName + ".prefab");
            DestroyImmediate(singletonPrefab);
            PrefabUtility.InstantiatePrefab(Resources.Load("_koncept-" + appName));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            compileNeeded = false;
            AssetDatabase.Refresh();

            Debug.Log("koncept.CodeGen - " + appName + " successfully created!");
        }

        /// <summary>
        /// Creates a new Manager for the currently opened koncept App.
        /// </summary>
        /// <param name="newManagerName">The name of the Manager to create.</param>
        private void CreateNewManager(string newManagerName)
        {
            if (Helpers.IsStringBlank(newManagerName))
            {
                Debug.LogWarning("koncept.CodeGen - Required field 'Manager Name' is empty.");
                return;
            }

            string appPath = Application.dataPath + "/" + app.appName + "/";
            if (!Helpers.DoesDirectoryExist(appPath))
            {
                Debug.LogError("koncept.CodeGen - The app for which this Manager is to be created for could not be found at " + appPath);
                return;
            }

            string scriptPath = appPath + "Scripts/Managers/" + newManagerName + ".cs";
            if (File.Exists(scriptPath))
            {
                Debug.LogError("koncept.CodeGen - The script for this Manager to be created already exists at " + scriptPath);
                return;
            }
            else
            {
                string script = File.ReadAllText(Application.dataPath + "/_koncept/Resources/ManagerTemplate.txt");
                script = Helpers.ReplaceTokens(script, newManagerName, app.appName);

                StreamWriter sw = File.CreateText(scriptPath);
                sw.Write(script);
                sw.Close();
                sw.Dispose();

                buildWaiting = true;
                compileNeeded = false;
                AssetDatabase.Refresh();
            }

            EditorGUIUtility.PingObject(app);
            Selection.SetActiveObjectWithContext(app, app);

            Debug.Log("koncept.CodeGen - " + scriptPath + " created successfully!");
        }

        /// <summary>
        /// Creates an instance of a Manager and adds it to the koncept Framework Prefab.
        /// </summary>
        /// <param name="managerName">The name of the Manager to add.</param>
        void AddManagerToPrefab(string managerName)
        {
            if (compileNeeded)
            {
                Debug.LogWarning("koncept.CodeGen - Waiting on Editor recompile.  Try again in a few seconds and/or after clicking somewhere in the Projects window.");
                return;
            }

            AssetDatabase.Refresh();

            string appPath = Application.dataPath + "/" + app.appName + "/";
            if (!Helpers.DoesDirectoryExist(appPath))
            {
                Debug.LogError("koncept.CodeGen - The app for which this Manager is to be created for could not be found at " + appPath);
                return;
            }

            string scriptPath = appPath + "Scripts/Managers/" + filterManagerName + ".cs";
            if (!File.Exists(scriptPath))
            {
                Debug.LogError("koncept.CodeGen - The script for this Manager was NOT found at " + scriptPath + ".  If you recently created this Manager, please click anywhere in your Projects window to force AssetDatabase to refresh.");
                return;
            }

            Type newManagerType = Helpers.GetType(managerName, "koncept." + app.appName);

            GameObject codeGenPrefab = new GameObject();
            codeGenPrefab.name = filterManagerName;
            codeGenPrefab.AddComponent(newManagerType);

            GameObject singletonPrefab = FindFirstObjectByType<koncept>().gameObject;
            codeGenPrefab.transform.SetParent(singletonPrefab.transform);
            PrefabUtility.SaveAsPrefabAsset(singletonPrefab, "Assets/" + app.appName + "/Resources/_koncept-" + app.appName + ".prefab");

            DestroyImmediate(codeGenPrefab);

            PrefabUtility.ApplyPrefabInstance(singletonPrefab, InteractionMode.AutomatedAction);

            compileNeeded = false;
            AssetDatabase.Refresh();

            GameObject go = GameObject.Find(filterManagerName);
            EditorGUIUtility.PingObject(go);
            Selection.SetActiveObjectWithContext(go, go);

            Debug.Log("koncept.CodeGen - " + managerName + " successfully added to the _koncept-" + app.appName + " Singleton Prefab Variant!");
        }

        /// <summary>
        /// Removes the instance of this Manager from the koncept Framework Prefab;
        /// </summary>
        /// <param name="managerObj">The name of the Manager to remove.</param>
        void RemoveManagerFromPrefab(GameObject managerObj)
        {
            string cachedName = managerObj.name;

            GameObject singletonPrefab = PrefabUtility.LoadPrefabContents("Assets/" + app.appName + "/Resources/_koncept-" + app.appName + ".prefab");

            foreach (Transform child in singletonPrefab.transform)
            {
                if (child.name == cachedName)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(singletonPrefab, "Assets/" + app.appName + "/Resources/_koncept-" + app.appName + ".prefab");
            PrefabUtility.UnloadPrefabContents(singletonPrefab);

            compileNeeded = false;
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(app);
            Selection.SetActiveObjectWithContext(app, app);

            Debug.Log("koncept.CodeGen - " + cachedName + " successfully removed to the _koncept-" + app.appName + " Singleton Prefab Variant!");
        }

        /// <summary>
        /// Creates a UIData and UI script with a given name, then queues the creation of a UI prefab.
        /// </summary>
        /// <param name="filterUIName">The name of the UI to create.</param>
        private void CreateNewUIPackage(string filterUIName)
        {
            if (Helpers.IsStringBlank(filterUIName))
            {
                Debug.LogWarning("koncept.CodeGen - Required field 'UI Name' is empty.");
                return;
            }

            string appPath = Application.dataPath + "/" + app.appName + "/";
            if (!Helpers.DoesDirectoryExist(appPath))
            {
                Debug.LogError("koncept.CodeGen - The app for which this UI is to be created for could not be found at " + appPath);
                return;
            }

            string soPath = appPath + "Scripts/UI/ScriptableObjects/" + filterUIName + "Data.cs";
            string uiScriptPath = appPath + "Scripts/UI/" + filterUIName + ".cs";
            if (File.Exists(soPath))
            {
                Debug.LogError("koncept.CodeGen - The script for this UI to be created already exists at " + soPath);
                return;
            }
            else if (File.Exists(uiScriptPath))
            {
                Debug.LogError("koncept.CodeGen - The script for this UIData to be created already exists at " + uiScriptPath);
                return;
            }
            else
            {
                // Create UIData directory
                AssetDatabase.CreateFolder("Assets\\" + app.appName + "\\Prefabs\\UIData", filterUIName);

                // Create UIData script
                string soScript = File.ReadAllText(Application.dataPath + "/_koncept/Resources/UIDataTemplate.txt");
                soScript = Helpers.ReplaceTokens(soScript, filterUIName, app.appName);

                StreamWriter sw = File.CreateText(soPath);
                sw.Write(soScript);
                sw.Close();
                sw.Dispose();

                // Create UI script
                string uiScript = File.ReadAllText(Application.dataPath + "/_koncept/Resources/UITemplate.txt");
                uiScript = Helpers.ReplaceTokens(uiScript, filterUIName, app.appName);

                StreamWriter sw2 = File.CreateText(uiScriptPath);
                sw2.Write(uiScript);
                sw2.Close();
                sw2.Dispose();

                // Wait for recompile, then create UI Prefab and add UI script to its root
                uiBuildWaiting = true;
                compileNeeded = true;
                AssetDatabase.Refresh();
            }

            Debug.Log("koncept.CodeGen - " + filterUIName + " UIData & UI script created successfully!");
        }

        /// <summary>
        /// Creates a UI prefab for a given UI and attaches its UI script to its root.
        /// </summary>
        /// <param name="filterUIName">The UI to create a prefab for.</param>
        private void CreateUIPrefab(string filterUIName)
        {
            if (Helpers.IsStringBlank(filterUIName))
            {
                Debug.LogWarning("koncept.CodeGen - Required field 'UI Name' is empty.");
                return;
            }

            Type uiComponent = Helpers.GetType(filterUIName, "koncept." + app.appName);
            if (uiComponent == null)
            {
                Debug.LogError("koncept.CodeGen - UI Type for " + filterUIName + " not found.");
                return;
            }

            string appPath = Application.dataPath + "/" + app.appName + "/";
            if (!Helpers.DoesDirectoryExist(appPath))
            {
                Debug.LogError("koncept.CodeGen - The app for which this UI Prefab is to be created for could not be found at " + appPath);
                return;
            }

            string prefabPath = appPath + "Prefabs/UI/" + filterUIName + ".prefab";
            if (!File.Exists(prefabPath))
            {
                GameObject devCanvas = new GameObject();
                devCanvas.AddComponent<Canvas>();
                GameObject codeGenPrefab = Instantiate(Resources.Load("UIPrefabTemplate"), devCanvas.transform) as GameObject;
                codeGenPrefab.AddComponent(uiComponent);
                PrefabUtility.SaveAsPrefabAsset(codeGenPrefab, prefabPath);
                DestroyImmediate(codeGenPrefab);
                DestroyImmediate(devCanvas);

                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError("koncept.CodeGen - The UI Prefab to be created was already found at " + appPath);
                return;
            }

            Debug.Log("koncept.CodeGen - " + filterUIName + " UI Prefab created successfully!");
            selectedUI = uiComponent.Name;
        }

        private void CreateScene(string destPath)
        {
            string rootSourcePath = "Assets/_koncept/Resources/Root.unity";
            FileUtil.CopyFileOrDirectory(rootSourcePath, destPath);

            // Unload all scenes except the pre-load scene
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetSceneAt(i);
                bool doNotDestroy = false;

                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    if (go.TryGetComponent(out koncept framework))
                        doNotDestroy = true;
                }

                if (!doNotDestroy)
                    EditorSceneManager.UnloadSceneAsync(scene);
            }

            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(destPath, OpenSceneMode.Additive);
            EditorSceneManager.SaveScene(EditorSceneManager.GetSceneByName("Root"), destPath);

            string script = File.ReadAllText(Application.dataPath + "/_koncept/Resources/ForceRecompile.txt");
            StreamWriter sw = File.CreateText("Assets/" + app.appName + "/ForceRecompile.cs");
            sw.Write(script);
            sw.Close();
            sw.Dispose();

            compileNeeded = true;
            sceneBuildWaiting = true;
            AssetDatabase.Refresh();

            Debug.Log("koncept.CodeGen - New Scene created successfully at " + destPath);
        }

        void ApplyFrameworkPrefabToNewSceneRoot()
        {
            // Find the Root gameObject in the new scene
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetSceneAt(i);

                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    if (go.TryGetComponent(out Root root))
                    {
                        GameObject frameworkPrefabInstance = AssetDatabase.LoadAssetAtPath("Assets/" + app.appName + "/Resources/_koncept-" + app.appName + ".prefab", typeof(GameObject)) as GameObject;
                        root.frameworkPrefab = frameworkPrefabInstance;

                        EditorSceneManager.SetActiveScene(scene);
                    }
                }
            }

            File.Delete("Assets/" + app.appName + "/ForceRecompile.cs.meta");
            File.Delete("Assets/" + app.appName + "/ForceRecompile.cs");

            UpdateSceneBuildSettings();

            AssetDatabase.Refresh();
        }

        void UpdateSceneBuildSettings()
        {
            // Add scenes for this app to Build Settings
            List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();

            // Pre-load scene always first if it exists
            string preloadScene = "Assets/" + app.appName + "/Scenes/_koncept-" + app.appName + ".unity";
            if (File.Exists(preloadScene))
                editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(preloadScene, true));

            for (int i = 0; i < associatedScenes.Count; i++)
            {
                if (associatedScenes[i] != "_koncept-" + app.appName)
                {
                    string scenePath = "Assets/" + app.appName + "/Scenes/" + associatedScenes[i] + ".unity";
                    editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                }
            }

            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        }

        #endregion

        #region Component Configuration

        int selectedComponent = -1;
        int selectedField = -1;
        IEnumerable<Component> components;
        Dictionary<Type, FieldInfo[]> componentFields;
        bool managerRemovalConfirm = false;
        bool managerScriptRemovalConfirm = false;
        bool uiRemovalConfirm = false;
        bool sceneRemovalConfirm = false;

        private void SelectionChanged()
        {
            if (Selection.activeGameObject != null)
            {
                BuildComponentsList(Selection.activeGameObject);
                BuildComponentFieldsList(components);

                switch (SelectedManagerType)
                {
                    case ManagementType.Manager:
                        if (Selection.activeGameObject.GetComponent<Manager>() != null)
                        {
                            selectedManager = app.GetManager(Selection.activeGameObject.name);
                            managerRemovalConfirm = false;
                            managerScriptRemovalConfirm = false;
                        }
                        break;
                    case ManagementType.Scene:
                        break;
                    case ManagementType.UI:
                        uiRemovalConfirm = false;
                        break;
                    default:
                        break;
                }
            }
        }

        private void BuildComponentsList(GameObject obj)
        {
            selectedComponent = 0;
            components = obj.GetComponents<Component>().Where(c => c is Manager);
        }

        private void BuildComponentFieldsList(IEnumerable<Component> components)
        {
            selectedField = 0;
            componentFields = new Dictionary<Type, FieldInfo[]>();

            foreach (var component in components)
            {
                var componentType = component.GetType();
                var publicFields = componentType.GetFields().Where((field) => field.IsPublic).ToArray();

                if (!componentFields.ContainsKey(componentType))
                {
                    componentFields.Add(componentType, publicFields);
                }
            }
        }

        Manager selectedManager;

        void HandleManagerComponentsSelection()
        {
            if (components == null || componentFields == null)
            {
                SelectionChanged();
            }

            if (selectedManager == null)
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label("Highlight a koncept Manager to access its properties.", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;
                return;
            }

            EditorGUILayout.LabelField("[" + selectedManager.name + "]", EditorStyles.largeLabel);

            if (selectedComponent >= 0)
            {
                if (componentFields.Count >= selectedComponent && componentFields.Count > 0)
                {
                    string[] stringArrayOfFields = componentFields[components.ToArray()[selectedComponent].GetType()]
                        .Select((field) => field.Name)
                        .ToArray();

                    List<string> tempList = stringArrayOfFields.ToList();
                    tempList.Remove("ScriptExecutePriority");

                    if (tempList.Count == 0)
                        tempList.Add("No public fields found on this Manager");

                    stringArrayOfFields = tempList.ToArray();

                    EditorGUILayout.LabelField("Accessible Properties", EditorStyles.largeLabel);
                    selectedField = GUILayout.SelectionGrid(selectedField,
                        stringArrayOfFields,
                        componentFields.Count(),
                        EditorStyles.whiteMiniLabel);
                }

                GUILayout.Label("Script Execute Priority [" + selectedManager.ScriptExecutePriority + "]", EditorStyles.largeLabel);
                GUILayout.Label("A Manager's script execute priority determines the older in which it initializes when the koncept App starts up in relation to other Managers.  Lower values initialize first.", EditorStyles.wordWrappedMiniLabel);

                if (Selection.activeGameObject.name != "UIManager" && Selection.activeGameObject.name != "SceneManager")
                {
                    if (GUILayout.Button("Edit " + selectedManager.name + ".cs [#]"))
                    {
                        Helpers.OpenComponentInVisualStudioIDE((MonoBehaviour)components.ToArray()[selectedComponent], app.appName);
                    }

                    Color cachedColor = GUI.contentColor;
                    GUI.contentColor = Color.yellow;

                    if (!managerRemovalConfirm)
                    {
                        if (GUILayout.Button("Remove " + selectedManager.name + " from the Framework Prefab [-]"))
                        {
                            managerRemovalConfirm = true;
                        }
                    }
                    else if (managerRemovalConfirm)
                    {
                        if (GUILayout.Button("Click again to confirm removal"))
                        {
                            RemoveManagerFromPrefab(Selection.activeGameObject);
                        }
                    }

                    GUI.contentColor = cachedColor;
                }
            }
        }

        string newUIDataName = "NewUIData";
        string[] uiDatas;
        string[] UIDatas
        {
            get
            {
                return uiDatas;
            }
            set
            {
                if (uiDatas == value)
                    return;

                uiDatas = value;
            }
        }

        private void HandleUISelection(string selectedUI)
        {
            GUILayout.Label("[" + selectedUI + "]", EditorStyles.largeLabel);

            // Create new UIData instance
            newUIDataName = EditorGUILayout.TextField("UIData Name: ", newUIDataName);
            if (GUILayout.Button("Create New UIData Instance [+]"))
            {
                CreateScriptableObjectInstance(newUIDataName, selectedUI + "Data");
            }

            if (UIDatas == null)
                return;

            // Display list of UIData instances and ping them in the Project window
            GUILayout.Label(selectedUI + "Data Instances [" + UIDatas.Length.ToString() + "]", EditorStyles.largeLabel);

            if (UIDatas.Length > 0)
            {
                foreach (string uD in UIDatas)
                {
                    if (GUILayout.Button(uD))
                    {
                        string assetPath = "Assets/" + app.appName + "/Prefabs/UIData/" + selectedUI + "/" + uD + ".asset";
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    }
                }
            }
            else
            {
                Color cachedColor = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label("No UIData Instances found, create some using the above form.", EditorStyles.wordWrappedLabel);
                GUI.contentColor = cachedColor;
            }
        }

        void CreateScriptableObjectInstance(string soName, string soTypeName)
        {
            string soDirPath = "Assets/" + app.appName + "/Prefabs/UIData/" + selectedUI;

            if (!Helpers.DoesDirectoryExist(soDirPath))
            {
                Debug.LogError("koncept.CodeGen - UIData Prefab directory for " + selectedUI + " could not be found at " + soDirPath);
                return;
            }

            string prefabPath = "Assets/" + app.appName + "/Prefabs/UI/" + selectedUI + ".prefab";

            if (!File.Exists(prefabPath))
            {
                Debug.LogError("koncept.CodeGen - UIData Prefab for " + selectedUI + " could not be found at " + prefabPath);
                return;
            }

            UIData newUIData = ScriptableObject.CreateInstance(Helpers.GetType(soTypeName, "koncept." + app.appName)) as UIData;

            GameObject prefabLoad = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;

            newUIData.Prefab = prefabLoad;

            string uiDataPath = AssetDatabase.GenerateUniqueAssetPath("Assets/" + app.appName + "/Prefabs/UIData/" + selectedUI + "/" + soName + ".asset");
            AssetDatabase.CreateAsset(newUIData, uiDataPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newUIData;

            Debug.Log("koncept.CodeGen - New UIData instance successfully created at " + uiDataPath);
        }

        private void HandleSceneSelection(string selectedScene)
        {
            GUILayout.Label("[" + selectedScene + "]", EditorStyles.largeLabel);
            if (GUILayout.Button("Open Scene"))
            {
                // Unload all scenes except the pre-load scene
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetSceneAt(i);
                    bool doNotDestroy = false;

                    foreach (GameObject go in scene.GetRootGameObjects())
                    {
                        if (go.TryGetComponent(out koncept framework))
                            doNotDestroy = true;
                    }

                    if (!doNotDestroy)
                        EditorSceneManager.UnloadSceneAsync(scene);
                }

                // Load selected scene
                EditorSceneManager.OpenScene("Assets/" + app.appName + "/Scenes/" + selectedScene + ".unity", OpenSceneMode.Additive);
                UnityEngine.SceneManagement.Scene sceneToActivate = EditorSceneManager.GetSceneByName(selectedScene);
                EditorSceneManager.SetActiveScene(sceneToActivate);
            }
        }

        public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        void UpdateAssociatedUIs(string filePath)
        {
            associatedUIs.Clear();

            try
            {
                DirectoryInfo dir = new DirectoryInfo(filePath);
                FileInfo[] info = dir.GetFiles("*.*");

                foreach (FileInfo f in info)
                {
                    string fileName = f.ToString().Split('\\').Last();

                    if (!fileName.Contains(".meta"))
                    {
                        fileName = fileName.Substring(0, fileName.Length - 7);
                        associatedUIs.Add(fileName);
                    }
                }
            }
            catch
            {
                // Do fucking nothing
            }
        }

        void UpdateAssociatedScenes(string filePath)
        {
            associatedScenes.Clear();

            try
            {
                DirectoryInfo dir = new DirectoryInfo(filePath);
                FileInfo[] info = dir.GetFiles("*.*");

                foreach (FileInfo f in info)
                {
                    string fileName = f.ToString().Split('\\').Last();

                    if (!fileName.Contains(".meta"))
                    {
                        fileName = fileName.Substring(0, fileName.Length - 6);
                        associatedScenes.Add(fileName);
                    }
                }
            }
            catch
            {
                // Do fucking nothing
            }
        }

        #endregion
    }
}