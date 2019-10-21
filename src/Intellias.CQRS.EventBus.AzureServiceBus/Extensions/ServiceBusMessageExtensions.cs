﻿using System.Text;
using Intellias.CQRS.Core;
using Intellias.CQRS.Core.Events;
using Intellias.CQRS.Core.Messages;
using Microsoft.Azure.ServiceBus;

namespace Intellias.CQRS.EventBus.AzureServiceBus.Extensions
{
    /// <summary>
    /// Service bus extensions.
    /// </summary>
    public static class ServiceBusMessageExtensions
    {
        /// <summary>
        /// Event to service-bus message.
        /// </summary>
        /// <param name="event">event.</param>
        /// <returns>Message.</returns>
        public static Message ToBusMessage(this IEvent @event) =>
            new Message(Encoding.UTF8.GetBytes(@event.ToJson()))
            {
                MessageId = @event.Id,
                ContentType = @event.GetType().AssemblyQualifiedName,
                PartitionKey = @event.AggregateRootId,
                CorrelationId = @event.CorrelationId,
                SessionId = AbstractMessage.GlobalSessionId,
                Label = @event.GetType().Name
            };
    }
}