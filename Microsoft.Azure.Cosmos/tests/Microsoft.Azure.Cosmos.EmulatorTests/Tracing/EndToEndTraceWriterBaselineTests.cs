﻿namespace Microsoft.Azure.Cosmos.EmulatorTests.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.Azure.Cosmos.CosmosElements;
    using Microsoft.Azure.Cosmos.Diagnostics;
    using Microsoft.Azure.Cosmos.SDK.EmulatorTests;
    using Microsoft.Azure.Cosmos.Services.Management.Tests.BaselineTest;
    using Microsoft.Azure.Cosmos.Tracing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;
    using static Microsoft.Azure.Cosmos.SDK.EmulatorTests.TransportClientHelper;

    [VisualStudio.TestTools.UnitTesting.TestClass]
    public sealed class EndToEndTraceWriterBaselineTests : BaselineTests<EndToEndTraceWriterBaselineTests.Input, EndToEndTraceWriterBaselineTests.Output>
    {
        public static CosmosClient client;
        public static Database database;
        public static Container container;

        [ClassInitialize()]
        public static async Task ClassInitAsync(TestContext context)
        {
            client = Microsoft.Azure.Cosmos.SDK.EmulatorTests.TestCommon.CreateCosmosClient(useGateway: false);
            EndToEndTraceWriterBaselineTests.database = await client.CreateDatabaseAsync(
                    Guid.NewGuid().ToString(),
                    cancellationToken: default);

            EndToEndTraceWriterBaselineTests.container = await EndToEndTraceWriterBaselineTests.database.CreateContainerAsync(
                    id: Guid.NewGuid().ToString(),
                    partitionKeyPath: "/id",
                    throughput: 20000);

            for (int i = 0; i < 100; i++)
            {
                CosmosObject cosmosObject = CosmosObject.Create(
                    new Dictionary<string, CosmosElement>()
                    {
                        { "id", CosmosString.Create(i.ToString()) }
                    });

                await container.CreateItemAsync(JToken.Parse(cosmosObject.ToString()));
            }
        }

        [ClassCleanup()]
        public static async Task ClassCleanupAsync()
        {
            if(database != null)
            {
                await EndToEndTraceWriterBaselineTests.database.DeleteStreamAsync();
            }
        }

        [TestMethod]
        public async Task ReadFeedAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            //----------------------------------------------------------------
            //  ReadFeed
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                FeedIteratorInternal feedIterator = (FeedIteratorInternal)container.GetItemQueryStreamIterator(
                    queryText: null);

                List<ITrace> traces = new List<ITrace>();
                while (feedIterator.HasMoreResults)
                {
                    ResponseMessage responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("ReadFeed", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  ReadFeed Typed
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                FeedIteratorInternal<JToken> feedIterator = (FeedIteratorInternal<JToken>)container
                    .GetItemQueryIterator<JToken>(queryText: null);

                List<ITrace> traces = new List<ITrace>();
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<JToken> response = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)response.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("ReadFeed Typed", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  ReadFeed Public API
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                FeedIterator feedIterator = container.GetItemQueryStreamIterator(
                    queryText: null);

                List<ITrace> traces = new List<ITrace>();

                while (feedIterator.HasMoreResults)
                {
                    ResponseMessage responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("ReadFeed Public API", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  ReadFeed Public API Typed
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                FeedIterator<JToken> feedIterator = container
                    .GetItemQueryIterator<JToken>(queryText: null);

                List<ITrace> traces = new List<ITrace>();

                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<JToken> responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("ReadFeed Public API Typed", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            this.ExecuteTestSuite(inputs);
        }

        [TestMethod]
        public async Task ChangeFeedAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            //----------------------------------------------------------------
            //  ChangeFeed
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ContainerInternal containerInternal = (ContainerInternal)container;
                FeedIteratorInternal feedIterator = (FeedIteratorInternal)containerInternal.GetChangeFeedStreamIterator(
                    ChangeFeedStartFrom.Beginning(),
                    ChangeFeedMode.Incremental);

                List<ITrace> traces = new List<ITrace>();
                while (feedIterator.HasMoreResults)
                {
                    ResponseMessage responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                    if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotModified)
                    {
                        break;
                    }
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("ChangeFeed", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  ChangeFeed Typed
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ContainerInternal containerInternal = (ContainerInternal)container;
                FeedIteratorInternal<JToken> feedIterator = (FeedIteratorInternal<JToken>)containerInternal.GetChangeFeedIterator<JToken>(
                    ChangeFeedStartFrom.Beginning(),
                    ChangeFeedMode.Incremental);

                List<ITrace> traces = new List<ITrace>();
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<JToken> responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotModified)
                    {
                        break;
                    }

                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("ChangeFeed Typed", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  ChangeFeed Public API
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ContainerInternal containerInternal = (ContainerInternal)container;
                FeedIterator feedIterator = containerInternal.GetChangeFeedStreamIterator(
                    ChangeFeedStartFrom.Beginning(),
                    ChangeFeedMode.Incremental);

                List<ITrace> traces = new List<ITrace>();

                while (feedIterator.HasMoreResults)
                {
                    ResponseMessage responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotModified)
                    {
                        break;
                    }

                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("ChangeFeed Public API", traceForest, startLineNumber, endLineNumber));
            }
            //---------------------------------------------------------------- 

            //----------------------------------------------------------------
            //  ChangeFeed Public API Typed
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ContainerInternal containerInternal = (ContainerInternal)container;
                FeedIterator<JToken> feedIterator = containerInternal.GetChangeFeedIterator<JToken>(
                    ChangeFeedStartFrom.Beginning(),
                    ChangeFeedMode.Incremental);

                List<ITrace> traces = new List<ITrace>();

                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<JToken> responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotModified)
                    {
                        break;
                    }

                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("ChangeFeed Public API Typed", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  ChangeFeed Estimator
            //----------------------------------------------------------------
            {
                Container leaseContainer = await EndToEndTraceWriterBaselineTests.database.CreateContainerAsync(
                    id: Guid.NewGuid().ToString(),
                    partitionKeyPath: "/id");

                ChangeFeedProcessor processor = container
                .GetChangeFeedProcessorBuilder(
                    processorName: "test",
                    onChangesDelegate: (IReadOnlyCollection<dynamic> docs, CancellationToken token) => Task.CompletedTask)
                .WithInstanceName("random")
                .WithLeaseContainer(leaseContainer)
                .Build();

                await processor.StartAsync();

                // Letting processor initialize
                await Task.Delay(2000);

                await processor.StopAsync();

                startLineNumber = GetLineNumber();
                ChangeFeedEstimator estimator = container.GetChangeFeedEstimator(
                    "test",
                    leaseContainer);
                using FeedIterator<ChangeFeedProcessorState> feedIterator = estimator.GetCurrentStateIterator();

                List<ITrace> traces = new List<ITrace>();

                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<ChangeFeedProcessorState> responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Change Feed Estimator", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            this.ExecuteTestSuite(inputs);
        }

        [TestMethod]
        public async Task QueryAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            //----------------------------------------------------------------
            //  Query
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                FeedIteratorInternal feedIterator = (FeedIteratorInternal)container.GetItemQueryStreamIterator(
                    queryText: "SELECT * FROM c");

                List<ITrace> traces = new List<ITrace>();
                while (feedIterator.HasMoreResults)
                {
                    ResponseMessage responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Query", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Query Typed
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                FeedIteratorInternal<JToken> feedIterator = (FeedIteratorInternal<JToken>)container.GetItemQueryIterator<JToken>(
                    queryText: "SELECT * FROM c");

                List<ITrace> traces = new List<ITrace>();
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<JToken> response = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)response.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Query Typed", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Query Public API
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                FeedIterator feedIterator = container.GetItemQueryStreamIterator(
                    queryText: "SELECT * FROM c");

                List<ITrace> traces = new List<ITrace>();

                while (feedIterator.HasMoreResults)
                {
                    ResponseMessage responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Query Public API", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Query Public API Typed
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                FeedIterator<JToken> feedIterator = container.GetItemQueryIterator<JToken>(
                    queryText: "SELECT * FROM c");

                List<ITrace> traces = new List<ITrace>();

                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<JToken> responseMessage = await feedIterator.ReadNextAsync(cancellationToken: default);
                    ITrace trace = ((CosmosTraceDiagnostics)responseMessage.Diagnostics).Value;
                    traces.Add(trace);
                }

                ITrace traceForest = TraceJoiner.JoinTraces(traces);
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Query Public API Typed", traceForest, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            this.ExecuteTestSuite(inputs);
        }

        [TestMethod]
        public async Task TypedPointOperationsAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            //----------------------------------------------------------------
            //  Point Write
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                CosmosObject cosmosObject = CosmosObject.Create(
                    new Dictionary<string, CosmosElement>()
                    {
                        { "id", CosmosString.Create(9001.ToString()) }
                    });

                ItemResponse<JToken> itemResponse = await container.CreateItemAsync(JToken.Parse(cosmosObject.ToString()));

                ITrace trace = ((CosmosTraceDiagnostics)itemResponse.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Write", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Read
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ItemResponse<JToken> itemResponse = await container.ReadItemAsync<JToken>(
                    id: "9001",
                    partitionKey: new Cosmos.PartitionKey("9001"));

                ITrace trace = ((CosmosTraceDiagnostics)itemResponse.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Read", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Replace
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                CosmosObject cosmosObject = CosmosObject.Create(
                    new Dictionary<string, CosmosElement>()
                    {
                        { "id", CosmosString.Create(9001.ToString()) },
                        { "someField", CosmosString.Create(9001.ToString()) }
                    });

                ItemResponse<JToken> itemResponse = await container.ReplaceItemAsync(
                    JToken.Parse(cosmosObject.ToString()),
                    id: "9001",
                    partitionKey: new Cosmos.PartitionKey("9001"));

                ITrace trace = ((CosmosTraceDiagnostics)itemResponse.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Replace", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Patch (This test creates a 500 on the backend ...)
            //----------------------------------------------------------------
            //{
            //    startLineNumber = GetLineNumber();
            //    ContainerInternal containerInternal = (ContainerInternal)container;
            //    List<PatchOperation> patchOperations = new List<PatchOperation>()
            //    {
            //        PatchOperation.Replace("/someField", "42")
            //    };
            //    ItemResponse<JToken> patchResponse = await containerInternal.PatchItemAsync<JToken>(
            //        id: "9001",
            //        partitionKey: new PartitionKey("9001"),
            //        patchOperations: patchOperations);

            //    ITrace trace = ((CosmosTraceDiagnostics)patchResponse.Diagnostics).Value;
            //    endLineNumber = GetLineNumber();

            //    inputs.Add(new Input("Point Patch", trace, startLineNumber, endLineNumber));
            //}
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Delete
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ItemRequestOptions requestOptions = new ItemRequestOptions();
                ItemResponse<JToken> itemResponse = await container.DeleteItemAsync<JToken>(
                    id: "9001",
                    partitionKey: new PartitionKey("9001"),
                    requestOptions: requestOptions);

                ITrace trace = ((CosmosTraceDiagnostics)itemResponse.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Delete", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            this.ExecuteTestSuite(inputs);
        }

        [TestMethod]
        public async Task StreamPointOperationsAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            //----------------------------------------------------------------
            //  Point Write
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                CosmosObject cosmosObject = CosmosObject.Create(
                    new Dictionary<string, CosmosElement>()
                    {
                        { "id", CosmosString.Create(9001.ToString()) }
                    });

                ResponseMessage itemResponse = await container.CreateItemStreamAsync(
                    new MemoryStream(Encoding.UTF8.GetBytes(cosmosObject.ToString())),
                    new Cosmos.PartitionKey("9001"));

                ITrace trace = ((CosmosTraceDiagnostics)itemResponse.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Write", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Read
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ResponseMessage itemResponse = await container.ReadItemStreamAsync(
                    id: "9001",
                    partitionKey: new Cosmos.PartitionKey("9001"));

                ITrace trace = ((CosmosTraceDiagnostics)itemResponse.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Read", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Replace
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                CosmosObject cosmosObject = CosmosObject.Create(
                    new Dictionary<string, CosmosElement>()
                    {
                        { "id", CosmosString.Create(9001.ToString()) },
                        { "someField", CosmosString.Create(9001.ToString()) }
                    });

                ResponseMessage itemResponse = await container.ReplaceItemStreamAsync(
                    new MemoryStream(Encoding.UTF8.GetBytes(cosmosObject.ToString())),
                    id: "9001",
                    partitionKey: new Cosmos.PartitionKey("9001"));

                ITrace trace = ((CosmosTraceDiagnostics)itemResponse.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Replace", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Patch (this one is flaky)
            //----------------------------------------------------------------
            //{
            //    startLineNumber = GetLineNumber();
            //    ItemRequestOptions requestOptions = new ItemRequestOptions();
            //    ContainerInternal containerInternal = (ContainerInternal)container;
            //    List<PatchOperation> patch = new List<PatchOperation>()
            //    {
            //        PatchOperation.Replace("/someField", "42")
            //    };
            //    ResponseMessage patchResponse = await containerInternal.PatchItemStreamAsync(
            //        id: "9001",
            //        partitionKey: new PartitionKey("9001"),
            //        patchOperations: patch,
            //        requestOptions: requestOptions);

            //    ITrace trace = ((CosmosTraceDiagnostics)patchResponse.Diagnostics).Value;
            //    endLineNumber = GetLineNumber();

            //    inputs.Add(new Input("Point Patch", trace, startLineNumber, endLineNumber));
            //}
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Delete
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ItemRequestOptions requestOptions = new ItemRequestOptions();
                ContainerInternal containerInternal = (ContainerInternal)container;
                ResponseMessage itemResponse = await containerInternal.DeleteItemStreamAsync(
                    id: "9001",
                    partitionKey: new PartitionKey("9001"),
                    requestOptions: requestOptions);

                ITrace trace = ((CosmosTraceDiagnostics)itemResponse.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Delete", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            this.ExecuteTestSuite(inputs);
        }

        [TestMethod]
        public async Task PointOperationsExceptionsAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            //----------------------------------------------------------------
            //  Point Operation With Request Timeout
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ItemRequestOptions requestOptions = new ItemRequestOptions();

                Guid exceptionActivityId = Guid.NewGuid();
                string transportExceptionDescription = "transportExceptionDescription" + Guid.NewGuid();
                Container containerWithTransportException = TransportClientHelper.GetContainerWithItemTransportException(
                    database.Id,
                    container.Id,
                    exceptionActivityId,
                    transportExceptionDescription);

                //Checking point operation diagnostics on typed operations
                ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity();

                ITrace trace = null;
                try
                {
                    ItemResponse<ToDoActivity> createResponse = await containerWithTransportException.CreateItemAsync<ToDoActivity>(
                      item: testItem,
                      requestOptions: requestOptions);
                    Assert.Fail("Should have thrown a request timeout exception");
                }
                catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                {
                    trace = ((CosmosTraceDiagnostics)ce.Diagnostics).Value;
                }
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Operation with Request Timeout", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Operation With Throttle
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                string errorMessage = "Mock throttle exception" + Guid.NewGuid().ToString();
                Guid exceptionActivityId = Guid.NewGuid();
                // Set a small retry count to reduce test time
                CosmosClient throttleClient = TestCommon.CreateCosmosClient(builder =>
                    builder.WithThrottlingRetryOptions(TimeSpan.FromSeconds(5), 5)
                    .WithTransportClientHandlerFactory(transportClient => new TransportClientWrapper(
                           transportClient,
                           (uri, resourceOperation, request) => TransportClientHelper.ReturnThrottledStoreResponseOnItemOperation(
                                uri,
                                resourceOperation,
                                request,
                                exceptionActivityId,
                                errorMessage)))
                    );

                ItemRequestOptions requestOptions = new ItemRequestOptions();
                Container containerWithThrottleException = throttleClient.GetContainer(
                    database.Id,
                    container.Id);

                //Checking point operation diagnostics on typed operations
                ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity();
                ITrace trace = null;
                try
                {
                    ItemResponse<ToDoActivity> createResponse = await containerWithThrottleException.CreateItemAsync<ToDoActivity>(
                      item: testItem,
                      requestOptions: requestOptions);
                    Assert.Fail("Should have thrown a request timeout exception");
                }
                catch (CosmosException ce) when ((int)ce.StatusCode == (int)Documents.StatusCodes.TooManyRequests)
                {
                    trace = ((CosmosTraceDiagnostics)ce.Diagnostics).Value;
                }
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Operation With Throttle", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Operation With Forbidden
            //----------------------------------------------------------------
            {
                List<int> stringLength = new List<int>();
                foreach (int maxCount in new int[] { 1, 2, 4 })
                {
                    startLineNumber = GetLineNumber();
                    int count = 0;
                    List<(string, string)> activityIdAndErrorMessage = new List<(string, string)>(maxCount);
                    Guid transportExceptionActivityId = Guid.NewGuid();
                    string transportErrorMessage = $"TransportErrorMessage{Guid.NewGuid()}";
                    Guid activityIdScope = Guid.Empty;
                    void interceptor(Uri uri, Documents.ResourceOperation operation, Documents.DocumentServiceRequest request)
                    {
                        Assert.AreNotEqual(System.Diagnostics.Trace.CorrelationManager.ActivityId, Guid.Empty, "Activity scope should be set");

                        if (request.ResourceType == Documents.ResourceType.Document)
                        {
                            if (activityIdScope == Guid.Empty)
                            {
                                activityIdScope = System.Diagnostics.Trace.CorrelationManager.ActivityId;
                            }
                            else
                            {
                                Assert.AreEqual(System.Diagnostics.Trace.CorrelationManager.ActivityId, activityIdScope, "Activity scope should match on retries");
                            }

                            if (count >= maxCount)
                            {
                                TransportClientHelper.ThrowTransportExceptionOnItemOperation(
                                    uri,
                                    operation,
                                    request,
                                    transportExceptionActivityId,
                                    transportErrorMessage);
                            }

                            count++;
                            string activityId = Guid.NewGuid().ToString();
                            string errorMessage = $"Error{Guid.NewGuid()}";

                            activityIdAndErrorMessage.Add((activityId, errorMessage));
                            TransportClientHelper.ThrowForbiddendExceptionOnItemOperation(
                                uri,
                                request,
                                activityId,
                                errorMessage);
                        }
                    }

                    Container containerWithTransportException = TransportClientHelper.GetContainerWithIntercepter(
                        database.Id,
                        container.Id,
                        interceptor);
                    //Checking point operation diagnostics on typed operations
                    ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity();

                    ITrace trace = null;
                    try
                    {
                        ItemResponse<ToDoActivity> createResponse = await containerWithTransportException.CreateItemAsync<ToDoActivity>(
                          item: testItem);
                        Assert.Fail("Should have thrown a request timeout exception");
                    }
                    catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                    {
                        trace = ((CosmosTraceDiagnostics)ce.Diagnostics).Value;
                        stringLength.Add(trace.ToString().Length);
                    }

                    endLineNumber = GetLineNumber();
                    inputs.Add(new Input($"Point Operation With Forbidden + Max Count = {maxCount}", trace, startLineNumber, endLineNumber));
                }

                // Check if the exception message is not growing exponentially
                Assert.IsTrue(stringLength.Count > 2);
                for (int i = 0; i < stringLength.Count - 1; i++)
                {
                    int currLength = stringLength[i];
                    int nextLength = stringLength[i + 1];
                    Assert.IsTrue(nextLength < currLength * 2,
                        $"The diagnostic string is growing faster than linear. Length: {currLength}, Next Length: {nextLength}");
                }
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Point Operation With Service Unavailable Exception
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ItemRequestOptions requestOptions = new ItemRequestOptions();

                Guid exceptionActivityId = Guid.NewGuid();
                string ServiceUnavailableExceptionDescription = "ServiceUnavailableExceptionDescription" + Guid.NewGuid();
                Container containerWithTransportException = TransportClientHelper.GetContainerWithItemServiceUnavailableException(
                    database.Id,
                    container.Id,
                    exceptionActivityId,
                    ServiceUnavailableExceptionDescription);

                //Checking point operation diagnostics on typed operations
                ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity();

                ITrace trace = null;
                try
                {
                    ItemResponse<ToDoActivity> createResponse = await containerWithTransportException.CreateItemAsync<ToDoActivity>(
                      item: testItem,
                      requestOptions: requestOptions);
                    Assert.Fail("Should have thrown a Service Unavailable Exception");
                }
                catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    trace = ((CosmosTraceDiagnostics)ce.Diagnostics).Value;                    
                }
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Point Operation with Service Unavailable", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            this.ExecuteTestSuite(inputs);
        }

        [TestMethod]
        public async Task BatchOperationsAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            //----------------------------------------------------------------
            //  Standard Batch
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                string pkValue = "DiagnosticTestPk";
                TransactionalBatch batch = container.CreateTransactionalBatch(new PartitionKey(pkValue));
                BatchCore batchCore = (BatchCore)batch;
                List<PatchOperation> patch = new List<PatchOperation>()
                {
                    PatchOperation.Remove("/cost")
                };

                List<ToDoActivity> createItems = new List<ToDoActivity>();
                for (int i = 0; i < 50; i++)
                {
                    ToDoActivity item = ToDoActivity.CreateRandomToDoActivity(pk: pkValue);
                    createItems.Add(item);
                    batch.CreateItem<ToDoActivity>(item);
                }

                for (int i = 0; i < 20; i++)
                {
                    batch.ReadItem(createItems[i].id);
                    batchCore.PatchItem(createItems[i].id, patch);
                }

                TransactionalBatchRequestOptions requestOptions = null;
                TransactionalBatchResponse response = await batch.ExecuteAsync(requestOptions);

                Assert.IsNotNull(response);
                ITrace trace = ((CosmosTraceDiagnostics)response.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Batch Operation", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            this.ExecuteTestSuite(inputs);
        }

        [TestMethod]
        public async Task BulkOperationsAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            //----------------------------------------------------------------
            //  Standard Bulk
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                string pkValue = "DiagnosticBulkTestPk";
                CosmosClient bulkClient = TestCommon.CreateCosmosClient(builder => builder.WithBulkExecution(true));
                Container bulkContainer = bulkClient.GetContainer(database.Id, container.Id);
                List<Task<ItemResponse<ToDoActivity>>> createItemsTasks = new List<Task<ItemResponse<ToDoActivity>>>();
                for (int i = 0; i < 10; i++)
                {
                    ToDoActivity item = ToDoActivity.CreateRandomToDoActivity(pk: pkValue);
                    createItemsTasks.Add(bulkContainer.CreateItemAsync<ToDoActivity>(item, new PartitionKey(item.id)));
                }

                await Task.WhenAll(createItemsTasks);

                List<ITrace> traces = new List<ITrace>();
                foreach (Task<ItemResponse<ToDoActivity>> createTask in createItemsTasks)
                {
                    ItemResponse<ToDoActivity> itemResponse = await createTask;
                    Assert.IsNotNull(itemResponse);

                    ITrace trace = ((CosmosTraceDiagnostics)itemResponse.Diagnostics).Value;
                    traces.Add(trace);
                }

                endLineNumber = GetLineNumber();

                foreach (ITrace trace in traces)
                {
                    inputs.Add(new Input("Bulk Operation", trace, startLineNumber, endLineNumber));
                }
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Bulk with retry on throttle
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                string errorMessage = "Mock throttle exception" + Guid.NewGuid().ToString();
                Guid exceptionActivityId = Guid.NewGuid();
                // Set a small retry count to reduce test time
                CosmosClient throttleClient = TestCommon.CreateCosmosClient(builder =>
                    builder.WithThrottlingRetryOptions(TimeSpan.FromSeconds(5), 3)
                    .WithBulkExecution(true)
                    .WithTransportClientHandlerFactory(transportClient => new TransportClientWrapper(
                           transportClient,
                           (uri, resourceOperation, request) => TransportClientHelper.ReturnThrottledStoreResponseOnItemOperation(
                                uri,
                                resourceOperation,
                                request,
                                exceptionActivityId,
                                errorMessage)))
                    );

                ItemRequestOptions requestOptions = new ItemRequestOptions();
                Container containerWithThrottleException = throttleClient.GetContainer(
                    database.Id,
                    container.Id);

                ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity();
                ITrace trace = null;
                try
                {
                    ItemResponse<ToDoActivity> createResponse = await containerWithThrottleException.CreateItemAsync<ToDoActivity>(
                      item: testItem,
                      partitionKey: new PartitionKey(testItem.id),
                      requestOptions: requestOptions);
                    Assert.Fail("Should have thrown a throttling exception");
                }
                catch (CosmosException ce) when ((int)ce.StatusCode == (int)Documents.StatusCodes.TooManyRequests)
                {
                    trace = ((CosmosTraceDiagnostics)ce.Diagnostics).Value;
                }
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Bulk Operation With Throttle", trace, startLineNumber, endLineNumber));
            }

            this.ExecuteTestSuite(inputs);
        }

        [TestMethod]
        public async Task MiscellanousAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            //----------------------------------------------------------------
            //  Custom Handler
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                TimeSpan delayTime = TimeSpan.FromSeconds(2);
                RequestHandler requestHandler = new RequestHandlerSleepHelper(delayTime);
                CosmosClient cosmosClient = TestCommon.CreateCosmosClient(builder =>
                    builder.AddCustomHandlers(requestHandler));

                DatabaseResponse databaseResponse = await cosmosClient.CreateDatabaseAsync(Guid.NewGuid().ToString());
                EndToEndTraceWriterBaselineTests.AssertCustomHandlerTime(
                    databaseResponse.Diagnostics.ToString(),
                    requestHandler.FullHandlerName,
                    delayTime);

                ITrace trace = ((CosmosTraceDiagnostics)databaseResponse.Diagnostics).Value;
                await databaseResponse.Database.DeleteAsync();
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Custom Handler", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Non Data Plane
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                RequestOptions requestOptions = new RequestOptions();
                DatabaseResponse databaseResponse = await client.CreateDatabaseAsync(
                    id: Guid.NewGuid().ToString(),
                    requestOptions: requestOptions);
                ITrace trace = ((CosmosTraceDiagnostics)databaseResponse.Diagnostics).Value;
                await databaseResponse.Database.DeleteAsync();
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Custom Handler", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            this.ExecuteTestSuite(inputs);
        }

        [TestMethod]
        public async Task ReadManyAsync()
        {
            List<Input> inputs = new List<Input>();

            int startLineNumber;
            int endLineNumber;

            for (int i = 0; i < 5; i++)
            {
                ToDoActivity item = ToDoActivity.CreateRandomToDoActivity("pk" + i, "id" + i);
                await container.CreateItemAsync(item);
            }

            List<(string, PartitionKey)> itemList = new List<(string, PartitionKey)>();
            for (int i = 0; i < 5; i++)
            {
                itemList.Add(("id" + i, new PartitionKey(i.ToString())));
            }

            //----------------------------------------------------------------
            //  Read Many Stream
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                ITrace trace;
                using (ResponseMessage responseMessage = await container.ReadManyItemsStreamAsync(itemList))
                {
                    trace = responseMessage.Trace;
                }
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Read Many Stream Api", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            //----------------------------------------------------------------
            //  Read Many Typed
            //----------------------------------------------------------------
            {
                startLineNumber = GetLineNumber();
                FeedResponse<ToDoActivity> feedResponse = await container.ReadManyItemsAsync<ToDoActivity>(itemList);
                ITrace trace = ((CosmosTraceDiagnostics)feedResponse.Diagnostics).Value;
                endLineNumber = GetLineNumber();

                inputs.Add(new Input("Read Many Typed Api", trace, startLineNumber, endLineNumber));
            }
            //----------------------------------------------------------------

            this.ExecuteTestSuite(inputs);
        }

        public override Output ExecuteTest(Input input)
        {
            ITrace traceForBaselineTesting = CreateTraceForBaslineTesting(input.Trace, parent: null);

            string text = TraceWriter.TraceToText(traceForBaselineTesting);
            string json = TraceWriter.TraceToJson(traceForBaselineTesting);

            AssertTraceProperites(input.Trace);
            Assert.IsTrue(text.Contains("Client Side Request Stats"), $"All diagnostics should have request stats: {text}");
            Assert.IsTrue(json.Contains("Client Side Request Stats"), $"All diagnostics should have request stats: {json}");
            Assert.IsTrue(text.Contains("Client Configuration"), $"All diagnostics should have Client Configuration: {text}");
            Assert.IsTrue(json.Contains("Client Configuration"), $"All diagnostics should have Client Configuration: {json}");
            
            return new Output(text, JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.Indented));
        }

        private static TraceForBaselineTesting CreateTraceForBaslineTesting(ITrace trace, TraceForBaselineTesting parent)
        {
            TraceForBaselineTesting convertedTrace = new TraceForBaselineTesting(trace.Name, trace.Level, trace.Component, parent);

            foreach (ITrace child in trace.Children)
            {
                TraceForBaselineTesting convertedChild = CreateTraceForBaslineTesting(child, convertedTrace);
                convertedTrace.children.Add(convertedChild);
            }

            foreach (KeyValuePair<string, object> kvp in trace.Data)
            {
                convertedTrace.AddDatum(kvp.Key, kvp.Value);
            }

            return convertedTrace;
        }

        private static void AssertCustomHandlerTime(
            string diagnostics, 
            string handlerName,
            TimeSpan delay)
        {
            JObject jObject = JObject.Parse(diagnostics);
            JObject handlerChild = EndToEndTraceWriterBaselineTests.FindChild(
                handlerName, 
                jObject);
            Assert.IsNotNull(handlerChild);
            JToken delayToken = handlerChild["duration in milliseconds"];
            Assert.IsNotNull(delayToken);
            double itraceDelay = delayToken.ToObject<double>();
            Assert.IsTrue(TimeSpan.FromMilliseconds(itraceDelay) > delay);
        }

        private static JObject FindChild(
            string name,
            JObject jObject)
        {
            if(jObject == null)
            {
                return null;
            }

            JToken nameToken = jObject["name"];
            if(nameToken != null && nameToken.ToString() == name)
            {
                return jObject;
            }

            JArray jArray = jObject["children"]?.ToObject<JArray>();
            if(jArray != null)
            {
                foreach(JObject child in jArray)
                {
                    JObject response = EndToEndTraceWriterBaselineTests.FindChild(name, child);
                    if(response != null)
                    {
                        return response;
                    }
                }
            }

            return null;
        }


        private static void AssertTraceProperites(ITrace trace)
        {
            if (trace.Name == "ReadManyItemsStreamAsync" || 
                trace.Name == "ReadManyItemsAsync")
            {
                return; // skip test for read many as the queries are done in parallel
            }

            if (trace.Name == "Change Feed Estimator Read Next Async")
            {
                return; // Change Feed Estimator issues parallel requests
            }

            if (trace.Children.Count == 0)
            {
                // Base case
                return;
            }

            // Trace stopwatch should be greater than the sum of all children's stop watches
            TimeSpan rootTimeSpan = trace.Duration;
            TimeSpan sumOfChildrenTimeSpan = TimeSpan.Zero;
            foreach (ITrace child in trace.Children)
            {
                sumOfChildrenTimeSpan += child.Duration;
                AssertTraceProperites(child);
            }

            if (rootTimeSpan < sumOfChildrenTimeSpan)
            {
                Assert.Fail();
            }
        }

        private static int GetLineNumber([CallerLineNumber] int lineNumber = 0)
        {
            return lineNumber;
        }

        public sealed class Input : BaselineTestInput
        {
            private static readonly string[] sourceCode = File.ReadAllLines($"Tracing\\{nameof(EndToEndTraceWriterBaselineTests)}.cs");

            internal Input(string description, ITrace trace, int startLineNumber, int endLineNumber)
                : base(description)
            {
                this.Trace = trace ?? throw new ArgumentNullException(nameof(trace));
                this.StartLineNumber = startLineNumber;
                this.EndLineNumber = endLineNumber;
            }

            internal ITrace Trace { get; }

            public int StartLineNumber { get; }

            public int EndLineNumber { get; }

            public override void SerializeAsXml(XmlWriter xmlWriter)
            {
                xmlWriter.WriteElementString(nameof(this.Description), this.Description);
                xmlWriter.WriteStartElement("Setup");
                ArraySegment<string> codeSnippet = new ArraySegment<string>(
                    sourceCode,
                    this.StartLineNumber,
                    this.EndLineNumber - this.StartLineNumber - 1);

                string setup;
                try
                {
                    setup =
                    Environment.NewLine
                    + string
                        .Join(
                            Environment.NewLine,
                            codeSnippet
                                .Select(x => x != string.Empty ? x.Substring("            ".Length) : string.Empty))
                    + Environment.NewLine;
                }
                catch (Exception)
                {
                    throw;
                }
                xmlWriter.WriteCData(setup ?? "asdf");
                xmlWriter.WriteEndElement();
            }
        }

        public sealed class Output : BaselineTestOutput
        {
            public Output(string text, string json)
            {
                this.Text = text ?? throw new ArgumentNullException(nameof(text));
                this.Json = json ?? throw new ArgumentNullException(nameof(json));
            }

            public string Text { get; }

            public string Json { get; }

            public override void SerializeAsXml(XmlWriter xmlWriter)
            {
                xmlWriter.WriteStartElement(nameof(this.Text));
                xmlWriter.WriteCData(this.Text);
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement(nameof(this.Json));
                xmlWriter.WriteCData(this.Json);
                xmlWriter.WriteEndElement();
            }
        }

        private sealed class TraceForBaselineTesting : ITrace
        {
            public readonly Dictionary<string, object> data;
            public readonly List<ITrace> children;

            public TraceForBaselineTesting(
                string name,
                TraceLevel level,
                TraceComponent component,
                TraceForBaselineTesting parent)
            {
                this.Name = name ?? throw new ArgumentNullException(nameof(name));
                this.Level = level;
                this.Component = component;
                this.Parent = parent;
                this.children = new List<ITrace>();
                this.data = new Dictionary<string, object>();
            }

            public string Name { get; }

            public Guid Id => Guid.Empty;

            public DateTime StartTime => DateTime.MinValue;

            public TimeSpan Duration => TimeSpan.Zero;

            public TraceLevel Level { get; }

            public TraceComponent Component { get; }

            public ITrace Parent { get; }

            public IReadOnlyList<ITrace> Children => this.children;

            public IReadOnlyDictionary<string, object> Data => this.data;

            public IReadOnlyList<(string, Uri)> RegionsContacted => new List<(string, Uri)>();

            public void AddDatum(string key, TraceDatum traceDatum)
            {
                this.data[key] = traceDatum;
            }

            public void AddDatum(string key, object value)
            {
                if (key.Contains("CPU"))
                {
                    // Redacted To Not Change The Baselines From Run To Run
                    return;
                }

                this.data[key] = "Redacted To Not Change The Baselines From Run To Run";
            }

            public void Dispose()
            {
            }

            public ITrace StartChild(string name)
            {
                return this.StartChild(name, TraceComponent.Unknown, TraceLevel.Info);
            }

            public ITrace StartChild(string name, TraceComponent component, TraceLevel level)
            {
                TraceForBaselineTesting child = new TraceForBaselineTesting(name, level, component, parent: this);
                this.AddChild(child);
                return child;
            }

            public void AddChild(ITrace trace)
            {
                this.children.Add(trace);
            }

            public static TraceForBaselineTesting GetRootTrace()
            {
                return new TraceForBaselineTesting("Trace For Baseline Testing", TraceLevel.Info, TraceComponent.Unknown, parent: null);
            }

            public void UpdateRegionContacted(TraceDatum traceDatum)
            {
                //NoImplementation
            }

            public void AddOrUpdateDatum(string key, object value)
            {
                if (key.Contains("CPU"))
                {
                    // Redacted To Not Change The Baselines From Run To Run
                    return;
                }

                this.data[key] = "Redacted To Not Change The Baselines From Run To Run";
            }
        }

        private sealed class RequestHandlerSleepHelper : RequestHandler
        {
            TimeSpan timeToSleep;

            public RequestHandlerSleepHelper(TimeSpan timeToSleep)
            {
                this.timeToSleep = timeToSleep;
            }

            public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(this.timeToSleep);
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
