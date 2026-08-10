namespace ChatService;

public class AppSettings
{
    public string Secret { get; set; }
    public string[] AllowedChatOrigins { get; set; }
}

public class RabbitMqSettings
{
    public string ConnectionString { get; set; }
}