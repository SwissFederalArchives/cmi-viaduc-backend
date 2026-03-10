CREATE OR ALTER View v_SyncAction
AS
SELECT
SyncAction.SyncActionId as SyncActionId,
       SyncAction.ArchiveRecordId ,
       SyncAction.ActionType ,
       SyncAction.ActionStatus ,
       SyncAction.NumberOfTries ,
       SyncAction.CreatedOn ,
       SyncAction.ModifiedOn ,
       sal.SyncActionLogId  as SyncActionLogId,
  (SELECT LogDate
   FROM syncActionLog
   WHERE  SyncActionLogId = sal.SyncActionLogId) AS LogDate,
  (SELECT ErrorReason
   FROM syncActionLog
   WHERE SyncActionLogId = sal.SyncActionLogId)  AS ErrorReason,
  (SELECT CASE
              WHEN errorReason IS NULL THEN ActionStatusHistory
              ELSE Concat(ActionStatusHistory, ' - ', errorReason)
          END
   FROM syncActionLog
   WHERE SyncActionLogId = sal.SyncActionLogId) AS ActionStatusHistory
FROM SyncAction
LEFT JOIN SyncActionLog AS sal ON SyncAction.SyncActionId = sal.SyncActionId
WHERE sal.SyncActionLogId IS NULL
  OR sal.SyncActionLogId =
    (SELECT max(syncActionLogId)
     FROM syncActionLog
     WHERE SyncActionId = SyncAction.SyncActionId) 
     
GO