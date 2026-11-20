public class ChatMessage
{
    public int Id { get; set; }
    public string Room { get; set; } = "";
    public string User { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTimeOffset SentAt { get; set; }
}
