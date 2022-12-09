// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public class PartitionTests : IClassFixture<SqlDataStoreTestsFixture>
{
    private readonly SqlDataStoreTestsFixture _fixture;

    public PartitionTests(SqlDataStoreTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WhenNewPartitionIsCreated_Then_ItIsRetrievable()
    {
        string partitionName = "test";

        await _fixture.PartitionStore.AddPartitionAsync(partitionName);
        PartitionEntry partition = await _fixture.PartitionStore.GetPartitionAsync(partitionName);

        Assert.NotNull(partition);
    }

    [Fact]
    public async Task WhenTwoNewPartitionIsCreated_Then_ItThrowsException()
    {
        string partitionName = "test";

        await _fixture.PartitionStore.AddPartitionAsync(partitionName);
        PartitionEntry partition = await _fixture.PartitionStore.GetPartitionAsync(partitionName);

        Assert.NotNull(partition);

        await Assert.ThrowsAsync<DataPartitionsAlreadyExistsException>(async () => await _fixture.PartitionStore.AddPartitionAsync(partitionName));
    }

    [Fact]
    public async Task WhenGetPartitionsIsCalled_Then_DefaultPartitionRecordIsReturned()
    {
        IEnumerable<PartitionEntry> partitionEntries = await _fixture.PartitionStore.GetPartitionsAsync();

        Assert.Contains(partitionEntries, p => p.PartitionKey == DefaultPartition.Key);
    }

    [Fact]
    public async Task WhenGetPartitionIsCalledWithDefaultPartitionName_Then_DefaultPartitionRecordIsReturned()
    {
        PartitionEntry partitionEntry = await _fixture.PartitionStore.GetPartitionAsync(DefaultPartition.Name);

        Assert.Equal(DefaultPartition.Key, partitionEntry.PartitionKey);
    }
}
