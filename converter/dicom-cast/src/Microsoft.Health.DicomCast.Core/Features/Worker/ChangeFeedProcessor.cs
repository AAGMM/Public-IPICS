// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.State;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Polly.Timeout;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker;

/// <summary>
/// Provides functionality to process the change feed.
/// </summary>
public class ChangeFeedProcessor : IChangeFeedProcessor
{
    internal const int DefaultLimit = 10;
    private static readonly Func<ILogger, IDisposable> LogProcessingDelegate = LoggerMessage.DefineScope("Processing change feed.");

    private readonly IChangeFeedRetrieveService _changeFeedRetrieveService;
    private readonly IFhirTransactionPipeline _fhirTransactionPipeline;
    private readonly ISyncStateService _syncStateService;
    private readonly IExceptionStore _exceptionStore;
    private readonly ILogger<ChangeFeedProcessor> _logger;

    public ChangeFeedProcessor(
        IChangeFeedRetrieveService changeFeedRetrieveService,
        IFhirTransactionPipeline fhirTransactionPipeline,
        ISyncStateService syncStateService,
        IExceptionStore exceptionStore,
        ILogger<ChangeFeedProcessor> logger)
    {
        _changeFeedRetrieveService = EnsureArg.IsNotNull(changeFeedRetrieveService, nameof(changeFeedRetrieveService));
        _fhirTransactionPipeline = EnsureArg.IsNotNull(fhirTransactionPipeline, nameof(fhirTransactionPipeline));
        _syncStateService = EnsureArg.IsNotNull(syncStateService, nameof(syncStateService));
        _exceptionStore = EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc/>
    public async Task ProcessAsync(TimeSpan pollIntervalDuringCatchup, CancellationToken cancellationToken)
    {
        using (LogProcessingDelegate(_logger))
        {
            SyncState state = await _syncStateService.GetSyncStateAsync(cancellationToken);

            while (true)
            {
                // Retrieve the change feed for any changes after checking the sequence number of the latest event
                long latest = await _changeFeedRetrieveService.RetrieveLatestSequenceAsync(cancellationToken);
                IReadOnlyList<ChangeFeedEntry> changeFeedEntries = await _changeFeedRetrieveService.RetrieveChangeFeedAsync(
                    state.SyncedSequence,
                    DefaultLimit,
                    cancellationToken);

                // If there are no events because nothing available, then skip processing for now
                if (changeFeedEntries.Count == 0 && latest == state.SyncedSequence)
                {
                    _logger.LogInformation("No new DICOM events to process.");
                    return;
                }

                // Otherwise, process any new entries and increment the sequence
                long maxSequence = changeFeedEntries.Count > 0 ? changeFeedEntries[^1].Sequence : state.SyncedSequence + DefaultLimit;
                await ProcessChangeFeedEntriesAsync(changeFeedEntries, cancellationToken);

                var newSyncState = new SyncState(maxSequence, Clock.UtcNow);
                await _syncStateService.UpdateSyncStateAsync(newSyncState, cancellationToken);
                state = newSyncState;

                _logger.LogInformation("Processed DICOM events sequenced {SequenceId}-{MaxSequence}.", state.SyncedSequence + 1, maxSequence);
                await Task.Delay(pollIntervalDuringCatchup, cancellationToken);
            }
        }
    }

    private async Task ProcessChangeFeedEntriesAsync(IEnumerable<ChangeFeedEntry> changeFeedEntries, CancellationToken cancellationToken)
    {
        // Process each change feed as a FHIR transaction.
        foreach (ChangeFeedEntry changeFeedEntry in changeFeedEntries)
        {
            try
            {
                if (!(changeFeedEntry.Action == ChangeFeedAction.Create && changeFeedEntry.State == ChangeFeedState.Deleted))
                {
                    await _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, cancellationToken);
                    _logger.LogInformation("Successfully processed DICOM event with SequenceID: {SequenceId}", changeFeedEntry.Sequence);
                }
                else
                {
                    _logger.LogInformation("Skip DICOM event with SequenceId {SequenceId} due to deletion before processing creation.", changeFeedEntry.Sequence);
                }
            }
            catch (Exception ex) when (ex is FhirNonRetryableException or DicomTagException or TimeoutRejectedException)
            {
                string studyInstanceUid = changeFeedEntry.StudyInstanceUid;
                string seriesInstanceUid = changeFeedEntry.SeriesInstanceUid;
                string sopInstanceUid = changeFeedEntry.SopInstanceUid;
                long changeFeedSequence = changeFeedEntry.Sequence;

                ErrorType errorType = ErrorType.FhirError;

                if (ex is DicomTagException)
                {
                    errorType = ErrorType.DicomError;
                }
                else if (ex is TimeoutRejectedException)
                {
                    errorType = ErrorType.TransientFailure;
                }

                await _exceptionStore.WriteExceptionAsync(changeFeedEntry, ex, errorType, cancellationToken);

                _logger.LogError(
                    "Failed to process DICOM event with SequenceID: {SequenceId}, StudyUid: {StudyInstanceUid}, SeriesUid: {SeriesInstanceUid}, instanceUid: {SopInstanceUid}  and will not be retried further. Continuing to next event.",
                    changeFeedSequence,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid);
            }
        }
    }
}
