﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public interface IWorkitemStore
    {
        Task<long> AddWorkitemAsync(int partitionKey, WorkitemDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default);
    }
}
