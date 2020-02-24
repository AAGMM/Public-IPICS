﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class RetrieveTransactionResourceRenderedTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly ITestOutputHelper output;

        public RetrieveTransactionResourceRenderedTests(HttpIntegrationTestFixture<Startup> fixture, ITestOutputHelper output)
        {
            Client = new DicomWebClient(fixture.HttpClient);
            this.output = output;
        }

        protected DicomWebClient Client { get; set; }

        [Fact(Skip = "This validates ability to handle parallel processing. This is long running but may come in handy if issues are found in certain environments")]
        public void ConvertCanHandleParallelProcessing()
        {
            var maxFiles = 500;
            var random = new Random();

            var fromList = new List<string>
            {
                "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "RLELossless",
            };

            var fromTsList = fromList.Select(x =>
                (name: x, transferSyntax: (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null)));

            var files = new List<DicomFile>();

            for (int i = 0; i < maxFiles; i++)
            {
                files.Add(Samples.CreateRandomDicomFileWith8BitPixelData(transferSyntax: fromTsList.ElementAt(random.Next(fromList.Count - 1)).transferSyntax.UID.UID));
            }

            Parallel.ForEach(
                files,
                file =>
                {
                    var bmp = new DicomImage(file.Dataset).ToBitmap();

                    using (MemoryStream ms = RecyclableMemoryStreamManagerAccessor.Instance.GetStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        Assert.NotEqual(0, ms.Length);
                    }
                });
        }

        [Fact]
        public async Task GivenValidDicomFile_WhenRetrievingRendered_ShouldReturnValidImage()
        {
            var fromList = new List<string>
            {
                "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "RLELossless",
            };
            var fromTsList = fromList.Select(x =>
                (name: x, transferSyntax: (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null)));

            foreach (var ts in fromTsList)
            {
                var dicomFile = Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: ts.transferSyntax.UID.UID, frames: 2);

                HttpResult<DicomDataset> postResponse = await Client.PostAsync(new[] { dicomFile });
                Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

                var studyInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                var seriesInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                var sopInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

                var getResponse = await Client.GetInstanceRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/jpeg", false);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                var img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Jpeg, img.RawFormat);

                getResponse = await Client.GetInstanceRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/png", false);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Png, img.RawFormat);

                getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/jpeg", false, 1);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Jpeg, img.RawFormat);

                getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/jpeg", false, 2);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Jpeg, img.RawFormat);

                getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/png", false, 1);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Png, img.RawFormat);

                getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/png", false, 2);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Png, img.RawFormat);
            }
        }

        [Fact]
        public async Task GivenValidDicomFile_WhenRetrievingRenderedThumbnail_ShouldReturnValidImage()
        {
            var fromList = new List<string>
            {
                "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "RLELossless",
            };
            var fromTsList = fromList.Select(x =>
                (name: x, transferSyntax: (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null)));

            foreach (var ts in fromTsList)
            {
                var dicomFile = Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: ts.transferSyntax.UID.UID, frames: 2);

                HttpResult<DicomDataset> postResponse = await Client.PostAsync(new[] { dicomFile });
                Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

                var studyInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                var seriesInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                var sopInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

                var getResponse = await Client.GetInstanceRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/jpeg", true);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                var img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Jpeg, img.RawFormat);
                Assert.Equal(200, img.Width);
                Assert.Equal(200, img.Height);

                getResponse = await Client.GetInstanceRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/png", true);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Png, img.RawFormat);
                Assert.Equal(200, img.Width);
                Assert.Equal(200, img.Height);

                getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/jpeg", true, 1);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Jpeg, img.RawFormat);
                Assert.Equal(200, img.Width);
                Assert.Equal(200, img.Height);

                getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/jpeg", true, 2);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Jpeg, img.RawFormat);
                Assert.Equal(200, img.Width);
                Assert.Equal(200, img.Height);

                getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/png", true, 1);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Png, img.RawFormat);
                Assert.Equal(200, img.Width);
                Assert.Equal(200, img.Height);

                getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/png", true, 2);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Png, img.RawFormat);
                Assert.Equal(200, img.Width);
                Assert.Equal(200, img.Height);
            }
        }

        [Fact]
        public async Task GivenInvalidDicomFile_WhenRetrievingRendered_ShouldReturnEmptyStream()
        {
            var seriesInstanceUID = DicomUID.Generate();
            var studyInstanceUID = DicomUID.Generate();
            var sopInstanceUID = DicomUID.Generate();

            var dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUID.UID,
                seriesInstanceUID.UID,
                sopInstanceUID.UID,
                transferSyntax: DicomTransferSyntax.JPEG2000Lossless.UID.UID,
                encode: false);

            HttpResult<DicomDataset> postResponse = await Client.PostAsync(new[] { dicomFile });
            Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

            var getResponse = await Client.GetInstanceRenderedAsync(studyInstanceUID.UID, seriesInstanceUID.UID, sopInstanceUID.UID, "image/jpeg");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(0, getResponse.Value.Single().Length);

            getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID.UID, seriesInstanceUID.UID, sopInstanceUID.UID, "image/jpeg", false, 1);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(0, getResponse.Value.Single().Length);
        }

        [Fact]
        public async Task GivenValidMultiFrameDicomFile_WhenRetrievingMultipleRenderedFrames_ShouldReturnBadRequest()
        {
            var seriesInstanceUID = DicomUID.Generate();
            var studyInstanceUID = DicomUID.Generate();
            var sopInstanceUID = DicomUID.Generate();

            var dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUID.UID,
                seriesInstanceUID.UID,
                sopInstanceUID.UID,
                frames: 2);

            HttpResult<DicomDataset> postResponse = await Client.PostAsync(new[] { dicomFile });
            Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

            var getResponse = await Client.GetFramesRenderedAsync(studyInstanceUID.UID, seriesInstanceUID.UID, sopInstanceUID.UID, "image/jpeg", false, 1, 2);
            Assert.Equal(HttpStatusCode.BadRequest, getResponse.StatusCode);
        }

        [Fact]
        public async Task GivenValidDicomFile_WhenRequestingUnsupportedMediaType_ShouldReturnBadRequest()
        {
            var seriesInstanceUID = DicomUID.Generate();
            var studyInstanceUID = DicomUID.Generate();
            var sopInstanceUID = DicomUID.Generate();

            var dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUID.UID,
                seriesInstanceUID.UID,
                sopInstanceUID.UID);

            HttpResult<DicomDataset> postResponse = await Client.PostAsync(new[] { dicomFile });
            Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

            var getResponse = await Client.GetInstanceRenderedAsync(studyInstanceUID.UID, seriesInstanceUID.UID, sopInstanceUID.UID, "image/tiff", false);
            Assert.Equal(HttpStatusCode.NotAcceptable, getResponse.StatusCode);
        }
    }
}
