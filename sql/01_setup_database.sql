USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'TaxiDb')
BEGIN
    CREATE DATABASE TaxiDb;
END
GO

USE TaxiDb;
GO

DROP TABLE IF EXISTS dbo.TaxiTrips;
GO

CREATE TABLE dbo.TaxiTrips
(
    id                    BIGINT         NOT NULL IDENTITY(1,1),
    tpep_pickup_datetime  DATETIME2      NOT NULL,
    tpep_dropoff_datetime DATETIME2      NOT NULL,
    passenger_count       TINYINT        NOT NULL,
    trip_distance         DECIMAL(10, 2) NOT NULL,
    store_and_fwd_flag    VARCHAR(3)     NOT NULL,
    PULocationID          INT            NOT NULL,
    DOLocationID          INT            NOT NULL,
    fare_amount           DECIMAL(10, 2) NOT NULL,
    tip_amount            DECIMAL(10, 2) NOT NULL,

    CONSTRAINT PK_TaxiTrips PRIMARY KEY CLUSTERED (id ASC)
        WITH (FILLFACTOR = 90)
);
GO

ALTER TABLE dbo.TaxiTrips
    ADD trip_duration_seconds AS
        DATEDIFF(SECOND, tpep_pickup_datetime, tpep_dropoff_datetime) PERSISTED;
GO

CREATE NONCLUSTERED INDEX IX_TaxiTrips_PULocation_Tip
    ON dbo.TaxiTrips (PULocationID ASC)
    INCLUDE (tip_amount);

CREATE NONCLUSTERED INDEX IX_TaxiTrips_TripDistance_Desc
    ON dbo.TaxiTrips (trip_distance DESC);

CREATE NONCLUSTERED INDEX IX_TaxiTrips_Duration_Desc
    ON dbo.TaxiTrips (trip_duration_seconds DESC);

CREATE NONCLUSTERED INDEX IX_TaxiTrips_PULocation_Pickup
    ON dbo.TaxiTrips (PULocationID ASC, tpep_pickup_datetime ASC)
    INCLUDE (tpep_dropoff_datetime, passenger_count, trip_distance,
             fare_amount, tip_amount, store_and_fwd_flag, DOLocationID);
GO