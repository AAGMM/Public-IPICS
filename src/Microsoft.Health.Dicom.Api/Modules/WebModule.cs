﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Api.Web;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.Api.Modules
{
    public class WebModule : IStartupModule
    {
        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.Add<MultipartReaderStreamToSeekableStreamConverter>()
                .Singleton()
                .AsImplementedInterfaces();

            services.Decorate<ISeekableStreamConverter, LoggingSeekableStreamConverter>();

            services.Add<AspNetCoreMultipartReaderFactory>()
                .Singleton()
                .AsImplementedInterfaces();
        }
    }
}
