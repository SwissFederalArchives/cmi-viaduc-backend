/* *** Für UserId Spalten Foreign Keys ergänzen *** */

DELETE FROM DownloadToken WHERE UserId NOT IN (SELECT ID FROM ApplicationUser)

DELETE FROM Favorite WHERE LIST IN (SELECT ID FROM FavoriteList WHERE UserId NOT IN (SELECT ID FROM ApplicationUser))
DELETE FROM FavoriteList WHERE UserId NOT IN (SELECT ID FROM ApplicationUser)

DELETE FROM OrderItem WHERE OrderId IN (SELECT ID FROM Ordering WHERE UserId NOT IN (SELECT ID FROM ApplicationUser))
DELETE FROM Ordering WHERE UserId NOT IN (SELECT ID FROM ApplicationUser)

GO

ALTER TABLE [DownloadToken] WITH CHECK ADD CONSTRAINT FK_DownloadToken_User FOREIGN KEY([UserId]) REFERENCES [ApplicationUser] ([ID])
ALTER TABLE [DownloadToken] CHECK CONSTRAINT FK_DownloadToken_User
GO

ALTER TABLE [FavoriteList] WITH CHECK ADD CONSTRAINT FK_FavoriteList_User FOREIGN KEY([UserId]) REFERENCES [ApplicationUser] ([ID])
ALTER TABLE [FavoriteList] CHECK CONSTRAINT FK_FavoriteList_User
GO

ALTER TABLE [Ordering] WITH CHECK ADD CONSTRAINT FK_Ordering_User FOREIGN KEY([UserId]) REFERENCES [ApplicationUser] ([ID])
ALTER TABLE [Ordering] CHECK CONSTRAINT FK_Ordering_User
GO


/* *** Indexe erstellen für Foreign Keys  *** */

CREATE INDEX IX_ApplicationRoleFeature_InsertedByUserId ON [ApplicationRoleFeature] ([InsertedByUserId])
CREATE INDEX IX_ApplicationRoleFeature_RoleId ON [ApplicationRoleFeature] ([RoleId])
GO

CREATE INDEX IX_ApplicationRoleUser_RoleId ON [ApplicationRoleUser] ([RoleId])
CREATE INDEX IX_ApplicationRoleUser_UserId ON [ApplicationRoleUser] ([UserId])
CREATE INDEX IX_ApplicationRoleUser_InsertedByUserId ON [ApplicationRoleUser] ([InsertedByUserId])
GO

CREATE INDEX IX_ApplicationUserAblieferndeStelle_AblieferndeStelleId ON [ApplicationUserAblieferndeStelle] ([AblieferndeStelleId])
CREATE INDEX IX_ApplicationUserAblieferndeStelle_UserId ON [ApplicationUserAblieferndeStelle] ([UserId])
GO

CREATE INDEX IX_ApproveStatusHistory_OrderItemId ON [ApproveStatusHistory] ([OrderItemId])
GO

CREATE INDEX IX_AsTokenMapping_AblieferndeStelleId ON [AsTokenMapping] ([AblieferndeStelleId])
CREATE INDEX IX_AsTokenMapping_TokenId ON [AsTokenMapping] ([TokenId])
GO

CREATE INDEX IX_DownloadReasonHistory_UserId ON [DownloadReasonHistory] ([UserId])
CREATE INDEX IX_DownloadReasonHistory_ReasonId ON [DownloadReasonHistory] ([ReasonId])
GO

CREATE INDEX IX_DownloadToken_UserId ON [DownloadToken] ([UserId])
GO

CREATE INDEX IX_Favorite_List ON [Favorite] ([List])
GO

CREATE INDEX IX_FavoriteList_UserId ON [FavoriteList] ([UserId])
GO

CREATE INDEX IX_Ordering_ArtDerArbeit ON [Ordering] ([ArtDerArbeit])
CREATE INDEX IX_Ordering_UserId ON [Ordering] ([UserId])
GO

CREATE INDEX IX_OrderItem_Reason ON [OrderItem] ([Reason])
CREATE INDEX IX_OrderItem_OrderId ON [OrderItem] ([OrderId])
GO

CREATE INDEX IX_StatusHistory_OrderItemId ON [StatusHistory] ([OrderItemId])
GO
