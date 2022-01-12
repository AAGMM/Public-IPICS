﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class QueryTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>, IAsyncLifetime
    {
        private readonly IDicomWebClient _client;
        private readonly DicomInstancesManager _instancesManager;

        public QueryTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _client = fixture.GetDicomWebClient();
            _instancesManager = new DicomInstancesManager(_client);
        }

        [Fact]
        public async Task GivenSearchRequest_WithUnsupportedTag_ReturnBadRequest()
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.QueryStudyAsync("Modality=CT"));

            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
            Assert.Equal(exception.ResponseMessage, string.Format(DicomCoreResource.UnsupportedSearchParameter, "Modality", "study"));
        }

        [Fact]
        public async Task GivenSearchRequest_WithValidParamsAndNoMatchingResult_ReturnNoContent()
        {
            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudyAsync("StudyDate=20200101");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        [Trait("Category", "bvt")]
        public async Task GivenSearchRequest_AllStudyLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyDate, "20190101" },
            });
            DicomDataset unMatchedInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyDate, "20190102" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudyAsync("StudyDate=20190101");

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.NotEmpty(datasets);
            DicomDataset testDataResponse = datasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
            Assert.NotNull(testDataResponse);
            ValidationHelpers.ValidateResponseDataset(QueryResource.AllStudies, matchInstance, testDataResponse);
        }

        [Fact]
        public async Task GivenSearchRequest_AllStudyLevelOnPatientName_MatchIsCaseIncensitiveAndAccentIncensitive()
        {
            string randomNamePart = RandomString(7);
            string patientName = $"Hall^{randomNamePart}^Tá";
            string patientNameWithNoAccent = $"Hall^{randomNamePart}^Ta";

            await PostDicomFileAsync(new DicomDataset
            {
                { DicomTag.PatientName, patientName },
                { DicomTag.SpecificCharacterSet, "ISO_IR 192" },
            });

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudyAsync($"PatientName={patientNameWithNoAccent}");

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Single(datasets);
            DicomDataset testDataResponse = datasets[0];
            Assert.NotNull(testDataResponse);
            Assert.Equal(patientName, testDataResponse.GetString(DicomTag.PatientName));
        }

        [Fact]
        public async Task GivenSearchRequest_StudySeriesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "MRI" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyInstanceUID, studyId },
                 { DicomTag.Modality, "CT" },
            });

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudySeriesAsync(studyId, "Modality=MRI");

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Single(datasets);
            ValidationHelpers.ValidateResponseDataset(QueryResource.StudySeries, matchInstance, datasets[0]);
        }

        [Fact]
        public async Task GivenSearchRequest_AllSeriesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "MRI" },
            });
            var seriesId = matchInstance.GetSingleValue<string>(DicomTag.SeriesInstanceUID);

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QuerySeriesAsync("Modality=MRI");

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.NotNull(datasets);
            DicomDataset testDataResponse = datasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.SeriesInstanceUID) == seriesId);
            Assert.NotNull(testDataResponse);
            ValidationHelpers.ValidateResponseDataset(QueryResource.AllSeries, matchInstance, testDataResponse);
        }

        [Fact]
        public async Task GivenSearchRequest_StudyInstancesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "MRI" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyInstanceUID, studyId },
                 { DicomTag.Modality, "CT" },
            });

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudyInstanceAsync(studyId, "Modality=MRI");

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Single(datasets);
            ValidationHelpers.ValidateResponseDataset(QueryResource.StudyInstances, matchInstance, datasets[0]);
        }

        [Fact]
        public async Task GivenSearchRequest_StudySeriesInstancesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync();
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            var seriesId = matchInstance.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            var instanceId = matchInstance.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.StudyInstanceUID, studyId },
                 { DicomTag.SeriesInstanceUID, seriesId },
            });

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudySeriesInstanceAsync(studyId, seriesId, $"SOPInstanceUID={instanceId}");

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.Single(datasets);
            ValidationHelpers.ValidateResponseDataset(QueryResource.StudySeriesInstances, matchInstance, datasets[0]);
        }

        [Fact]
        public async Task GivenSearchRequest_AllInstancesLevel_MatchResult()
        {
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.Modality, "XRAY" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryInstancesAsync("Modality=XRAY");

            DicomDataset[] datasets = await response.ToArrayAsync();

            Assert.NotNull(datasets);
            DicomDataset testDataResponse = datasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
            Assert.NotNull(testDataResponse);
            ValidationHelpers.ValidateResponseDataset(QueryResource.AllInstances, matchInstance, testDataResponse);
        }

        [Fact]
        public async Task GivenSearchRequest_PatientNameFuzzyMatch_MatchResult()
        {
            string randomNamePart = RandomString(7);
            DicomDataset matchInstance2 = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.PatientName, $"Jonathan^{randomNamePart}^Stone Hall^^" },
            });
            var studyId2 = matchInstance2.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            DicomDataset matchInstance1 = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.PatientName, $"Jon^{randomNamePart}^StoneHall" },
            });
            var studyId1 = matchInstance1.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            // Retrying the query 3 times, to give sql FT index time to catch up
            int retryCount = 0;
            DicomDataset testDataResponse1 = null;
            DicomDataset[] responseDatasets = null;

            while (retryCount < 3 || testDataResponse1 == null)
            {
                using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudyAsync(
                    $"PatientName={randomNamePart}&FuzzyMatching=true");

                responseDatasets = await response.ToArrayAsync();

                testDataResponse1 = responseDatasets?.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId1);
                retryCount++;
            }

            Assert.NotNull(testDataResponse1);
            ValidationHelpers.ValidateResponseDataset(QueryResource.AllStudies, matchInstance1, testDataResponse1);

            DicomDataset testDataResponse2 = responseDatasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId2);
            Assert.NotNull(testDataResponse2);
            ValidationHelpers.ValidateResponseDataset(QueryResource.AllStudies, matchInstance2, testDataResponse2);
        }

        [Fact]
        public async Task GivenSearchRequest_ReferringPhysicianNameFuzzyMatch_MatchResult()
        {
            string randomNamePart = RandomString(7);
            DicomDataset matchInstance = await PostDicomFileAsync(new DicomDataset()
            {
                 { DicomTag.ReferringPhysicianName, $"dr^{randomNamePart}^Stone Hall^^" },
            });
            var studyId = matchInstance.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            // Retrying the query 3 times, to give sql FT index time to catch up
            int retryCount = 0;
            DicomDataset testDataResponse = null;
            DicomDataset[] responseDatasets = null;

            while (retryCount < 3 || testDataResponse == null)
            {
                using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudyAsync(
                    $"ReferringPhysicianName={randomNamePart}&FuzzyMatching=true");

                responseDatasets = await response.ToArrayAsync();

                testDataResponse = responseDatasets?.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.StudyInstanceUID) == studyId);
                retryCount++;
            }

            Assert.NotNull(testDataResponse);
            Assert.Equal(matchInstance.GetSingleValue<string>(DicomTag.ReferringPhysicianName), testDataResponse.GetSingleValue<string>(DicomTag.ReferringPhysicianName));
        }

        [Fact]
        public async Task GivenSearchRequest_OHIFViewerStudyQuery_ReturnsOK()
        {
            var ohifViewerQuery = $"limit=25&offset=0&includefield=00081030%2C00080060&StudyDate=19521125-20210507";

            // client is checking the success response and throws exception otherwise
            using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudyAsync(ohifViewerQuery);
        }

        private static string RandomString(int length)
        {
            var random = new Random();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task<DicomDataset> PostDicomFileAsync(DicomDataset metadataItems = null)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile();

            if (metadataItems != null)
            {
                dicomFile1.Dataset.AddOrUpdate(metadataItems);
            }

            await _instancesManager.StoreAsync(new[] { dicomFile1 });
            return dicomFile1.Dataset;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _instancesManager.DisposeAsync();
        }
    }
}
