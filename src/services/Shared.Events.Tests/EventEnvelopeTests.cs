using FluentAssertions;
using Xunit;

namespace Shared.Events.Tests;

public class EventEnvelopeTests
{
    private class TestEvent : EventEnvelope
    {
        public override string EventType => "TestEvent";
        public string TestData { get; set; } = string.Empty;
    }

    [Fact]
    public void EventEnvelope_ShouldGenerateUniqueEventId()
    {
        // Arrange & Act
        var event1 = new TestEvent();
        var event2 = new TestEvent();

        // Assert
        event1.EventId.Should().NotBeNullOrEmpty();
        event2.EventId.Should().NotBeNullOrEmpty();
        event1.EventId.Should().NotBe(event2.EventId, "each event should have a unique ID");
    }

    [Fact]
    public void EventEnvelope_ShouldSetDefaultSchemaVersion()
    {
        // Arrange & Act
        var testEvent = new TestEvent();

        // Assert
        testEvent.SchemaVersion.Should().Be("1.0");
    }

    [Fact]
    public void EventEnvelope_ShouldSetOccurredAtToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var testEvent = new TestEvent();
        var afterCreation = DateTime.UtcNow;

        // Assert
        testEvent.OccurredAt.Should().BeOnOrAfter(beforeCreation);
        testEvent.OccurredAt.Should().BeOnOrBefore(afterCreation);
        testEvent.OccurredAt.Kind.Should().Be(DateTimeKind.Utc, "timestamps must be UTC");
    }

    [Fact]
    public void EventEnvelope_ShouldAllowSettingProducer()
    {
        // Arrange
        var testEvent = new TestEvent();
        const string expectedProducer = "ProspectService";

        // Act
        testEvent.Producer = expectedProducer;

        // Assert
        testEvent.Producer.Should().Be(expectedProducer);
    }

    [Fact]
    public void EventEnvelope_ShouldAllowSettingCorrelationId()
    {
        // Arrange
        var testEvent = new TestEvent();
        var expectedCorrelationId = Guid.NewGuid().ToString();

        // Act
        testEvent.CorrelationId = expectedCorrelationId;

        // Assert
        testEvent.CorrelationId.Should().Be(expectedCorrelationId);
    }

    [Fact]
    public void EventEnvelope_ShouldAllowSettingCausationId()
    {
        // Arrange
        var testEvent = new TestEvent();
        var expectedCausationId = Guid.NewGuid().ToString();

        // Act
        testEvent.CausationId = expectedCausationId;

        // Assert
        testEvent.CausationId.Should().Be(expectedCausationId);
    }

    [Fact]
    public void EventEnvelope_ShouldExposeEventType()
    {
        // Arrange & Act
        var testEvent = new TestEvent();

        // Assert
        testEvent.EventType.Should().Be("TestEvent");
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.1")]
    [InlineData("3.0.1")]
    public void EventEnvelope_ShouldAllowCustomSchemaVersions(string version)
    {
        // Arrange
        var testEvent = new TestEvent();

        // Act
        testEvent.SchemaVersion = version;

        // Assert
        testEvent.SchemaVersion.Should().Be(version);
    }

    [Fact]
    public void EventEnvelope_ShouldAllowSettingSubject()
    {
        // Arrange
        var testEvent = new TestEvent();
        const string expectedSubject = "prospect/12345";

        // Act
        testEvent.Subject = expectedSubject;

        // Assert
        testEvent.Subject.Should().Be(expectedSubject);
    }
}
