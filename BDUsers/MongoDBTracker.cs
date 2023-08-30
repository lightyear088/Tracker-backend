using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Telegram.Bot.Examples.WebHook2.BDUsers;

public class MongoDBTraker
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string  PlanFixId { get; set; }
    public string Status { get; set; }
    public string Login { get; set; }
    public string Company { get; set; }
    public string NickName { get; set; }
    public long ChatId { get; set; }
    public string Token { get; set; }
    public string EMail { get; set; }
    public List<Work> Tasks { get; set; } = new List<Work>();
}

public class Work
{
    public string TaskName { get; set; }
    public string TaskDescription { get; set; }
    public DateTime GettingStarted { get; set; }
    public string TimeWork { get; set;}
}
