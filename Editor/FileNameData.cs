namespace NaxtorGames.AssetRenamer.EditorScripts
{
    public struct FileNameData
    {
        public string FileName { get; set; }
        public string FileTypeEnding { get; }

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

        public FileNameData(string fullFileName)
        {
            if (string.IsNullOrWhiteSpace(fullFileName))
            {
                this.FileName = string.Empty;
                this.FileTypeEnding = string.Empty;
                return;
            }

            string[] splitForTypeEnding = fullFileName.Split('.');

            this.FileName = splitForTypeEnding[0];
            if (splitForTypeEnding.Length < 2)
            {
                this.FileTypeEnding = string.Empty;
            }
            else
            {
                this.FileTypeEnding = splitForTypeEnding[splitForTypeEnding.Length - 1];
            }
        }
    }
}