

namespace fr.nexess.toolbox {

    using System.Xml;
    using System.Xml.XPath;

    public class XmlConfigure {

        public static bool keyExists(XmlElement xml, string key, ref XmlNode nodeFound) {

            XmlNodeList nodeList = xml.GetElementsByTagName("add");
            foreach (XmlNode node in nodeList) {
                if (node.Attributes[0].Value == key) {
                    nodeFound = node;
                    return true;
                }
            }

            return false;
        }

        public static string getValue(XmlElement xml, string key) {
            
            // check if it exists in the file
            XmlNode nodeFound = null;
            if (keyExists(xml, key, ref nodeFound) == true) {
                return getValue(nodeFound, key);
            }
            return null;
        }

        public static string getValue(XmlNode node, string key) {
            if (node != null && node.Attributes[0].Value != null) {
                return node.Attributes[1].Value;
            }
            return null;
        }

        public static void replaceValue(XmlNode node, string value) {
            if (node != null && node.Attributes[0].Value != null) {
                if (value != null) {
                    node.Attributes[1].Value = value;
                }
            }
        }

        public static void updateKeyValue(XmlElement xml, string key, string value) {
            // check if it exists in the file
            XmlNode nodeFound = null;
            if (keyExists(xml, key, ref nodeFound) == true) {
                // replace the value
                replaceValue(nodeFound, value);
            }
                // else add it to the file
            else {
                XmlConfigure.addKeyValue(xml, key, value);
            }
        }

        public static void addKeyValue(XmlElement xml, string key, string value) {

            XPathNavigator navigator = xml.CreateNavigator();
            XPathNavigator tag = navigator.SelectSingleNode("appSettings");
            XmlWriter writer = tag.AppendChild();
            writer.WriteStartElement("add");
            writer.WriteAttributeString("key", key);
            writer.WriteAttributeString("value", value);
            writer.Close();
        }

    }
}
