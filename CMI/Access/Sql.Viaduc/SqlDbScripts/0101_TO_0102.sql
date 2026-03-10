/* ---------------------------------------------------------------------- */
/* Add indices to table SyncAction                                        */
/* ---------------------------------------------------------------------- */



CREATE NONCLUSTERED INDEX [IDX_SyncAction_6] ON [SyncAction] ([CreatedOn] ASC);
GO

CREATE NONCLUSTERED INDEX [IDX_SyncAction_7]
ON [SyncAction] ([CreatedOn] ASC, [ModifiedOn] ASC, [ActionStatus] ASC, [NumberOfTries] ASC);
GO


/* ---------------------------------------------------------------------- */
/* Add indices to table  SyncActionLog                                    */
/* ---------------------------------------------------------------------- */

CREATE NONCLUSTERED INDEX IDX_SyncActionLog_2
ON SyncActionLog (SyncActionId DESC, SyncActionLogId DESC) INCLUDE (LogDate, ErrorReason, ActionStatusHistory);
GO

/* ---------------------------------------------------------------------- */
/* Alter SyncAction view to use Outer Apply to improve perfomance         */
/* ---------------------------------------------------------------------- */

CREATE OR ALTER View v_SyncAction
AS
SELECT
    s.SyncActionId,
    s.ArchiveRecordId,
    s.ActionType,
    s.ActionStatus,
    s.NumberOfTries,
    s.CreatedOn,
    s.ModifiedOn,
    sal.SyncActionLogId,
    sal.LogDate,
    sal.ErrorReason,
    CASE
        WHEN sal.ErrorReason IS NULL THEN sal.ActionStatusHistory
        ELSE CONCAT(sal.ActionStatusHistory, ' - ', sal.ErrorReason)
    END AS ActionStatusHistory
FROM SyncAction s
OUTER APPLY (
    SELECT TOP 1 
        l.SyncActionLogId,
        l.LogDate,
        l.ErrorReason,
        l.ActionStatusHistory
    FROM SyncActionLog l
    WHERE l.SyncActionId = s.SyncActionId
    ORDER BY l.SyncActionLogId DESC
) sal     
GO