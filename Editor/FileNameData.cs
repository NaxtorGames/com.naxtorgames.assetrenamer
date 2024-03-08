using Debug = UnityEngine.Debug;

namespace NaxtorGames.AssetRenamer.EditorScripts
{
    public struct FileNameData
    {
        public string FileName;
        public readonly string FileTypeEnding;

        public readonly string FullFileName
        {
            get
            {
                if (string.IsNullOrEmpty(FileTypeEnding))
                {
                    return FileName;
                }
                else
                {
                    return $"{FileName}.{FileTypeEnding}";
                }
            }
        }

        public FileNameData(string fullFileName)
        {
            if (string.IsNullOrEmpty(fullFileName))
            {
                Debug.LogError("Name is Null or Empty");
                FileName = string.Empty;
                FileTypeEnding = string.Empty;
                return;
            }

            string[] splitForTypeEnding = fullFileName.Split('.');

            FileName = splitForTypeEnding[0];
            if (splitForTypeEnding.Length < 2)
            {
                FileTypeEnding = string.Empty;
            }
            else
            {
                FileTypeEnding = splitForTypeEnding[splitForTypeEnding.Length - 1];
            }
        }
    }
}