// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed;

public interface IChangeFeedStore
{
    Task<ChangeFeedEntry> GetChangeFeedLatestAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChangeFeedEntry>> GetChangeFeedAsync(DateTimeOffsetRange range, long offset, int limit, CancellationToken cancellationToken = default);
}
