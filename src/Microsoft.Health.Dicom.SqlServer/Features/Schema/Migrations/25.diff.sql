﻿SET XACT_ABORT ON

BEGIN TRANSACTION
GO
/*************************************************************
    Stored procedure for adding a partition.
**************************************************************/
--
-- STORED PROCEDURE
--     AddPartition
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Adds a partition.
--
-- PARAMETERS
--     @partitionName
--         * The client-provided data partition name.
--
-- RETURN VALUE
--     The partition.
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AddPartition
    @partitionName  VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @createdDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @partitionKey INT

    SELECT @partitionKey = PartitionKey
    FROM dbo.Partition
    WHERE PartitionName = @partitionName

    IF @@ROWCOUNT <> 0
        THROW 50409, 'Partition already exists', 1;

    -- Insert Partition
    SET @partitionKey = NEXT VALUE FOR dbo.PartitionKeySequence

    INSERT INTO dbo.Partition
        (PartitionKey, PartitionName, CreatedDate)
    VALUES
        (@partitionKey, @partitionName, @createdDate)

    SELECT @partitionKey, @partitionName, @createdDate

    COMMIT TRANSACTION
END
GO

COMMIT TRANSACTION

