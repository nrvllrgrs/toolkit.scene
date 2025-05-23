using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Reflection;
using System.Linq;
using ToolkitEngine.SceneManagement;

namespace ToolkitEditor.SceneManagement
{
	[CustomEditor(typeof(SubScene))]
	public class SubsceneEditor : Editor
	{
		#region Fields

		private SubScene m_subscene;

		private static int ObjectsSelected = 0;

		#endregion

		#region Methods

		private void OnEnable()
		{
			m_subscene = target as SubScene;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			{
				if (!m_subscene.IsLoaded)
				{
					if (GUILayout.Button("Open"))
					{
						m_subscene.OpenSubscene();
					}
				}
				else
				{
					if (GUILayout.Button("Close"))
					{
						m_subscene.CloseSubscene(false);
					}

					EditorGUI.BeginDisabledGroup(!m_subscene.IsDirty());
					{
						if (GUILayout.Button("Revert"))
						{
							m_subscene.CloseSubscene(false, true);
							m_subscene.OpenSubscene();
						}
						if (GUILayout.Button("Save"))
						{
							m_subscene.SaveSubScene();
						}
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndHorizontal();
		}

		[MenuItem("GameObject/Toolkit/Scene/New Subscene From Selection", false, 10)]
		public static void CreateCustomGameObject(MenuCommand menuCommand)
		{
			//Prevent multi-select of objects from executing menuCommand multiple times.
			if (ObjectsSelected < Selection.transforms.Length)
			{
				ObjectsSelected++;

				if (ObjectsSelected < Selection.transforms.Length)
					return;
			}
			ObjectsSelected = 0;

			//Get the transforms from the selection and log the names.
			Transform[] selectionTransforms = Selection.GetTransforms(SelectionMode.TopLevel);
			List<GameObject> gameObjectsForSubscene = new List<GameObject>();
			for (int i = 0; i < selectionTransforms.Length; i++)
			{
				gameObjectsForSubscene.Add(selectionTransforms[i].gameObject);
			}
			SubScene.CreateSubsceneFromGameObjects(gameObjectsForSubscene, "New SubScene");
		}

		[InitializeOnLoadMethod]
		public void Initialize()
		{
			ObjectsSelected = 0;

			SceneManager.sceneLoaded += HandleSceneLoaded;
			SceneManager.sceneUnloaded += HandleSceneClosed;
		}

		public void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
		{
			SubScene s = target as SubScene;
			if (s.EditingScene == scene)
			{
				EditorApplication.RepaintHierarchyWindow();
			}
		}

		public void HandleSceneClosed(Scene scene)
		{
			SubScene s = target as SubScene;
			if (s.EditingScene == scene)
			{
				EditorApplication.RepaintHierarchyWindow();
			}
		}

		#endregion
	}

	[InitializeOnLoad]/// <summary> Sets a background color for game objects in the Hierarchy tab</summary>
	public class SubsceneHierarchyDrawer
	{
		private static Vector2 offset = new Vector2(20, 1);
		private static List<Scene> scenesClosing = new List<Scene>();

		static SubsceneHierarchyDrawer()
		{
			EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
		}

		private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
		{
			var obj = EditorUtility.InstanceIDToObject(instanceID);
			if (obj != null)
			{
				Color backgroundColor = Color.white;
				Color textColor = Color.white;

				SubScene subscene;
				// Write your object name in the hierarchy.
				if (obj is GameObject)
				{
					if ((obj as GameObject).TryGetComponent<SubScene>(out subscene))
					{
						float val = 41f / 255f;
						backgroundColor = new Color(val, val, val);
						textColor = new Color(0.9f, 0.9f, 0.9f);
						DrawSubsceneHeader(selectionRect, backgroundColor, textColor, obj, subscene);
						DrawToggle(selectionRect, subscene, obj);

						bool isExpanded = ItemIsExpanded(subscene.gameObject);
						if (subscene.transform.childCount > 0 && subscene.IsLoaded)
						{
							bool newIsExpanded = EditorGUI.Foldout(new Rect(selectionRect.x - selectionRect.height + 5, selectionRect.y, selectionRect.width + 50, selectionRect.height), isExpanded, "");
							if (isExpanded != newIsExpanded)
							{
								SetExpanded(subscene.gameObject, newIsExpanded);
							}
						}
					}

					SubScene parent = SubsceneParent((obj as GameObject).transform);
					if (parent != null)
					{
						int depth = GetDepth((obj as GameObject).transform);
						Rect rect = new Rect(selectionRect.x - 14 * depth, selectionRect.y, selectionRect.width, selectionRect.height);
						DrawVerticalLine(rect, 1f, parent.HierarchyColor);
					}
				}
			}
		}

		private static void DrawSubsceneHeader(Rect selectionRect, Color backgroundColor, Color textColor, Object obj, SubScene subScene)
		{
			Rect offsetRect = new Rect(selectionRect.position + offset, selectionRect.size);
			Rect bgRect = new Rect(selectionRect.x - selectionRect.height, selectionRect.y, selectionRect.width + 50, selectionRect.height);

			//Draw the Rect that gives the GO a different BG Color
			EditorGUI.DrawRect(bgRect, backgroundColor);
			//Bold Title
			string name = obj.name;
			if (subScene.IsDirty())
			{
				name += "*";
			}
			EditorGUI.LabelField(offsetRect, name, new GUIStyle()
			{
				normal = new GUIStyleState() { textColor = textColor },
				fontStyle = FontStyle.Bold
			});

			//Draw the Unity Logo on the subscene Object
			GUIContent tex = EditorGUIUtility.IconContent("UnityLogo");
			if (tex != null)
			{
				EditorGUI.LabelField(new Rect(selectionRect.position, new Vector2(selectionRect.height, selectionRect.height)), tex);
			}
		}

		private static void DrawToggle(Rect selectionRect, SubScene subscene, Object obj)
		{
			bool newLoaded = GUI.Toggle(new Rect(selectionRect.xMax - selectionRect.height, selectionRect.yMin, selectionRect.height, selectionRect.height), subscene.IsLoaded, "");
			if (newLoaded != subscene.IsLoaded)
			{
				if (subscene.IsLoaded && !newLoaded)
				{
					SetExpanded(obj, false);
					subscene.CloseSubscene(false);
				}
				else if (!subscene.IsLoaded && newLoaded)
				{
					subscene.OpenSubscene();
				}

				EditorApplication.RepaintHierarchyWindow();
			}
		}

		private static int CountDecendants(Transform transformToCount)
		{
			int childCount = transformToCount.childCount;// direct child count.
			foreach (Transform child in transformToCount.GetComponentsInChildren<Transform>())
			{
				if (child == transformToCount)
					continue;
				
				childCount += CountDecendants(child);// add child direct children count.
			}
			return childCount;
		}

		/// <summary>
		///  Expand or collapse object in Hierarchy
		/// </summary>
		/// <param name="obj">The object to expand or collapse</param>
		/// <param name="expand">A boolean to indicate if you want to expand or collapse the object</param>
		public static void SetExpanded(Object obj, bool expand)
		{
			object sceneHierarchy = GetHierarchyWindowType().GetProperty("sceneHierarchy").GetValue(GetHierarchyWindow());
			var methodInfo = sceneHierarchy.GetType().GetMethod("ExpandTreeViewItem", BindingFlags.NonPublic | BindingFlags.Instance);

			methodInfo.Invoke(sceneHierarchy, new object[] { obj.GetInstanceID(), expand });
		}

		static System.Type GetHierarchyWindowType()
		{
			return typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
		}

		static EditorWindow GetHierarchyWindow()
		{
			EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
			return EditorWindow.focusedWindow;
		}

		static bool ItemIsExpanded(Object ob)
		{
			var _sceneHierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
			var _getExpandedIDs = _sceneHierarchyWindowType.GetMethod("GetExpandedIDs", BindingFlags.NonPublic | BindingFlags.Instance);
			var _lastInteractedHierarchyWindow = _sceneHierarchyWindowType.GetProperty("lastInteractedHierarchyWindow", BindingFlags.Public | BindingFlags.Static);
			if (_lastInteractedHierarchyWindow == null)
				return false;

			var _expandedIDs = _getExpandedIDs.Invoke(_lastInteractedHierarchyWindow.GetValue(null), null) as int[];

			// Is expanded?
			return _expandedIDs.Contains(ob.GetInstanceID());
		}

		public static void DrawVerticalLine(Rect originalRect, float barWidth, Color color)
		{
			Rect lineRect = new Rect(
				originalRect.x - originalRect.height + 1f,
				originalRect.y, //+ originalRect.height,
				barWidth,
				originalRect.height
			);

			GUI.DrawTexture(lineRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 1, color, Vector4.zero, 0);
			EditorGUI.DrawRect(lineRect, color);
		}

		private static SubScene SubsceneParent(Transform transform)
		{
			var trans = transform.parent;
			while (trans != null)
			{
				if (trans.TryGetComponent<SubScene>(out SubScene s))
					return s;

				trans = trans.parent;
			}
			return null;
		}

		private static int GetDepth(Transform transform)
		{
			int count = 0;
			var trans = transform.parent;
			while (trans != null)
			{
				count++;
				trans = trans.parent;
			}
			return count;
		}
	}
}