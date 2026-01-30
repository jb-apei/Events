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
    public void ProspectCreated_ShouldRequireProspectId()
    {
        // Arrange
        var prospectId = Guid.NewGuid();
        var prospectCreated = new ProspectCreated
        {
            ProspectId = prospectId
        };

        // Act & Assert
        prospectCreated.ProspectId.Should().Be(prospectId);
    }

    [Fact]
    public void ProspectCreated_ShouldContainFirstName()
    {
        // Arrange
        const string firstName = "John";
        var prospectCreated = new ProspectCreated
        {
            FirstName = firstName
        };

        // Act & Assert
        prospectCreated.FirstName.Should().Be(firstName);
    }

    [Fact]
    public void ProspectCreated_ShouldContainLastName()
    {
        // Arrange
        const string lastName = "Doe";
        var prospectCreated = new ProspectCreated
        {
            LastName = lastName
        };

        // Act & Assert
        prospectCreated.LastName.Should().Be(lastName);
    }

    [Fact]
    public void ProspectCreated_ShouldContainEmail()
    {
        // Arrange
        const string email = "john.doe@example.com";
        var prospectCreated = new ProspectCreated
        {
            Email = email
        };

        // Act & Assert
        prospectCreated.Email.Should().Be(email);
    }

    [Fact]
    public void ProspectCreated_ShouldContainPhone()
    {
        // Arrange
        const string phone = "+1-555-1234";
        var prospectCreated = new ProspectCreated
        {
            Phone = phone
        };

        // Act & Assert
        prospectCreated.Phone.Should().Be(phone);
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
    public void ProspectCreated_ShouldSupportFullProspectData()
    {
        // Arrange
        var prospectId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();

        var prospectCreated = new ProspectCreated
        {
            ProspectId = prospectId,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Phone = "+1-555-5678",
            Producer = "ProspectService",
            CorrelationId = correlationId,
            Subject = $"prospect/{prospectId}"
        };

        // Act & Assert
        prospectCreated.ProspectId.Should().Be(prospectId);
        prospectCreated.FirstName.Should().Be("Jane");
        prospectCreated.LastName.Should().Be("Smith");
        prospectCreated.Email.Should().Be("jane.smith@example.com");
        prospectCreated.Phone.Should().Be("+1-555-5678");
        prospectCreated.Producer.Should().Be("ProspectService");
        prospectCreated.CorrelationId.Should().Be(correlationId);
        prospectCreated.Subject.Should().Be($"prospect/{prospectId}");
    }
}
