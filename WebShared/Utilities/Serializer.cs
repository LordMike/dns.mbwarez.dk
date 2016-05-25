using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace WebShared.Utilities
{
    public static class Serializer
    {
        public static void Serialize<T>(T obj, string file)
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));

            XDocument doc = new XDocument();
            using (XmlWriter writer = doc.CreateWriter())
                ser.Serialize(writer, obj);

            doc.Save(file);
        }

        public static T Deserialize<T>(string file)
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));

            XDocument doc = XDocument.Load(file);
            XmlReader reader = doc.CreateReader();
            return (T)ser.Deserialize(reader);
        }
    }
}