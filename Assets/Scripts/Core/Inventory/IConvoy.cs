namespace ProjectAstra.Core
{
    /// <summary>
    /// Stub interface for the supply convoy (UM-08). UnitInventory and InventoryAcquisition
    /// query Convoy.Current to decide whether the "send to convoy" path is offered.
    /// </summary>
    public interface IConvoy
    {
        bool IsAvailable { get; }
        bool TryDeposit(InventoryItem item);
    }

    public sealed class NullConvoy : IConvoy
    {
        public static readonly NullConvoy Instance = new();
        private NullConvoy() { }

        public bool IsAvailable => false;
        public bool TryDeposit(InventoryItem item) => false;
    }

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
