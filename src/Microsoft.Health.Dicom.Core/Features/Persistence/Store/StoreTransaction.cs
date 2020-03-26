﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Persistence.Store
{
    public class StoreTransaction : IDisposable
    {
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomMetadataService _dicomInstanceMetadataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private IDictionary<DicomInstance, DicomDataset> _metadataInstances = new Dictionary<DicomInstance, DicomDataset>();
        private bool _disposed;

        public StoreTransaction(
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataService dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore)
        {
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));

            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomIndexDataStore = dicomIndexDataStore;
        }

        public async Task StoreDicomFileAsync(Stream dicomFileStream, DicomFile dicomFile, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomFileStream, nameof(dicomFileStream));
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

            var dicomInstance = DicomInstance.Create(dicomFile.Dataset);
            var blobStorageName = GetBlobStorageName(dicomInstance);

            // If a file with the same name exists, a conflict exception will be thrown.
            dicomFileStream.Seek(0, SeekOrigin.Begin);
            await _dicomBlobDataStore.AddFileAsStreamAsync(blobStorageName, dicomFileStream, cancellationToken: cancellationToken);

            // Strip the DICOM file down to the tags we want to store for metadata.
            DicomMetadata.RemoveBulkDataVRs(dicomFile.Dataset);
            _metadataInstances.Add(dicomInstance, dicomFile.Dataset);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task AbortAsync()
        {
            await Task.WhenAll(_metadataInstances.Keys.Select(x => _dicomBlobDataStore.DeleteFileIfExistsAsync(GetBlobStorageName(x))));
            _metadataInstances.Clear();
        }

        public async Task CommitAsync()
        {
            if (_metadataInstances.Count == 0)
            {
                return;
            }

            // Commit per series grouping
            // TODO: Synchronize the stores
            foreach (IGrouping<string, KeyValuePair<DicomInstance, DicomDataset>> grouping in _metadataInstances.GroupBy(x => x.Key.StudyInstanceUID + x.Key.SeriesInstanceUID))
            {
                DicomDataset[] seriesArray = grouping.Select(x => x.Value).ToArray();
                foreach (DicomDataset metadataInstance in seriesArray)
                {
                    await _dicomInstanceMetadataStore.AddInstanceMetadataAsync(metadataInstance);
                    await _dicomIndexDataStore.IndexInstanceAsync(metadataInstance);
                }
            }

            _metadataInstances.Clear();
        }

        // TODO delete this when DicomInstance is deleted
        internal static string GetBlobStorageName(DicomInstance dicomInstance)
           => $"{dicomInstance.StudyInstanceUID}/{dicomInstance.SeriesInstanceUID}/{dicomInstance.SopInstanceUID}";

        internal static string GetBlobStorageName(DicomInstanceIdentifier dicomInstance)
            => $"{dicomInstance.StudyInstanceUid}/{dicomInstance.SeriesInstanceUid}/{dicomInstance.SopInstanceUid}";

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Task.WaitAll(AbortAsync());
            }

            _disposed = true;
        }
    }
}
