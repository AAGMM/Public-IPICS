﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Api.Modules.HealthChecks
{
    public class HealthCheckConfiguration : Microsoft.Health.Api.Features.HealthChecks.HealthCheckConfiguration
    {
        public HealthCheckConfiguration(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}
