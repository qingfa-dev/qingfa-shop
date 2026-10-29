using Application.Common.Messaging.Inbox;
using Application.Common.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityFramework.Inbox;

/// <summary>Maps <see cref="InboxMessage"/> to the <c>messaging.inbox_messages</c> table.</summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    /// <summary>Applies the inbox table mapping and retention index.</summary>
    /// <param name="builder">The entity type builder for <see cref="InboxMessage"/>.</param>
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        // Table: the inbox shares the messaging schema with the outbox.
        builder.ToTable("inbox_messages", PersistenceConstant.Schema.Messaging);

        // Key: a message is processed once per consumer.
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

        // Retention: delete processed rows by age.
        builder.HasIndex(message => message.ProcessedOnUtc);
    }
}