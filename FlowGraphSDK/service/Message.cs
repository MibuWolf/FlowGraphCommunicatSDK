using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;

namespace Service
{
    internal class Message
    {
        public string AppName
        {
            get; set;
        }

        public string ModuleName
        {
            get; set;
        }

        public string ClassName
        {
            get; set;
        }

        public string FuctionName
        {
            get; set;
        }

        public Object[] Data
        {
            get; set;
        }

        public void Initialze(string jsonStr)
        {
            Object[] objs = JsonConvert.DeserializeObject<Object[]>(jsonStr);
            AppName = objs[0].ToString();
            ModuleName = objs[1].ToString();
            Object[] data = JsonConvert.DeserializeObject<Object[]>(objs[2].ToString());
            if (data != null)
            {
                ClassName = data[0].ToString();
                FuctionName = data[1].ToString();
                Data = JsonConvert.DeserializeObject<Object[]>(data[2].ToString());
            }
        }

        public override string ToString()
        {
            Object[] data = new Object[] { ClassName, FuctionName, Data };
            Object[] msg = new Object[] { AppName, ModuleName, data };
            return JsonConvert.SerializeObject(msg);
        }

        private byte[] IntToBytes(int value)
        {
            byte[] src = new byte[4];
            src[3] = (byte)((value >> 24) & 0xFF);
            src[2] = (byte)((value >> 16) & 0xFF);
            src[1] = (byte)((value >> 8) & 0xFF);
            src[0] = (byte)(value & 0xFF);
            return src;
        }

        public byte[] ToBytes()
        {
            byte[] decBytes = Encoding.UTF8.GetBytes(this.ToString());
            int len = decBytes.Length;
            byte[] bytes = new byte[len + 4];
            byte[] lenBytes = IntToBytes(len);
            //for (int i = 0; i < lenBytes.Length; ++i)
            //{
            //    bytes[i] = lenBytes[i];
            //}
            lenBytes.CopyTo(bytes, 0);

            
            //for (int i = 0; i < decBytes.Length; ++i)
            //{
            //    bytes[i + 4] = decBytes[i];
            //}

            Array.Copy(decBytes, 0, bytes, 4, decBytes.Length);
            return bytes;
        }
    }
}
