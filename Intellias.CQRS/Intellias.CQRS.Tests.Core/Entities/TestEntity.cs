﻿using Intellias.CQRS.Core.Domain;
using Intellias.CQRS.Tests.Core.Commands;
using Intellias.CQRS.Tests.Core.Events;

namespace Intellias.CQRS.Tests.Core.Entities
{
    /// <summary>
    /// Test aggregate root
    /// </summary>
    public class TestEntity : AggregateRoot<TestState>
    {
        /// <summary>
        /// Main constructor of the class
        /// </summary>
        /// <param name="id"></param>
        public TestEntity(string id) : base(id)
        {
            
        }

        /// <summary>
        /// Creates an instance
        /// </summary>
        public void Create(TestCreateCommand command)
        {
            PublishEvent(new TestCreatedEvent
            {
                AggregateRootId = Id,
                TestData = command.TestData
            });
        }
        /// <summary>
        /// Updates the data
        /// </summary>
        /// <param name="command"></param>
        public void Update(TestUpdateCommand command)
        {
            PublishEvent(new TestUpdatedEvent
            {
                AggregateRootId = Id,
                TestData = command.TestData
            });
        }

        /// <summary>
        /// Deactivates an instance
        /// </summary>
        public void Deactivate()
        {
            PublishEvent(new TestDeletedEvent
            {
                AggregateRootId = Id
            });
        }
    }
}
