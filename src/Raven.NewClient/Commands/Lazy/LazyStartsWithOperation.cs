// -----------------------------------------------------------------------
//  <copyright file="LazyStartsWithOperation.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Raven.NewClient.Client.Data;
using Raven.NewClient.Client.Document;
using Raven.NewClient.Client.Data.Queries;
using Raven.NewClient.Client.Document.Batches;
using Raven.NewClient.Client.Json;
using Sparrow.Json;

namespace Raven.NewClient.Client.Commands.Lazy
{
    public class LazyStartsWithOperation<T> : ILazyOperation
    {
        private readonly string _keyPrefix;

        private readonly string _matches;

        private readonly string _exclude;

        private readonly int _start;

        private readonly int _pageSize;

        private readonly InMemoryDocumentSessionOperations _sessionOperations;

        private readonly RavenPagingInformation _pagingInformation;

        private readonly string _skipAfter;

        public LazyStartsWithOperation(string keyPrefix, string matches, string exclude, int start, int pageSize, InMemoryDocumentSessionOperations sessionOperations, RavenPagingInformation pagingInformation, string skipAfter)
        {
            _keyPrefix = keyPrefix;
            _matches = matches;
            _exclude = exclude;
            _start = start;
            _pageSize = pageSize;
            _sessionOperations = sessionOperations;
            _pagingInformation = pagingInformation;
            _skipAfter = skipAfter;
        }

        public GetRequest CreateRequest()
        {
            var actualStart = _start;

            var nextPage = _pagingInformation != null && _pagingInformation.IsForPreviousPage(_start, _pageSize);
            if (nextPage)
                actualStart = _pagingInformation.NextPageStart;

            return new GetRequest
            {
                Url = "/docs",
                Query = "?" +
                    string.Format(
                        "startsWith={0}&matches={3}&exclude={4}&start={1}&pageSize={2}&next-page={5}&skipAfter={6}",
                        Uri.EscapeDataString(_keyPrefix),
                        actualStart,
                        _pageSize,
                        Uri.EscapeDataString(_matches ?? ""),
                        Uri.EscapeDataString(_exclude ?? ""),
                        nextPage ? "true" : "false",
                        _skipAfter)
            };
        }

        public object Result { get; set; }

        public QueryResult QueryResult { get; set; }

        public bool RequiresRetry { get; set; }

        public void HandleResponse(GetResponse response)
        {
            var getDocumentResult = JsonDeserializationClient.GetDocumentResult((BlittableJsonReaderObject)response.Result);

            _pagingInformation?.Fill(_start, _pageSize, getDocumentResult.NextPageStart);

            var finalResults = new List<T>();
            foreach (BlittableJsonReaderObject document in getDocumentResult.Results)
            {
                var newDocumentInfo = DocumentInfo.GetNewDocumentInfo(document);
                _sessionOperations.DocumentsById[newDocumentInfo.Id] = newDocumentInfo;
                if (newDocumentInfo.Id == null)
                {
                    finalResults.Add(default(T));
                    continue;
                }
                if (_sessionOperations.IsDeleted(newDocumentInfo.Id))
                {
                    finalResults.Add(default(T));
                    continue;
                }
                DocumentInfo doc;
                if (_sessionOperations.DocumentsById.TryGetValue(newDocumentInfo.Id, out doc))
                {
                    finalResults.Add(_sessionOperations.TrackEntity<T>(doc));
                    continue;
                }

                finalResults.Add(default(T));
            }
            Result = finalResults.ToArray();
        }
    }
}
