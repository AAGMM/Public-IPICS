// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Migration;

/// <summary>
/// Represents the options for a "migration" function.
/// </summary>
public class MigrationFilesOptions
{
    internal const string SectionName = "MigratingFiles";

    /// <summary>
    /// Gets or sets the number of DICOM instances processed by a single activity.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of threads available for each batch.
    /// </summary>
    [Range(-1, int.MaxValue)]
    public int MaxParallelThreads { get; set; } = -1;

    /// <summary>
    /// Gets or sets the maximum number of concurrent batches processed at a given time.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxParallelBatches { get; set; } = 10;

    /// <summary>
    /// Gets or sets the <see cref="ActivityRetryOptions"/> for migration activities.
    /// </summary>
    public ActivityRetryOptions RetryOptions { get; set; }
}
