using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ToolkitEngine.SceneManagement
{
	[ExecuteAlways]
	public class SubScene : MonoBehaviour
	{
		#region Fields

		/// <summary>
		/// The Runtime representation of the Scene Asset. 
		/// </summary>
		[HideInInspector]
		public Scene EditingScene;

		/// <summary>
		/// The Scene that the SubScene represents.
		/// </summary>
		[Tooltip("The Scene that the SubScene represents.")]
		public SceneReference scene;

		/// <summary>
		/// The SubScene loads on Start if true.
		/// </summary>
		[Tooltip("The SubScene loads on Start if true.")]
		public bool AutoLoadScene;

		#endregion

		#region Properties

		/// <summary>
		/// Returns AssetDatabase.GetAssetPath(SceneAsset);
		/// </summary>
		public string EditableScenePath => scene.path;

		public Color HierarchyColor = Color.white;

		public bool IsLoaded => EditingScene.isLoaded;

		/// <summary>
		/// Get a Hash128 GUID from the existing scene asset.
		/// </summary>
		public Hash128 SceneGUID
		{
			get
			{
				int data = scene.GetHashCode();
				return Hash128.Compute(ref data);
			}
		}

		#endregion

		#region Methods

		public void Start()
		{
			if (AutoLoadScene)
			{
				OpenSubscene();
			}
		}

		/// <summary>
		/// Loads in the Subscene. 
		/// </summary>
		/// <returns>True if the Scene is open, false otherwise. Remember that scenes are loaded Asynchronously.</returns>
		public bool OpenSubscene(UnityAction callback = null)
		{
			if (!scene.isValidSceneAsset)
				return false;

			Scene activeScene;
			if (Application.isPlaying)
			{
				activeScene = SceneManager.GetActiveScene();
				AsyncOperation op = SceneManager.LoadSceneAsync(scene.name, LoadSceneMode.Additive);
				op.completed += (x) =>
				{
					Debug.Log("Loaded Scene");
					EditingScene = SceneManager.GetSceneByName(scene.name);
					LoadSubsceneGameObjects();
				};

				if (callback != null)
				{
					op.completed += (x) =>
					{
						callback();
					};
				}
				SceneManager.SetActiveScene(activeScene);
			}
#if UNITY_EDITOR
			else
			{
				activeScene = SceneManager.GetActiveScene();
				EditingScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
				LoadSubsceneGameObjects();
				ClearDirty();

				SceneManager.SetActiveScene(activeScene);
			}
#endif

			EditingScene.isSubScene = true;
			return IsLoaded;
		}

		/// <summary>
		/// In the case that the subscene loses it's reference to the Scene represented by it's scene asset, you can attempt to reconnect it using this method. Will only work if the scene is open.
		/// </summary>
		/// <returns>True if the EditingScene reference is not null, false if it is null.</returns>
		public bool ReconnectEditingScene()
		{
			EditingScene = SceneManager.GetSceneByName(scene.name);
			return EditingScene.name == scene.name && EditingScene.IsValid();
		}

		/// <summary>
		/// Closes the Subscene.
		/// </summary>
		/// <param name="SaveSubsceneOnClose"></param>
		/// <returns>True if the subscene is closed. False otherwise.</returns>
		public bool CloseSubscene(bool SaveSubsceneOnClose)
		{
			if (!IsLoaded)
				return true;

			bool dirty = IsDirty();
			UnloadSubsceneGameObjects();

			if (Application.isPlaying)
			{
				AsyncOperation op = SceneManager.UnloadSceneAsync(EditingScene);
				op.completed += (x) =>
				{
					Debug.Log("Scene Unloaded");
				};
			}
#if UNITY_EDITOR
			else
			{
				if (dirty)
				{
					if (SaveSubsceneOnClose || EditorUtility.DisplayDialog(
						"Subscene Has Been Modified",
						$"Do you want to save the changes you made in the subscene:\n {name}\n\nYour changes will be lost if you don't save them.",
						"Save",
						"Don't Save"))
					{
						SaveSubScene();
					}
				}

				EditorSceneManager.CloseScene(EditingScene, true);
			}
#endif
			ClearDirty();
			return !IsLoaded;
		}

		/// <summary>
		/// Move all of the Gameobjects in the subscene to the active scene, and parent them to the Subscene Object.
		/// </summary>
		private void LoadSubsceneGameObjects()
		{
			if (EditingScene == null || !EditingScene.isLoaded)
			{
				Debug.LogWarning("Tried to load subscene objects when the editingScene was not loaded.");
				return;
			}

			GameObject[] gameobjects = EditingScene.GetRootGameObjects();
			foreach (GameObject obj in gameobjects)
			{
				SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
				obj.transform.SetParent(transform);
				obj.hideFlags |= HideFlags.DontSave;
			}

#if UNITY_EDITOR
			EditorApplication.RepaintHierarchyWindow();
#endif
		}

		private void UnloadSubsceneGameObjects()
		{
			if (EditingScene == null || !EditingScene.isLoaded)
				return;

			foreach (Transform child in GetComponentsInChildren<Transform>())
			{
				if (child == transform || child.parent != transform)
					continue;

				child.gameObject.hideFlags &= ~HideFlags.DontSave;
				child.SetParent(null);
				SceneManager.MoveGameObjectToScene(child.gameObject, EditingScene);
			}
		}

		// Close the scene if the gameobject is destroyed so there isn't an unaccounted for Scene open in the hierarchy.
		// By default, save the subscene if working in the editor.
		public void OnDestroy()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				CloseSubscene(true);
				return;
			}
#endif
			CloseSubscene(false);
		}

		#endregion

		#region Editor-Only
#if UNITY_EDITOR

		/// <summary>
		/// Saves the scene represented by the subscene.
		/// </summary>
		/// <returns></returns>
		public bool SaveSubScene()
		{
			if (Application.isPlaying)
				return false;

			if (!IsDirty())
				return false;

			// Move gameObjects back to subscene before saving
			UnloadSubsceneGameObjects();

			bool saved = EditorSceneManager.SaveScene(EditingScene);

			// Move gameObjects to scene after saving
			LoadSubsceneGameObjects();

			if (saved)
			{
				ClearDirty();
			}
			return saved;
		}

		public void ClearDirty()
		{
			EditorUtility.ClearDirty(gameObject);

			foreach (var t in GetComponentsInChildren<Transform>())
			{
				EditorUtility.ClearDirty(t);
			}
		}

		public bool IsDirty()
		{
			if (EditorUtility.IsDirty(gameObject.GetInstanceID()))
				return true;

			foreach (Transform t in GetComponentsInChildren<Transform>())
			{
				if (EditorUtility.IsDirty(t))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Creates a new SubScene and moves the subSceneRootObject to it. Creates new Scene Asset for the SubScene and adds it to the build.  
		/// </summary>
		/// <param name="subSceneRootObject">The GameObject to move to the new SubScene</param>
		/// <param name="sceneAssetName">The name of the Scene Asset created for the SubScene.</param>
		/// <param name="closeSubsceneOnCreation">Closes the SubScene after Creation if true.</param>
		/// <returns>The new SubScene with the subSceneRootObject as a scene root object.</returns>
		public static SubScene CreateSubsceneFromGameObject(GameObject subSceneRootObject, string sceneAssetName, bool closeSubsceneOnCreation = false)
		{
			GameObject[] rootObjects = new GameObject[] { subSceneRootObject };
			return CreateSubsceneFromGameObjects(rootObjects, sceneAssetName, closeSubsceneOnCreation);
		}


		/// <summary>
		/// Creates a new empty SubScene. Creates new Scene Asset for the SubScene and adds it to the build.  
		/// </summary>
		/// <param name="sceneAssetName">The name of the Scene Asset created for the SubScene.</param>
		/// <param name="closeSubsceneOnCreation">Closes the SubScene after Creation if true.</param>
		/// <returns>The new empty SubScene.</returns>
		public static SubScene CreateEmptySubscene(string sceneAssetName, bool closeSubsceneOnCreation = false)
		{
			//Just pass in an empty list of gameobjects to move to the new Scene
			return CreateSubsceneFromGameObjects(new List<GameObject>(), sceneAssetName, closeSubsceneOnCreation);
		}

		/// <summary>
		/// Creates a new SubScene and moves the subSceneRootObjects to it. Creates new Scene Asset for the SubScene and adds it to the build.  
		/// </summary>
		/// <param name="subSceneRootObject">The GameObject to move to the new SubScene</param>
		/// <param name="sceneAssetName">The name of the Scene Asset created for the SubScene.</param>
		/// <param name="closeSubsceneOnCreation">Closes the SubScene after Creation if true.</param>
		/// <returns>The new SubScene with the subSceneRootObjects as scene root object.</returns>
		public static SubScene CreateSubsceneFromGameObjects(List<GameObject> subSceneRootObjects, string sceneAssetName, bool closeSubsceneOnCreation = false)
		{
			return CreateSubsceneFromGameObjects(subSceneRootObjects.ToArray(), sceneAssetName, closeSubsceneOnCreation);
		}

		/// <summary>
		/// Creaates a new SubScene and moves the subSceneRootObjects to it. Creates new Scene Asset for the SubScene and adds it to the build.  
		/// </summary>
		/// <param name="subSceneRootObject">The GameObject to move to the new SubScene</param>
		/// <param name="sceneAssetName">The name of the Scene Asset created for the SubScene.</param>
		/// <param name="closeSubsceneOnCreation">Closes the SubScene after Creation if true.</param>
		/// <returns>The new SubScene with the subSceneRootObjects as scene root object.</returns>
		public static SubScene CreateSubsceneFromGameObjects(GameObject[] subSceneRootObjects, string sceneAssetName, bool closeSubsceneOnCreation = false)
		{
			//Create the proper path for the scenes if it does not already exist.
			if (!AssetDatabase.IsValidFolder("Assets/Scenes/"))
			{
				AssetDatabase.CreateFolder("Assets", "Scenes");
			}
			if (!AssetDatabase.IsValidFolder("Assets/Scenes/Subscenes"))
			{
				AssetDatabase.CreateFolder("Assets/Scenes", "Subscenes");
			}

			/*Create a new scene and scene asset. If you want to change the
			default subscene location yourself, you can just change this path.*/
			string path = "Assets/Scenes/Subscenes/" + sceneAssetName + ".unity";
			Scene activeScene = SceneManager.GetActiveScene();
			path = AssetDatabase.GenerateUniqueAssetPath(path);
			int indexOfName = path.LastIndexOf('/') + 1;
			string sceneName = path.Substring(indexOfName, path.Length - indexOfName - ".unity".Length);
			Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
			scene.name = sceneName;

			/*You have to set the original active scene as the active scene so that 
			instantiated objects are spawned in the current scene*/
			SceneManager.SetActiveScene(activeScene);
			//Move the gameobject to the new scene
			foreach (GameObject rootObject in subSceneRootObjects)
			{
				SceneManager.MoveGameObjectToScene(rootObject, scene);
			}
			EditorSceneManager.SaveScene(scene, path);

			/*Get the scene asset, create a new gameobject with subscene component, 
			set the subscene scene as the newly created scene*/
			SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
			GameObject subSceneGO = new GameObject(scene.name);
			SubScene subScene = subSceneGO.AddComponent<SubScene>();
			subScene.AutoLoadScene = false;
			subScene.scene.path = AssetDatabase.GetAssetPath(sceneAsset);
			subScene.EditingScene = scene;

			/*Get the list of scenes in the Editor Build Settings, Add the new scene to 
			 the list. Assign the scenes property as the updated list.*/
			List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
			scenes.Add(new EditorBuildSettingsScene(path, true));
			EditorBuildSettings.scenes = scenes.ToArray();
			if (closeSubsceneOnCreation)
			{
				subScene.CloseSubscene(true);
			}
			else
			{
				subScene.OpenSubscene();
			}
			return subScene;
		}

		//This method is for changing the Hierarchy color. Repainting the hierarchy window allows you to see the color change immediately.
		public void OnValidate()
		{
			EditorApplication.RepaintHierarchyWindow();
		}

#endif
		#endregion
	}
}