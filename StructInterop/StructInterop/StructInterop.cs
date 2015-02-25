using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace StructInterop
{
    public static class StructInterOp
    {
        static readonly ConstructorInfo IntPtrCtor = typeof(IntPtr).GetConstructor(new[] { typeof(void*) });
        static readonly MethodInfo MarshalCopy = typeof(Marshal).GetMethod("Copy", new[] { typeof(IntPtr), typeof(byte[]), typeof(int), typeof(int) });
        private static class DelegateHolder<T> where T : struct
        {
            // ReSharper disable MemberHidesStaticFromOuterClass
            // ReSharper disable StaticMemberInGenericType
            public static readonly Type TypeOfT = typeof(T);
            public static readonly int SizeInBytes = Marshal.SizeOf(TypeOfT);

            public static readonly Func<T, byte[]> Serialize = CreateSerializationDelegate();
            public static readonly Func<byte[], T> Deserialize = CreateDeserializationDelegate();


            //public static T[] Serialize(T value)
            //{
            //    IntPtr p = new IntPtr(&value);
            //    byte[] result = new byte[sizeof(T)];
            //    Marshal.Copy(p, result, 0, result.Length);
            //    return result;
            //}
            private static Func<T, byte[]> CreateSerializationDelegate()
            {
                var dm = new DynamicMethod("Serialize" + TypeOfT.Name,
                    typeof(byte[]),
                    new[] { TypeOfT },
                    Assembly.GetExecutingAssembly().ManifestModule);
                dm.DefineParameter(1, ParameterAttributes.None, "value");

                var generator = dm.GetILGenerator();
                generator.DeclareLocal(typeof(byte[]));

                //IntPtr p = new IntPtr(&value);
                generator.Emit(OpCodes.Ldarga_S, (byte)0);
                generator.Emit(OpCodes.Conv_U);
                generator.Emit(OpCodes.Newobj, IntPtrCtor);

                //byte[] result = new byte[sizeof(T)]; 
                OpCode ldcStructSize = SizeInBytes < sbyte.MaxValue ? OpCodes.Ldc_I4_S : OpCodes.Ldc_I4;
                generator.Emit(ldcStructSize, SizeInBytes);
                generator.Emit(OpCodes.Newarr, typeof(byte));

                //Marshal.Copy(p, result, 0, result.Length);
                generator.Emit(OpCodes.Stloc_0);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldlen);
                generator.Emit(OpCodes.Conv_I4);
                generator.EmitCall(OpCodes.Call, MarshalCopy, null);

                //return result
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ret);

                return (Func<T, byte[]>)dm.CreateDelegate(typeof(Func<T, byte[]>));
            }

            //public static T Deserialize(byte[] data)
            //{
            //    fixed (byte* pData = &data[0])
            //    {
            //        return *(T*)pData;
            //    }
            //}
            private static Func<byte[], T> CreateDeserializationDelegate()
            {
                var dm = new DynamicMethod("Deserialize" + TypeOfT.Name,
                                            TypeOfT,
                                            new[] { typeof(byte[]) },
                                            Assembly.GetExecutingAssembly().ManifestModule);
                dm.DefineParameter(1, ParameterAttributes.None, "data");
                var generator = dm.GetILGenerator();
                generator.DeclareLocal(typeof(byte).MakePointerType(), pinned: true);
                generator.DeclareLocal(TypeOfT);

                //fixed (byte* pData = &data[0])
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Ldelema, typeof(byte));
                generator.Emit(OpCodes.Stloc_0);

                // return *(T*)pData;
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Conv_I);
                generator.Emit(OpCodes.Ldobj, TypeOfT);
                generator.Emit(OpCodes.Ret);
                return (Func<byte[], T>)dm.CreateDelegate(typeof(Func<byte[], T>));
            }
        }


        /// <summary>
        /// Do not check array bounds, possible buffer overflow
        /// </summary>
        public static T Deserialize<T>(byte[] data) where T : struct
        {
            return DelegateHolder<T>.Deserialize(data);
        }

        /// <summary>
        /// Check array bounds
        /// </summary>
        public static T DeserializeSafe<T>(byte[] data) where T : struct
        {
            if (DelegateHolder<T>.SizeInBytes != data.Length)
                throw new ArgumentException(string.Format("Struct size is {0} bytes but array is {1} bytes length", DelegateHolder<T>.SizeInBytes, data.Length));
            return DelegateHolder<T>.Deserialize(data);
        }

        /// <summary>
        /// Marshal struct in byte array without any type information
        /// </summary>
        public static byte[] Serialize<T>(this T value) where T : struct
        {
            return DelegateHolder<T>.Serialize(value);
        }
    }
}
