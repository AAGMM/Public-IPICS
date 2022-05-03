﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Api.Features.Routing;

internal class KnownRouteNames
{
    internal const string VersionedRetrieveStudy = nameof(VersionedRetrieveStudy);
    internal const string VersionedPartitionRetrieveStudy = nameof(VersionedPartitionRetrieveStudy);
    internal const string RetrieveStudy = nameof(RetrieveStudy);
    internal const string PartitionRetrieveStudy = nameof(PartitionRetrieveStudy);

    internal const string VersionedPartitionRetrieveSeries = nameof(VersionedPartitionRetrieveSeries);
    internal const string PartitionRetrieveSeries = nameof(PartitionRetrieveSeries);
    internal const string VersionedRetrieveSeries = nameof(VersionedRetrieveSeries);
    internal const string RetrieveSeries = nameof(RetrieveSeries);

    internal const string VersionedPartitionRetrieveInstance = nameof(VersionedPartitionRetrieveInstance);
    internal const string PartitionRetrieveInstance = nameof(PartitionRetrieveInstance);
    internal const string VersionedRetrieveInstance = nameof(VersionedRetrieveInstance);
    internal const string RetrieveInstance = nameof(RetrieveInstance);

    internal const string VersionedPartitionRetrieveFrame = nameof(VersionedPartitionRetrieveFrame);
    internal const string PartitionRetrieveFrame = nameof(PartitionRetrieveFrame);
    internal const string VersionedRetrieveFrame = nameof(VersionedRetrieveFrame);
    internal const string RetrieveFrame = nameof(RetrieveFrame);

    internal const string VersionedOperationStatus = nameof(VersionedOperationStatus);
    internal const string OperationStatus = nameof(OperationStatus);

    internal const string VersionedGetExtendedQueryTag = nameof(VersionedGetExtendedQueryTag);
    internal const string GetExtendedQueryTag = nameof(GetExtendedQueryTag);

    internal const string VersionedGetExtendedQueryTagErrors = nameof(VersionedGetExtendedQueryTagErrors);
    internal const string GetExtendedQueryTagErrors = nameof(GetExtendedQueryTagErrors);

    internal const string VersionedPartitionStoreInstance = nameof(VersionedPartitionStoreInstance);
    internal const string PartitionStoreInstance = nameof(PartitionStoreInstance);
    internal const string VersionedStoreInstance = nameof(VersionedStoreInstance);
    internal const string StoreInstance = nameof(StoreInstance);

    internal const string VersionedPartitionStoreInstancesInStudy = nameof(VersionedPartitionStoreInstancesInStudy);
    internal const string PartitionStoreInstancesInStudy = nameof(PartitionStoreInstancesInStudy);
    internal const string VersionedStoreInstancesInStudy = nameof(VersionedStoreInstancesInStudy);
    internal const string StoreInstancesInStudy = nameof(StoreInstancesInStudy);

    internal const string VersionedPartitionAddWorkitemInstance = nameof(VersionedPartitionAddWorkitemInstance);
    internal const string PartitionedAddWorkitemInstance = nameof(PartitionedAddWorkitemInstance);
    internal const string VersionedAddWorkitemInstance = nameof(VersionedAddWorkitemInstance);
    internal const string AddWorkitemInstance = nameof(AddWorkitemInstance);

    internal const string VersionedPartitionRetrieveWorkitemInstance = nameof(VersionedPartitionRetrieveWorkitemInstance);
    internal const string PartitionedRetrieveWorkitemInstance = nameof(PartitionedRetrieveWorkitemInstance);
    internal const string VersionedRetrieveWorkitemInstance = nameof(VersionedRetrieveWorkitemInstance);
    internal const string RetrieveWorkitemInstance = nameof(RetrieveWorkitemInstance);

    internal const string VersionedPartitionChangeStateWorkitemInstance = nameof(VersionedPartitionChangeStateWorkitemInstance);
    internal const string PartitionedChangeStateWorkitemInstance = nameof(PartitionedChangeStateWorkitemInstance);
    internal const string VersionedChangeStateWorkitemInstance = nameof(VersionedChangeStateWorkitemInstance);
    internal const string ChangeStateWorkitemInstance = nameof(ChangeStateWorkitemInstance);

    internal const string VersionedPartitionCancelWorkitemInstance = nameof(VersionedPartitionCancelWorkitemInstance);
    internal const string PartitionedCancelWorkitemInstance = nameof(PartitionedCancelWorkitemInstance);
    internal const string VersionedCancelWorkitemInstance = nameof(VersionedCancelWorkitemInstance);
    internal const string CancelWorkitemInstance = nameof(CancelWorkitemInstance);
}
