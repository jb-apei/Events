using FluentAssertions;
using Shared.Events;
using Shared.Events.Instructors;
using Xunit;

namespace InstructorService.Tests;

public class InstructorServiceTests
{
    [Fact]
    public void InstructorCreated_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var instructorCreated = new InstructorCreated();

        // Assert
        instructorCreated.EventType.Should().Be("InstructorCreated");
        instructorCreated.Producer.Should().Be("InstructorService");
    }

    [Fact]
    public void InstructorCreated_ShouldContainDataPayload()
    {
        // Arrange & Act
        var instructorCreated = new InstructorCreated
        {
            Data = new InstructorCreatedData
            {
                InstructorId = 789,
                FirstName = "Alice",
                LastName = "Johnson",
                Email = "alice.johnson@example.com"
            }
        };

        // Assert
        instructorCreated.Data.Should().NotBeNull();
        instructorCreated.Data.InstructorId.Should().Be(789);
        instructorCreated.Data.FirstName.Should().Be("Alice");
        instructorCreated.Data.LastName.Should().Be("Johnson");
        instructorCreated.Data.Email.Should().Be("alice.johnson@example.com");
    }

    [Fact]
    public void InstructorCreated_ShouldInheritFromEventEnvelope()
    {
        // Arrange & Act
        var instructorCreated = new InstructorCreated();

        // Assert
        instructorCreated.Should().BeAssignableTo<EventEnvelope>();
        instructorCreated.EventId.Should().NotBeNullOrEmpty();
        instructorCreated.SchemaVersion.Should().Be("1.0");
        instructorCreated.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void InstructorCreated_ShouldSerializeToJson()
    {
        // Arrange
        var instructorCreated = new InstructorCreated
        {
            Data = new InstructorCreatedData
            {
                InstructorId = 999,
                FirstName = "Bob",
                LastName = "Brown",
                Email = "bob.brown@example.com"
            }
        };

        // Act
        var json = EventSerializer.Serialize(instructorCreated);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"eventType\":\"InstructorCreated\"");
        json.Should().Contain("\"instructorId\":999");
        json.Should().Contain("\"firstName\":\"Bob\"");
        json.Should().Contain("\"data\":{");
    }

    [Fact]
    public void InstructorCreated_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = $@"{{
            ""eventId"":""{Guid.NewGuid()}"",
            ""eventType"":""InstructorCreated"",
            ""schemaVersion"":""1.0"",
            ""occurredAt"":""{DateTime.UtcNow:O}"",
            ""producer"":""InstructorService"",
            ""correlationId"":""{Guid.NewGuid()}"",
            ""subject"":""instructor/555"",
            ""data"": {{
                ""instructorId"":555,
                ""firstName"":""Carol"",
                ""lastName"":""Davis"",
                ""email"":""carol.davis@example.com""
            }}
        }}";

        // Act
        var deserialized = EventSerializer.Deserialize<InstructorCreated>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.EventType.Should().Be("InstructorCreated");
        deserialized.Data.Should().NotBeNull();
        deserialized.Data.InstructorId.Should().Be(555);
        deserialized.Data.FirstName.Should().Be("Carol");
        deserialized.Data.Email.Should().Be("carol.davis@example.com");
    }
}
