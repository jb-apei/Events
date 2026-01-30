using FluentAssertions;
using Shared.Events.Prospects;
using System.Text.Json;
using Xunit;

namespace Shared.Events.Tests;

public class EventSerializerTests
{
    [Fact]
    public void EventSerializer_ShouldSerializeProspectCreatedToJson()
    {
        // Arrange
        var prospectId = Guid.NewGuid();
        var prospectCreated = new ProspectCreated
        {
            ProspectId = prospectId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Producer = "ProspectService"
        };

        // Act
        var json = EventSerializer.Serialize(prospectCreated);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"eventType\":\"ProspectCreated\"");
        json.Should().Contain($"\"prospectId\":\"{prospectId}\"");
        json.Should().Contain("\"firstName\":\"Test\"");
        json.Should().Contain("\"email\":\"test@example.com\"");
    }

    [Fact]
    public void EventSerializer_ShouldDeserializeProspectCreatedFromJson()
    {
        // Arrange
        var prospectId = Guid.NewGuid();
        var json = $@"{{
            ""eventId"":""{Guid.NewGuid()}"",
            ""eventType"":""ProspectCreated"",
            ""schemaVersion"":""1.0"",
            ""occurredAt"":""{DateTime.UtcNow:O}"",
            ""producer"":""ProspectService"",
            ""correlationId"":""{Guid.NewGuid()}"",
            ""causationId"":""{Guid.NewGuid()}"",
            ""subject"":""prospect/{prospectId}"",
            ""prospectId"":""{prospectId}"",
            ""firstName"":""Test"",
            ""lastName"":""User"",
            ""email"":""test@example.com"",
            ""phone"":""+1-555-1234""
        }}";

        // Act
        var deserialized = EventSerializer.Deserialize<ProspectCreated>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.EventType.Should().Be("ProspectCreated");
        deserialized.ProspectId.Should().Be(prospectId);
        deserialized.FirstName.Should().Be("Test");
        deserialized.LastName.Should().Be("User");
        deserialized.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void EventSerializer_ShouldPreserveDateTimeUtcDuringSerialization()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;
        var prospectCreated = new ProspectCreated
        {
            ProspectId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            OccurredAt = occurredAt
        };

        // Act
        var json = EventSerializer.Serialize(prospectCreated);
        var deserialized = EventSerializer.Deserialize<ProspectCreated>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
        deserialized.OccurredAt.Should().BeCloseTo(occurredAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void EventSerializer_ShouldHandleNullableFields()
    {
        // Arrange
        var prospectCreated = new ProspectCreated
        {
            ProspectId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = null // Nullable field
        };

        // Act
        var json = EventSerializer.Serialize(prospectCreated);
        var deserialized = EventSerializer.Deserialize<ProspectCreated>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Phone.Should().BeNull();
    }

    [Fact]
    public void EventSerializer_ShouldThrowOnInvalidJson()
    {
        // Arrange
        const string invalidJson = "{ invalid json }";

        // Act
        Action act = () => EventSerializer.Deserialize<ProspectCreated>(invalidJson);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void EventSerializer_ShouldSerializeWithCamelCasePropertyNames()
    {
        // Arrange
        var prospectCreated = new ProspectCreated
        {
            ProspectId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var json = EventSerializer.Serialize(prospectCreated);

        // Assert
        json.Should().Contain("\"prospectId\""); // camelCase
        json.Should().Contain("\"firstName\""); // camelCase
        json.Should().Contain("\"lastName\""); // camelCase
        json.Should().NotContain("\"ProspectId\""); // PascalCase should not exist
    }
}
