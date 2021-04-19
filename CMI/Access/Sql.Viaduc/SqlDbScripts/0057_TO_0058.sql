
/* Created By / Inserted By aus ApplicationRoleFeature entfernen */
ALTER TABLE ApplicationRoleFeature DROP CONSTRAINT DF_ApplicationRoleFeature_Inserted
ALTER TABLE ApplicationRoleFeature DROP CONSTRAINT FK_ApplicationRoleFeature_ApplicationUser_InsertedByUserId
DROP INDEX IX_ApplicationRoleFeature_InsertedByUserId ON ApplicationRoleFeature;  

ALTER TABLE ApplicationRoleFeature DROP COLUMN InsertedByUserId
ALTER TABLE ApplicationRoleFeature DROP COLUMN Inserted

/* Created By / Inserted By aus ApplicationRoleUser entfernen */
ALTER TABLE ApplicationRoleUser DROP CONSTRAINT FK_ApplicationRoleUser_ApplicationUser_InsertedByUserId
Alter TABLE ApplicationRoleUser DROP CONSTRAINT DF_ApplicationRoleUser_Inserted
DROP INDEX IX_ApplicationRoleUser_InsertedByUserId ON ApplicationRoleUser;  

ALTER TABLE ApplicationRoleUser DROP COLUMN InsertedByUserId
ALTER TABLE ApplicationRoleUser DROP COLUMN Inserted

/* User 'Administrator' entfernen, weil dieser zu Problemen führt und nicht gebraucht wird */
DECLARE @UserID AS nvarchar(200);
SET @UserID = 'Administrator'; 

DELETE ApplicationRoleUser WHERE UserId = @UserID
DELETE ApplicationUserAblieferndeStelle WHERE UserId = @UserID
DELETE StatusHistory WHERE OrderItemId IN (SELECT ID FROM OrderItem WHERE  OrderId IN ( SELECT ID FROM Ordering WHERE UserId = @UserID))
DELETE ApproveStatusHistory WHERE OrderItemId IN (SELECT ID FROM OrderItem WHERE  OrderId IN ( SELECT ID FROM Ordering WHERE UserId = @UserID))
DELETE OrderItem WHERE OrderId IN (SELECT ID FROM Ordering WHERE UserId = @UserID)
DELETE Ordering WHERE UserId = @UserID
DELETE UsageStatisticDisplay WHERE UserId = @UserID
DELETE UsageStatisticDownload WHERE UserId = @UserID
DELETE UserRoleHistory WHERE ApplicationUserId = @UserID
DELETE DownloadReasonHistory WHERE UserId = @UserID
DELETE DownloadLog WHERE UserId = @UserID

DELETE Favorite WHERE List IN (SELECT ID FROM FavoriteList WHERE UserId = @UserID)
DELETE FavoriteList WHERE UserId = @UserID
DELETE ApplicationUser WHERE ID = @UserID
