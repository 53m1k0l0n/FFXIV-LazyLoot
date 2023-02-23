using System.Runtime.InteropServices;

namespace LazyLoot.Util
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct LootItem
    {
        [FieldOffset(0)]
        public uint ObjectId;

        [FieldOffset(4)]
        public uint IndexRelatedToObject;

        [FieldOffset(8)]
        public uint ItemId;

        [FieldOffset(32)]
        public RollState RollState;

        [FieldOffset(36)]
        public RollOption RolledState;

        [FieldOffset(44)]
        public float LeftRollTime;

        public bool Rolled => RolledState > 0;

        public bool Valid => ObjectId != 3758096384U && ObjectId > 0U;
    }
}