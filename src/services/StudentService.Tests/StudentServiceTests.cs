using FluentAssertions;
using Shared.Events;
using Shared.Events.Students;
using Xunit;

namespace StudentService.Tests;

public class StudentServiceTests
{
    [Fact]
    public void StudentCreated_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var studentCreated = new StudentCreated();

        // Assert
        studentCreated.EventType.Should().Be("StudentCreated");
        studentCreated.Producer.Should().Be("StudentService");
    }

    [Fact]
    public void StudentCreated_ShouldContainDataPayload()
    {
        // Arrange & Act
        var studentCreated = new StudentCreated
        {
            Data = new StudentCreatedData
            {
                StudentId = 456,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com"
            }
        };

        // Assert
        studentCreated.Data.Should().NotBeNull();
        studentCreated.Data.StudentId.Should().Be(456);
        studentCreated.Data.FirstName.Should().Be("Jane");
        studentCreated.Data.LastName.Should().Be("Smith");
        studentCreated.Data.Email.Should().Be("jane.smith@example.com");
    }

    [Fact]
    public void StudentCreated_ShouldInheritFromEventEnvelope()
    {
        // Arrange & Act
        var studentCreated = new StudentCreated();

        // Assert
        studentCreated.Should().BeAssignableTo<EventEnvelope>();
        studentCreated.EventId.Should().NotBeNullOrEmpty();
        studentCreated.SchemaVersion.Should().Be("1.0");
        studentCreated.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void StudentCreated_ShouldSerializeToJson()
    {
        // Arrange
        var studentCreated = new StudentCreated
        {
            Data = new StudentCreatedData
            {
                StudentId = 789,
                FirstName = "Test",
                LastName = "User",
                Email = "test.user@example.com"
            }
        };

        // Act
        var json = EventSerializer.Serialize(studentCreated);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"eventType\":\"StudentCreated\"");
        json.Should().Contain("\"studentId\":789");
        json.Should().Contain("\"firstName\":\"Test\"");
        json.Should().Contain("\"data\":{");
    }

    [Fact]
    public void StudentCreated_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = $@"{{
            ""eventId"":""{Guid.NewGuid()}"",
            ""eventType"":""StudentCreated"",
            ""schemaVersion"":""1.0"",
            ""occurredAt"":""{DateTime.UtcNow:O}"",
            ""producer"":""StudentService"",
            ""correlationId"":""{Guid.NewGuid()}"",
            ""subject"":""student/123"",
            ""data"": {{
                ""studentId"":123,
                ""firstName"":""John"",
                ""lastName"":""Doe"",
                ""email"":""john.doe@example.com""
            }}
        }}";

        // Act
        var deserialized = EventSerializer.Deserialize<StudentCreated>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.EventType.Should().Be("StudentCreated");
        deserialized.Data.Should().NotBeNull();
        deserialized.Data.StudentId.Should().Be(123);
        deserialized.Data.FirstName.Should().Be("John");
        deserialized.Data.Email.Should().Be("john.doe@example.com");
    }
}
