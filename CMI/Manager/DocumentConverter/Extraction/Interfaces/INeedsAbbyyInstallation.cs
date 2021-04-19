namespace CMI.Manager.DocumentConverter.Extraction.Interfaces
{
    public interface INeedsAbbyyInstallation
    {
        string PathToAbbyyFrEngineDll { get; set; }
        bool PathToAbbyFrEngineDllHasBeenSet { get; set; }
        string MissingAbbyyPathInstallationMessage { get; set; }
        bool MissingAbbyyPathInstallationMessageHasBeenSet { get; set; }
    }
}