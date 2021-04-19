namespace CMI.Contract.Common
{
    /// <summary>
    ///     Indicates how long a file will be allowed to remain in the cache
    /// </summary>
    public enum CacheRetentionCategory
    {
        UsageCopyPublic, // öffentliche Gebrauchskopien
        UsageCopyEB, // Gebrauchskopien unter Schutzfrist mit Einsichtsbewilligung(EB)
        UsageCopyAB, // Gebrauchskopien unter Schutzfrist mit Auskunftsgesuchsbewilligung(AB)
        UsageCopyBarOrAS, // Gebrauchskopien unter Schutzfrist für BAR- und AS-Benutzer
        UsageCopyBenutzungskopie // Gebrauchskopien die aus einer Benutzungskopie erstellt wurden
    }
}