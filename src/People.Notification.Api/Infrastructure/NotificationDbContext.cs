﻿using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using People.Mongo;
using People.Notification.Api.Models;

namespace People.Notification.Api.Infrastructure
{
    public sealed class NotificationDbContext : MongoDbContext
    {
        static NotificationDbContext()
        {
            BsonClassMap.RegisterClassMap<EmailProvider>(map =>
            {
                map.AutoMap();
                map.MapIdProperty(x => x.Id);
            });
            
            BsonClassMap.RegisterClassMap<Sendgrid>();
            BsonClassMap.RegisterClassMap<Gmail>();
        }

        public NotificationDbContext(IOptions<MongoDbOptions> settings)
            : base(settings.Value)
        {
        }

        public IMongoCollection<EmailProvider> EmailProviders =>
            Database.GetCollection<EmailProvider>("email_providers");
        
        public override async Task OnModelCreatingAsync()
        {
            await CreateCollectionsAsync(
                EmailProviders.CollectionNamespace.CollectionName
            );
            
            await CreateIndexesAsync(EmailProviders,
                new CreateIndexModel<EmailProvider>(
                    Builders<EmailProvider>.IndexKeys.Descending(x => x.Balance),
                    new CreateIndexOptions {Name = "Balance"}
                )
            );
        }
    }
}
