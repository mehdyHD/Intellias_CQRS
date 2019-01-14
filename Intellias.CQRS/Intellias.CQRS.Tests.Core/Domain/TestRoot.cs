﻿using Intellias.CQRS.Core.Domain;
using Intellias.CQRS.Core.Messages;
using Intellias.CQRS.Tests.Core.Commands;
using Intellias.CQRS.Tests.Core.Events;

namespace Intellias.CQRS.Tests.Core.Domain
{
    /// <inheritdoc cref="IAggregateRoot"/>
    public class TestRoot : AggregateRoot<TestState>
    {
        /// <summary>
        /// 
        /// </summary>
        public TestRoot(string id) : base(id)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public TestRoot(TestCreateCommand command) : base(command.AggregateRootId)
        {
            PublishEvent(new TestCreatedEvent
            {
                TestData = command.TestData
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public IExecutionResult Update(TestUpdateCommand command)
        {
            if (command.TestData.Length < 10)
            {
                return ExecutionResult.Fail("text too small");
            }

            PublishEvent(new TestUpdatedEvent
            {
                TestData = command.TestData,
            });

            return ExecutionResult.Success;
        }

        /// <summary>
        /// 
        /// </summary>
        public IExecutionResult Deactivate()
        {
            PublishEvent(new TestDeletedEvent());
            return ExecutionResult.Success;
        }
    }
}