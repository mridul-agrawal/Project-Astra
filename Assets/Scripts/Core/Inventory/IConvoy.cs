namespace ProjectAstra.Core
{
    // Stub interface for the supply convoy (UM-08). UnitInventory and
    // InventoryAcquisition query Convoy.Current to decide whether the
    // "send to convoy" path is offered.
    public interface IConvoy
    {
        bool IsAvailable { get; }
        bool TryDeposit(InventoryItem item);
    }

    // Default no-op convoy. Used until ConvoyBootstrap swaps in a real
    // SupplyConvoy at scene start.
    public sealed class NullConvoy : IConvoy
    {
        public static readonly NullConvoy Instance = new();
        private NullConvoy() { }

        public bool IsAvailable => false;
        public bool TryDeposit(InventoryItem item) => false;
    }

    // Global Convoy pointer. Setting to null silently degrades to NullConvoy
    // so call sites never have to null-check.
    public static class Convoy
    {
        private static IConvoy _current = NullConvoy.Instance;

        public static IConvoy Current
        {
            get => _current;
            set => _current = value ?? NullConvoy.Instance;
        }
    }
}
