﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;

internal class SqlExtendedQueryTagStoreV1 : ISqlExtendedQueryTagStore
{
    public virtual SchemaVersion Version => SchemaVersion.V1;

    public virtual Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AddExtendedQueryTagsAsync(
        IReadOnlyCollection<AddExtendedQueryTagEntry> extendedQueryTagEntries,
        int maxAllowedCount,
        bool ready = false,
        CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<ExtendedQueryTagStoreJoinEntry> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<IReadOnlyList<ExtendedQueryTagStoreJoinEntry>> GetExtendedQueryTagsAsync(int limit, int offset, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task DeleteExtendedQueryTagAsync(string tagPath, string vr, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<IReadOnlyList<ExtendedQueryTagStoreJoinEntry>> GetExtendedQueryTagsAsync(IReadOnlyCollection<int> queryTagKeys, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AssignReindexingOperationAsync(IReadOnlyCollection<int> queryTagKeys, Guid operationId, bool returnIfCompleted = false, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    public virtual Task<IReadOnlyList<int>> CompleteReindexingAsync(IReadOnlyCollection<int> queryTagKeys, CancellationToken cancellationToken = default)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }

    ///<inheritdoc/>
    public virtual Task<ExtendedQueryTagStoreJoinEntry> UpdateQueryStatusAsync(string tagPath, QueryStatus queryStatus, CancellationToken cancellationToken)
    {
        throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
    }
}
