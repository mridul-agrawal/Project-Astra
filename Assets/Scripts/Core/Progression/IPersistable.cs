namespace ProjectAstra.Core.Progression
{
    // Seam for the future campaign-save ticket. Any subsystem that owns
    // session-scoped data which will eventually need to survive a restart
    // implements this. Serialize() returns a plain DTO that JsonUtility or
    // a future binary serializer can consume; Restore(DTO) rebuilds the
    // subsystem from one.
    //
    // No call sites today. The save-system ticket will walk all IPersistable
    // instances and snapshot them.
    public interface IPersistable<TDto>
    {
        TDto Serialize();
        void Restore(TDto dto);
    }
}
