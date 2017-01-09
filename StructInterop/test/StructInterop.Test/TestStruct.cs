using System.Runtime.InteropServices;

namespace UnitTests
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TestStruct
    {
        public short A;
        public short B;
        public long C;
    }
}
