﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Intellias.CQRS.Core.Commands;

namespace Intellias.CQRS.Tests.Core.Fakes
{
    /// <inheritdoc />
    public class InProcessCommandBus : ICommandBus
    {
        private readonly Dictionary<Type, ICommandHandler<ICommand>> funcs;

        /// <summary>
        /// Creates command bus
        /// </summary>
        public InProcessCommandBus()
        {
            funcs = new Dictionary<Type, ICommandHandler<ICommand>>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void AddHandler<T>(ICommandHandler<T> handler) where T : ICommand
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var abstractHandler = (ICommandHandler<ICommand>)new CommandHandlerWrapper<T>(handler);

            if (funcs.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Command Handler for command {typeof(T)} already exists");
            }
            else
            {
                funcs.Add(typeof(T), abstractHandler);
            }
        }

        /// <inheritdoc />
        public async Task<ICommandResult> PublishAsync(ICommand msg)
        {
            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }

            var func = funcs[msg.GetType()];

            var result = await func.HandleAsync(msg);

            return await Task.FromResult(result);
        }
    }
}
