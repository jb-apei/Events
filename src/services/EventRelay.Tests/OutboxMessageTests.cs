using FluentAssertions;
using Shared.Events;
using Shared.Events.Prospects;
using Shared.Events.Students;
using Xunit;

namespace EventRelay.Tests;

public class OutboxMessageTests
{
    [Fact]
    public void OutboxMessage_ShouldHaveRequiredProperties()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var prospectCreated = new ProspectCreated
        {
            EventId = eventId,
            Data = new ProspectCreatedData
            {
                ProspectId = 123,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            }
        };

        // Act
        var payload = EventSerializer.Serialize(prospectCreated);

        // Assert
        payload.Should().NotBeNullOrEmpty();
        payload.Should().Contain($"\"eventId\":\"{eventId}\"");
        payload.Should().Contain("\"eventType\":\"ProspectCreated\"");
        payload.Should().Contain("\"prospectId\":123");
    }

    [Fact]
    public void OutboxMessage_ShouldSerializeEventEnvelope()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var prospectCreated = new ProspectCreated
        {
            Data = new ProspectCreatedData
            {
                ProspectId = 456,
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com"
            },
            CorrelationId = correlationId,
            Subject = "prospect/456"
        };

        // Act
        var json = EventSerializer.Serialize(prospectCreated);
        var deserialized = EventSerializer.Deserialize<ProspectCreated>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.EventId.Should().Be(prospectCreated.EventId);
        deserialized.CorrelationId.Should().Be(correlationId);
        deserialized.Subject.Should().Be("prospect/456");
        deserialized.Data.ProspectId.Should().Be(456);
    }

    [Fact]
    public void OutboxMessage_ShouldPreserveEventMetadata()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var eventId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var causationId = Guid.NewGuid().ToString();

        var prospectCreated = new ProspectCreated
        {
            EventId = eventId,
            OccurredAt = occurredAt,
            CorrelationId = correlationId,
            CausationId = causationId,
            Subject = "prospect/789",
            Data = new ProspectCreatedData
            {
                ProspectId = 789,
                FirstName = "Bob",
                LastName = "Smith",
                Email = "bob@example.com"
            }
        };

        // Act
        var json = EventSerializer.Serialize(prospectCreated);
        var deserialized = EventSerializer.Deserialize<ProspectCreated>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.EventId.Should().Be(eventId);
        deserialized.OccurredAt.Should().BeCloseTo(occurredAt, TimeSpan.FromSeconds(1));
        deserialized.CorrelationId.Should().Be(correlationId);
        deserialized.CausationId.Should().Be(causationId);
        deserialized.Subject.Should().Be("prospect/789");
    }

    [Fact]
    public void OutboxMessage_ShouldHandleMultipleEventTypes()
    {
        // Arrange
        var prospectCreated = new ProspectCreated
        {
            Data = new ProspectCreatedData { ProspectId = 1, FirstName = "A", LastName = "B", Email = "a@b.com" }
        };
        var studentCreated = new StudentCreated
        {
            Data = new StudentCreatedData { StudentId = 2, FirstName = "C", LastName = "D", Email = "c@d.com" }
        };

        // Act
        var prospectJson = EventSerializer.Serialize(prospectCreated);
        var studentJson = EventSerializer.Serialize(studentCreated);

        // Assert
        prospectJson.Should().Contain("\"eventType\":\"ProspectCreated\"");
        studentJson.Should().Contain("\"eventType\":\"StudentCreated\"");
        prospectJson.Should().NotContain("StudentCreated");
        studentJson.Should().NotContain("ProspectCreated");
    }
}
