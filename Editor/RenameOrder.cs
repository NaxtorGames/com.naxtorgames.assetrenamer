using Serializable = System.SerializableAttribute;
using Math = System.Math;
using StringBuilder = System.Text.StringBuilder;
using HideInInspector = UnityEngine.HideInInspector;

namespace NaxtorGames.AssetRenamer.EditorScripts
{
    [Serializable]
    public sealed class RenameOrder
    {
        [HideInInspector]
        public string OrderName;
        public bool Execute;
        public EditType EditType;

        //Rename
        public string RenameName;
        public string RenameSuffix;
        public int RenameDigitsCount = 2;

        //Replace
        public string ReplaceText;
        public string ReplaceWithText;

        //Insert
        public string InsertText;
        public int InsertIndex;
        public bool ReverseInsert;

        public RenameOrder(EditType editType = EditType.Rename)
        {
            Execute = true;
            EditType = editType;
            OrderName = null;

            RenameName = string.Empty;
            RenameSuffix = "_";
            RenameDigitsCount = 2;

            ReplaceText = string.Empty;
            ReplaceWithText = string.Empty;

            InsertText = string.Empty;
            InsertIndex = 0;
            ReverseInsert = false;

            UpdateOrderName();
        }

        /// <summary>
        /// A deep copy of an Asset Rename Order.
        /// </summary>
        public RenameOrder(RenameOrder assetRenameOrderToCopy)
        {
            Execute = assetRenameOrderToCopy.Execute;
            EditType = assetRenameOrderToCopy.EditType;

            RenameName = new string(assetRenameOrderToCopy.RenameName);
            RenameSuffix = new string(assetRenameOrderToCopy.RenameSuffix);
            RenameDigitsCount = assetRenameOrderToCopy.RenameDigitsCount;

            ReplaceText = new string(assetRenameOrderToCopy.ReplaceText);
            ReplaceWithText = new string(assetRenameOrderToCopy.ReplaceWithText);

            InsertText = new string(assetRenameOrderToCopy.InsertText);
            InsertIndex = assetRenameOrderToCopy.InsertIndex;
            ReverseInsert = assetRenameOrderToCopy.ReverseInsert;

            UpdateOrderName();
        }

        public void UpdateOrderName()
        {
            OrderName = EditType switch
            {
                EditType.Rename => "Rename",
                EditType.Replace => "Replace",
                EditType.Insert => "Insert",
                _ => "None",
            };
        }

        public bool ExecuteOrder(ref FileNameData fileNameData, bool isPreview, int index = -1)
        {
            if (!Execute)
            {
                return false;
            }

            return EditType switch
            {
                EditType.Rename => Rename(ref fileNameData, isPreview, index),
                EditType.Replace => Replace(ref fileNameData, isPreview),
                EditType.Insert => Insert(ref fileNameData, isPreview),
                _ => false
            };
        }

        private bool Rename(ref FileNameData fileNameData, bool isPreview, int index = -1)
        {
            if (fileNameData.IsAsset && (RenameName.Contains('.') || RenameSuffix.Contains('.')))
            {
                string error = $"Cannot rename '{fileNameData.FullFileName}' — the new name or suffix contains a '.' which is not allowed for assets.";
                if (isPreview)
                {
                    UnityEngine.Debug.LogWarning(error);
                }
                else
                {
                    throw new System.ArgumentException(error, $"{nameof(RenameName)} / {nameof(RenameSuffix)}");
                }
            }

            if (index == -1)
            {
                fileNameData.FileName = $"{RenameName}{RenameSuffix}";
            }
            else
            {
                string numberSuffix = RenameDigitsCount > 0 ? (index + 1).ToString(FormatNumber(RenameDigitsCount)) : string.Empty;
                fileNameData.FileName = $"{RenameName}{RenameSuffix}{numberSuffix}";
            }

            return true;
        }

        private bool Replace(ref FileNameData fileNameData, bool isPreview)
        {
            if (string.IsNullOrEmpty(ReplaceText) || !fileNameData.FileName.Contains(ReplaceText))
            {
                return false;
            }

            if (fileNameData.IsAsset && ReplaceText.Contains('.'))
            {
                string error = $"Cannot replace '{ReplaceText}' from '{fileNameData.FullFileName}' — the replace text contains a '.' which is not allowed for assets.";
                if (isPreview)
                {
                    UnityEngine.Debug.LogWarning(error);
                }
                else
                {
                    throw new System.ArgumentException(error, nameof(ReplaceText));
                }
            }

            fileNameData.FileName = fileNameData.FileName.Replace(ReplaceText, ReplaceWithText);
            return true;
        }

        private bool Insert(ref FileNameData fileNameData, bool isPreview)
        {
            if (string.IsNullOrEmpty(InsertText))
            {
                return false;
            }

            if (fileNameData.IsAsset && InsertText.Contains('.'))
            {
                string error = $"Cannot insert '{InsertText}' to '{fileNameData.FullFileName}' - the insert text contains a '.' which is not allowed for assets.";
                if (isPreview)
                {
                    UnityEngine.Debug.LogWarning(error);
                }
                else
                {
                    throw new System.ArgumentException(error, nameof(InsertText));
                }
            }

            int insertIndex = Math.Clamp(InsertIndex, 0, fileNameData.FileName.Length);

            if (ReverseInsert)
            {
                int indexToInsert = Math.Max(0, fileNameData.FileName.Length - insertIndex);
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < fileNameData.FileName.Length; j++)
                {
                    if (insertIndex > 0 && j == indexToInsert)
                    {
                        _ = sb.Append(InsertText);
                    }

                    _ = sb.Append(fileNameData.FileName[j]);
                }
                if (insertIndex == 0)
                {
                    _ = sb.Append(InsertText);
                }

                fileNameData.FileName = sb.ToString();
            }
            else
            {
                fileNameData.FileName = fileNameData.FileName.Insert(insertIndex, InsertText);
            }

            return true;
        }

        public static string FormatNumber(int number)
        {
            string suffix = string.Empty;
            for (int i = 0; i < number; i++)
            {
                suffix += "0";
            }
            return suffix;
        }
    }
}