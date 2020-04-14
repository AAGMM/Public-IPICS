﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public interface IDicomIndexDataStore
    {
        Task IndexInstanceAsync(DicomDataset instance, CancellationToken cancellationToken = default);

        Task DeleteStudyIndexAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

        Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default);

        Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default);
    }
}
