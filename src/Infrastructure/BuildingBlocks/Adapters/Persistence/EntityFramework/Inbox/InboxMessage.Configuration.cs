using Application.BuildingBlocks.Messaging.Inbox;
using Application.BuildingBlocks.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Inbox;

/// <summary>Maps <see cref="InboxMessage"/> to the <c>messaging.inbox_messages</c> table.</summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    /// <summary>Applies the inbox table mapping and retention index.</summary>
    /// <param name="builder">The entity type builder for <see cref="InboxMessage"/>.</param>
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        // Map: the inbox shares the messaging schema with the outbox.
        builder.ToTable("inbox_messages", PersistenceConstant.Schema.Messaging);

        // Map: a message is processed once per consumer.
        builder.HasKey(message => new { message.MessageId, message.Consumer });

        builder.Property(message => message.MessageId)
            .HasColumnName("message_id")
            .ValueGeneratedNever();

        builder.Property(message => message.Consumer)
            .HasColumnName("consumer")
            .IsRequired();

        builder.Property(message => message.ProcessedOnUtc)
            .HasColumnName("processed_on_utc")
            .IsRequired();

        // Update: retention deletes processed rows by age.
        builder.HasIndex(message => message.ProcessedOnUtc);
    }
}