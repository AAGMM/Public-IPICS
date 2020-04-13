﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public class DicomStoreHandler : IRequestHandler<DicomStoreRequest, DicomStoreResponse>
    {
        private readonly IDicomInstanceEntryReaderManager _dicomInstanceEntryReaderManager;
        private readonly IDicomStoreService _dicomStoreService;

        public DicomStoreHandler(
            IDicomInstanceEntryReaderManager dicomInstanceEntryReaderManager,
            IDicomStoreService dicomStoreService)
        {
            EnsureArg.IsNotNull(dicomInstanceEntryReaderManager, nameof(dicomInstanceEntryReaderManager));
            EnsureArg.IsNotNull(dicomStoreService, nameof(dicomStoreService));

            _dicomInstanceEntryReaderManager = dicomInstanceEntryReaderManager;
            _dicomStoreService = dicomStoreService;
        }

        /// <inheritdoc />
        public async Task<DicomStoreResponse> Handle(
            DicomStoreRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            DicomStoreRequestValidator.ValidateRequest(message);

            // Find a reader that can parse the request body.
            IDicomInstanceEntryReader dicomInstanceEntryReader = _dicomInstanceEntryReaderManager.FindReader(message.RequestContentType);

            if (dicomInstanceEntryReader == null)
            {
                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, message.RequestContentType));
            }

            // Read list of entries.
            IReadOnlyList<IDicomInstanceEntry> dicomInstanceEntries = await dicomInstanceEntryReader.ReadAsync(
                    message.RequestContentType,
                    message.RequestBody,
                    cancellationToken);

            // Process list of entries.
            return await _dicomStoreService.ProcessAsync(dicomInstanceEntries, message.StudyInstanceUid, cancellationToken);
        }
    }
}
