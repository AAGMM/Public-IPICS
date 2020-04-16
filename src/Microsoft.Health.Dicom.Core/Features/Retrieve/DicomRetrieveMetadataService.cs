﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class DicomRetrieveMetadataService : IDicomRetrieveMetadataService
    {
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly ILogger<DicomRetrieveMetadataService> _logger;

        public DicomRetrieveMetadataService(
            IDicomInstanceStore dicomInstanceStore,
            IDicomMetadataStore dicomMetadataStore,
            ILogger<DicomRetrieveMetadataService> logger)
        {
            EnsureArg.IsNotNull(dicomInstanceStore, nameof(dicomInstanceStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomInstanceStore = dicomInstanceStore;
            _dicomMetadataStore = dicomMetadataStore;
            _logger = logger;
        }

        public async Task<DicomRetrieveMetadataResponse> RetrieveStudyInstanceMetadataAsync(string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            IEnumerable<DicomInstanceIdentifier> retrieveInstances = await _dicomInstanceStore.GetInstancesToRetrieve(
                ResourceType.Study,
                studyInstanceUid,
                seriesInstanceUid: null,
                sopInstanceUid: null,
                cancellationToken);

            return await RetrieveMetadata(retrieveInstances, cancellationToken);
        }

        public async Task<DicomRetrieveMetadataResponse> RetrieveSeriesInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default)
        {
            IEnumerable<DicomInstanceIdentifier> retrieveInstances = await _dicomInstanceStore.GetInstancesToRetrieve(
                 ResourceType.Series,
                 studyInstanceUid,
                 seriesInstanceUid,
                 sopInstanceUid: null,
                 cancellationToken);

            return await RetrieveMetadata(retrieveInstances, cancellationToken);
        }

        public async Task<DicomRetrieveMetadataResponse> RetrieveSopInstanceMetadataAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            IEnumerable<DicomInstanceIdentifier> retrieveInstances = await _dicomInstanceStore.GetInstancesToRetrieve(
                 ResourceType.Instance,
                 studyInstanceUid,
                 seriesInstanceUid,
                 sopInstanceUid,
                 cancellationToken);

            return await RetrieveMetadata(retrieveInstances, cancellationToken);
        }

        private async Task<DicomRetrieveMetadataResponse> RetrieveMetadata(IEnumerable<DicomInstanceIdentifier> retrieveInstances, CancellationToken cancellationToken)
        {
            var dataset = new List<DicomDataset>();

            foreach (var id in retrieveInstances)
            {
                try
                {
                    DicomDataset ds = await _dicomMetadataStore.GetInstanceMetadataAsync(id, cancellationToken);
                    dataset.Add(ds);
                }
                catch (DicomDataStoreException e)
                {
                    if (e.StatusCode == (int)HttpStatusCode.NotFound)
                    {
                        _logger.LogError($"Unable to retrieve metadata for the specified SopInstanceUID: {id}");
                        throw new DicomInstanceNotFoundException();
                    }

                    throw;
                }
            }

            return new DicomRetrieveMetadataResponse(HttpStatusCode.OK, dataset);
        }
    }
}
