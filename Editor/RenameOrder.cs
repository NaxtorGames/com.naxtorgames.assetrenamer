using Serializable = System.SerializableAttribute;
using Math = System.Math;
using StringBuilder = System.Text.StringBuilder;
using HideInInspector = UnityEngine.HideInInspector;
using Object = UnityEngine.Object;

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

        public bool ExecuteOrder(ref FileNameData fileNameData, int index = -1)
        {
            if (!Execute)
            {
                return false;
            }

            return EditType switch
            {
                EditType.Rename => Rename(ref fileNameData, index),
                EditType.Replace => Replace(ref fileNameData),
                EditType.Insert => Insert(ref fileNameData),
                _ => false
            };
        }

        private bool Rename(ref FileNameData fileNameData, int index = -1)
        {
            if (index == -1)
            {
                fileNameData.FileName = $"{RenameName}{RenameSuffix}";
            }
            else
            {
                fileNameData.FileName = $"{RenameName}{RenameSuffix}{index + 1:00}";
            }

            return true;
        }

        private bool Replace(ref FileNameData fileNameData)
        {
            if (string.IsNullOrEmpty(ReplaceText) || !fileNameData.FileName.Contains(ReplaceText))
            {
                return false;
            }

            fileNameData.FileName = fileNameData.FileName.Replace(ReplaceText, ReplaceWithText);
            return true;
        }

        private bool Insert(ref FileNameData fileNameData)
        {
            if (string.IsNullOrEmpty(InsertText))
            {
                return false;
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
    }
}