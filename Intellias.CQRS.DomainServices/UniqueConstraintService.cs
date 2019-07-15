﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Intellias.CQRS.Core.Results;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Protocol;

namespace Intellias.CQRS.DomainServices
{
    /// <summary>
    /// Unique-constraint service
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class UniqueConstraintService : IUniqueConstraintService
    {
        private readonly CloudTable table;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="account"></param>
        public UniqueConstraintService(CloudStorageAccount account)
        {
            var client = account.CreateCloudTableClient();
            table = client.GetTableReference(typeof(UniqueConstraintService).Name);

            if (!table.ExistsAsync().GetAwaiter().GetResult())
            {
                table.CreateIfNotExistsAsync().Wait();
            }
        }

        /// <inheritdoc />
        public async Task<IExecutionResult> RemoveConstraintAsync(string indexName, string value)
        {
            try
            {
                await table.ExecuteAsync(TableOperation.Delete(new TableEntity
                {
                    PartitionKey = indexName,
                    RowKey = value,
                    Timestamp = DateTimeOffset.UtcNow,
                    ETag = "*"
                }));
            }
            catch (StorageException e)
            {
                var errorCode = e.RequestInformation.ExtendedErrorInformation.ErrorCode;
                if (StorageErrorCodeStrings.ResourceNotFound.Equals(errorCode, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new FailedResult($"The name '{value}' is not in use. Please enter another one.");
                }

                return new FailedResult("Delete operation failed.");
            }

            return new SuccessfulResult();
        }


        /// <inheritdoc />
        public async Task<IExecutionResult> ReserveConstraintAsync(string indexName, string value)
        {
            try
            {
                await table.ExecuteAsync(TableOperation.Insert(new TableEntity
                {
                    PartitionKey = indexName,
                    RowKey = value,
                    Timestamp = DateTimeOffset.UtcNow,
                    ETag = "*"
                }));
            }
            catch (StorageException e)
            {
                var errorCode = e.RequestInformation.ExtendedErrorInformation.ErrorCode;
                if (TableErrorCodeStrings.EntityAlreadyExists.Equals(errorCode, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new FailedResult($"The name '{value}' is already in use. Please enter another one.");
                }

                return new FailedResult("Reserve operation failed.");
            }

            return new SuccessfulResult();
        }

        /// <inheritdoc />
        public async Task<IExecutionResult> UpdateConstraintAsync(string indexName, string oldValue, string newValue)
        {
            var updateOperation = new TableBatchOperation();

            updateOperation.Delete(new TableEntity
            {
                PartitionKey = indexName,
                RowKey = oldValue,
                Timestamp = DateTimeOffset.UtcNow,
                ETag = "*"
            });
            updateOperation.Insert(new TableEntity
            {
                PartitionKey = indexName,
                RowKey = newValue,
                Timestamp = DateTimeOffset.UtcNow,
                ETag = "*"
            });

            try
            {
                await table.ExecuteBatchAsync(updateOperation);
            }
            catch (StorageException e)
            {
                var errorCode = e.RequestInformation.ExtendedErrorInformation.ErrorCode;
                if (TableErrorCodeStrings.EntityAlreadyExists.Equals(errorCode, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new FailedResult($"The name '{newValue}' is already in use. Please enter another one.");
                }

                if (StorageErrorCodeStrings.ResourceNotFound.Equals(errorCode, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new FailedResult($"The name '{oldValue}' is not in use. Please enter another one.");
                }

                return new FailedResult("Update operation failed.");
            }

            return new SuccessfulResult();
        }
    }
}