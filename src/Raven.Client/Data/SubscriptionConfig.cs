// -----------------------------------------------------------------------
//  <copyright file="SubscriptionDocument.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System;

namespace Raven.Abstractions.Data
{
    public class SubscriptionConfig
    {
        public long SubscriptionId { get; set; }
        public SubscriptionCriteria Criteria { get; set; }
        public long? AckEtag { get; set; }
        public string TimeOfSendingLastBatch { get; set; }
        public string TimeOfLastClientActivity { get; set; }
    }
}