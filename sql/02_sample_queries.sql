USE TaxiDb;
GO

-- Find out which PULocationId has the highest tip_amount on average.
SELECT TOP 1
    PULocationID,
    AVG(tip_amount) AS avg_tip_amount
FROM TaxiTrips
GROUP BY PULocationID
ORDER BY avg_tip_amount DESC;

-- Find the top 100 longest fares in terms of trip_distance.
SELECT TOP 100 *
FROM TaxiTrips
ORDER BY trip_distance DESC;

-- Find the top 100 longest fares in terms of time spent traveling.
SELECT TOP 100 *
FROM TaxiTrips
ORDER BY trip_duration_seconds DESC;

-- Search where part of the conditions is PULocationId.
SELECT *
FROM TaxiTrips
WHERE PULocationID IN (100, 200)
  AND tip_amount > 0;