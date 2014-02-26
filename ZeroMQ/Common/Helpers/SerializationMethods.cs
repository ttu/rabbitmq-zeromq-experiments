using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Common
{
    public static class SerializationMethods
    {
        public static byte[] ToByteArray(this object obj)
        {
            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static T FromByteArray<T>(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes))
            {
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}