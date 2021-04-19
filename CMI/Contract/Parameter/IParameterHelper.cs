namespace CMI.Contract.Parameter
{
    public interface IParameterHelper
    {
        /// <summary>
        ///     Ein Setting sollte nicht oder nur sehr kurz zwischengespeichert werden. Sonst sollte das Setting mit dieser Methode
        ///     geholt werden.
        /// </summary>
        T GetSetting<T>() where T : ISetting;

        string SaveSetting<T>(Parameter parameter) where T : ISetting;
    }
}