using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Intellias.CQRS.Core.Queries.Mutable;
using Intellias.CQRS.QueryStore.AzureTable.Options;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Intellias.CQRS.QueryStore.AzureTable.Mutable
{
    /// <summary>
    /// Azure Storage Account Table storage for <see cref="IMutableQueryModel"/>.
    /// </summary>
    /// <typeparam name="TQueryModel">Type of the query model.</typeparam>
    public class MutableQueryModelTableStorage<TQueryModel> :
        IMutableQueryModelReader<TQueryModel>,
        IMutableQueryModelWriter<TQueryModel>
        where TQueryModel : IMutableQueryModel, new()
    {
        private readonly IOptionsMonitor<TableStorageOptions> options;
        private readonly Lazy<CloudTable> tableClient;

        public MutableQueryModelTableStorage(IOptionsMonitor<TableStorageOptions> options)
        {
            this.options = options;
            tableClient = new Lazy<CloudTable>(CreateTable);
        }

        /// <inheritdoc />
        public async Task<TQueryModel> FindAsync(string id)
        {
            var operation = TableOperation.Retrieve<MutableTableEntity>(typeof(TQueryModel).Name, id);
            var result = await tableClient.Value.ExecuteAsync(operation);
            var entity = (MutableTableEntity)result.Result;

            return entity == null ? default : entity.DeserializeQueryModel();
        }

        /// <inheritdoc />
        public async Task<TQueryModel> GetAsync(string id)
        {
            var queryModel = await FindAsync(id);
            if (queryModel == null)
            {
                throw new KeyNotFoundException($"No query model '{typeof(TQueryModel)}' with id '{id}' is found.");
            }

            return queryModel;
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<TQueryModel>> GetAllAsync()
        {
            return QueryAllSegmentedAsync(new TableQuery<MutableTableEntity>());
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<TQueryModel>> GetAllAsync(IReadOnlyCollection<string> ids)
        {
            var idsChunks = ids
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / options.CurrentValue.QueryChunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            var results = new List<TQueryModel>();
            var partitionKeyCondition = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, typeof(TQueryModel).Name);
            foreach (var idsChunk in idsChunks)
            {
                var rowKeyCondition = idsChunk
                    .Select(id => TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, id))
                    .Aggregate((current, next) => TableQuery.CombineFilters(current, TableOperators.Or, next));

                var filter = TableQuery.CombineFilters(partitionKeyCondition, TableOperators.And, rowKeyCondition);
                var query = new TableQuery<MutableTableEntity>().Where(filter);

                results.AddRange(await QueryAllSegmentedAsync(query));
            }

            return results;
        }

        /// <inheritdoc />
        public Task CreateAsync(TQueryModel model)
        {
            var entity = new MutableTableEntity(model);
            var operation = TableOperation.Insert(entity);

            return tableClient.Value.ExecuteAsync(operation);
        }

        /// <inheritdoc />
        public Task ReplaceAsync(TQueryModel model)
        {
            var entity = new MutableTableEntity(model);
            var operation = TableOperation.Replace(entity);

            return tableClient.Value.ExecuteAsync(operation);
        }

        private async Task<IReadOnlyCollection<TQueryModel>> QueryAllSegmentedAsync(TableQuery<MutableTableEntity> query)
        {
            var results = new List<TQueryModel>();
            var continuationToken = new TableContinuationToken();

            do
            {
                var querySegment = await tableClient.Value.ExecuteQuerySegmentedAsync(query, continuationToken);
                var queryResults = querySegment.Results.Select(entity => entity.DeserializeQueryModel());

                results.AddRange(queryResults);

                continuationToken = querySegment.ContinuationToken;
            }
            while (continuationToken != null);

            return results;
        }

        private CloudTable CreateTable()
        {
            var client = CloudStorageAccount
                .Parse(options.CurrentValue.ConnectionString)
                .CreateCloudTableClient();

            var tableName = this.options.CurrentValue.TableNamePrefix + typeof(TQueryModel).Name;
            var tableReference = client.GetTableReference(tableName);
            if (!tableReference.ExistsAsync().GetAwaiter().GetResult())
            {
                tableReference.CreateIfNotExistsAsync().GetAwaiter().GetResult();
            }

            return tableReference;
        }

        private class MutableTableEntity : TableEntity
        {
            public MutableTableEntity()
            {
            }

            public MutableTableEntity(TQueryModel queryModel)
            {
                PartitionKey = typeof(TQueryModel).Name;
                RowKey = queryModel.Id;
                ETag = string.IsNullOrWhiteSpace(queryModel.ETag) ? "*" : queryModel.ETag;
                JsonQueryModel = JsonConvert.SerializeObject(queryModel);
            }

            public string JsonQueryModel { get; set; }

            public TQueryModel DeserializeQueryModel()
            {
                if (string.IsNullOrWhiteSpace(JsonQueryModel))
                {
                    return default;
                }

                var queryModel = JsonConvert.DeserializeObject<TQueryModel>(JsonQueryModel);

                queryModel.Timestamp = Timestamp;
                queryModel.ETag = ETag;

                return queryModel;
            }
        }
    }
}