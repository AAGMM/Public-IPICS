﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomMetadataServiceTests : IClassFixture<DicomBlobStorageTestsFixture>
    {
        private readonly IDicomMetadataService _dicomMetadataService;

        public DicomMetadataServiceTests(DicomBlobStorageTestsFixture fixture)
        {
            _dicomMetadataService = fixture.DicomInstanceMetadataStore;
        }

        [Fact]
        public async Task GivenInvalidParameters_WhenAddingDeletingFetchingInstanceMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataService.AddInstanceMetadataAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataService.AddInstanceMetadataAsync(new DicomDataset()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataService.AddInstanceMetadataAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
            }));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataService.AddInstanceMetadataAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
            }));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataService.DeleteInstanceMetadataAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataService.GetInstanceMetadataAsync(null));
        }

        [Fact]
        public async Task GivenAnUnknownDicomInstance_WhenFetchingInstanceMetadata_NotFoundDataStoreExceptionIsThrown()
        {
            var dicomInstanceId = new DicomInstanceIdentifier(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                version: 0);
            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataService.GetInstanceMetadataAsync(dicomInstanceId));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenAnUnknownDicomInstance_WhenDeletingInstanceMetadata_NotFoundDataStoreExceptionIsThrown()
        {
            var dicomInstance = new DicomInstance(
                studyInstanceUID: TestUidGenerator.Generate(),
                seriesInstanceUID: TestUidGenerator.Generate(),
                sopInstanceUID: TestUidGenerator.Generate());
            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataService.DeleteInstanceMetadataAsync(dicomInstance));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenExistingMetadata_WhenAdding_ConflictExceptionIsThrown()
        {
            DicomDataset dicomDataset = CreateValidMetadataDataset();
            var dicomInstance = DicomInstance.Create(dicomDataset);
            var dicomInstanceId = new DicomInstanceIdentifier(
                studyInstanceUid: dicomInstance.StudyInstanceUID,
                seriesInstanceUid: dicomInstance.SeriesInstanceUID,
                sopInstanceUid: dicomInstance.SopInstanceUID,
                version: 0);
            await _dicomMetadataService.AddInstanceMetadataAsync(dicomDataset);
            DicomDataset storedMetadata = await _dicomMetadataService.GetInstanceMetadataAsync(dicomInstanceId);
            Assert.NotNull(storedMetadata);

            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataService.AddInstanceMetadataAsync(dicomDataset));
            Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);

            await _dicomMetadataService.DeleteInstanceMetadataAsync(dicomInstance);

            exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataService.DeleteInstanceMetadataAsync(dicomInstance));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenAddedInstanceMetadata_WhenDeletingAgain_NotFoundExceptionIsThrown()
        {
            DicomDataset dicomDataset = CreateValidMetadataDataset();
            var dicomInstance = DicomInstance.Create(dicomDataset);
            var dicomInstanceId = new DicomInstanceIdentifier(
                studyInstanceUid: dicomInstance.StudyInstanceUID,
                seriesInstanceUid: dicomInstance.SeriesInstanceUID,
                sopInstanceUid: dicomInstance.SopInstanceUID,
                version: 0);

            await _dicomMetadataService.AddInstanceMetadataAsync(dicomDataset);
            DicomDataset storedMetadata = await _dicomMetadataService.GetInstanceMetadataAsync(dicomInstanceId);
            Assert.NotNull(storedMetadata);

            await _dicomMetadataService.DeleteInstanceMetadataAsync(dicomInstance);

            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataService.DeleteInstanceMetadataAsync(dicomInstance));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        private DicomDataset CreateValidMetadataDataset()
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
            };
        }
    }
}
