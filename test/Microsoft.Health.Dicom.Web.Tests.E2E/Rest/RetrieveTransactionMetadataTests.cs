﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class RetrieveTransactionMetadataTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;

        public RetrieveTransactionMetadataTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(" ")]
        [InlineData("345%^&")]
        public async Task GivenARequestWithInvalidIdentifier_WhenRetrievingStudyMetadata_TheServerShouldReturnBadRequest(string invalidIdentifier)
        {
            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.GetStudyMetadataAsync(invalidIdentifier);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("aaaa-bbbb", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb", " ")]
        [InlineData("aaaa-bbbb", "345%^&")]
        [InlineData("aaaa-bbbb", "aaaa-bbbb")]
        public async Task GivenARequestWithInvalidIdentifier_WhenRetrievingSeriesMetadata_TheServerShouldReturnBadRequest(string studyInstanceUID, string seriesInstanceUID)
        {
            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.GetSeriesMetadataAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "345%^&")]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "aaaa-bbbb2")]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "aaaa-bbbb1")]
        public async Task GivenARequestWithInvalidIdentifier_WhenRetrievingInstanceMetadata_TheServerShouldReturnBadRequest(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            HttpResult<IReadOnlyList<DicomDataset>> response = await _client.GetInstanceMetadataAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("application/data")]
        [InlineData("application/json")]
        public async Task GivenAnIncorrectAcceptHeader_WhenRetrievingResource_NotAcceptableIsReturned(string acceptHeader)
        {
            // Study
            await RetrieveTransactionResourceTests.ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveStudyMetadataUriFormat, Guid.NewGuid().ToString()),
                acceptHeader);

            // Series
            await RetrieveTransactionResourceTests.ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveSeriesMetadataUriFormat, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                acceptHeader);

            // Instance
            await RetrieveTransactionResourceTests.ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveInstanceMetadataUriFormat, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                acceptHeader);
        }

        [Fact]
        public async Task GivenInvalidInstanceIdentifer_WhenRetrievingInstanceSeriesStudyMetadata_NotFoundStatusCodeReturned()
        {
            DicomDataset storedInstance = await PostDicomFileAsync();
            var dicomInstance = DicomDatasetIdentifier.Create(storedInstance);

            HttpResult<IReadOnlyList<DicomDataset>> metadata = await _client.GetInstanceMetadataAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, TestUidGenerator.Generate());
            Assert.Equal(HttpStatusCode.NotFound, metadata.StatusCode);

            metadata = await _client.GetSeriesMetadataAsync(dicomInstance.StudyInstanceUid, TestUidGenerator.Generate());
            Assert.Equal(HttpStatusCode.NotFound, metadata.StatusCode);

            metadata = await _client.GetStudyMetadataAsync(TestUidGenerator.Generate());
            Assert.Equal(HttpStatusCode.NotFound, metadata.StatusCode);
        }

        [Fact]
        public async Task GivenStoredDicomFile_WhenRetrievingMetadata_MetadataIsRetrievedCorrectly()
        {
            DicomDataset storedInstance = await PostDicomFileAsync(new DicomDataset()
            {
                { DicomTag.SeriesDescription, "A test series" },
                { DicomTag.PixelData, new byte[] { 1, 2, 3 } },
                new DicomSequence(DicomTag.RegistrationSequence, new DicomDataset()
                {
                    { DicomTag.PatientName, "Test^Patient" },
                    { DicomTag.PixelData, new byte[] { 1, 2, 3 } },
                }),
                { DicomTag.StudyDate, DateTime.UtcNow },
                { new DicomTag(0007, 0008), "Private Tag" },
            });
            var dicomInstance = DicomDatasetIdentifier.Create(storedInstance);

            HttpResult<IReadOnlyList<DicomDataset>> metadata = await _client.GetStudyMetadataAsync(dicomInstance.StudyInstanceUid);
            Assert.Single(metadata.Value);
            ValidateResponseMetadataDataset(storedInstance, metadata.Value.Single());

            metadata = await _client.GetSeriesMetadataAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid);
            Assert.Single(metadata.Value);
            ValidateResponseMetadataDataset(storedInstance, metadata.Value.Single());

            metadata = await _client.GetInstanceMetadataAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
            Assert.Single(metadata.Value);
            ValidateResponseMetadataDataset(storedInstance, metadata.Value.Single());
        }

        private static void ValidateResponseMetadataDataset(DicomDataset storedDataset, DicomDataset retrievedDataset)
        {
            // Trim the stored dataset to the expected items in the repsonse metadata dataset (remove non-supported value representations).
            DicomDataset expectedDataset = storedDataset.Clone();
            expectedDataset.RemoveBulkDataVrs();

            // Compare result datasets by serializing.
            var jsonDicomConverter = new JsonDicomConverter();
            Assert.Equal(
                JsonConvert.SerializeObject(expectedDataset, jsonDicomConverter),
                JsonConvert.SerializeObject(retrievedDataset, jsonDicomConverter));
            Assert.Equal(expectedDataset.Count(), retrievedDataset.Count());
        }

        private async Task<DicomDataset> PostDicomFileAsync(DicomDataset metadataItems = null)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile();

            if (metadataItems != null)
            {
                dicomFile1.Dataset.AddOrUpdate(metadataItems);
            }

            HttpResult<DicomDataset> response = await _client.PostAsync(new[] { dicomFile1 });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            DicomSequence successSequence = response.Value.GetSequence(DicomTag.ReferencedSOPSequence);
            ValidationHelpers.ValidateSuccessSequence(successSequence, dicomFile1.Dataset);

            return dicomFile1.Dataset;
        }
    }
}
