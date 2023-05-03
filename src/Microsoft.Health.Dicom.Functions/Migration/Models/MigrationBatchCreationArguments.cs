// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Migration.Models;

/// <summary>
/// Represents the options for creating batches for blob migration.
/// </summary>
public sealed class MigrationBatchCreationArguments
{
    /// <summary>
    /// Gets or sets the optional inclusive maximum watermark.
    /// </summary>
    public long? MaxWatermark { get; }

    /// <summary>
    /// Gets or sets the number of DICOM instances processed by a single activity.
    /// </summary>
    public int BatchSize { get; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent batches processed at a given time.
    /// </summary>
    public int MaxParallelBatches { get; }

    /// <summary>
    /// Gets or sets the start filter stamp
    /// </summary>
    public DateTimeOffset StartFilterTimeStamp { get; set; }

    /// <summary>
    /// Gets or sets the end filter stamp
    /// </summary>
    public DateTimeOffset EndFilterTimeStamp { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationBatchCreationArguments"/> class with the specified values.
    /// </summary>
    /// <param name="maxWatermark">The optional inclusive maximum watermark.</param>
    /// <param name="batchSize">The number of DICOM instances processed by a single activity.</param>
    /// <param name="maxParallelBatches">The maximum number of concurrent batches processed at a given time.</param>
    /// <param name="startTimeStamp">Start filter stamp</param>
    /// <param name="endTimeStamp">End filter stamp</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="batchSize"/> is less than <c>1</c>.</para>
    /// <para>-or-</para>
    /// <para><paramref name="maxParallelBatches"/> is less than <c>1</c>.</para>
    /// </exception>
    public MigrationBatchCreationArguments(long? maxWatermark, int batchSize, int maxParallelBatches, DateTimeOffset startTimeStamp, DateTimeOffset endTimeStamp)
    {
        EnsureArg.IsGte(batchSize, 1, nameof(batchSize));
        EnsureArg.IsGte(maxParallelBatches, 1, nameof(maxParallelBatches));

        BatchSize = batchSize;
        MaxParallelBatches = maxParallelBatches;
        MaxWatermark = maxWatermark;
        StartFilterTimeStamp = startTimeStamp;
        EndFilterTimeStamp = endTimeStamp;
    }
}
