using System.Net.Http.Headers;

using System.Text;
using System.Xml;
using MongoDB.Driver;
using System.Xml.Serialization;
using Telegram.Bot.Examples.WebHook2.BDUsers;
using Telegram.Bot.Examples.WebHook.Incoming;
using Telegram.Bot.Examples.WebHook.Incoming.Serializer;
using Telegram.Bot.Types;
using PlanFixApiGetUser;
using System.Data;

namespace Parsec
{
    public class Parser
    {

        public Task<T> MakePostRequestAsync<T>(string url, UserData data, Dictionary<string, string>? headers = null )
        {
            return MakeRequestAsync<T>(url, data, headers, HttpMethod.Post);
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

        public async Task<T> MakeRequestAsync<T>(string url, UserData data, Dictionary<string, string>? headers = null, HttpMethod? method = null)
        {
            var collection = GetDBTable<MongoDBTraker>();
            var filter = Builders<MongoDBTraker>.Filter.Eq(x => x.ChatId, data.Chat_Id);
            var all_data = await collection.Find(filter).ToListAsync();

            var userName = "b3dd7315fea58cc59f70bd138ed692a6"; // это ключ организации планфикса, в дальнейшем надо сделать, чтобы при при регистрации руководителя он вводил ещё и токен своей организации
            var userPassword = all_data[0].Token;

            // создание базавой аунтификации
            var authenticationString = $"{userName}:{userPassword}";
            var base64String = Convert.ToBase64String(
               System.Text.Encoding.ASCII.GetBytes(authenticationString));

            var httpRequestMessage = new HttpRequestMessage(method ?? HttpMethod.Post, url);

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64String);

            // передача нужных параметров для xml документа
            Users users = new Users { Id = all_data[0].PlanFixId };
            Members members = new Members { Users = users };
            Tasks tasks = new Tasks { Template = "Канбан: Разработка ПО", Description = data.Description, Members = members, Status = "Беклог", Title = data.Name };
            var update = Builders<MongoDBTraker>.Update.Set("NameTask", data.Name);

            var existingData = await collection
                   .Find(x => x.ChatId == data.Chat_Id)
                   .FirstOrDefaultAsync();
            if (existingData != null)
            {
                string[] people = { "I" };
                var userobj = people.Select(x => new Work()
                {
                    TaskName = data.Name,
                    TaskDescription = data.Description,
                    GettingStarted = data.StartTime
                });

                existingData.Tasks = userobj.ToList();

                await collection.ReplaceOneAsync(x => x.Id.Equals(existingData.Id), existingData);
            }
                Request objectToSerialize = new Request();
            objectToSerialize.Method = "task.add";
            objectToSerialize.Account = all_data[0].Company;
            objectToSerialize.Tasks = tasks;

            XmlSerializer xmlSerializer1 = new System.Xml.Serialization.XmlSerializer(objectToSerialize.GetType());

            using (StreamWriter streamWriter = new StreamWriter(@"C:\Users\User\OneDrive\Документы\web_huuk\Incoming\XmlTest.xml"))
            {
             xmlSerializer1.Serialize(streamWriter, objectToSerialize);
            }
            
            XmlDocument parameters = new XmlDocument();
            parameters.Load(@"C:\Users\User\OneDrive\Документы\web_huuk\Incoming\XmlTest.xml");



            if (parameters != null)
            {
                StringWriter sw = new StringWriter();
                XmlTextWriter tx = new XmlTextWriter(sw);
                parameters.WriteTo(tx);

                string formParameters = sw.ToString();// 

                httpRequestMessage.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(formParameters)));
                httpRequestMessage.Content.Headers.Add("Content-Type", "text/xml");
            }

            var httpClient = new HttpClient();
            var httpResponseMessage = httpClient.Send(httpRequestMessage);
            Console.WriteLine(httpResponseMessage);


            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream =
                    await httpResponseMessage.Content.ReadAsStreamAsync();
                XmlRootAttribute xRoot = new XmlRootAttribute();
                xRoot.ElementName = "response";
                // xRoot.Namespace = "http://www.cpandl.com";
                xRoot.IsNullable = true;
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T), xRoot);
                var test = await httpResponseMessage.Content.ReadAsStringAsync();
                return (T)xmlSerializer.Deserialize(contentStream);
            }
            else
            {
                using var contentStream =
                    await httpResponseMessage.Content.ReadAsStreamAsync();

                var error = System.Text.Json.JsonSerializer.Deserialize<object>(contentStream);
                Console.WriteLine($"MakeRequest to url {url} failed. {error}");

            }
            return default(T);
        }


    }

}

