namespace Telegram.Bot.Examples.WebHook.IncomingDate
{
    public class DateData // полученные данные храняться в классе
    {   
        public long Chat_Id { get; set; }
        public string Hours { get; set; }
        public string Minutes { get; set; }
        public string Seconds { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }


    }
}
