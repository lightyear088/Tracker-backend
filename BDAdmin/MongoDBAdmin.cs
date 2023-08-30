using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Telegram.Bot.Examples.WebHook.BDAdmin
{
    public class MongoDBAdmin
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Status { get; set; }
        public string Admin { get; set; }
        public long ChatId { get; set; }
        public string Company { get; set; }
        public string ApiKey { get; set; }
    }
}
