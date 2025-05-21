using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using ToolkitEngine.SceneManagement;

namespace ToolkitEditor.SceneManagement
{
    [CustomEditor(typeof(TransmissionCollection))]
    public class TransmissionCollectionEditor : BaseToolkitEditor
    {
		#region Fields

		private TransmissionCollection m_collection;

		private SerializedProperty m_transmissions;
		private HashSet<Transmission> m_dirtyTransmissions = new();

		// Used to map reorderable list to evaluator
		private ReorderableList m_reorderableList;

		#endregion

		#region Methods

		private void OnEnable()
		{
			m_collection = target as TransmissionCollection;

			m_transmissions = serializedObject.FindProperty(nameof(m_transmissions));

			m_reorderableList = new ReorderableList(new List<Transmission>(m_collection.transmissionList), typeof(Transmission), true, false, true, true);

			m_reorderableList.drawElementCallback += (rect, index, isActive, isFocused) =>
			{
				if (!index.Between(0, m_collection.transmissionList.Count - 1))
					return;

				var transmissionProp = m_transmissions.GetArrayElementAtIndex(index);
				if (transmissionProp == null)
					return;

				var serializedTransmission = new SerializedObject(transmissionProp.objectReferenceValue);
				var transmission = serializedTransmission.targetObject as Transmission;

				++EditorGUI.indentLevel;
				{
					EditorGUI.BeginChangeCheck();
					{
						transmission.name = EditorGUIRectLayout.TextField(ref rect, string.Empty, transmission.name);
					}
					if (EditorGUI.EndChangeCheck())
					{
						m_dirtyTransmissions.Add(transmission);
					}
				}
				--EditorGUI.indentLevel;
			};
			m_reorderableList.elementHeightCallback += ElementHeightCallback;

			m_reorderableList.onCanAddCallback += OnCanAddCallback;
			m_reorderableList.onAddDropdownCallback += OnAddDropdownCallback;
			m_reorderableList.onReorderCallbackWithDetails += OnReorderCallback;
			m_reorderableList.onCanRemoveCallback += OnCanRemoveCallback;
			m_reorderableList.onRemoveCallback += OnRemoveCallback;
		}

		private void OnDisable()
		{
			foreach (var transmission in m_dirtyTransmissions)
			{
				AssetUtil.SaveSubAsset(transmission);
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			m_reorderableList.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region ReorderableList Callbacks

		private float ElementHeightCallback(int index)
		{
			if (!index.Between(0, m_collection.transmissionList.Count - 1))
				return 0f;

			var transmissionProp = m_transmissions.GetArrayElementAtIndex(index);
			if (transmissionProp == null)
				return 0f;

			float height = EditorGUIUtility.singleLineHeight
				+ EditorGUIUtility.standardVerticalSpacing;

			return height;

		}

		private bool OnCanAddCallback(ReorderableList list)
		{
			return true;
		}

		private void OnAddDropdownCallback(Rect buttonRect, ReorderableList list)
		{
			var transmission = CreateInstance<Transmission>();
			transmission.name = Guid.NewGuid().ToString();

			m_collection.transmissionList.Add(transmission);
			AssetDatabase.AddObjectToAsset(transmission, m_collection);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			m_reorderableList.list = m_collection.transmissionList;
			Repaint();
		}

		private void OnReorderCallback(ReorderableList list, int oldIndex, int newIndex)
		{
			m_collection.transmissionList = list.list as List<Transmission>;
		}

		private bool OnCanRemoveCallback(ReorderableList list)
		{
			return m_collection.transmissionList.Count > 0;
		}

		private void OnRemoveCallback(ReorderableList list)
		{
			var transmission = m_collection.transmissionList[list.index];

			m_collection.transmissionList.RemoveAt(list.index);
			list.list = m_collection.transmissionList;
			list.index = Mathf.Clamp(list.index, 0, m_collection.transmissionList.Count - 1);

			AssetDatabase.RemoveObjectFromAsset(transmission);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		#endregion
	}
}