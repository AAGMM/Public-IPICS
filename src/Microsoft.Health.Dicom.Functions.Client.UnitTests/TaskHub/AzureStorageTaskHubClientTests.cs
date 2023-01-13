// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public class AzureStorageTaskHubClientTests
{
    private readonly LeasesContainer _leasesContainer = Substitute.For<LeasesContainer>(Substitute.For<BlobServiceClient>("UseDevelopmentStorage=true"), "Foo");
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true");
    private readonly TableServiceClient _tableServiceClient = Substitute.For<TableServiceClient>("UseDevelopmentStorage=true");
    private readonly AzureStorageTaskHubClient _client;

    public AzureStorageTaskHubClientTests()
    {
        _client = new AzureStorageTaskHubClient(_leasesContainer, _queueServiceClient, _tableServiceClient, NullLoggerFactory.Instance);
    }

    [Fact]
    public async Task GivenMissingLeases_WhenGettingTaskHub_ThenReturnNull()
    {
        using var tokenSource = new CancellationTokenSource();

        _leasesContainer.GetTaskHubInfoAsync(tokenSource.Token).Returns((TaskHubInfo)null);

        Assert.Null(await _client.GetTaskHubAsync(tokenSource.Token));

        await _leasesContainer.Received(1).GetTaskHubInfoAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenAvailableLeases_WhenGettingTaskHub_ThenReturnObject()
    {
        using var tokenSource = new CancellationTokenSource();
        var taskHubInfo = new TaskHubInfo { PartitionCount = 4, TaskHubName = "TestTaskHub" };

        _leasesContainer.GetTaskHubInfoAsync(tokenSource.Token).Returns(taskHubInfo);

        Assert.IsType<AzureStorageTaskHub>(await _client.GetTaskHubAsync(tokenSource.Token));

        await _leasesContainer.Received(1).GetTaskHubInfoAsync(tokenSource.Token);
    }
}
