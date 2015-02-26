using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StructInterop;

namespace UnitTests
{
    [TestClass]
    public class StructInteropTests
    {
        [TestMethod]
        public void Example()
        {
            byte[] bytes = { 0x50, 0x02, 0x58, 0x04, 0x54, 0x34, 0x1a, 0x00, 0x00, 0x00, 0x00, 0x00 };

            TestStruct x = StructInterOp.DeserializeSafe<TestStruct>(bytes);
            byte[] serialized = x.Serialize();

            Assert.AreEqual(bytes.Length, serialized.Length);
            foreach (byte b in serialized)
            {
                Trace.Write(string.Format("{0:X2} ", b));
            }
        }
    }
}
