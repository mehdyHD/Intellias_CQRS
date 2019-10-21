﻿using Newtonsoft.Json;

namespace Intellias.CQRS.Core.Results
{
    /// <summary>
    /// ExecutionError.
    /// </summary>
    public class ExecutionError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionError"/> class.
        /// </summary>
        /// <param name="message">Reason of failure.</param>
        public ExecutionError(string message)
        {
            Message = message;
            Code = ErrorCodes.UnhandledError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionError"/> class.
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="message">Error Message.</param>
        public ExecutionError(string source, string message)
        {
            Source = source;
            Message = message;
            Code = ErrorCodes.ValidationFailed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionError"/> class.
        /// </summary>
        /// <param name="code">Code.</param>
        /// <param name="source">Source.</param>
        /// <param name="message">Error Message.</param>
        public ExecutionError(string code, string source, string message)
        {
            Code = code;
            Source = source;
            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionError"/> class.
        /// </summary>
        [JsonConstructor]
        protected ExecutionError()
        {
        }

        /// <summary>
        /// Error code.
        /// </summary>
        [JsonProperty]
        public string Code { get; protected set; } = string.Empty;

        /// <summary>
        /// Error Source, optional.
        /// </summary>
        [JsonProperty]
        public string Source { get; protected set; } = string.Empty;

        /// <summary>
        /// Reason of failure.
        /// </summary>
        [JsonProperty]
        public string Message { get; protected set; } = string.Empty;

        /// <summary>
        /// ErrorMessage.
        /// </summary>
        /// <returns>Converted String.</returns>
        public override string ToString()
        {
            return $"{Code}: '{Message}'";
        }
    }
}