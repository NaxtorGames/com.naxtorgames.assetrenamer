using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NaxtorGames.Utillity.EditorScripts;

using static UnityEditor.EditorGUILayout;

namespace NaxtorGames.AssetRenamer.EditorScripts
{
    public sealed class AssetRenamer_EditorWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Asset Renamer";
        private const int MAX_OBJECTS_FOR_AUTO_PREVIEW = 25;

        private static readonly Vector2 s_minWindowSize = new Vector2(375.0f, 220.0f);

        private static AssetRenamer_EditorWindow s_visibleWindow = null;
        private static GUIStyle s_windowStyle = null;
        private static GUIStyle s_richTextLabel = null;

        private static bool s_foldoutAssets = true;
        private static bool s_foldoutOrders = true;
        private static bool s_foldoutPreview = false;

        private readonly AssetRenamer _assetRenamer = new AssetRenamer();
        private readonly Dictionary<RenameOrder, bool> _renameOrderFoldoutStatus = new Dictionary<RenameOrder, bool>();

        [SerializeField] private List<Object> _assetsToRename = new List<Object>();
        [SerializeField] private bool _forceAutoPreview = false;

        private SerializedObject _thisSerializedObject = null;
        private SerializedProperty _assetsToRenameProperty = null;

        private Vector2 _windowScrollPosition = Vector2.zero;
        private Vector2 _aseetsScrollPosition = Vector2.zero;
        private Vector2 _orderScrollPosition = Vector2.zero;
        private Vector2 _previewScrollPosition = Vector2.zero;

        private static GUIStyle RichTextLabel
        {
            get
            {
                s_richTextLabel ??= new GUIStyle(GUI.skin.label)
                {
                    richText = true
                };

                return s_richTextLabel;
            }
        }
        private static GUIStyle WindowStyle
        {
            get
            {
                s_windowStyle ??= new GUIStyle(GUI.skin.window)
                {
                    margin = new RectOffset(4, 4, 4, 2),
                    padding = new RectOffset(2, 2, 6, 4)
                };

                return s_windowStyle;
            }
        }

        [MenuItem(UtillityData.TOOL_MENU_PATH + WINDOW_NAME)]
        public static void OpenWindow()
        {
            if (s_visibleWindow == null)
            {
                s_visibleWindow = GetWindow<AssetRenamer_EditorWindow>(WINDOW_NAME);
                return;
            }

            s_visibleWindow.minSize = new Vector2(128.0f, 296.0f);

            Rect windowRect = s_visibleWindow.position;
            windowRect.height = 296.0f;
            s_visibleWindow.position = windowRect;

            s_visibleWindow.Show();
        }

        private void OnEnable()
        {
            this.minSize = s_minWindowSize;
            _assetsToRename ??= new List<Object>();

            _thisSerializedObject = new SerializedObject(this);
            _assetsToRenameProperty = EditorHelpers.CreatePropertyField(_thisSerializedObject, nameof(_assetsToRename));

            _renameOrderFoldoutStatus.Clear();
            for (int i = 0; i < _assetRenamer.OrderCount; i++)
            {
                _renameOrderFoldoutStatus.Add(_assetRenamer.GetRenameOrderAtIndex(i), false);
            }
        }

        private void OnGUI()
        {
            _thisSerializedObject.Update();

            Space();

            _windowScrollPosition = BeginScrollView(_windowScrollPosition);

            DrawAssetList();

            DrawOrders();

            GUILayout.FlexibleSpace();

            DrawPreview();

            EndScrollView();

            if (GUI.changed)
            {
                _assetRenamer.UpdateOrderNames();

                if (_forceAutoPreview)
                {
                    ExecuteRenaming(preview: true);
                }
            }

            _ = _thisSerializedObject.ApplyModifiedProperties();
        }

        private void OnDestroy()
        {
            s_visibleWindow = null;
        }

        private void DrawAssetList()
        {
            _ = BeginVertical(WindowStyle);

            _ = BeginHorizontal();

            s_foldoutAssets = Foldout(s_foldoutAssets, s_foldoutAssets ? "Assets" : $"Assets ({_assetsToRename.Count})", true, EditorStyles.foldoutHeader);

            EditorGUI.BeginDisabledGroup(_assetsToRename.Count <= 0);
            if (GUILayout.Button("Clear", GUILayout.Width(75.0f)))
            {
                _assetsToRename.Clear();
                _assetRenamer.ClearPreviewNames();
            }
            EditorGUI.EndDisabledGroup();

            EndHorizontal();

            if (s_foldoutAssets)
            {
                if (_assetsToRenameProperty.isExpanded)
                {
                    _aseetsScrollPosition = BeginScrollView(_aseetsScrollPosition, GUI.skin.box,
                                GUILayout.MinHeight(64.0f - 8.0f + (1 * (EditorGUIUtility.singleLineHeight + 2.0f))),
                                GUILayout.MaxHeight(64.0f - 8.0f + (Mathf.Clamp(_assetsToRename.Count, 1, 12) * (EditorGUIUtility.singleLineHeight + 2.0f))));

                    EditorHelpers.DrawPropertyField(_assetsToRenameProperty, true);

                    EndScrollView();
                }
                else
                {
                    EditorHelpers.DrawPropertyField(_assetsToRenameProperty, true);
                }
            }

            EndVertical();
        }

        private void DrawOrders()
        {
            _ = BeginVertical(WindowStyle);

            _ = BeginHorizontal();

            s_foldoutOrders = Foldout(s_foldoutOrders, $"Orders ({_assetRenamer.OrderCount})", true, EditorStyles.foldoutHeader);

            if (GUILayout.Button("Add", GUILayout.Width(50.0f)))
            {
                AddNewOrder(_assetRenamer.CreateNewOrder());
                GUI.FocusControl(null);
            }
            if (_assetRenamer.OrderCount > 0 && GUILayout.Button("Clear", GUILayout.Width(50.0f)))
            {
                _assetRenamer.ClearOrders();
                _renameOrderFoldoutStatus.Clear();
                GUI.FocusControl(null);
            }
            EndHorizontal();

            if (s_foldoutOrders)
            {
                if (_assetRenamer.OrderCount > 0)
                {
                    _orderScrollPosition = BeginScrollView(_orderScrollPosition,
                        GUILayout.MinHeight(_assetRenamer.OrderCount == 1 ? 128.0f : 0.0f));

                    for (int i = 0; i < _assetRenamer.OrderCount; i++)
                    {
                        DrawRenameOrder(_assetRenamer.GetRenameOrderAtIndex(i), i);
                    }

                    GUILayout.FlexibleSpace();
                    EndScrollView();
                }
                else
                {
                    HelpBox("No orders available!", MessageType.Info);
                }

                _ = BeginHorizontal();
                EditorGUI.BeginDisabledGroup(_assetsToRename.Count == 0 || _assetRenamer.OrderCount == 0);
                if (GUILayout.Button("Execute Renaming"))
                {
                    ExecuteRenaming(preview: false);
                }
                EditorGUI.EndDisabledGroup();
                EndHorizontal();
            }

            EndVertical();
        }

        private void DrawPreview()
        {
            _ = BeginVertical(WindowStyle);

            _ = BeginHorizontal();

            s_foldoutPreview = Foldout(s_foldoutPreview, "Preview", true, EditorStyles.foldoutHeader);

            _forceAutoPreview = ToggleLeft(new GUIContent("Auto", $"Updates the Preview List every time anything in this Window changes.\nMax Object count for auto preview: {MAX_OBJECTS_FOR_AUTO_PREVIEW}"), _forceAutoPreview, GUILayout.Width(50.0f));

            EditorGUI.BeginDisabledGroup(_assetsToRename.Count == 0 || _assetRenamer.OrderCount == 0);
            if (GUILayout.Button("Update", GUILayout.Width(75.0f)))
            {
                ExecuteRenaming(preview: true);
            }
            EditorGUI.EndDisabledGroup();
            EndHorizontal();

            if (s_foldoutPreview)
            {
                bool listIsToLong = _assetsToRename.Count > MAX_OBJECTS_FOR_AUTO_PREVIEW;
                if (listIsToLong)
                {
                    HelpBox($"Auto Prieview is disabled while more than {MAX_OBJECTS_FOR_AUTO_PREVIEW} Objects are selected.\nManual Preview still works.", MessageType.Info);
                }

                DrawPreviewNames();
            }


            EndVertical();
        }

        private void DrawRenameOrder(RenameOrder renameOrder, int listIndex)
        {
            _ = BeginVertical(WindowStyle);
            _ = BeginHorizontal();
            _renameOrderFoldoutStatus[renameOrder] = Foldout(_renameOrderFoldoutStatus[renameOrder], renameOrder.OrderName, true, EditorStyles.foldoutHeader);
            renameOrder.Execute = ToggleLeft(new GUIContent("", "Should the order be executed?"), renameOrder.Execute, GUILayout.Width(16.0f));
            if (GUILayout.Button(new GUIContent("x2", "Duplicate Order"), GUILayout.Width(25.0f)))
            {
                AddNewOrder(_assetRenamer.CreateNewOrder(renameOrder));
                GUI.FocusControl(null);
            }
            EditorGUI.BeginDisabledGroup(listIndex <= 0);
            if (GUILayout.Button(new GUIContent("^", "Move Up"), GUILayout.Width(25.0f)))
            {
                _assetRenamer.MoveOrderUp(renameOrder, listIndex);
                GUI.FocusControl(null);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(listIndex >= _assetRenamer.OrderCount - 1);
            if (GUILayout.Button(new GUIContent("v", "Move Down"), GUILayout.Width(25.0f)))
            {
                _assetRenamer.MoveOrderDown(renameOrder, listIndex);
                GUI.FocusControl(null);
            }
            EditorGUI.EndDisabledGroup();
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button(new GUIContent("X", "Delete Order"), GUILayout.Width(25.0f)))
            {
                RemoveOrder(renameOrder);
                GUI.FocusControl(null);
                EndHorizontal();

                EndVertical();
                return;
            }
            GUI.backgroundColor = defaultColor;
            EndHorizontal();

            if (_renameOrderFoldoutStatus[renameOrder])
            {
                _ = BeginVertical(WindowStyle);
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

            EndVertical();
        }

        private void DrawPreviewNames()
        {
            if (_assetRenamer.PreviewNameCount <= 0)
            {
                return;
            }

            _ = BeginVertical(WindowStyle);
            _previewScrollPosition = BeginScrollView(_previewScrollPosition,
                GUILayout.MinHeight(16.0f + (1 * EditorGUIUtility.singleLineHeight * 3.0f)),
                GUILayout.MaxHeight(16.0f + (Mathf.Min(6, _assetsToRename.Count) * EditorGUIUtility.singleLineHeight * 3.0f)));

            EditorGUI.BeginDisabledGroup(true);
            for (int i = 0; i < _assetRenamer.PreviewNameCount; i++)
            {
                _ = BeginVertical(WindowStyle, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight * 4.0f));
                _ = TextArea(_assetRenamer.GetPreviewNameAtIndex(i), RichTextLabel);
                EndVertical();
            }
            EditorGUI.EndDisabledGroup();
            EndScrollView();

            EndVertical();
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

        private void AddNewOrder(RenameOrder newAssetRenameOrder)
        {
            _assetRenamer.AddNewOrder(newAssetRenameOrder);
            _renameOrderFoldoutStatus.Add(newAssetRenameOrder, true);
        }

        private void RemoveOrder(RenameOrder assetRenameOrderToRemove)
        {
            _assetRenamer.RemoveOrder(assetRenameOrderToRemove);
            _ = _renameOrderFoldoutStatus.Remove(assetRenameOrderToRemove);
        }
    }
}