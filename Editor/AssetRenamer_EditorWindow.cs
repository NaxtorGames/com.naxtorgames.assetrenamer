using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NaxtorGames.Utillity.EditorScripts;

using static UnityEditor.EditorGUILayout;

namespace NaxtorGames.AssetRenamer.EditorScripts
{
    public sealed class AssetRenamer_EditorWindow : EditorWindow
    {
        private static readonly Vector2 s_minWindowSize = new Vector2(375.0f, 220.0f);

        private static AssetRenamer_EditorWindow s_visibleWindow = null;

        private const string WINDOW_NAME = "Asset Renamer";
        private const int MAX_OBJECTS_FOR_AUTO_PREVIEW = 25;

        private readonly AssetRenamer _assetRenamer = new AssetRenamer();
        private readonly Dictionary<AssetRenameOrder, bool> _renameOrderFoldoutStatus = new Dictionary<AssetRenameOrder, bool>();

        [SerializeField] private List<Object> _assetsToRename = new List<Object>();
        [SerializeField] private bool _forceAutoPreview = false;

        private SerializedObject _thisSerializedObject = null;
        private SerializedProperty _assetsToRenameProperty = null;

        private GUIStyle _richTextLable = null;
        private Vector2 _objectsInListScrollPosition = Vector2.zero;
        private Vector2 _renameOrderScrollPosition = Vector2.zero;
        private Vector2 _previewScrollPosition = Vector2.zero;

        [MenuItem(UtillityData.TOOL_MENU_PATH + "/" + WINDOW_NAME, priority = -100)]
        public static void OpenWindow()
        {
            if (s_visibleWindow == null)
            {
                s_visibleWindow = GetWindow<AssetRenamer_EditorWindow>(WINDOW_NAME);
                return;
            }

            s_visibleWindow.Show();
        }

        private void OnEnable()
        {
            this.minSize = s_minWindowSize;
            if (_assetsToRename == null)
            {
                _assetsToRename = new List<Object>();
            }

            _thisSerializedObject = new SerializedObject(this);
            _assetsToRenameProperty = EditorHelpers.CreatePropertyField(_thisSerializedObject, nameof(_assetsToRename));

            _renameOrderFoldoutStatus.Clear();
            for (int i = 0; i < _assetRenamer.RenameOrderCount; i++)
            {
                _renameOrderFoldoutStatus.Add(_assetRenamer.GetRenameOrderAtIndex(i), false);
            }
        }

        private void OnGUI()
        {
            if (_richTextLable == null)
            {
                _richTextLable = new GUIStyle(GUI.skin.label)
                {
                    richText = true
                };
            }

            _thisSerializedObject.Update();
            BeginHorizontal();
            EditorGUI.BeginDisabledGroup(_assetsToRename.Count <= 0);

            if (GUILayout.Button("Clear Asset List"))
            {
                _assetsToRename.Clear();
                _assetRenamer.ClearPreviewNames();
            }
            EditorGUI.EndDisabledGroup();
            EndHorizontal();
            Space();

            EditorGUI.indentLevel++;
            _objectsInListScrollPosition = BeginScrollView(_objectsInListScrollPosition);
            EditorHelpers.DrawPropertyField(_assetsToRenameProperty, true);

            EndScrollView();

            Space();
            BeginHorizontal();
            if (_assetRenamer.RenameOrderCount > 0 && GUILayout.Button("Clear Orders"))
            {
                _assetRenamer.ClearOrders();
                _renameOrderFoldoutStatus.Clear();
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("Add Order"))
            {
                AddNewOrder(_assetRenamer.CreateNewOrder());
                GUI.FocusControl(null);
            }
            EndHorizontal();

            _renameOrderScrollPosition = BeginScrollView(_renameOrderScrollPosition);
            for (int i = 0; i < _assetRenamer.RenameOrderCount; i++)
            {
                DrawRenameOrder(_assetRenamer.GetRenameOrderAtIndex(i), i);
            }
            EndScrollView();

            Space();

            bool listIsToLong = _assetsToRename.Count >= MAX_OBJECTS_FOR_AUTO_PREVIEW;

            EditorGUI.BeginDisabledGroup(listIsToLong);
            _forceAutoPreview = Toggle(new GUIContent("Auto Preview", $"Updates the Preview List every time anything in this Window changes.\nMax Object count for auto preview: {MAX_OBJECTS_FOR_AUTO_PREVIEW}"), _forceAutoPreview);
            EditorGUI.EndDisabledGroup();

            if (listIsToLong)
            {
                HelpBox($"Auto Prieview is disabled while more than {MAX_OBJECTS_FOR_AUTO_PREVIEW} Objects are selected.\nManual Preview still works.", MessageType.Info);
            }

            Space();
            BeginHorizontal();
            if (GUILayout.Button("Execute Renaming"))
            {
                ExecuteRenaming(preview: false);
            }
            if (GUILayout.Button("Preview Renaming"))
            {
                ExecuteRenaming(preview: true);
            }
            EndHorizontal();
            Space();

            DrawPreviewNames();

            if (GUI.changed)
            {
                _assetRenamer.UpdateOrderNames();

                if (_forceAutoPreview)
                {
                    ExecuteRenaming(preview: true);
                }
            }

            _thisSerializedObject.ApplyModifiedProperties();
        }

        private void OnDestroy()
        {
            s_visibleWindow = null;
            _richTextLable = null;
        }

        private void DrawRenameOrder(AssetRenameOrder renameOrder, int listIndex)
        {
            BeginHorizontal();
            _renameOrderFoldoutStatus[renameOrder] = Foldout(_renameOrderFoldoutStatus[renameOrder], renameOrder.OrderName);
            Space(10.0f);
            renameOrder.Execute = Toggle("Execute", renameOrder.Execute, GUILayout.Width(175.0f));
            if (GUILayout.Button(new GUIContent("X", "Delete Order"), GUILayout.Width(25.0f)))
            {
                _assetRenamer.RemoveOrder(renameOrder);
                GUI.FocusControl(null);
                EndHorizontal();
                return;
            }
            if (GUILayout.Button(new GUIContent("x2", "Duplicate Order"), GUILayout.Width(25.0f)))
            {
                AddNewOrder(_assetRenamer.CreateNewOrder(renameOrder));
                GUI.FocusControl(null);
            }
            EditorGUI.BeginDisabledGroup(listIndex <= 0);
            if (GUILayout.Button(new GUIContent("^", "Move Up"), GUILayout.MaxWidth(25.0f)))
            {
                _assetRenamer.MoveOrderUp(renameOrder, listIndex);
                GUI.FocusControl(null);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(listIndex >= _assetRenamer.RenameOrderCount - 1);
            if (GUILayout.Button(new GUIContent("v", "Move Down"), GUILayout.MaxWidth(25.0f)))
            {
                _assetRenamer.MoveOrderDown(renameOrder, listIndex);
                GUI.FocusControl(null);
            }
            EditorGUI.EndDisabledGroup();
            EndHorizontal();

            if (_renameOrderFoldoutStatus[renameOrder])
            {
                BeginVertical(GUI.skin.box);
                renameOrder.EditType = (EditType)EnumPopup("Edit Type", renameOrder.EditType);
                EditorGUI.indentLevel++;
                switch (renameOrder.EditType)
                {
                    case EditType.Rename:
                        renameOrder.RenameName = TextField("New Name", renameOrder.RenameName);
                        renameOrder.RenameSuffix = TextField("Suffix", renameOrder.RenameSuffix);
                        LabelField($"Example: {renameOrder.RenameName}{renameOrder.RenameSuffix}01");
                        break;
                    case EditType.Replace:
                        renameOrder.ReplaceText = TextField("Replace", renameOrder.ReplaceText);
                        renameOrder.ReplaceWithText = TextField("With", renameOrder.ReplaceWithText);
                        break;
                    case EditType.Insert:
                        renameOrder.InsertText = TextField("Insert Text", renameOrder.InsertText);
                        renameOrder.InsertIndex = Mathf.Max(0, IntField("Insert Index", renameOrder.InsertIndex));
                        renameOrder.ReverseInsert = Toggle("Reverse Insert", renameOrder.ReverseInsert);
                        break;
                    default:
                        break;
                }
                EditorGUI.indentLevel--;
                EndVertical();
            }
        }

        private void DrawPreviewNames()
        {
            if (_assetRenamer.PreviewNameCount <= 0)
            {
                return;
            }

            Space();
            EditorGUI.indentLevel++;

            _previewScrollPosition = BeginScrollView(_previewScrollPosition, GUILayout.MaxHeight(250.0f));
            BeginVertical(GUI.skin.box);

            EditorGUI.BeginDisabledGroup(true);
            for (int i = 0; i < _assetRenamer.PreviewNameCount; i++)
            {
                TextArea(_assetRenamer.GetPreviwNameAtIndex(i), _richTextLable);
                Space(3.0f);
            }
            EditorGUI.EndDisabledGroup();

            EndVertical();
            EditorGUI.indentLevel--;

            EndScrollView();
        }

        /// <param name="preview">if true only names are saved but not changed.</param>
        private void ExecuteRenaming(bool preview)
        {
            _assetRenamer.ClearPreviewNames();

            if (_assetsToRename.Count == 1)
            {
                _assetRenamer.ExecuteRenameOrders(_assetsToRename[0], preview: preview);
            }
            else
            {
                for (int i = 0; i < _assetsToRename.Count; i++)
                {
                    _assetRenamer.ExecuteRenameOrders(_assetsToRename[i], index: i, preview: preview);
                }
            }
        }

        private void AddNewOrder(AssetRenameOrder newAssetRenameOrder)
        {
            _assetRenamer.AddNewOrder(newAssetRenameOrder);
            _renameOrderFoldoutStatus.Add(newAssetRenameOrder, true);
        }

        private void RemoveOrder(AssetRenameOrder assetRenameOrderToRemove)
        {
            _assetRenamer.RemoveOrder(assetRenameOrderToRemove);
            _renameOrderFoldoutStatus.Remove(assetRenameOrderToRemove);
        }
    }
}