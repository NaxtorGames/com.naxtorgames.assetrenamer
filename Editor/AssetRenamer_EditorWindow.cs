using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NaxtorGames.Utilities.EditorScripts;

using static UnityEditor.EditorGUILayout;

using Object = UnityEngine.Object;

namespace NaxtorGames.AssetRenamer.EditorScripts
{
    public sealed class AssetRenamer_EditorWindow : EditorWindow
    {
        private const string ARROW_UP = "\u2191";
        private const string ARROW_DOWN = "\u2193";
        private const string WINDOW_NAME = "Asset Renamer";
        private const int MAX_OBJECTS_FOR_AUTO_PREVIEW = 25;

        private static readonly Vector2 s_minWindowSize = new Vector2(375.0f, 220.0f);

        private static AssetRenamer_EditorWindow s_visibleWindow = null;
        private static GUIStyle s_windowStyle = null;
        private static GUIStyle s_richTextLabel = null;

        private static bool s_foldoutAssets = true;
        private static bool s_foldoutOrders = true;
        private static bool s_foldoutPreview = false;

        private static bool s_removeEmptiesOnValidate = true;
        private static bool s_removeDuplicatesOnValidate = true;

        private readonly AssetRenamer _assetRenamer = new AssetRenamer();
        private readonly Dictionary<RenameOrder, bool> _renameOrderFoldoutStatus = new Dictionary<RenameOrder, bool>();

        [SerializeField] private List<Object> _assetsToRename = new List<Object>();
        [SerializeField] private bool _enableAutoPreview = false;

        private SerializedObject _thisSerializedObject = null;
        private SerializedProperty _assetsToRenameProperty = null;

        private Vector2 _windowScrollPosition = Vector2.zero;
        private Vector2 _aseetsScrollPosition = Vector2.zero;
        private Vector2 _orderScrollPosition = Vector2.zero;
        private Vector2 _previewScrollPosition = Vector2.zero;

        private int _assetDuplicates = 0;
        private int _assetEmpties = 0;

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

        [MenuItem(UtilityData.TOOL_MENU_PATH + WINDOW_NAME)]
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

                if (_enableAutoPreview && s_foldoutPreview && _assetsToRename.Count < MAX_OBJECTS_FOR_AUTO_PREVIEW)
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

            s_foldoutAssets = Foldout(s_foldoutAssets, (s_foldoutAssets ? "Assets" : $"Assets ({_assetsToRename.Count})") + $" [Empties: {_assetEmpties} | Duplicates: {_assetDuplicates}]", true, EditorStyles.foldoutHeader);

            EditorGUI.BeginDisabledGroup(_assetsToRename.Count == 0);
            if (GUILayout.Button(new GUIContent("Check", "Check for duplicates or empty entries."), GUILayout.Width(50.0f)))
            {
                ValidateAssets(false, false, out _assetEmpties, out _assetDuplicates);
            }
            if (GUILayout.Button(new GUIContent("Clear", "Clear asset list."), GUILayout.Width(50.0f)))
            {
                _assetsToRename.Clear();
                _assetRenamer.ClearPreviewNames();
                _assetEmpties = 0;
                _assetDuplicates = 0;
            }
            EditorGUI.EndDisabledGroup();

            EndHorizontal();

            if (s_foldoutAssets)
            {
                EditorGUI.indentLevel++;

                if (_assetsToRename.Count > 0)
                {
                    _ = BeginHorizontal();

                    float fieldWidth = EditorGUIUtility.fieldWidth;
                    float labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.fieldWidth = 16.0f;
                    EditorGUIUtility.labelWidth = 75.0f;
                    s_removeEmptiesOnValidate = ToggleLeft(new GUIContent("Empties"), s_removeEmptiesOnValidate);
                    s_removeDuplicatesOnValidate = ToggleLeft(new GUIContent("Duplicates"), s_removeDuplicatesOnValidate);
                    EditorGUIUtility.fieldWidth = fieldWidth;
                    EditorGUIUtility.labelWidth = labelWidth;

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(new GUIContent("Remove", "Remove duplicates or empty entries when option is checked."), GUILayout.Width(103.0f)))
                    {
                        ValidateAssets(s_removeEmptiesOnValidate, s_removeDuplicatesOnValidate, out _assetEmpties, out _assetDuplicates);
                    }

                    EndHorizontal();
                }

                bool assetsChanged;

                if (_assetsToRenameProperty.isExpanded)
                {
                    _aseetsScrollPosition = BeginScrollView(_aseetsScrollPosition, GUI.skin.box,
                                GUILayout.MinHeight(64.0f - 8.0f + (1 * (EditorGUIUtility.singleLineHeight + 2.0f))),
                                GUILayout.MaxHeight(64.0f - 8.0f + (Mathf.Clamp(_assetsToRename.Count, 1, 12) * (EditorGUIUtility.singleLineHeight + 2.0f))));

                    EditorGUI.BeginChangeCheck();
                    _ = PropertyField(_assetsToRenameProperty, new GUIContent("", "Assets to rename"), true);
                    assetsChanged = EditorGUI.EndChangeCheck();

                    EndScrollView();
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    _ = PropertyField(_assetsToRenameProperty, new GUIContent("", "Assets to rename"), true);
                    assetsChanged = EditorGUI.EndChangeCheck();
                }

                if (assetsChanged)
                {
                    ValidateAssets(false, false, out _assetEmpties, out _assetDuplicates);
                }
                EditorGUI.indentLevel--;
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
                    const float SINGLE_ORDER_SIZE = 160.0f;
                    _orderScrollPosition = BeginScrollView(_orderScrollPosition,
                        GUILayout.MinHeight(_assetRenamer.OrderCount == 1 ? SINGLE_ORDER_SIZE : 0.0f));

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
                if (GUILayout.Button("Execute Orders"))
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

            if (_assetRenamer.PreviewNameCount == 0)
            {
                GUI.enabled = false;
                _ = Foldout(false, new GUIContent("Preview", "To preview items they have to be build first."), true, EditorStyles.foldoutHeader);
                GUI.enabled = true;
            }
            else
            {
                s_foldoutPreview = Foldout(s_foldoutPreview, "Preview", true, EditorStyles.foldoutHeader);
            }

            _enableAutoPreview = ToggleLeft(new GUIContent("Auto", $"Updates the Preview List every time anything in this Window changes.\nMax Object count for auto preview: {MAX_OBJECTS_FOR_AUTO_PREVIEW}"), _enableAutoPreview, GUILayout.Width(50.0f));

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
            if (GUILayout.Button(new GUIContent(ARROW_UP, "Move Up"), GUILayout.Width(25.0f)))
            {
                _assetRenamer.MoveOrderUp(renameOrder, listIndex);
                GUI.FocusControl(null);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(listIndex >= _assetRenamer.OrderCount - 1);
            if (GUILayout.Button(new GUIContent(ARROW_DOWN, "Move Down"), GUILayout.Width(25.0f)))
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
                        const int MIN_DIGITS = 0;
                        const int MAX_DIGITS = 8;

                        renameOrder.RenameName = TextField("New Name", renameOrder.RenameName);
                        renameOrder.RenameSuffix = TextField("Suffix", renameOrder.RenameSuffix);
                        renameOrder.RenameDigitsCount = IntSlider("Digits", renameOrder.RenameDigitsCount, MIN_DIGITS, MAX_DIGITS);
                        string numberSuffix = renameOrder.RenameDigitsCount > 0 ? 1.ToString(RenameOrder.FormatNumber(renameOrder.RenameDigitsCount)) : string.Empty;
                        LabelField($"Example: {renameOrder.RenameName}{renameOrder.RenameSuffix}{numberSuffix}");
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
                GUILayout.MaxHeight(16.0f + (Mathf.Min(6, _assetsToRename.Count - _assetEmpties) * EditorGUIUtility.singleLineHeight * 3.0f)));

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

        /// <param name="preview">if true only names are saved but not changed.</param>
        private void ExecuteRenaming(bool preview)
        {
            _assetRenamer.ClearPreviewNames();

            if (_assetsToRename.Count == 1)
            {
                _assetRenamer.ExecuteRenameOrders(_assetsToRename[0], isPreview: preview);
            }
            else
            {
                for (int i = 0; i < _assetsToRename.Count; i++)
                {
                    _assetRenamer.ExecuteRenameOrders(_assetsToRename[i], index: i, isPreview: preview);
                }
            }
        }

        private void ValidateAssets(
            bool removeEmpties,
            bool removeDuplicates,
            out int empties,
            out int duplicates)
        {
            if (_assetsToRename == null || _assetsToRename.Count == 0)
            {
                empties = 0;
                duplicates = 0;
                return;
            }

            if (removeEmpties)
            {
                _ = _assetsToRename.RemoveAll(asset => asset == null);
            }

            if (removeDuplicates)
            {
                _assetsToRename = _assetsToRename.Distinct().ToList();
                duplicates = 0;
            }
            else
            {
                duplicates = _assetsToRename
                    .GroupBy(asset => asset)
                    .Where(group => group.Count() > 1)
                    .Count();
            }

            empties = _assetsToRename.Count(asset => asset == null);
        }
    }
}