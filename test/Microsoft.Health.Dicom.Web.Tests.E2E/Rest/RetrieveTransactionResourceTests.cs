﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class RetrieveTransactionResourceTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public static readonly List<string> SupportedTransferSyntaxesFor8BitTranscoding = new List<string>
        {
            "DeflatedExplicitVRLittleEndian",
            "ExplicitVRBigEndian",
            "ExplicitVRLittleEndian",
            "ImplicitVRLittleEndian",
            "JPEG2000Lossless",
            "JPEG2000Lossy",
            "JPEGProcess1",
            "JPEGProcess2_4",
            "RLELossless",
        };

        public static readonly List<string> SupportedTransferSyntaxesForOver8BitTranscoding = new List<string>
        {
            "DeflatedExplicitVRLittleEndian",
            "ExplicitVRBigEndian",
            "ExplicitVRLittleEndian",
            "ImplicitVRLittleEndian",
            "RLELossless",
        };

        public RetrieveTransactionResourceTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        [Fact]
        public async Task GivenADicomInstanceWithMultipleFrames_WhenRetrievingFrames_TheServerShouldReturnOK()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid, frames: 2);
            var dicomInstance = dicomFile1.Dataset.ToDicomInstanceIdentifier();
            HttpResult<DicomDataset> response = await _client.PostAsync(new[] { dicomFile1 }, studyInstanceUid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            HttpResult<IReadOnlyList<Stream>> frames = await _client.RetrieveFramesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, frames: 1);
            Assert.NotNull(frames);
            Assert.Equal(HttpStatusCode.OK, frames.StatusCode);
            Assert.Single(frames.Value);
            AssertPixelDataEqual(DicomPixelData.Create(dicomFile1.Dataset).GetFrame(0), frames.Value[0]);

            frames = await _client.RetrieveFramesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, frames: 2);
            Assert.NotNull(frames);
            Assert.Equal(HttpStatusCode.OK, frames.StatusCode);
            Assert.Single(frames.Value);
            AssertPixelDataEqual(DicomPixelData.Create(dicomFile1.Dataset).GetFrame(1), frames.Value[0]);

            frames = await _client.RetrieveFramesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, frames: new[] { 1, 2 });
            Assert.NotNull(frames);
            Assert.Equal(HttpStatusCode.OK, frames.StatusCode);
            Assert.Equal(2, frames.Value.Count);
            AssertPixelDataEqual(DicomPixelData.Create(dicomFile1.Dataset).GetFrame(0), frames.Value[0]);
            AssertPixelDataEqual(DicomPixelData.Create(dicomFile1.Dataset).GetFrame(1), frames.Value[1]);

            // Now check not found when 1 frame exists and the other doesn't.
            frames = await _client.RetrieveFramesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, frames: new[] { 2, 3 });
            Assert.Equal(HttpStatusCode.NotFound, frames.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveInstancesInStudyRequest_WhenStudyInstanceUidDoesNotExist_ThenServerShouldReturnNotFound()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.RetrieveStudyAsync(fakeStudyInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveInstancesInSeriesRequest_WhenStudyInstanceUidDoesNotExist_ThenServerShouldReturnNotFound()
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData();
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            string fakeStudyInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.RetrieveSeriesAsync(fakeStudyInstanceUid, dicomInstance.SeriesInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveInstancesInSeriesRequest_WhenSeriesInstanceUidDoesNotExist_ThenServerShouldReturnNotFound()
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData();
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            string fakeSeriesInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.RetrieveSeriesAsync(dicomInstance.StudyInstanceUid, fakeSeriesInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveInstancesInSeriesRequest_WhenStudyAndSeriesInstanceUidDoNotExist_ThenServerShouldReturnNotFound()
        {
            string fakeStudyInstanceUid = "1.2.345.6.7";
            string fakeSeriesInstanceUid = "1.2.345.6.8";
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.RetrieveSeriesAsync(fakeStudyInstanceUid, fakeSeriesInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveInstanceRequest_WhenStudyInstanceUidDoesNotExist_ThenServerShouldReturnNotFound()
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData();
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            string fakeStudyInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.RetrieveInstanceAsync(fakeStudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveInstanceRequest_WhenSeriesInstanceUidDoesNotExist_ThenServerShouldReturnNotFound()
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData();
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            string fakeSeriesInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.RetrieveInstanceAsync(dicomInstance.StudyInstanceUid, fakeSeriesInstanceUid, dicomInstance.SopInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveInstanceRequest_WhenSopInstanceUidDoesNotExist_ThenServerShouldReturnNotFound()
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData();
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            string fakeSopInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.RetrieveInstanceAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, fakeSopInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveFrameRequest_WhenSopInstanceUidDoesNotExist_ThenServerShouldReturnNotFound()
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(frames: 2);
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            string fakeSopInstanceUid = "1.2.345.6.7";
            HttpResult<IReadOnlyList<Stream>> response = await _client.RetrieveFramesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, fakeSopInstanceUid, frames: 1);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveFrameRequest_WhenFrameDoesNotExist_ThenServerShouldReturnNotFound()
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(frames: 2);
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            HttpResult<IReadOnlyList<Stream>> response = await _client.RetrieveFramesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, frames: 4);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("test")]
        [InlineData("0", "1", "invalid")]
        [InlineData("0.6", "1")]
        public async Task GivenARequestWithNonIntegerFrames_WhenRetrievingFrames_TheServerShouldReturnBadRequest(params string[] frames)
        {
            var requestUri = new Uri(string.Format(DicomWebClient.BaseRetrieveFramesUriFormat, DicomUID.Generate(), DicomUID.Generate(), DicomUID.Generate(), string.Join("%2C", frames)), UriKind.Relative);

            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Accept.Add(DicomWebClient.MediaTypeApplicationOctetStream);

                using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1, 2, -1)]
        public async Task GivenARequestWithFrameLessThanOrEqualTo0_WhenRetrievingFrames_TheServerShouldReturnBadRequest(params int[] frames)
        {
            HttpResult<IReadOnlyList<Stream>> response = await _client.RetrieveFramesAsync(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                frames: frames);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("unknown")]
        [InlineData("&&5")]
        public async Task GivenARequestWithInvalidTransferSyntax_WhenRetrievingResources_TheServerShouldReturnBadRequest(string transferSyntax)
        {
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.RetrieveStudyAsync(TestUidGenerator.Generate(), transferSyntax);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            response = await _client.RetrieveSeriesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), transferSyntax);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            response = await _client.RetrieveInstanceAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), transferSyntax);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            HttpResult<IReadOnlyList<Stream>> framesResponse =
                await _client.RetrieveFramesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), transferSyntax, 1);
            Assert.Equal(HttpStatusCode.BadRequest, framesResponse.StatusCode);
        }

        [Fact]
        public async Task GivenNonExistentIdentifiers_WhenRetrieving_TheServerReturnsNotFound()
        {
            HttpResult<IReadOnlyList<DicomFile>> response1 = await _client.RetrieveStudyAsync(TestUidGenerator.Generate());
            Assert.Equal(HttpStatusCode.NotFound, response1.StatusCode);
            HttpResult<IReadOnlyList<DicomFile>> response2 = await _client.RetrieveSeriesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate());
            Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
            HttpResult<IReadOnlyList<DicomFile>> response3 = await _client.RetrieveInstanceAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate());
            Assert.Equal(HttpStatusCode.NotFound, response3.StatusCode);
            HttpResult<IReadOnlyList<Stream>> response4 = await _client.RetrieveFramesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), frames: 1);
            Assert.Equal(HttpStatusCode.NotFound, response4.StatusCode);

            // Create a valid Study/ Series/ Instance with one frame
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            HttpResult<DicomDataset> storeResponse = await _client.PostAsync(new[] { dicomFile1 }, studyInstanceUid);
            Assert.Equal(HttpStatusCode.OK, storeResponse.StatusCode);

            HttpResult<IReadOnlyList<DicomFile>> response5 = await _client.RetrieveSeriesAsync(studyInstanceUid, TestUidGenerator.Generate());
            Assert.Equal(HttpStatusCode.NotFound, response5.StatusCode);
            HttpResult<IReadOnlyList<DicomFile>> response6 = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate());
            Assert.Equal(HttpStatusCode.NotFound, response6.StatusCode);
            HttpResult<IReadOnlyList<Stream>> response7 = await _client.RetrieveFramesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, frames: 1);
            Assert.Equal(HttpStatusCode.OK, response7.StatusCode);
            HttpResult<IReadOnlyList<Stream>> response8 = await _client.RetrieveFramesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, frames: 2);
            Assert.Equal(HttpStatusCode.NotFound, response8.StatusCode);
        }

        [Fact]
        public async Task GivenStoredDicomFileWithNoContent_WhenRetrieved_TheFileIsRetrievedCorrectly()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid);
            var dicomInstance = dicomFile1.Dataset.ToDicomInstanceIdentifier();
            HttpResult<DicomDataset> response = await _client.PostAsync(new[] { dicomFile1 }, studyInstanceUid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            DicomSequence successSequence = response.Value.GetSequence(DicomTag.ReferencedSOPSequence);

            string studyRetrieveLocation = response.Value.GetSingleValue<string>(DicomTag.RetrieveURL);
            string instanceRetrieveLocation = successSequence.Items[0].GetSingleValue<string>(DicomTag.RetrieveURL);

            HttpResult<IReadOnlyList<DicomFile>> studyByUrlRetrieve = await _client.RetrieveInstancesAsync(new Uri(studyRetrieveLocation));
            ValidateRetrieveTransaction(studyByUrlRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);
            HttpResult<IReadOnlyList<DicomFile>> instanceByUrlRetrieve = await _client.RetrieveInstancesAsync(new Uri(instanceRetrieveLocation));
            ValidateRetrieveTransaction(instanceByUrlRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);

            HttpResult<IReadOnlyList<DicomFile>> studyRetrieve = await _client.RetrieveStudyAsync(dicomInstance.StudyInstanceUid);
            ValidateRetrieveTransaction(studyRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);
            HttpResult<IReadOnlyList<DicomFile>> seriesRetrieve = await _client.RetrieveSeriesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid);
            ValidateRetrieveTransaction(seriesRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);
            HttpResult<IReadOnlyList<DicomFile>> instanceRetrieve = await _client.RetrieveInstanceAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
            ValidateRetrieveTransaction(instanceRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);
        }

        [Theory(Skip = "The file fails with validation.")]
        [InlineData("1.2.840.10008.1.2.4.100", HttpStatusCode.NotAcceptable)] // Unsupported conversion - a video codec
        [InlineData("Bogus TS", HttpStatusCode.BadRequest)] // A non-existent codec
        [InlineData("1.2.840.10008.5.1.4.1.1.1", HttpStatusCode.BadRequest)] // Valid UID, but not a transfer syntax
        public async Task GivenAnUnsupportedTransferSyntax_WhenRetrievingStudy_NotAcceptableIsReturned(string transferSyntax, HttpStatusCode expectedStatusCode)
        {
            IEnumerable<DicomFile> dicomFiles = Samples.GetDicomFilesForTranscoding();
            DicomFile dicomFile = dicomFiles.FirstOrDefault(f => (Path.GetFileNameWithoutExtension(f.File.Name) == "ExplicitVRLittleEndian"));
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            try
            {
                HttpResult<DicomDataset> postResponse = await _client.PostAsync(new[] { dicomFile });
                Assert.True(postResponse.StatusCode == HttpStatusCode.OK);
                HttpResult<IReadOnlyList<DicomFile>> getResponse = await _client.RetrieveInstanceAsync(
                    dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, transferSyntax);
                Assert.Equal(expectedStatusCode, getResponse.StatusCode);
            }
            finally
            {
                HttpStatusCode result = await _client.DeleteAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
                Assert.Equal(HttpStatusCode.OK, result);
            }
        }

        // Test that if no TS specified, we return the original TS w/o transcoding -
        // http://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_8.7.3.5.2:S
        // The wildcard value "*" indicates that the user agent will accept any Transfer Syntax.
        // This allows, for example, the origin server to respond without needing to encode an
        // existing representation to a new Transfer Syntax, or to respond with the
        // Explicit VR Little Endian Transfer Syntax regardless of the Transfer Syntax stored.
        [Fact]
        public async Task GivenAnUnsupportedTransferSyntax_WhenWildCardTsSpecified_OriginalImageReturned()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                transferSyntax: DicomTransferSyntax.HEVCH265Main10ProfileLevel51.UID.UID,
                encode: false);

            HttpResult<DicomDataset> postResponse = await _client.PostAsync(new[] { dicomFile });

            Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

            // Check for series
            HttpResult<IReadOnlyList<DicomFile>> seriesResponse = await _client.RetrieveSeriesAsync(
                studyInstanceUid,
                seriesInstanceUid,
                "*");

            Assert.Equal(HttpStatusCode.OK, seriesResponse.StatusCode);
            Assert.Equal(DicomTransferSyntax.HEVCH265Main10ProfileLevel51, seriesResponse.Value.Single().Dataset.InternalTransferSyntax);

            // Check for frame
            HttpResult<IReadOnlyList<Stream>> frameResponse = await _client.RetrieveFramesAsync(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                dicomTransferSyntax: "*",
                frames: 1);

            Assert.Equal(HttpStatusCode.OK, frameResponse.StatusCode);
            Assert.NotEqual(0, frameResponse.Value.Single().Length);
        }

        [Fact]
        public async Task GivenSupportedTransferSyntax_WhenNoTsSpecified_DefaultTsReturned()
        {
            var seriesInstanceUid = TestUidGenerator.Generate();
            var studyInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.DeflatedExplicitVRLittleEndian.UID.UID);

            try
            {
                HttpResult<DicomDataset> postResponse = await _client.PostAsync(new[] { dicomFile });

                Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

                HttpResult<IReadOnlyList<DicomFile>> retrieveResponse = await _client.RetrieveSeriesAsync(
                    studyInstanceUid,
                    seriesInstanceUid);

                Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);
                Assert.Equal(DicomTransferSyntax.ExplicitVRLittleEndian, retrieveResponse.Value.Single().Dataset.InternalTransferSyntax);
            }
            finally
            {
                HttpStatusCode result = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid);
                Assert.Equal(HttpStatusCode.NoContent, result);
            }
        }

        [Fact]
        public async Task GivenAMixOfTransferSyntaxes_WhenSomeAreSupported_PartialIsReturned()
        {
            var seriesInstanceUid = TestUidGenerator.Generate();
            var studyInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.HEVCH265Main10ProfileLevel51.UID.UID,
                encode: false);

            DicomFile dicomFile3 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ImplicitVRLittleEndian.UID.UID);

            HttpResult<DicomDataset> postResponse = await _client.PostAsync(new[] { dicomFile1, dicomFile2, dicomFile3 });

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            HttpResult<IReadOnlyList<DicomFile>> retrieveResponse = await _client.RetrieveSeriesAsync(
                studyInstanceUid,
                seriesInstanceUid,
                DicomTransferSyntax.JPEG2000Lossy.UID.UID);

            Assert.Equal(HttpStatusCode.PartialContent, retrieveResponse.StatusCode);
            Assert.Equal(2, retrieveResponse.Value.Count);
        }

        public static IEnumerable<object[]> Get8BitTranscoderCombos()
        {
            List<string> fromList = SupportedTransferSyntaxesFor8BitTranscoding;
            List<string> toList = SupportedTransferSyntaxesFor8BitTranscoding;

            return from x in fromList from y in toList select new[] { x, y };
        }

        public static IEnumerable<object[]> Get16BitTranscoderCombos()
        {
            List<string> fromList = SupportedTransferSyntaxesForOver8BitTranscoding;
            List<string> toList = SupportedTransferSyntaxesForOver8BitTranscoding;

            return from x in fromList from y in toList select new[] { x, y };
        }

        [Theory]
        [MemberData(nameof(Get16BitTranscoderCombos))]
        public async Task GivenSupported16bitTransferSyntax_WhenRetrievingStudyAndAskingForConversion_OKIsReturned(
            string tsFrom,
            string tsTo)
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: ((DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsFrom).GetValue(null)).UID.UID);
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            try
            {
                HttpResult<DicomDataset> postResponse = await _client.PostAsync(new[] { dicomFile });
                Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

                var expectedTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsTo).GetValue(null);

                HttpResult<IReadOnlyList<DicomFile>> retrieveResponse = await _client.RetrieveInstanceAsync(
                    dicomInstance.StudyInstanceUid,
                    dicomInstance.SeriesInstanceUid,
                    dicomInstance.SopInstanceUid,
                    expectedTransferSyntax.UID.UID);
                Assert.Equal(expectedTransferSyntax, retrieveResponse.Value.Single().Dataset.InternalTransferSyntax);
                Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);

                HttpResult<IReadOnlyList<Stream>> framesResponse = await _client.RetrieveFramesAsync(
                    dicomInstance.StudyInstanceUid,
                    dicomInstance.SeriesInstanceUid,
                    dicomInstance.SopInstanceUid,
                    expectedTransferSyntax.UID.UID,
                    1);
                Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);
                Assert.NotEqual(0, framesResponse.Value.Single().Length);
            }
            finally
            {
                HttpStatusCode result = await _client.DeleteAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
                Assert.Equal(HttpStatusCode.NoContent, result);
            }
        }

        [Theory(Skip = "The file fails with validation.")]
        [MemberData(nameof(Get8BitTranscoderCombos))]
        public async Task GivenSupported8bitTransferSyntax_WhenRetrievingStudyAndAskingForConversion_OKIsReturned(
            string tsFrom,
            string tsTo)
        {
            IEnumerable<DicomFile> dicomFiles = Samples.GetDicomFilesForTranscoding();
            DicomFile dicomFile = dicomFiles.FirstOrDefault(f => (Path.GetFileNameWithoutExtension(f.File.Name) == tsFrom));
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            try
            {
                HttpResult<DicomDataset> postResponse = await _client.PostAsync(new[] { dicomFile });
                Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

                var expectedTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsTo).GetValue(null);
                HttpResult<IReadOnlyList<DicomFile>> retrieveResponse = await _client.RetrieveInstanceAsync(
                    dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, expectedTransferSyntax.UID.UID);
                Assert.Equal(expectedTransferSyntax, retrieveResponse.Value.Single().Dataset.InternalTransferSyntax);
                Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);
                Assert.NotNull(retrieveResponse.Value.Single());
            }
            finally
            {
                HttpStatusCode result = await _client.DeleteAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
                Assert.Equal(HttpStatusCode.OK, result);
            }
        }

        [Theory(Skip = "The file fails with validation.")]
        [InlineData("1.2.840.10008.1.2.4.91")] // JPEG Process 1 - should work, but doesn't for this particular image. Not officially supported
        public async Task GivenAnExceptionDuringTranscoding_WhenRetrievingStudy_EmptyStreamIsReturned(string transferSyntax)
        {
            IEnumerable<DicomFile> dicomFiles = Samples.GetSampleDicomFiles();
            DicomFile dicomFile = dicomFiles.FirstOrDefault(f => (Path.GetFileNameWithoutExtension(f.File.Name) == "XRJPEGProcess1"));
            var dicomInstance = dicomFile.Dataset.ToDicomInstanceIdentifier();

            try
            {
                HttpResult<DicomDataset> postResponse = await _client.PostAsync(new[] { dicomFile });
                Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

                HttpResult<IReadOnlyList<DicomFile>> response = await _client.RetrieveInstanceAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, transferSyntax);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Null(response.Value.Single());
            }
            finally
            {
                HttpStatusCode result = await _client.DeleteAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
                Assert.Equal(HttpStatusCode.OK, result);
            }
        }

        [Theory]
        [InlineData("application/data")]
        [InlineData("application/json")]
        public async Task GivenAnIncorrectAcceptHeader_WhenRetrievingResource_NotAcceptableIsReturned(string acceptHeader)
        {
            // Study
            await ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveStudyUriFormat, TestUidGenerator.Generate()),
                acceptHeader);

            // Series
            await ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveSeriesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate()),
                acceptHeader);

            // Instance
            await ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveInstanceUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate()),
                acceptHeader);
        }

        [Theory]
        [InlineData("application/dicom")]
        [InlineData("application/data")]
        [InlineData("application/json")]
        public async Task GivenAnIncorrectAcceptHeader_WhenRetrievingFrames_NotAcceptableIsReturned(string acceptHeader)
        {
            await ValidateNotAcceptableResponseAsync(
                _client,
                string.Format(DicomWebClient.BaseRetrieveFramesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1),
                acceptHeader);
        }

        internal static async Task ValidateNotAcceptableResponseAsync(DicomWebClient dicomWebClient, string requestUri, string acceptHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add(HeaderNames.Accept, acceptHeader);
            using (HttpResponseMessage response = await dicomWebClient.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
            }
        }

        private void ValidateRetrieveTransaction(
            HttpResult<IReadOnlyList<DicomFile>> response,
            HttpStatusCode expectedStatusCode,
            DicomTransferSyntax expectedTransferSyntax,
            params DicomFile[] expectedFiles)
        {
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedFiles.Length, response.Value.Count);

            for (var i = 0; i < expectedFiles.Length; i++)
            {
                DicomFile expectedFile = expectedFiles[i];
                var expectedInstance = expectedFile.Dataset.ToDicomInstanceIdentifier();
                DicomFile actualFile = response.Value.First(x => x.Dataset.ToDicomInstanceIdentifier().Equals(expectedInstance));

                Assert.Equal(expectedTransferSyntax, response.Value[i].Dataset.InternalTransferSyntax);

                // If the same transfer syntax as original, the files should be exactly the same
                if (expectedFile.Dataset.InternalTransferSyntax == actualFile.Dataset.InternalTransferSyntax)
                {
                    var expectedFileArray = DicomFileToByteArray(expectedFile);
                    var actualFileArray = DicomFileToByteArray(actualFile);

                    Assert.Equal(expectedFileArray.Length, actualFileArray.Length);

                    for (var ii = 0; ii < expectedFileArray.Length; ii++)
                    {
                        Assert.Equal(expectedFileArray[ii], actualFileArray[ii]);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        private byte[] DicomFileToByteArray(DicomFile dicomFile)
        {
            using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
            {
                dicomFile.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private static void AssertPixelDataEqual(IByteBuffer expectedPixelData, Stream actualPixelData)
        {
            Assert.Equal(expectedPixelData.Size, actualPixelData.Length);
            Assert.Equal(0, actualPixelData.Position);
            for (var i = 0; i < expectedPixelData.Size; i++)
            {
                Assert.Equal(expectedPixelData.Data[i], actualPixelData.ReadByte());
            }
        }
    }
}
