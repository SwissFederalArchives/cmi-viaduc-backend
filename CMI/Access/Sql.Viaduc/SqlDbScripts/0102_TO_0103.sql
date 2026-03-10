CREATE OR ALTER View v_SyncNumberPerHour 
AS
SELECT Format(ISNULL([ModifiedOn], [CreatedOn]), 'yyyy-MM-dd HH') AS LastModified,
       CONVERT(DATE, ISNULL([ModifiedOn], [CreatedOn])) as LastModifiedDay,
       COUNT([SyncActionId]) AS RecordCount,
       [ActionStatus]
FROM [SyncAction]
GROUP BY Format(ISNULL([ModifiedOn], [CreatedOn]), 'yyyy-MM-dd HH'),
 CONVERT(DATE, ISNULL([ModifiedOn], [CreatedOn])) ,
         [ActionStatus];
GO
