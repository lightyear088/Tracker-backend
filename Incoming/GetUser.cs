using System.Xml.Serialization;

namespace PlanFixApiGetUser
{
    public class PlanFixTask
    {
        [XmlElement(ElementName = "id")]
        public string Chat_Id { get; set; }
        [XmlElement(ElementName = "title")]
        public string Title { get; set; }
        [XmlElement(ElementName = "description")]
        public string Description { get; set; }
    }


    public class Get
    {
        [XmlElement(ElementName = "task")]
        public PlanFixTask Task { get; set; }

    }
}
