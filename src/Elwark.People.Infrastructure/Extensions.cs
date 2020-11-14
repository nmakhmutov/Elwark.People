using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Domain.SeedWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elwark.People.Infrastructure
{
    internal static class Extensions
    {
        public static async Task DispatchDomainEventsAsync(this IMediator mediator, OAuthContext ctx,
            CancellationToken cancellationToken = default)
        {
            var domainEntities = ctx.ChangeTracker
                .Entries<Entity>()
                .Where(x => x.Entity.DomainEvents.Any())
                .ToList();

            var domainEvents = domainEntities
                .SelectMany(x => x.Entity.DomainEvents);

            IEnumerable<Task> tasks = domainEvents.Select(async x => await mediator.Publish(x, cancellationToken));

            await Task.WhenAll(tasks);

            domainEntities.ForEach(x => x.Entity.ClearDomainEvents());
        }

        public static PropertyBuilder<Uri> IsUrl(this PropertyBuilder<Uri> value) =>
            value.HasMaxLength(InfrastructureConstant.UrlLength)
                .HasConversion(
                    uri => uri.ToString(),
                    s => new Uri(s)
                );

        public static PropertyBuilder<Uri?> IsNullableUrl(this PropertyBuilder<Uri?> value) =>
            value.HasMaxLength(InfrastructureConstant.UrlLength)
                .HasConversion(
                    uri => uri == null ? null : uri.ToString(),
                    s => s == null ? null : new Uri(s)
                );

        public static PropertyBuilder<DateTimeOffset> IsCreatedAt(this PropertyBuilder<DateTimeOffset> value) =>
            value.ValueGeneratedOnAdd()
                .HasDefaultValueSql(@"timezone('utc'::text, now())")
                .IsRequired();

        public static PropertyBuilder<DateTimeOffset> IsUpdatedAt(this PropertyBuilder<DateTimeOffset> value) =>
            value.HasDefaultValueSql(@"timezone('utc'::text, now())")
                .IsRequired();
    }
}