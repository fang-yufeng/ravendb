﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Client.Document;
using Raven.Tests.Notifications;
using Xunit;

namespace FastTests.Client.Subscriptions
{
    public class CriteriaScript: SubscriptionTestBase
    {
        [Fact]
        public async Task BasicCriteriaTest()
        {
            using (var store = await GetDocumentStore().ConfigureAwait(false))
            using (var subscriptionManager = new DocumentSubscriptions(store))
            {
                CreateDocuments(store, 1);

                var lastEtag = store.GetLastWrittenEtag() ?? 0;
                CreateDocuments(store, 5);

                var subscriptionCriteria = new SubscriptionCriteria
                {
                    Collection = "Things",
                    FilterJavaScript = " return this.Name == 'ThingNo1'",
                };
                var subsId = subscriptionManager.Create(subscriptionCriteria, lastEtag);
                var subscription = subscriptionManager.Open<Thing>(subsId, new SubscriptionConnectionOptions()
                {
                    SubscriptionId = subsId
                });
                var list = new List<Thing>();
                subscription.Subscribe(x =>
                {
                    list.Add(x);
                });

                await AsyncSpin(() => list.Count == 4, 60000).ConfigureAwait(false);

                Assert.Equal(1, list.Count);
            }
        }
    }
}