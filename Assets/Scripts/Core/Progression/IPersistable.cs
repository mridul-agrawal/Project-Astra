namespace ProjectAstra.Core.Progression
{
    /// <summary>
    /// Seam for the future campaign-save ticket. Any subsystem that owns
    /// session-scoped data which will eventually need to survive a restart
    /// implements this. `Serialize()` returns a plain DTO that JsonUtility or
    /// a future binary serialiser can consume; `Restore(DTO)` rebuilds the
    /// subsystem from one.
    ///
    /// Today: no call-site. Tomorrow: a `CampaignSaveSystem` walks all
    /// IPersistable instances and snapshots them.
    /// </summary>
    public interface IPersistable<TDto>
    {
        TDto Serialize();
        void Restore(TDto dto);
    }
}
