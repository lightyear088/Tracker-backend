using Microsoft.AspNetCore.Mvc;
using Parsec;
using PlanFixApiGetUser;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Telegram.Bot.Filters;
using Telegram.Bot.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Examples.WebHook.Incoming;
using System.Xml.Linq;
using MongoDB.Driver;
using Telegram.Bot.Examples.WebHook2.BDUsers;
using Telegram.Bot.Examples.WebHook.Incoming.Serializer;
using Telegram.Bot.Examples.WebHook.IncomingDate;

namespace Telegram.Bot.Examples.WebHook.Controllers
{

    public class AccountController : ControllerBase
    {   
        // получение запроса при нажатии кнопки старт
        [HttpPost]
        public async Task Post([FromBody] UserData data)

        {
            string url = "https://api.planfix.ru/xml";
            Dictionary<string, string> headers = new Dictionary<string, string>();
            var parser = new Parser();
            var result = await parser.MakePostRequestAsync<Get>(url, data, headers);
        }
        // получение запроса при нажатии кнопки стоп
        [HttpPost]
        public async Task Date([FromBody] DateData dateData)
        {
            var collection = GetDBTable<MongoDBTraker>();
            var filter = Builders<MongoDBTraker>.Filter.Eq(x => x.ChatId, dateData.Chat_Id);
            var all_data = await collection.Find(filter).ToListAsync();
            var existingData = await collection
                   .Find(x => x.ChatId == dateData.Chat_Id)
                   .FirstOrDefaultAsync();
            if (existingData != null) // заполнение обьекта Tasks полученными данными
            {
                string[] people = { "I" };
                var userobj = people.Select(x => new Work()
                {
                    TimeWork = $"{dateData.Hours}:{dateData.Minutes}:{dateData.Seconds}",
                    TaskName = dateData.Name,
                    TaskDescription = dateData.Description,
                    GettingStarted = dateData.StartTime

                });

                existingData.Tasks = userobj.ToList();

                await collection.ReplaceOneAsync(x => x.Id.Equals(existingData.Id), existingData);
            }
        }

        static IMongoCollection<T> GetDBTable<T>()
        {
            string connectionString = "mongodb://localhost:27017";
            string dataBaSeName = "Tracker";
            string collectionName = "Users";

            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(dataBaSeName);
            return db.GetCollection<T>(collectionName);
        }
    }
}
