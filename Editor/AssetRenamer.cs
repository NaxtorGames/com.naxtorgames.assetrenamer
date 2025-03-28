using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace NaxtorGames.AssetRenamer.EditorScripts
{
    public sealed class AssetRenamer
    {
        public enum ObjectType
        {
            None,
            Asset,
            SceneObject
        }

        private readonly List<string> _previewNames = new List<string>();
        private readonly List<RenameOrder> _renameOrders = new List<RenameOrder>();

        public int OrderCount => _renameOrders.Count;
        public int PreviewNameCount => _previewNames.Count;

        public RenameOrder CreateNewOrder(EditType editType = EditType.Rename)
        {
            return new RenameOrder(editType);
        }

        public RenameOrder CreateNewOrder(RenameOrder renameOrderToCopy)
        {
            return new RenameOrder(renameOrderToCopy);
        }

        public void AddNewOrder(RenameOrder newAssetRenameOrder)
        {
            _renameOrders.Add(newAssetRenameOrder);
        }

        public void RemoveOrder(RenameOrder renameOrderToRemove)
        {
            _ = _renameOrders.Remove(renameOrderToRemove);
        }

        public RenameOrder GetRenameOrderAtIndex(int index)
        {
            if (index < 0 || index >= this.OrderCount)
            {
                Debug.LogWarning("Index is out of Range");
                return null;
            }

            return _renameOrders[index];
        }

        public void MoveOrderUp(RenameOrder renameOrder, int currentIndex)
        {
            int newIndex = currentIndex - 1;
            MoveOrder(renameOrder, currentIndex, newIndex);
        }

        public void MoveOrderDown(RenameOrder renameOrder, int currentIndex)
        {
            int newIndex = currentIndex + 1;
            MoveOrder(renameOrder, currentIndex, newIndex);
        }

        public void MoveOrder(RenameOrder renameOrder, int currentIndex, int newIndex)
        {
            _renameOrders.RemoveAt(currentIndex);
            _renameOrders.Insert(newIndex, renameOrder);
        }

        public void ExecuteRenameOrders(Object objectInstance, int index = -1, bool preview = false)
        {
            if (objectInstance == null)
            {
                return;
            }

            ObjectType objectType = GetAssetName(objectInstance, out string assetName, out string assetPath);

            if (objectType == ObjectType.None)
            {
                Debug.LogError($"{objectInstance.name} is neither an asset nor an scene object.");
                return;
            }

            FileNameData fileNameData = new FileNameData(assetName);

            foreach (RenameOrder renameOrder in _renameOrders)
            {
                _ = renameOrder.ExecuteOrder(ref fileNameData, index);
            }

            if (preview)
            {
                AddToPreviewNameList(assetName, fileNameData.FullFileName, index, objectType == ObjectType.Asset);
            }
            else
            {
                if (objectType == ObjectType.Asset)
                {
                    string result = AssetDatabase.RenameAsset(assetPath, fileNameData.FullFileName);

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        Debug.LogError(result);
                    }
                }
                else if (objectType == ObjectType.SceneObject)
                {
                    Undo.RecordObject(objectInstance, $"Scene object renamed");
                    objectInstance.name = fileNameData.FileName;
                }
            }
        }

        public void AddToPreviewNameList(string currentName, string newName, int elementIndex, bool isAsset)
        {
            if (elementIndex == -1)
            {
                elementIndex = 0;
            }

            _previewNames.Add($"<b>Element {elementIndex}: ({(isAsset ? "Asset" : "Scene Object")})</b>\n<b>{currentName}</b>\n<b>{newName}</b>");
        }

        public string GetPreviewNameAtIndex(int index)
        {
            if (index < 0 || index >= this.PreviewNameCount)
            {
                Debug.LogWarning("Index is out of Range");
                return null;
            }

            return _previewNames[index];
        }

        public void ClearPreviewNames()
        {
            _previewNames.Clear();
        }

        public void ClearOrders()
        {
            _renameOrders.Clear();
        }

        public void UpdateOrderNames()
        {
            foreach (RenameOrder renameOrder in _renameOrders)
            {
                renameOrder.UpdateOrderName();
            }
        }

        private static ObjectType GetAssetName(Object asset, out string assetName, out string assetPath)
        {
            if (asset == null)
            {
                assetName = string.Empty;
                assetPath = string.Empty;
                return ObjectType.None;
            }

            assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                assetName = asset.name;
                assetPath = string.Empty;
                return ObjectType.SceneObject;
            }
            else
            {
                assetName = GetAssetNameFromPath(assetPath);
                return ObjectType.Asset;
            }
        }

        private static string GetAssetNameFromPath(string assetPath)
        {
            string[] splitPath = assetPath.Split('/');

            return splitPath[splitPath.Length - 1];
        }
    }
}