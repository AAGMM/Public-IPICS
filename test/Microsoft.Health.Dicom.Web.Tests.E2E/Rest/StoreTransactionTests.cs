﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class StoreTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public StoreTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        [Fact]
        public async Task GivenRandomContent_WhenStoring_TheServerShouldReturnConflict()
        {
            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
            {
                HttpResult<DicomDataset> response = await _client.PostAsync(new[] { stream });
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            }
        }

        [Fact]
        public async Task GivenARequestWithInvalidStudyInstanceUID_WhenStoring_TheServerShouldReturnBadRequest()
        {
            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
            {
                HttpResult<DicomDataset> response = await _client.PostAsync(new[] { stream }, studyInstanceUID: new string('b', 65));
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("application/data")]
        [InlineData("application/dicom")]
        public async void GivenAnIncorrectAcceptHeader_WhenStoring_TheServerShouldReturnNotAcceptable(string acceptHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, acceptHeader);

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenAnNonMultipartRequest_WhenStoring_TheServerShouldReturnUnsupportedMediaType()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("form");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));
            request.Content = multiContent;

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenAMultipartRequestWithNoContent_WhenStoring_TheServerShouldReturnNoContent()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            request.Content = multiContent;

            HttpResult<DicomDataset> response = await _client.PostMultipartContentAsync(multiContent, "studies");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async void GivenAMultipartRequestWithEmptyContent_WhenStoring_TheServerShouldReturnConflict()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            request.Content = multiContent;

            HttpResult<DicomDataset> response = await _client.PostMultipartContentAsync(multiContent, "studies");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async void GivenAMultipartRequestWithAnInvalidMultipartSection_WhenStoring_TheServerShouldReturnAccepted()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            string studyInstanceUID = TestUidGenerator.Generate();
            try
            {
                DicomFile validFile = Samples.CreateRandomDicomFile(studyInstanceUID);

                await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
                {
                    await validFile.SaveAsync(stream);

                    var validByteContent = new ByteArrayContent(stream.ToArray());
                    validByteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
                    multiContent.Add(validByteContent);
                }

                request.Content = multiContent;

                HttpResult<DicomDataset> response = await _client.PostMultipartContentAsync(multiContent, "studies");
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
                ValidationHelpers.ValidateSuccessSequence(response.Value.GetSequence(DicomTag.ReferencedSOPSequence), validFile.Dataset);
            }
            finally
            {
                await _client.DeleteAsync(studyInstanceUID);
            }
        }

        [Fact]
        public async void GivenAllDifferentStudyInstanceUIDs_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnConflict()
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile();
            DicomFile dicomFile2 = Samples.CreateRandomDicomFile();

            var studyInstanceUID = TestUidGenerator.Generate();
            HttpResult<DicomDataset> response = await _client.PostAsync(
                new[] { dicomFile1, dicomFile2 }, studyInstanceUID);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(response.Value);
            Assert.True(response.Value.Count() == 2);

            Assert.EndsWith($"studies/{studyInstanceUID}", response.Value.GetSingleValue<string>(DicomTag.RetrieveURL));

            ValidationHelpers.ValidateFailureSequence(
                response.Value.GetSequence(DicomTag.FailedSOPSequence),
                StoreFailureCodes.MismatchStudyInstanceUIDFailureCode,
                dicomFile1.Dataset,
                dicomFile2.Dataset);
        }

        [Fact]
        public async void GivenOneDifferentStudyInstanceUID_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnAccepted()
        {
            var studyInstanceUID1 = TestUidGenerator.Generate();
            var studyInstanceUID2 = TestUidGenerator.Generate();

            try
            {
                DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID1);
                DicomFile dicomFile2 = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID2);

                HttpResult<DicomDataset> response = await _client.PostAsync(
                    new[] { dicomFile1, dicomFile2 }, studyInstanceUID1);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
                Assert.NotNull(response.Value);
                Assert.True(response.Value.Count() == 3);

                Assert.EndsWith($"studies/{studyInstanceUID1}", response.Value.GetSingleValue<string>(DicomTag.RetrieveURL));

                ValidationHelpers.ValidateSuccessSequence(response.Value.GetSequence(DicomTag.ReferencedSOPSequence), dicomFile1.Dataset);
                ValidationHelpers.ValidateFailureSequence(
                    response.Value.GetSequence(DicomTag.FailedSOPSequence),
                    StoreFailureCodes.MismatchStudyInstanceUIDFailureCode,
                    dicomFile2.Dataset);
            }
            finally
            {
                await _client.DeleteAsync(studyInstanceUID1);
                await _client.DeleteAsync(studyInstanceUID2);
            }
        }

        [Fact(Skip = "Store dataset validation pending in US#72595")]
        public async void GivenDatasetWithDuplicateIdentifiers_WhenStoring_TheServerShouldReturnConflict()
        {
            var studyInstanceUID = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID, studyInstanceUID);
            HttpResult<DicomDataset> response = await _client.PostAsync(new[] { dicomFile1 });
            Assert.False(response.Value.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            ValidationHelpers.ValidateFailureSequence(
                response.Value.GetSequence(DicomTag.FailedSOPSequence),
                StoreFailureCodes.ProcessingFailureCode,
                dicomFile1.Dataset);
        }

        [Fact]
        public async void GivenExistingDataset_WhenStoring_TheServerShouldReturnConflict()
        {
            var studyInstanceUID = TestUidGenerator.Generate();
            try
            {
                DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID);
                HttpResult<DicomDataset> response1 = await _client.PostAsync(new[] { dicomFile1 });
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                ValidationHelpers.ValidateSuccessSequence(response1.Value.GetSequence(DicomTag.ReferencedSOPSequence), dicomFile1.Dataset);
                Assert.False(response1.Value.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence _));

                HttpResult<DicomDataset> response2 = await _client.PostAsync(new[] { dicomFile1 });
                Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
                ValidationHelpers.ValidateFailureSequence(
                    response2.Value.GetSequence(DicomTag.FailedSOPSequence),
                    StoreFailureCodes.SopInstanceAlredyExistsFailureCode,
                    dicomFile1.Dataset);
            }
            finally
            {
                await _client.DeleteAsync(studyInstanceUID);
            }
        }
    }
}
