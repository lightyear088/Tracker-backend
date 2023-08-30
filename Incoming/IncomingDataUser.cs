namespace Telegram.Bot.Examples.WebHook.Incoming
{
    public class UserData // полученные данные храняться в классе
    {
        public long Chat_Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }

    }
}
