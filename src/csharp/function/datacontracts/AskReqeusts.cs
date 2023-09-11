using System.Collections.Generic;

public class AskRequest
{
    public string question { get; set; }
    public string index_name { get; set; }

    public List<AskChatMessage> chat_history { get; set; }
}

public class AskChatMessage
{
    public string role { get; set; }

    public string content { get; set; }
}
