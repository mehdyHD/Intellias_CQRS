﻿using Intellias.CQRS.Core.Queries;

namespace Intellias.CQRS.Tests.Core.Queries
{
    /// <summary>
    /// 
    /// </summary>
    public class DemoReadModel : IReadModel
    {
        /// <summary>
        /// 
        /// </summary>
        public string Id { set; get; }

        /// <summary>
        /// 
        /// </summary>
        public string TestData { set; get; }
    }
}
