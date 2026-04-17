namespace ProjectAstra.Core.UI
{
    // HM-08 / MT-04. Gates Unit Info Panel open on a unit that has not yet been
    // revealed by torchlight. Once revealed, a unit remains revealed for the
    // chapter — persistence is the provider's responsibility.
    //
    // Stub implementation returns true for every unit (nothing hidden).
    public interface IFogOfWarProvider
    {
        bool IsRevealed(UnitInstance unit);
    }
}
