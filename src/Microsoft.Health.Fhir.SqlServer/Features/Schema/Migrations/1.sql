-- NOTE: This script DROPS AND RECREATES all database objects.
-- Style guide: please see: https://github.com/ktaranov/sqlserver-kit/blob/master/SQL%20Server%20Name%20Convention%20and%20T-SQL%20Programming%20Style.md


/*************************************************************
    Drop existing objects
**************************************************************/

DECLARE @sql nvarchar(max) =''

SELECT @sql = @sql + 'DROP PROCEDURE '+ s.name + '.' + p.name +';'
FROM sys.procedures p
join sys.schemas s
on p.schema_id = s.schema_id

SELECT @sql = @sql + 'DROP TABLE '+ s.name + '.' + t.name +';'
from sys.tables t
join sys.schemas s
on t.schema_id = s.schema_id

SELECT @sql = @sql + 'DROP TYPE '+ s.name + '.' + tt.name +';'
FROM sys.table_types tt
join sys.schemas s
on tt.schema_id = s.schema_id

SELECT @sql = @sql + 'DROP SEQUENCE '+ s.name + '.' + sq.name +';'
FROM sys.sequences sq
join sys.schemas s
on sq.schema_id = s.schema_id

EXEC(@sql)

GO

DROP SCHEMA IF EXISTS dicom

GO

/*************************************************************
    Configure database
**************************************************************/

-- Enable RCSI
IF ((SELECT is_read_committed_snapshot_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON
END

-- Avoid blocking queries when statistics need to be rebuilt
IF ((SELECT is_auto_update_stats_async_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET AUTO_UPDATE_STATISTICS_ASYNC ON
END

-- Use ANSI behavior for null values
IF ((SELECT is_ansi_nulls_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET ANSI_NULLS ON
END

GO

/*************************************************************
    Schema bootstrap
**************************************************************/
CREATE SCHEMA dicom
GO

CREATE TABLE dicom.SchemaVersion
(
    Version int PRIMARY KEY,
    Status varchar(10)
)

INSERT INTO dicom.SchemaVersion
VALUES
    (1, 'started')

GO

--
--  STORED PROCEDURE
--      SelectCurrentSchemaVersion
--
--  DESCRIPTION
--      Selects the current completed schema version
--
--  RETURNS
--      The current version as a result set
--
CREATE PROCEDURE dicom.SelectCurrentSchemaVersion
AS
BEGIN
    SET NOCOUNT ON

    SELECT MAX(Version)
    FROM SchemaVersion
    WHERE Status = 'complete'
END
GO

--
--  STORED PROCEDURE
--      UpsertSchemaVersion
--
--  DESCRIPTION
--      Creates or updates a new schema version entry
--
--  PARAMETERS
--      @version
--          * The version number
--      @status
--          * The status of the version
--
CREATE PROCEDURE dicom.UpsertSchemaVersion
    @version int,
    @status varchar(10)
AS
    SET NOCOUNT ON

    IF EXISTS(SELECT *
        FROM dicom.SchemaVersion
        WHERE Version = @version)
    BEGIN
        UPDATE dicom.SchemaVersion
        SET Status = @status
        WHERE Version = @version
    END
    ELSE
    BEGIN
        INSERT INTO dicom.SchemaVersion
            (Version, Status)
        VALUES
            (@version, @status)
    END
GO
/*************************************************************
    Instance Table
    Dicom instances with unique Study, Series and Instance Uid
**************************************************************/
CREATE TABLE dicom.Instance (
    --instance keys
    StudyInstanceUid        VARCHAR(64) NOT NULL,
    SeriesInstanceUid       VARCHAR(64) NOT NULL,
    SopInstanceUid          VARCHAR(64) NOT NULL,
    --data consitency columns
    Watermark               BIGINT NOT NULL,
    Status                  TINYINT NOT NULL,
    LastStatusUpdatedDate   DATETIME2(7) NOT NULL,
    --audit columns
    CreatedDate             DATETIME2(7) NOT NULL
)

CREATE UNIQUE CLUSTERED INDEX IXC_dicom_Instance on dicom.Instance
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
) 

--Filter indexes
CREATE NONCLUSTERED INDEX IX_dicom_Instance_SeriesInstanceUid_SopInstanceUid on dicom.Instance
(
    SeriesInstanceUid,
    SopInstanceUid
) 
INCLUDE
(
    StudyInstanceUid,
    Status,
    Watermark
)

CREATE NONCLUSTERED INDEX IX_dicom_Instance_SopInstanceUid ON dicom.Instance
(
    SopInstanceUid
) 
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    Status,
    Watermark
)

--Cross apply indexes
CREATE NONCLUSTERED INDEX IX_dicom_Instance_StudyInstanceUid_Status_Watermark on dicom.Instance
(
    StudyInstanceUid,
    Status,
    Watermark DESC
)
INCLUDE
(
    SeriesInstanceUid,
    SopInstanceUid
)

CREATE NONCLUSTERED INDEX IX_dicom_Instance_StudyInstanceUid_SeriesInstanceUid_Status_Watermark on dicom.Instance
(
    StudyInstanceUid,
    SeriesInstanceUid,
    Status,
    Watermark DESC
)
INCLUDE
(
    SopInstanceUid
)

/*************************************************************
    Study Table
    Table containing normalized standard Study tags
**************************************************************/
CREATE TABLE dicom.StudyMetadataCore (
    --Key
    ID                          BIGINT NOT NULL, --PK
    --instance keys
    StudyInstanceUid            VARCHAR(64) NOT NULL,
    Version                     INT NOT NULL,
    --patient and study core
    PatientID                   NVARCHAR(64) NOT NULL,
    PatientName                 NVARCHAR(325) NULL,
    --PatientNameIndex AS REPLACE(PatientName, '^', ' '), --FT index,
    ReferringPhysicianName      NVARCHAR(325) NULL,
    StudyDate                   DATE NULL,
    StudyDescription            NVARCHAR(64) NULL,
    AccessionNumber             NVARCHAR(16) NULL,
)

CREATE UNIQUE CLUSTERED INDEX IXC_dicom_StudyMetadataCore ON dicom.StudyMetadataCore
(
    ID,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_dicom_StudyMetadataCore_PatientID ON dicom.StudyMetadataCore
(
    PatientID
)
INCLUDE
(
    ID,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_dicom_StudyMetadataCore_ReferringPhysicianName ON dicom.StudyMetadataCore
(
    ReferringPhysicianName
)
INCLUDE
(
    ID,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_dicom_StudyMetadataCore_StudyDate ON dicom.StudyMetadataCore
(
    StudyDate
)
INCLUDE

(
    ID,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_dicom_StudyMetadataCore_StudyDescription ON dicom.StudyMetadataCore
(
    StudyDescription
)
INCLUDE
(
    ID,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_dicom_StudyMetadataCore_AccessionNumber ON dicom.StudyMetadataCore
(
    AccessionNumber
)
INCLUDE
(
    ID,
    StudyInstanceUid
)

/*************************************************************
    Series Table
    Table containing normalized standard Series tags
**************************************************************/

CREATE TABLE dicom.SeriesMetadataCore (
    --Key
    ID                                  BIGINT NOT NULL, --FK
    --instance keys
    SeriesInstanceUid                   VARCHAR(64) NOT NULL,
    Version                             INT NOT NULL,
    --series core
    Modality                            NVARCHAR(16) NULL,
    PerformedProcedureStepStartDate     DATE NULL
) 

CREATE UNIQUE CLUSTERED INDEX IXC_dicom_SeriesMetadataCore ON dicom.SeriesMetadataCore
(
    ID,
    SeriesInstanceUid
)

CREATE NONCLUSTERED INDEX IX_dicom_SeriesMetadataCore_Modality ON dicom.SeriesMetadataCore
(
    Modality
)
INCLUDE
(
    ID,
    SeriesInstanceUid
)

CREATE NONCLUSTERED INDEX IX_dicom_SeriesMetadataCore_PerformedProcedureStepStartDate ON dicom.SeriesMetadataCore
(
    PerformedProcedureStepStartDate
)
INCLUDE
(
    ID,
    SeriesInstanceUid
)

GO


/*************************************************************
    Sequence for generating unique ids
**************************************************************/

CREATE SEQUENCE dicom.WatermarkSequence
    AS BIGINT
    START WITH 0
    INCREMENT BY 1
    MINVALUE 0
    NO CYCLE
    CACHE 1000000

CREATE SEQUENCE dicom.MetadataIdSequence
    AS BIGINT
    START WITH 0
    INCREMENT BY 1
    MINVALUE 0
    NO CYCLE
    CACHE 1000000
GO

/*************************************************************
    Stored procedures for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstance
--
-- DESCRIPTION
--     Adds a DICOM instance
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @patientID
--         * The ID of the patient.
--     @patientName
--         * The name of the patient.
--     @referringPhysicianName
--         * The referring physician name.
--     @studyDate
--         * The study date.
--     @studyDescription
--         * The study description.
--     @accessionNumber
--         * The accession number associated for the study.
--     @modality
--         * The modality associated for the series.
--     @performedProcedureStepStartDate
--         * The date when the procedure for the series was performed.
--
-- RETURN VALUE
--         None
--

CREATE PROCEDURE dicom.AddInstance
    @studyInstanceUid VARCHAR(64),
    @seriesInstanceUid VARCHAR(64),
    @sopInstanceUid VARCHAR(64),
    @patientID NVARCHAR(64),
    @patientName NVARCHAR(325) = NULL,
    @referringPhysicianName NVARCHAR(325) = NULL,
    @studyDate DATE = NULL,
    @studyDescription NVARCHAR(64) = NULL,
    @accessionNumber NVARCHAR(64) = NULL,
    @modality NVARCHAR(16) = NULL,
    @performedProcedureStepStartDate DATE = NULL,
    @initialStatus TINYINT
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = GETUTCDATE()
    DECLARE @existingStatus TINYINT
    DECLARE @metadataId BIGINT
    
    IF EXISTS
        (SELECT * 
        FROM dicom.Instance
        WHERE studyInstanceUid = @studyInstanceUid
        AND seriesInstanceUid = @seriesInstanceUid
        AND sopInstanceUid = @sopInstanceUid)
    BEGIN
        THROW 50409, 'Instance already exists', 1;
    END

    -- The instance does not exist, insert it.
    INSERT INTO dicom.Instance
        (studyInstanceUid, seriesInstanceUid, sopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate)
    VALUES
        (@studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, NEXT VALUE FOR dicom.WatermarkSequence, @initialStatus, @currentDate, @currentDate)

    -- Update the study metadata if needed.
    SELECT @metadataId = ID
    FROM dicom.StudyMetadataCore
    WHERE studyInstanceUid = @studyInstanceUid

    IF @@ROWCOUNT = 0
    BEGIN
        SET @metadataId = NEXT VALUE FOR dicom.MetadataIdSequence

        INSERT INTO dicom.StudyMetadataCore
            (ID, studyInstanceUid, Version, PatientID, PatientName, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
        VALUES
            (@metadataId, @studyInstanceUid, 0, @patientID, @patientName, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)
    END
    --ELSE BEGIN
        -- TODO: handle the versioning
    --END

    IF NOT EXISTS (SELECT * FROM dicom.SeriesMetadataCore WHERE ID = @metadataId AND seriesInstanceUid = @seriesInstanceUid)
    BEGIN
        INSERT INTO dicom.SeriesMetadataCore
            (ID, seriesInstanceUid, Version, Modality, PerformedProcedureStepStartDate)
        VALUES
            (@metadataId, @seriesInstanceUid, 0, @modality, @performedProcedureStepStartDate)
    END
    --ELSE BEGIN
        -- TODO: handle the versioning
    --END

    COMMIT TRANSACTION
GO
/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstance
--
-- DESCRIPTION
--     Gets valid dicom instances at study/series/instance level
--
-- PARAMETERS
--     @invalidStatus
--         * Filter criteria to search only valid instances
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE PROCEDURE dicom.GetInstance (
    @invalidStatus      TINYINT,
    @studyInstanceUid	VARCHAR(64),
    @seriesInstanceUid	VARCHAR(64) = NULL,
    @sopInstanceUid		VARCHAR(64) = NULL
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    IF ( @seriesInstanceUid IS NOT NULL AND @sopInstanceUid IS NOT NULL )
    BEGIN
        SELECT  StudyInstanceUid,
                SeriesInstanceUid,
                SopInstanceUid,
                Watermark
        FROM    dicom.Instance
        WHERE   StudyInstanceUid        = @studyInstanceUid
                AND SeriesInstanceUid   = @seriesInstanceUid
                AND SopInstanceUid      = @sopInstanceUid
                AND Status              <> @invalidStatus
    END
    ELSE IF ( @seriesInstanceUid IS NOT NULL )
    BEGIN
        SELECT  StudyInstanceUid,
                SeriesInstanceUid,
                SopInstanceUid,
                Watermark
        FROM    dicom.Instance
        WHERE   StudyInstanceUid        = @studyInstanceUid
                AND SeriesInstanceUid   = @seriesInstanceUid
                AND Status              <> @invalidStatus
    END
    ELSE
    BEGIN
        SELECT  StudyInstanceUid,
                SeriesInstanceUid,
                SopInstanceUid,
                Watermark
        FROM    dicom.Instance
        WHERE   StudyInstanceUid        = @studyInstanceUid
                AND Status              <> @invalidStatus
    END
END
GO
