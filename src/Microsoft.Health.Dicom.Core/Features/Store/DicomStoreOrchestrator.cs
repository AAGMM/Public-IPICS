﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to orchestrate the storing of the DICOM instance entry.
    /// </summary>
    public class DicomStoreOrchestrator : IDicomStoreOrchestrator
    {
        private readonly IDicomFileStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomInstanceMetadataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;

        public DicomStoreOrchestrator(
            IDicomFileStore dicomBlobDataStore,
            IDicomMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore)
        {
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));

            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomIndexDataStore = dicomIndexDataStore;
        }

        /// <inheritdoc />
        public async Task StoreDicomInstanceEntryAsync(
            IDicomInstanceEntry dicomInstanceEntry,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomInstanceEntry, nameof(dicomInstanceEntry));

            DicomDataset dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);

            Stream stream = await dicomInstanceEntry.GetStreamAsync(cancellationToken);

            // If a file with the same name exists, a conflict exception will be thrown.
            await _dicomBlobDataStore.AddAsync(
                dicomDataset.ToDicomInstanceIdentifier(),
                stream,
                cancellationToken: cancellationToken);

            // Strip the DICOM file down to the tags we want to store for metadata.
            dicomDataset.RemoveBulkDataVrs();

            await _dicomInstanceMetadataStore.AddInstanceMetadataAsync(dicomDataset);
            await _dicomIndexDataStore.IndexInstanceAsync(dicomDataset);
        }
    }
}
