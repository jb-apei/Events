using FluentAssertions;
using Shared.Events.Prospects;
using Xunit;

namespace Shared.Events.Tests;

public class ProspectCreatedTests
{
    [Fact]
    public void ProspectCreated_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var prospectCreated = new ProspectCreated();

        // Assert
        prospectCreated.EventType.Should().Be("ProspectCreated");
    }

    [Fact]
    public void ProspectCreated_ShouldContainDataPayload()
    {
        // Arrange & Act
        var prospectCreated = new ProspectCreated
        {
            Data = new ProspectCreatedData
            {
                ProspectId = 123,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            }
        };

        // Assert
        prospectCreated.Data.Should().NotBeNull();
        prospectCreated.Data.ProspectId.Should().Be(123);
        prospectCreated.Data.FirstName.Should().Be("John");
        prospectCreated.Data.LastName.Should().Be("Doe");
        prospectCreated.Data.Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public void ProspectCreated_ShouldInheritFromEventEnvelope()
    {
        // Arrange & Act
        var prospectCreated = new ProspectCreated();

        // Assert
        prospectCreated.Should().BeAssignableTo<EventEnvelope>();
        prospectCreated.EventId.Should().NotBeNullOrEmpty();
        prospectCreated.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ProspectCreated_ShouldSetProducerByDefault()
    {
        // Arrange & Act
        var prospectCreated = new ProspectCreated();

        // Assert
        prospectCreated.Producer.Should().Be("ProspectService");
    }

    [Fact]
    public void ProspectCreated_ShouldSupportFullEventMetadata()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var causationId = Guid.NewGuid().ToString();

        var prospectCreated = new ProspectCreated
        {
            Data = new ProspectCreatedData
            {
                ProspectId = 456,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Phone = "+1-555-5678"
            },
            CorrelationId = correlationId,
            CausationId = causationId,
            Subject = "prospect/456"
        };

        // Act & Assert
        prospectCreated.Data.ProspectId.Should().Be(456);
        prospectCreated.Data.FirstName.Should().Be("Jane");
        prospectCreated.Data.Email.Should().Be("jane.smith@example.com");
        prospectCreated.CorrelationId.Should().Be(correlationId);
        prospectCreated.CausationId.Should().Be(causationId);
        prospectCreated.Subject.Should().Be("prospect/456");
    }
}
