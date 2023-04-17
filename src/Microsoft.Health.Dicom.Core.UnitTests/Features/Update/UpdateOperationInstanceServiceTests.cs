// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Update;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Update;
public class UpdateOperationInstanceServiceTests
{
    private readonly IUpdateInstanceOperationService _updateInstanceOperationService;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly IDicomRequestContextAccessor _contextAccessor;

    public UpdateOperationInstanceServiceTests()
    {
        _guidFactory = Substitute.For<IGuidFactory>();
        _client = Substitute.For<IDicomOperationsClient>();
        _contextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _updateInstanceOperationService = new UpdateInstanceOperationService(_guidFactory, _client, _contextAccessor, NullLogger<UpdateInstanceOperationService>.Instance);
    }

    [Fact]
    public async Task WhenExistingOperationQueued_ThenExistingUpdateOperationExceptionShouldBeThrown()
    {
        IReadOnlyList<string> studyInstanceUids = new List<string>() { "1.2.3.4" };
        DicomDataset changeDataset = new DicomDataset();
        var updateSpec = new UpdateSpecification(studyInstanceUids, changeDataset);
        var id = Guid.NewGuid();
        var expected = new OperationReference(id, new Uri("https://dicom.contoso.io/unit/test/Operations/" + id, UriKind.Absolute));

        _client.FindOperationsAsync(Arg.Is(GetOperationPredicate()), CancellationToken.None)
            .Returns(new OperationReference[] { expected }.ToAsyncEnumerable());
        await Assert.ThrowsAsync<ExistingUpdateOperationException>(() =>
            _updateInstanceOperationService.QueueUpdateOperationAsync(updateSpec, CancellationToken.None));
    }

    [Fact]
    public async Task GivenValidInput_WhenNoExistingOperationQueued_ThenShouldSucceed()
    {
        IReadOnlyList<string> studyInstanceUids = new List<string>() { "1.2.3.4" };
        DicomDataset changeDataset = new DicomDataset();
        var updateSpec = new UpdateSpecification(studyInstanceUids, changeDataset);
        string href = "/operation";
        _client.FindOperationsAsync(Arg.Is(GetOperationPredicate()), CancellationToken.None)
            .Returns(AsyncEnumerable.Empty<OperationReference>());
        _contextAccessor.RequestContext.DataPartitionEntry = PartitionEntry.Default;
        var response = await _updateInstanceOperationService.QueueUpdateOperationAsync(updateSpec, CancellationToken.None);

        Assert.Equal(href, response.Href.ToString());
    }

    private static Expression<Predicate<OperationQueryCondition<DicomOperation>>> GetOperationPredicate()
    => (x) =>
        x.CreatedTimeFrom == DateTime.MinValue &&
        x.CreatedTimeTo == DateTime.MaxValue &&
        x.Operations.Single() == DicomOperation.Update &&
        x.Statuses.SequenceEqual(new OperationStatus[] { OperationStatus.NotStarted, OperationStatus.Running });
}
