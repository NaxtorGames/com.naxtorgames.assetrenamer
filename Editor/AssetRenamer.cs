using System.Collections.Generic;

using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;
using AssetDatabase = UnityEditor.AssetDatabase;

namespace NaxtorGames.AssetRenamer.EditorScripts
{
    public sealed class AssetRenamer
    {
        public int RenameOrderCount => RenameOrders.Count;
        public int PreviewNameCount => PreviewNames.Count;

        private readonly List<string> PreviewNames = new List<string>();
        private readonly List<AssetRenameOrder> RenameOrders = new List<AssetRenameOrder>();

        public AssetRenameOrder CreateNewOrder(EditType editType = EditType.Rename)
        {
            AssetRenameOrder newOrder = new AssetRenameOrder(editType);

            return newOrder;
        }

        public AssetRenameOrder CreateNewOrder(AssetRenameOrder assetRenameOrderToCopy)
        {
            AssetRenameOrder newOrder = new AssetRenameOrder(assetRenameOrderToCopy);

            return newOrder;
        }

        public void AddNewOrder(AssetRenameOrder newAssetRenameOrder)
        {
            RenameOrders.Add(newAssetRenameOrder);
        }

        public void RemoveOrder(AssetRenameOrder assetRenameOrderToRemove)
        {
            RenameOrders.Remove(assetRenameOrderToRemove);
        }

        public AssetRenameOrder GetRenameOrderAtIndex(int index)
        {
            if (index < 0 || index >= RenameOrderCount)
            {
                Debug.LogWarning("Index is out of Range");
                return null;
            }

            return RenameOrders[index];
        }

        public void MoveOrderUp(AssetRenameOrder assetRenameOrder, int currentIndex)
        {
            int newIndex = currentIndex - 1;
            MoveOrder(assetRenameOrder, currentIndex, newIndex);
        }

        public void MoveOrderDown(AssetRenameOrder assetRenameOrder, int currentIndex)
        {
            int newIndex = currentIndex + 1;
            MoveOrder(assetRenameOrder, currentIndex, newIndex);
        }

        public void MoveOrder(AssetRenameOrder assetRenameOrder, int currentIndex, int newIndex)
        {
            RenameOrders.RemoveAt(currentIndex);
            RenameOrders.Insert(newIndex, assetRenameOrder);
        }

        public void ExecuteRenameOrders(Object asset, int index = -1, bool preview = false)
        {
            if (asset == null)
            {
                return;
            }

            string assetName = GetAssetName(asset, out string assetPath);

            FileNameData fileNameData = new FileNameData(assetName);

            foreach (AssetRenameOrder renameOrder in RenameOrders)
            {
                renameOrder.ExecuteOrder(asset, ref fileNameData, index);
            }

            if (preview)
            {
                AddToPreviewNameList(assetName, fileNameData.FullFileName, index);
            }
            else
            {
                AssetDatabase.RenameAsset(assetPath, fileNameData.FullFileName);
            }
        }

        public void AddToPreviewNameList(string currentName, string newName, int elementIndex)
        {
            if (elementIndex == -1)
            {
                elementIndex = 0;
            }

            PreviewNames.Add($"<b>Element {elementIndex}:</b>\n<b>{currentName}</b>\n<b>{newName}</b>");
        }

        public string GetPreviwNameAtIndex(int index)
        {
            if (index < 0 || index >= PreviewNameCount)
            {
                Debug.LogWarning("Index is out of Range");
                return null;
            }

            return PreviewNames[index];
        }

        public void ClearPreviewNames()
        {
            PreviewNames.Clear();
        }

        public void ClearOrders()
        {
            RenameOrders.Clear();
        }

        public void UpdateOrderNames()
        {
            foreach (AssetRenameOrder renameOrder in RenameOrders)
            {
                renameOrder.UpdateOrderName();
            }
        }

        private static string GetAssetName(Object asset, out string assetPath)
        {
            assetPath = AssetDatabase.GetAssetPath(asset);

            return GetAssetNameFromPath(assetPath);
        }

        private static string GetAssetNameFromPath(string assetPath)
        {
            string[] splittedPath = assetPath.Split('/');

            return splittedPath[splittedPath.Length - 1];
        }
    }
}