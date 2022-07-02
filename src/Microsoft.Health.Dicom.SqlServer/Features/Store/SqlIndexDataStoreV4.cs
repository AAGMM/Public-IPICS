﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

/// <summary>
/// Sql IndexDataStore version 4.
/// </summary>
internal class SqlIndexDataStoreV4 : SqlIndexDataStoreV3
{
    public SqlIndexDataStoreV4(
        SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V4;

    public override async Task ReindexInstanceAsync(DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var rows = ExtendedQueryTagDataRowsBuilder.Build(dicomDataset, queryTags, Version);
            V4.IndexInstanceTableValuedParameters parameters = new V4.IndexInstanceTableValuedParameters(
                rows.StringRows,
                rows.LongRows,
                rows.DoubleRows,
                rows.DateTimeRows,
                rows.PersonNameRows);

            V4.IndexInstance.PopulateCommand(sqlCommandWrapper, watermark, parameters);

            try
            {
                await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                    SqlErrorCodes.Conflict => new PendingInstanceException(),
                    _ => new DataStoreException(ex),
                };

            }
        }
    }
}
