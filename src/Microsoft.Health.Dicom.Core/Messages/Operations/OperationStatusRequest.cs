﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Operations;

/// <summary>
/// Represents a request for the status of long-running DICOM operations.
/// </summary>
public class OperationStatusRequest : IRequest<OperationStatusResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationStatusRequest"/> class.
    /// </summary>
    /// <param name="operationId">The unique ID for a particular DICOM operation.</param>
    public OperationStatusRequest(Guid operationId)
        => OperationId = operationId;

    /// <summary>
    /// Gets the operation ID.
    /// </summary>
    /// <value>The unique ID that denotes a particular operation.</value>
    public Guid OperationId { get; }
}

