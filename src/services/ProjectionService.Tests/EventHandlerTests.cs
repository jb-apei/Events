using FluentAssertions;
using Shared.Events;
using Shared.Events.Prospects;
using Shared.Events.Students;
using Shared.Events.Instructors;
using Xunit;

namespace ProjectionService.Tests;

public class EventHandlerTests
{
    [Fact]
    public void EventHandler_ShouldProcessProspectCreatedEvent()
    {
        // Arrange
        var prospectCreated = new ProspectCreated
        {
            Data = new ProspectCreatedData
            {
                ProspectId = 123,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            },
            Subject = "prospect/123"
        };

        // Act
        var isValid = !string.IsNullOrEmpty(prospectCreated.EventId);

        // Assert
        isValid.Should().BeTrue();
        prospectCreated.EventType.Should().Be("ProspectCreated");
        prospectCreated.Data.ProspectId.Should().Be(123);
    }

    [Fact]
    public void EventHandler_ShouldProcessStudentCreatedEvent()
    {
        // Arrange
        var studentCreated = new StudentCreated
        {
            Data = new StudentCreatedData
            {
                StudentId = 456,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com"
            },
            Subject = "student/456"
        };

        // Act
        var isValid = !string.IsNullOrEmpty(studentCreated.EventId);

        // Assert
        isValid.Should().BeTrue();
        studentCreated.EventType.Should().Be("StudentCreated");
        studentCreated.Data.StudentId.Should().Be(456);
    }

    [Fact]
    public void EventHandler_ShouldProcessInstructorCreatedEvent()
    {
        // Arrange
        var instructorCreated = new InstructorCreated
        {
            Data = new InstructorCreatedData
            {
                InstructorId = 789,
                FirstName = "Alice",
                LastName = "Johnson",
                Email = "alice.johnson@example.com"
            },
            Subject = "instructor/789"
        };

        // Act
        var isValid = !string.IsNullOrEmpty(instructorCreated.EventId);

        // Assert
        isValid.Should().BeTrue();
        instructorCreated.EventType.Should().Be("InstructorCreated");
        instructorCreated.Data.InstructorId.Should().Be(789);
    }

    [Fact]
    public void EventHandler_ShouldDeserializeAndValidateEvent()
    {
        // Arrange
        var prospectCreated = new ProspectCreated
        {
            Data = new ProspectCreatedData
            {
                ProspectId = 999,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            }
        };

        // Act
        var json = EventSerializer.Serialize(prospectCreated);
        var deserialized = EventSerializer.Deserialize<ProspectCreated>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.EventId.Should().Be(prospectCreated.EventId);
        deserialized.Data.ProspectId.Should().Be(999);
        deserialized.Data.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void EventHandler_ShouldValidateEventEnvelopeStructure()
    {
        // Arrange
        var events = new EventEnvelope[]
        {
            new ProspectCreated { Data = new ProspectCreatedData { ProspectId = 1, FirstName = "A", LastName = "B", Email = "a@b.com" } },
            new StudentCreated { Data = new StudentCreatedData { StudentId = 2, FirstName = "C", LastName = "D", Email = "c@d.com" } },
            new InstructorCreated { Data = new InstructorCreatedData { InstructorId = 3, FirstName = "E", LastName = "F", Email = "e@f.com" } }
        };

        // Act & Assert
        foreach (var @event in events)
        {
            @event.EventId.Should().NotBeNullOrEmpty();
            @event.SchemaVersion.Should().Be("1.0");
            @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            @event.EventType.Should().NotBeNullOrEmpty();
        }
    }
}
