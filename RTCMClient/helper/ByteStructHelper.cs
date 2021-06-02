using log4net;
using System;
using System.Runtime.InteropServices;

namespace RTCMClient.helper
{
    //byte[] 和struct相互转换
    public class ByteStructHelper
    {
        private static ILog log = LogManager.GetLogger("ByteStuctHelper");

        /// <summary>
        ///  结构体转字节数组（按小端模式)
        ///  </summary>
        ///  <param name="obj">struct type</param>
        ///  <returns></returns>
        public static byte[] StructureToByteArray<T>(T obj) where T : struct
        {
            //得到结构体的大小
            int len = Marshal.SizeOf(obj);
            //创建byte数组
            byte[] arr = new byte[len];
            //分配结构体大小的内存空间
            IntPtr ptr = Marshal.AllocHGlobal(len);
            try
            {
                //将结构体拷到分配好的内存空间
                Marshal.StructureToPtr(obj, ptr, true);
                //从内存空间拷到byte数组
                Marshal.Copy(ptr, arr, 0, len);
                return arr;
            }
            catch (Exception ex)
            {
                log.Error("Error in StructToBytes", ex);
                //throw new Exception("Error in StructToBytes ! " + ex.Message);
            }
            finally
            {
                //释放内存空间
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        /// <summary>
        ///  结构体转字节数组（按大端模式）
        ///  </summary>
        ///  <param name="obj">struct type</param>
        ///  <returns></returns>
        public static byte[] StructureToByteArrayBigEndian(object obj)
        {
            object thisBoxed = obj;
            Type test = thisBoxed.GetType();
            int offset = 0;
            byte[] data = new byte[Marshal.SizeOf(thisBoxed)];
            object fieldValue;
            TypeCode typeCode;
            byte[] temp;
            //列举结构体的每个成员，并Reverse
            foreach (var field in test.GetFields())
            {
                //get value
                fieldValue = field.GetValue(thisBoxed);
                //get type
                typeCode = Type.GetTypeCode(fieldValue.GetType());
                switch (typeCode)
                {
                    //float
                    case TypeCode.Single:
                        {
                            temp = BitConverter.GetBytes((Single)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Single));
                            break;
                        }
                    case TypeCode.Int32:
                        {
                            temp = BitConverter.GetBytes((Int32)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Int32));
                            break;
                        }
                    case TypeCode.UInt32:
                        {
                            temp = BitConverter.GetBytes((UInt32)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(UInt32));
                            break;
                        }
                    case TypeCode.Int16:
                        {
                            temp = BitConverter.GetBytes((Int16)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Int16));
                            break;
                        }
                    case TypeCode.UInt16:
                        {
                            temp = BitConverter.GetBytes((UInt16)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(UInt16));
                            break;
                        }
                    case TypeCode.UInt64:
                        {
                            temp = BitConverter.GetBytes((UInt64)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(UInt64));
                            break;
                        }
                    case TypeCode.Int64:
                        {
                            temp = BitConverter.GetBytes((Int64)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Int64));
                            break;
                        }
                    case TypeCode.Double:
                        {
                            temp = BitConverter.GetBytes((Double)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Double));
                            break;
                        }
                    case TypeCode.Byte:
                        {
                            data[offset] = (Byte)fieldValue;
                            break;
                        }
                    default:
                        {
                            //  System.Diagnostics.Debug.Fail("No conversion provided for this type : " + typeCode.ToString());
                            break;
                        }
                }
                if (typeCode == TypeCode.Object)
                {
                    switch (field.FieldType.Name)
                    {
                        case "Byte[]":
                            int length = ((byte[])fieldValue).Length;
                            Array.Copy((byte[])fieldValue, 0, data, offset, length);
                            offset += length;
                            break;

                        case "Double[]":
                            double[] tempValue = (double[])fieldValue;
                            foreach (var item in tempValue)
                            {
                                byte[] tempItemArray = BitConverter.GetBytes(item);
                                Array.Reverse(tempItemArray);
                                Array.Copy(tempItemArray, 0, data, offset, tempItemArray.Length);
                                offset += tempItemArray.Length;
                            }
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    offset += Marshal.SizeOf(fieldValue);
                }
            }
            return data;
        }

        /// <summary>  字节数组转结构体(按小端模式)
        /// </summary>
        /// <param name="bytearray">字节数组</param>
        /// <param name="obj">目标结构体</param>
        /// <param name="startoffset">byteArray内的起始 位置</param>
        public static T ByteArrayToStructure<T>(byte[] byteArray) where T : struct
        {
            if (byteArray == null) return default(T);
            T type = new T();
            int len = Marshal.SizeOf(type);
            if (len > byteArray.Length)
                return (default(T));
            //分配结构体大小的内存空间
            IntPtr i = Marshal.AllocHGlobal(len);
            try
            {
                //将byte数组拷到分配好的内存空间
                Marshal.Copy(byteArray, 0, i, len);
                //将内存空间转换为目标结构体
                return (T)Marshal.PtrToStructure(i, type.GetType());
            }
            catch (Exception ex)
            {
                log.Error("Error in ByteArrayToStucture", ex);
                //throw new Exception("Error in ByteArrayToStructure ! " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(i);
            }
            return default(T);
            //释放内存空间
        }

        /// <summary>
        /// 字节数组转结构体(按大端模式)
        /// </summary>
        /// <param name="bytearray">字节数组</param>
        public static T ByteArrayToStructureBigEndian<T>(byte[] byteArray) where T : struct
        {
            if (byteArray == null) return default(T);
            T t = new T();
            int reverseStartOffset = 0;
            int len = Marshal.SizeOf(t);
            IntPtr i = Marshal.AllocHGlobal(len);
            byte[] tempArray = (byte[])byteArray.Clone();
            t = (T)Marshal.PtrToStructure(i, t.GetType());

            foreach (var field in typeof(T).GetFields())
            {
                object fieldValue = field.GetValue(t);
                TypeCode typeCode = Type.GetTypeCode(fieldValue.GetType());
                if (typeCode != TypeCode.Object)
                {//如果为值类型
                    Array.Reverse(tempArray, reverseStartOffset, Marshal.SizeOf(fieldValue));
                    reverseStartOffset += Marshal.SizeOf(fieldValue);
                }
                else
                {//如果为引用类型
                    reverseStartOffset += ((byte[])fieldValue).Length;
                }
            }
            try
            {
                //将字节数组复制到结构体指针
                Marshal.Copy(tempArray, 0, i, len);
            }
            catch (Exception ex)
            {
                log.Error("Error in ByteArrayToStructureBigEndian", ex);
                //throw new Exception("Error in ByteArrayToStructureBigEndian ! " + ex.Message);
            }
            t = (T)Marshal.PtrToStructure(i, typeof(T));
            Marshal.FreeHGlobal(i);
            return t;
        }
    }
}