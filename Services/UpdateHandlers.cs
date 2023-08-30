using MongoDB.Driver;
using Telegram.Bot.Examples.WebHook2.BDUsers;
using Telegram.Bot.Examples.WebHook.BDAdmin;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Services;

public class UpdateHandlers
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandlers> _logger;

    public UpdateHandlers(ITelegramBotClient botClient, ILogger<UpdateHandlers> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken) // ��������� ������ ��� ������� ������ 
   {    
        // ����� ������������ ������� ������ ���������� ��� ���
        if (callbackQuery.Data.StartsWith("accept"))
        {
            string[] slovar = callbackQuery.Data.Split(' '); // � callbackQuery ���������� ��������� ����������� �������� � ���� ������
            string defendant = slovar[1];
            string chat_id = slovar[2];
            string company = slovar[3];

            var collection = GetDBTableAdmin<MongoDBAdmin>();
            var filter = Builders<MongoDBAdmin>.Filter.Eq(x => x.Company, company);
            var all_data = await collection.Find(filter).ToListAsync();
            long chat_admin = all_data[0].ChatId;


            await _botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
            string status = "���� ������";
            string nick = defendant;
            _botClient.SendTextMessageAsync(chat_admin, $"�������� � ������� ����� ��� {defendant}");
            UpdateBdAdmin(status, chat_admin);
        }
        else if (callbackQuery.Data.StartsWith("refuse")) // ������ ������ ���������� � ��������� ������������� �� ����
        {
            string[] slovar = callbackQuery.Data.Split(' ');
            string defendant = slovar[1];
            string chat_id = slovar[2];
            string company = slovar[3];

            var collection = GetDBTableAdmin<MongoDBAdmin>();
            var filter = Builders<MongoDBAdmin>.Filter.Eq(x => x.Company, company);
            var all_data = await collection.Find(filter).ToListAsync();
            //long chat_admin = all_data[0].ChatId;
            long chat_admin = all_data[0].ChatId;

            var collection1 = GetDBTable<MongoDBTraker>();
            var filter1 = Builders<MongoDBTraker>.Filter.Eq("NickName", defendant);
            await collection1.FindOneAndDeleteAsync(filter1);

            await _botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
            _botClient.SendTextMessageAsync(chat_admin, $"�� ��������� ������ {defendant}");
            _botClient.SendTextMessageAsync(chat_id, $"������������ �������� ���� ������, ��������� � ��� ����� ���� ��� �� �� ���");

        }

    }
    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken) // ����� ������� ����������� �� �������� ������������ ��� ������� ���������� ������
    {
        try
        {
            _logger.LogInformation("Receive message type: {MessageType}", message.Type);
            if (message.Text is not { } messageText)
                return;


            string nick = message.From.Username;
            var chat_id = message.Chat.Id;
            var collection = GetDBTable<MongoDBTraker>(); // ������������� ���� ������, ����� ����� ����� ���
            var filter = Builders<MongoDBTraker>.Filter.Eq("ChatId", chat_id);
            var session = collection.Find(filter).FirstOrDefault();

            var collection1 = GetDBTableAdmin<MongoDBAdmin>();
            var filter1 = Builders<MongoDBAdmin>.Filter.Eq("ChatId", chat_id);
            var session1 = collection1.Find(filter1).FirstOrDefault();

            // ������� �� ������������ � ������ 
            if (message.Text == "/start")
            {
                var action = messageText.Split(' ')[0] switch
                {
                    "/inline_keyboard" => Register(_botClient, message, cancellationToken),
                    _ => Usage(_botClient, message, cancellationToken)
                };
                Message sentMessage = await action;
                _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
            }
            else if (message.Text == "/register")
            {
                if (session == null)
                {
                    session = await CreateBdTraker(nick, chat_id);
                    Register(_botClient, message, cancellationToken);
                }
                else
                {
                    if (session.Status.StartsWith("���������� ������"))
                    {
                        _botClient.SendTextMessageAsync(chat_id, "�� �� ��������� �����������");
                    }
                    else
                    {
                        _botClient.SendTextMessageAsync(chat_id, "�� ��� ������ ������\n" +
    $"������ ������ - {session.Status}");
                    }

                }
            }
            else if (messageText == "/createcompany")
            {
                if (session1 == null)
                {
                    session1 = await CreateBdAdmin(nick, chat_id);
                    RegisterAdmin(_botClient, message, cancellationToken);
                }
                else
                {
                    _botClient.SendTextMessageAsync(chat_id, "�� ��� ���������, ������");
                }

            }
            else if (session != null && session.Status.StartsWith("���������� ������"))  // ���� �������� ����������� �� ��� ���������� ������ �� ��� ������� ������������ ����� ����������� � ���� ���� ����� �� ���������
            {
                Register(_botClient, message, cancellationToken);
            }
            else if (session1 != null && session1.Status.StartsWith("�����������"))
            {
                RegisterAdmin(_botClient, message, cancellationToken);
            }
            else if (session1.Status == "���� ������")
            {
                Answer(_botClient, message, cancellationToken);
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"������� �������� ������� {ex.Message}");

        }
    }

    static async Task<Message> Register(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken) // ����������� ��������
    {
        bool statuskeyboard = true;
        long chat_id = message.Chat.Id;
        var collection = GetDBTable<MongoDBTraker>();
        var filter = Builders<MongoDBTraker>.Filter.Eq(x => x.ChatId, chat_id);
        var all_data = await collection.Find(filter).ToListAsync();
        string[] step1 = all_data[0].Status.Split(' ');
        int step = Int32.Parse(step1[2]);
        if (all_data[0].Status.StartsWith("���������� ������"))
        {
            switch (step) // ���� ���������� �����������, ��� ������ ������������ ��� ����� �� ���������� ������, ������ ����������� � ���� ������
            {
                case 4:
                    string status = "���������� ������ 3";
                    UpdateBd(status, chat_id);

                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "������� �������� ����� �������� ���, ��� ��� ������� � PlanFix",
                    cancellationToken: cancellationToken);

                case 3:
                    string company = message.Text;
                    status = "���������� ������ 2";
                    UpdateBd(status, chat_id);
                    UpdateBdCompany(chat_id, company);

                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "������� ����� ������ �������� PlanFix",
                    cancellationToken: cancellationToken);

                case 2:
                    string login = message.Text;
                    status = "���������� ������ 1";
                    UpdateBd(status, chat_id);
                    UpdateBdLogin(chat_id, login);

                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "������� �������� ����� ����� �� ������� ��������������� ��� ������� PlanFix",
                    cancellationToken: cancellationToken);

                case 1:
                    string email = message.Text;
                    status = "���������� ������ 0";
                    UpdateBd(status, chat_id);
                    UpdateBdEMail(chat_id, email);

                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "������� ���������� ����� ������ �������� PlanFix, ���������� ��� ����� ����� �� ������ ����� � ������� ����������",
                    cancellationToken: cancellationToken);

                case 0:
                    string planfixid = message.Text;
                    status = "�������� �������������";
                    UpdateBd(status, chat_id);
                    UpdateBdPlanFixId(chat_id, planfixid);
                    �allTheBoss(botClient, message, cancellationToken, chat_id); // ������������ ��������� �������� �������� ��������� � ������� ������� ��� ��������� ������

                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "������ ������� �������, �������� ������������� �� ������ ������������",
                    cancellationToken: cancellationToken);

                default:
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "",
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "�� ��� ����������������\n" +
                  $"������ - {all_data[0].Status}",
            cancellationToken: cancellationToken);
        }
    }

    static async Task<Message> RegisterAdmin(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken) // �������� �����������, �� ������ ��� ������������
    {
        bool statuskeyboard = true;
        long chat_id = message.Chat.Id;
        var collection = GetDBTableAdmin<MongoDBAdmin>();
        var filter = Builders<MongoDBAdmin>.Filter.Eq(x => x.ChatId, chat_id);
        var all_data = await collection.Find(filter).ToListAsync();
        string[] step1 = all_data[0].Status.Split(' ');
        int step = Int32.Parse(step1[1]);

        if (all_data[0].Status.StartsWith("�����������"))
        {
            switch (step)
            {
                case 2:
                    string status = "����������� 1";
                    UpdateBdAdmin(status, chat_id);

                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "������� �������� ����� �������� ���, ��� ������� � PlanFix",
                    cancellationToken: cancellationToken);

                case 1:
                    string company = message.Text;
                    status = "����������� 0";
                    UpdateBdAdmin(status, chat_id);
                    UpdateBdACompany(chat_id, company);

                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "������� Api Key ������ �������� PlanFix",
                    cancellationToken: cancellationToken);

                case 0:
                    string apikey = message.Text;
                    status = "������";
                    UpdateBdAdmin(status, chat_id);
                    UpdateBdApiKey(chat_id, apikey);

                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "���������� ������� ���������! ������ �� ��� �������",
                    cancellationToken: cancellationToken);


                default:
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "RE ZERO",
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "�� ��� ����������������\n" +
                  $"������ - {all_data[0].Status}",
            cancellationToken: cancellationToken); ;
        }
    }

    static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken) // ����
    {
        const string usage = "����������������� ����� �� ������ ���� ���� �� ��� ��� �� �������\n" +
                             "/register\n" +
                             "���� �� �������� �� �������� ������� �������� ������ �������� ����� �������� � Api Key PlanFix�\n" +
                             "/createcompany";


        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    async static Task<MongoDBTraker> CreateBdTraker(string nick, long chat_id) // �������� ���� ������ ��� ������������
    {
        string connectionString = "mongodb://localhost:27017";
        string dataBaSeName = "Tracker";
        string collectionName = "Users";

        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dataBaSeName);
        var storage = db.GetCollection<MongoDBTraker>(collectionName);
        var info = new MongoDBTraker { NickName = nick, ChatId = chat_id, Status = "���������� ������ 4"};

        await storage.InsertOneAsync(info);
        return info;
    }

    async static Task<MongoDBAdmin> CreateBdAdmin(string nick, long chat_id) // �������� ���� ������ ��� ������������
    {
        string connectionString = "mongodb://localhost:27017";
        string dataBaSeName = "Tracker";
        string collectionName = "Admin";

        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dataBaSeName);
        var storage = db.GetCollection<MongoDBAdmin>(collectionName);
        var info = new MongoDBAdmin { Admin = nick, ChatId = chat_id, Status = "����������� 2"};

        await storage.InsertOneAsync(info);
        return info;
    }
    // ��������� ��������� ��� ������
    async static Task UpdateBd(string status, long chat_id)
    {
        var collection = GetDBTable<MongoDBTraker>();
        var filter = Builders<MongoDBTraker>.Filter.Eq("ChatId", chat_id);
        var update = Builders<MongoDBTraker>.Update.Set("Status", status);
        collection.UpdateOne(filter, update);
    }

    async static Task UpdateBdAdmin(string status, long chat_admin)
    {
        var collection = GetDBTableAdmin<MongoDBAdmin>();
        var filter = Builders<MongoDBAdmin>.Filter.Eq("ChatId", chat_admin);
        var update = Builders<MongoDBAdmin>.Update.Set("Status", status);
        collection.UpdateOne(filter, update);
    }

    async static Task UpdateBdCompany(long chat_id, string company)
    {
        var collection = GetDBTable<MongoDBTraker>();
        var filter = Builders<MongoDBTraker>.Filter.Eq("ChatId", chat_id);
        var update = Builders<MongoDBTraker>.Update.Set("Company", company);
        collection.UpdateOne(filter, update);
    }
    async static Task UpdateBdLogin(long chat_id, string login)
    {
        var collection = GetDBTable<MongoDBTraker>();
        var filter = Builders<MongoDBTraker>.Filter.Eq("ChatId", chat_id);
        var update = Builders<MongoDBTraker>.Update.Set("Login", login);
        collection.UpdateOne(filter, update);
    }

    async static Task UpdateBdEMail(long chat_id, string email)
    {
        var collection = GetDBTable<MongoDBTraker>();
        var filter = Builders<MongoDBTraker>.Filter.Eq("ChatId", chat_id);
        var update = Builders<MongoDBTraker>.Update.Set("EMail", email);
        collection.UpdateOne(filter, update);
    }
    async static Task UpdateBdPlanFixId(long chat_id, string planfixid)
    {
        var collection = GetDBTable<MongoDBTraker>();
        var filter = Builders<MongoDBTraker>.Filter.Eq("ChatId", chat_id);
        var update = Builders<MongoDBTraker>.Update.Set("PlanFixId", planfixid);
        collection.UpdateOne(filter, update);
    }
    async static Task UpdateBdACompany(long chat_id, string company)
    {
        var collection = GetDBTableAdmin<MongoDBAdmin>();
        var filter = Builders<MongoDBAdmin>.Filter.Eq("ChatId", chat_id);
        var update = Builders<MongoDBAdmin>.Update.Set("Company", company);
        collection.UpdateOne(filter, update);
    }
    async static Task UpdateBdApiKey(long chat_id, string apikey)
    {
        var collection = GetDBTableAdmin<MongoDBAdmin>();
        var filter = Builders<MongoDBAdmin>.Filter.Eq("ChatId", chat_id);
        var update = Builders<MongoDBAdmin>.Update.Set("ApiKey", apikey);
        collection.UpdateOne(filter, update);
    }

    async static Task �allTheBoss(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long chat_id)// �������� ������������ ����� � ������� ������� ��� ��������� ������
    {
        var collection = GetDBTable<MongoDBTraker>();
        var filter = Builders<MongoDBTraker>.Filter.Eq("ChatId", chat_id);
        var company1 = await collection.Find(filter).ToListAsync();
        string company = company1[0].Company;
        string nick = message.From.Username;


        var collection1 = GetDBTableAdmin<MongoDBAdmin>();
        var filter1 = Builders<MongoDBAdmin>.Filter.Eq("Company", company);
        var all_data = await collection1.Find(filter1).ToListAsync();

        InlineKeyboardMarkup keyboard = new(new[]
{
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "������� ������", callbackData: $"accept {nick} {chat_id} {company}"),
                    InlineKeyboardButton.WithCallbackData(text: "��������� ������", callbackData: $"refuse {nick} {chat_id} {company}")
                }
                });
        await botClient.SendTextMessageAsync(all_data[0].ChatId, $"{nick} �������� ��� ����� �� ��������� ������", replyMarkup: keyboard);
    }

    async static Task Answer(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken) // ���������� ���� ������ � ��������� ��� ��������� ������, ������������ ������ ����� ����������� ��� ������ ����������
    {
        long owner = message.Chat.Id;
        string PLANFIXTOKEN = message.Text;
        var collection = GetDBTableAdmin<MongoDBAdmin>();
        var filter = Builders<MongoDBAdmin>.Filter.Eq("ChatId", owner);
        var all_data = await collection.Find(filter).ToListAsync();
        string company = all_data[0].Company;
        var update = Builders<MongoDBAdmin>.Update.Set("Status", "...");
        collection.UpdateOne(filter, update);

        var collection1 = GetDBTable<MongoDBTraker>();
        var filter1 = Builders<MongoDBTraker>.Filter.Eq("Company", company);
        var all_data1 = await collection1.Find(filter1).ToListAsync();
        var update1 = Builders<MongoDBTraker>.Update.Set("Token", PLANFIXTOKEN);
        var update2 = Builders<MongoDBTraker>.Update.Set("Status", "������� �������������");
        collection1.UpdateOne(filter1, update1);
        collection1.UpdateOne(filter1, update2);

        botClient.SendTextMessageAsync(all_data1[0].ChatId, $"������������ ������ ���� ������, ������ � �� ���� ����� ��� ����������� � PlanFix, ������� ������!");
        botClient.SendTextMessageAsync(owner, $"{all_data1[0].NickName} �������� � ���� ����, �� ����� ���������� ��������� �� ���");
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

    static IMongoCollection<T> GetDBTableAdmin<T>()
    {
        string connectionString = "mongodb://localhost:27017";
        string dataBaSeName = "Tracker";
        string collectionName = "Admin";

        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dataBaSeName);
        return db.GetCollection<T>(collectionName);
    }

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }



#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}