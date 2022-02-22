﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Telemetry.Diagnostics
{
    using System;
    using System.Net;
    using Documents;

    internal class CosmosInstrumentationNoOp : ICosmosInstrumentation
    {
        public void MarkFailed(Exception ex)
        {
            // NoOp
        }

        public void AddAttributesToScope()
        {
            // NoOp
        }

        public void Record(Uri accountName, string userAgent, ConnectionMode connectionMode)
        {
            // NoOp
        }

        public void Record(CosmosDiagnostics diagnostics)
        {
            // NoOp
        }

        public void Dispose()
        {
            // NoOp
        }

        void ICosmosInstrumentation.Record(double? requestCharge, string operationType, HttpStatusCode? statusCode, string databaseId, string containerId, string queryText, string subStatusCode, string pageSize)
        {
            // NoOp
        }

        void ICosmosInstrumentation.RecordWithException(double? requestCharge, string operationType, HttpStatusCode? statusCode, string databaseId, string containerId, Exception exception, string queryText, string subStatusCode, string pageSize)
        {
            // NoOp
        }
    }
}
