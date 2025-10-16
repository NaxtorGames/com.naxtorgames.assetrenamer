namespace NaxtorGames.AssetRenamer.EditorScripts
{
    public struct FileNameData
    {
        public string FileName { get; set; }
        public string FileTypeEnding { get; }
        public bool IsAsset { get; set; }

        public readonly string FullFileName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.FileTypeEnding))
                {
                    return this.FileName;
                }
                else
                {
                    return $"{this.FileName}.{this.FileTypeEnding}";
                }
            }
        }

        public FileNameData(string fullFileName, bool isAsset)
        {
            if (string.IsNullOrWhiteSpace(fullFileName))
            {
                this.FileName = string.Empty;
                this.FileTypeEnding = string.Empty;
                this.IsAsset = false;
                return;
            }

            this.IsAsset = isAsset;

            if (!isAsset)
            {
                this.FileName = fullFileName;
                this.FileTypeEnding = string.Empty;
                return;
            }

            string[] splitForTypeEnding = fullFileName.Split('.');

            if (splitForTypeEnding.Length < 2)
            {
                this.FileName = fullFileName;
                this.FileTypeEnding = string.Empty;
            }
            else
            {
                string lastSplit = splitForTypeEnding[splitForTypeEnding.Length - 1];
                this.FileName = string.Join('_', splitForTypeEnding, 0, splitForTypeEnding.Length - 1);
                this.FileTypeEnding = lastSplit;
            }
        }
    }
}