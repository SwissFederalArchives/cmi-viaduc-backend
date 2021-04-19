
-- Spalten hinzufügen
ALTER TABLE DownloadReasonHistory ADD 

	Signatur NVARCHAR(MAX) NULL,
	Dossiertitel NVARCHAR(MAX) NULL,
	Aktenzeichen NVARCHAR(MAX) NULL,
	Entstehungszeitraum NVARCHAR(MAX) NULL,
	Bestand NVARCHAR(MAX) NULL,
	Teilbestand NVARCHAR(MAX) NULL,
	Ablieferung NVARCHAR(MAX) NULL,
	ZustaendigeStelleVe NVARCHAR(MAX) NULL,
	Schutzfristverzeichnung NVARCHAR(MAX) NULL,
	ZugaenglichkeitGemaessBga NVARCHAR(MAX) NULL,
	
	FirstName NVARCHAR(200) NULL,
	FamilyName NVARCHAR(200) NULL,
	Organization NVARCHAR(200) NULL,
	EmailAddress NVARCHAR(200) NULL,
	RolePublicClient NVARCHAR(3) NULL,
	AsAccessTokensUser NVARCHAR(MAX) NULL,
	ZustaendigeStellenUser NVARCHAR(MAX) NULL
