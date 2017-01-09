using UnitTests;
using StructInterop.Core;
using Xunit;

namespace StructInterop.Test
{
    public class Test
    {
        [Fact]
        public void MainTest()
        {
            byte[] bytes = { 0x50, 0x02, 0x58, 0x04, 0x54, 0x34, 0x1a, 0x00, 0x00, 0x00, 0x00, 0x00 };

            TestStruct x = StructInterop.Core.StructInterop.DeserializeSafe<TestStruct>(bytes);
            byte[] serialized = x.Serialize();

            Assert.Equal(bytes, serialized);
        }
    }
}
