using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Telegram.Bot.Examples.WebHook.Incoming.Serializer // класс преобразуемый в xml документ
{
    [Serializable]
    public class Users
        {

            [XmlElement(ElementName = "id")]
            public string Id { get; set; }
        }

        [XmlRoot(ElementName = "members")]
    [Serializable]
    public class Members
        {

            [XmlElement(ElementName = "users")]
            public Users Users { get; set; }
        }

        [XmlRoot(ElementName = "task")]
    [Serializable]
    public class Tasks
        {

            [XmlElement(ElementName = "template")]
            public string Template { get; set; }

            [XmlElement(ElementName = "title")]
            public string Title { get; set; }

            [XmlElement(ElementName = "description")]
            public string Description { get; set; }

            [XmlElement(ElementName = "status")]
            public string Status { get; set; }

            [XmlElement(ElementName = "members")]
            public Members Members { get; set; }
        }
        [XmlRoot(ElementName = "request")]

    [Serializable]
    public class Request
        {

            [XmlElement(ElementName = "account")]
            public string Account { get; set; }

            [XmlElement(ElementName = "task")]
            public Tasks Tasks { get; set; }

            [XmlAttribute(AttributeName = "method")]
            public string Method { get; set; }
        }

    
}
