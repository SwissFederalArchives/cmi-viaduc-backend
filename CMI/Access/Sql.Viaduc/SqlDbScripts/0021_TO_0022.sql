-- ---------------------------------------------------------------------------------------
-- Neue Tabelle
-- ---------------------------------------------------------------------------------------

CREATE TABLE [dbo].[DownloadToken]
(
	Token NVARCHAR(100) NOT NULL CONSTRAINT PK_FileToken PRIMARY KEY CLUSTERED,
	ArchiveRecordId INT NOT NULL,
	ExpiryTime DATETIME2 NOT NULL,
	IpAdress NVARCHAR(max) NOT NULL,
);
GO
