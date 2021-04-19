namespace CMI.Contract.Asset
{
    public class AssetPackageSizeDefinition
    {
        public int MaxSmallSizeInMB { get; set; }
        public int MaxMediumSizeInMB { get; set; }
        public int MaxLargeSizeInMB { get; set; }
        public int ExtraLargeSizeInMB { get; set; }
    }
}