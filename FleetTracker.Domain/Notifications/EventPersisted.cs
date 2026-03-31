using FleetTracker.Domain.Events;
using MediatR;

namespace FleetTracker.Domain.Notifications;

public record EventPersisted(DomainEvent Event) : INotification;
