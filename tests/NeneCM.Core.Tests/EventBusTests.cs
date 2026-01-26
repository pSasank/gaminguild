using NeneCM.Core.Events;
using Xunit;

namespace NeneCM.Core.Tests;

public class EventBusTests
{
    [Fact]
    public void Subscribe_And_Publish_CallsHandler()
    {
        var eventBus = new EventBus();
        TurnAdvancedEvent? receivedEvent = null;

        eventBus.Subscribe<TurnAdvancedEvent>(e => receivedEvent = e);
        eventBus.Publish(new TurnAdvancedEvent { NewTurn = 5, NewMonth = 6, NewYear = 2024 });

        Assert.NotNull(receivedEvent);
        Assert.Equal(5, receivedEvent.NewTurn);
        Assert.Equal(6, receivedEvent.NewMonth);
        Assert.Equal(2024, receivedEvent.NewYear);
    }

    [Fact]
    public void Subscribe_MultipleHandlers_AllCalled()
    {
        var eventBus = new EventBus();
        int callCount = 0;

        eventBus.Subscribe<TurnAdvancedEvent>(_ => callCount++);
        eventBus.Subscribe<TurnAdvancedEvent>(_ => callCount++);
        eventBus.Subscribe<TurnAdvancedEvent>(_ => callCount++);

        eventBus.Publish(new TurnAdvancedEvent { NewTurn = 1 });

        Assert.Equal(3, callCount);
    }

    [Fact]
    public void Unsubscribe_RemovesHandler()
    {
        var eventBus = new EventBus();
        int callCount = 0;
        Action<TurnAdvancedEvent> handler = _ => callCount++;

        eventBus.Subscribe(handler);
        eventBus.Publish(new TurnAdvancedEvent { NewTurn = 1 });
        Assert.Equal(1, callCount);

        bool removed = eventBus.Unsubscribe(handler);
        Assert.True(removed);

        eventBus.Publish(new TurnAdvancedEvent { NewTurn = 2 });
        Assert.Equal(1, callCount); // Should not have increased
    }

    [Fact]
    public void Unsubscribe_ReturnsFalse_WhenHandlerNotFound()
    {
        var eventBus = new EventBus();
        Action<TurnAdvancedEvent> handler = _ => { };

        bool removed = eventBus.Unsubscribe(handler);

        Assert.False(removed);
    }

    [Fact]
    public void Publish_NoSubscribers_DoesNotThrow()
    {
        var eventBus = new EventBus();

        var exception = Record.Exception(() =>
            eventBus.Publish(new TurnAdvancedEvent { NewTurn = 1 }));

        Assert.Null(exception);
    }

    [Fact]
    public void Publish_DifferentEventTypes_OnlyCallsMatchingHandlers()
    {
        var eventBus = new EventBus();
        int turnEventCount = 0;
        int policyEventCount = 0;

        eventBus.Subscribe<TurnAdvancedEvent>(_ => turnEventCount++);
        eventBus.Subscribe<PolicyImplementedEvent>(_ => policyEventCount++);

        eventBus.Publish(new TurnAdvancedEvent { NewTurn = 1 });

        Assert.Equal(1, turnEventCount);
        Assert.Equal(0, policyEventCount);

        eventBus.Publish(new PolicyImplementedEvent { PolicyId = 1 });

        Assert.Equal(1, turnEventCount);
        Assert.Equal(1, policyEventCount);
    }

    [Fact]
    public void GetSubscriberCount_ReturnsCorrectCount()
    {
        var eventBus = new EventBus();

        Assert.Equal(0, eventBus.GetSubscriberCount<TurnAdvancedEvent>());

        eventBus.Subscribe<TurnAdvancedEvent>(_ => { });
        Assert.Equal(1, eventBus.GetSubscriberCount<TurnAdvancedEvent>());

        eventBus.Subscribe<TurnAdvancedEvent>(_ => { });
        Assert.Equal(2, eventBus.GetSubscriberCount<TurnAdvancedEvent>());

        // Different event type should have 0
        Assert.Equal(0, eventBus.GetSubscriberCount<PolicyImplementedEvent>());
    }

    [Fact]
    public void ClearAll_RemovesAllSubscribers()
    {
        var eventBus = new EventBus();
        int callCount = 0;

        eventBus.Subscribe<TurnAdvancedEvent>(_ => callCount++);
        eventBus.Subscribe<PolicyImplementedEvent>(_ => callCount++);

        eventBus.ClearAll();

        eventBus.Publish(new TurnAdvancedEvent { NewTurn = 1 });
        eventBus.Publish(new PolicyImplementedEvent { PolicyId = 1 });

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Clear_RemovesSubscribersForSpecificType()
    {
        var eventBus = new EventBus();
        int turnEventCount = 0;
        int policyEventCount = 0;

        eventBus.Subscribe<TurnAdvancedEvent>(_ => turnEventCount++);
        eventBus.Subscribe<PolicyImplementedEvent>(_ => policyEventCount++);

        eventBus.Clear<TurnAdvancedEvent>();

        eventBus.Publish(new TurnAdvancedEvent { NewTurn = 1 });
        eventBus.Publish(new PolicyImplementedEvent { PolicyId = 1 });

        Assert.Equal(0, turnEventCount);
        Assert.Equal(1, policyEventCount);
    }

    [Fact]
    public void Publish_HandlerThrowsException_OtherHandlersStillCalled()
    {
        var eventBus = new EventBus();
        int successfulCalls = 0;

        eventBus.Subscribe<TurnAdvancedEvent>(_ => successfulCalls++);
        eventBus.Subscribe<TurnAdvancedEvent>(_ => throw new InvalidOperationException("Test exception"));
        eventBus.Subscribe<TurnAdvancedEvent>(_ => successfulCalls++);

        // Should not throw, and other handlers should be called
        var exception = Record.Exception(() =>
            eventBus.Publish(new TurnAdvancedEvent { NewTurn = 1 }));

        Assert.Null(exception);
        Assert.Equal(2, successfulCalls);
    }

    [Fact]
    public void Events_HaveTimestamp()
    {
        var before = DateTime.UtcNow;
        var gameEvent = new TurnAdvancedEvent { NewTurn = 1 };
        var after = DateTime.UtcNow;

        Assert.True(gameEvent.Timestamp >= before);
        Assert.True(gameEvent.Timestamp <= after);
    }
}
