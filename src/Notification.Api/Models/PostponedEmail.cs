using MongoDB.Bson;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace Notification.Api.Models;

public class PostponedEmail
{
    public PostponedEmail(string email, string subject, string body, DateTime sendAt)
    {
        Id = ObjectId.GenerateNewId();
        Email = email;
        Subject = subject;
        Body = body;
        SendAt = sendAt;
        Version = int.MinValue;
    }

    public ObjectId Id { get; private set; }

    public int Version { get; set; }

    public string Email { get; private set; }

    public string Subject { get; private set; }

    public string Body { get; private set; }

    public DateTime SendAt { get; private set; }
}
