﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public class GetCustomTagsServiceTests
    {
        private ICustomTagStore _customTagStore;
        private IDicomTagParser _dicomTagParser;
        private IGetCustomTagsService _getCustomTagsService;

        public GetCustomTagsServiceTests()
        {
            _customTagStore = Substitute.For<ICustomTagStore>();
            _dicomTagParser = Substitute.For<IDicomTagParser>();
            _getCustomTagsService = new GetCustomTagsService(_customTagStore, _dicomTagParser);
        }

        [Fact]
        public async Task GivenRequestForAllTags_WhenNoTagsAreStored_ThenEmptyListShouldBeReturned()
        {
            _customTagStore.GetAllCustomTagsAsync(default).Returns(new List<CustomTagEntry>());
            GetAllCustomTagsResponse response = await _getCustomTagsService.GetAllCustomTagsAsync(default);

            Assert.Empty(response.CustomTags);
        }

        [Fact]
        public async Task GivenRequestForAllTags_WhenMultipleTagsAreStored_ThenCustomTagEntryListShouldBeReturned()
        {
            CustomTagEntry tag1 = new CustomTagEntry("45456767", DicomVRCode.AE.ToString(), CustomTagLevel.Instance, CustomTagStatus.Added);
            CustomTagEntry tag2 = new CustomTagEntry("01012323", DicomVRCode.FL.ToString(), CustomTagLevel.Series, CustomTagStatus.Reindexing);

            List<CustomTagEntry> storedEntries = new List<CustomTagEntry>() { tag1, tag2 };

            _customTagStore.GetAllCustomTagsAsync(default).Returns(storedEntries);
            GetAllCustomTagsResponse response = await _getCustomTagsService.GetAllCustomTagsAsync(default);

            List<CustomTagEntry> result = storedEntries.Except(response.CustomTags).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GivenRequestForCustomTag_WhenTagDoesntExist_ThenCustomTagEntryShouldBeNull()
        {
            string tagPath = "(0101,2323)";
            string storedTagPath = "01012323";
            CustomTagEntry stored = null;

            _dicomTagParser.ParseFormattedTagPath(tagPath).Returns(storedTagPath);
            _customTagStore.GetCustomTagAsync(storedTagPath, default).Returns(stored);
            GetCustomTagResponse response = await _getCustomTagsService.GetCustomTagAsync(tagPath);

            Assert.Null(response.CustomTag);
        }

        [Fact]
        public async Task GivenRequestForCustomTag_WhenTagExists_ThenCustomTagEntryShouldBeReturned()
        {
            string tagPath = "(0101,2323)";
            string storedTagPath = "01012323";
            CustomTagEntry stored = new CustomTagEntry("01012323", DicomVRCode.AE.ToString(), CustomTagLevel.Instance, CustomTagStatus.Added);

            _dicomTagParser.ParseFormattedTagPath(tagPath).Returns(storedTagPath);
            _customTagStore.GetCustomTagAsync(storedTagPath, default).Returns(stored);
            GetCustomTagResponse response = await _getCustomTagsService.GetCustomTagAsync(tagPath);

            Assert.Equal(stored, response.CustomTag);
        }
    }
}
