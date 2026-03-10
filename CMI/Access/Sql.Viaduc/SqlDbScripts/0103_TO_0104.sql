/* ---------------------------------------------------------------------- */
/* Alter table "dbo.ManuelleKorrektur"                                    */
/* ---------------------------------------------------------------------- */

ALTER TABLE [ManuelleKorrektur] ALTER COLUMN [ZuständigeStelle] NVARCHAR(255) NULL
GO

/* ---------------------------------------------------------------------- */
/* Script increases the length of ZuständigeStelle to nvarchar(510)       */
/* Created on:            2025-10-17 12:05                                */
/* ---------------------------------------------------------------------- */

/* ---------------------------------------------------------------------- */
/* Rebuild the view "dbo.v_ManuelleKorrektur"                             */
/* ---------------------------------------------------------------------- */

CREATE OR ALTER VIEW [dbo].[v_ManuelleKorrektur]
AS
SELECT    
    ManuelleKorrekturId, 
    VeId, 
    Signatur, 
    Schutzfristende, 
    Titel, 
    ErzeugtAm, 
    ErzeugtVon, 
    GeändertAm, 
    GeändertVon, 
    Anonymisierungsstatus, 
    Kommentar, 
    Hierachiestufe, 
    Aktenzeichen, 
    Entstehungszeitraum, 
    ZugänglichkeitGemässBGA, 
    Schutzfristverzeichnung, 
    CAST(ZuständigeStelle AS NVARCHAR(255)) AS ZuständigeStelle,  
    Publikationsrechte, 
    AnonymisiertZumErfassungszeitpunk,

    (SELECT Original
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'Titel' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS TitelGemAIS,
    (SELECT Automatisch
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'Titel' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS TitelAutomatischAnonymisiert,
    (SELECT Manuell
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'Titel' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS TitelManuellKorrigiert,

    (SELECT Original
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'Darin' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS DarinGemAIS,
    (SELECT Automatisch
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'Darin' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS DarinAutomatischAnonymisiert,
    (SELECT Manuell
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'Darin' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS DarinManuellKorrigiert,

    (SELECT Original
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'ZusatzkomponenteZac1' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS ZusatzkomponenteGemAIS,
    (SELECT Automatisch
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'ZusatzkomponenteZac1' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS ZusatzkomponenteAutomatischAnonymisiert,
    (SELECT Manuell
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'ZusatzkomponenteZac1' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS ZusatzkomponenteManuellKorrigiert,

    (SELECT Original
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'BemerkungZurVe' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS ZusaetzlicheInformationenGemAIS,
    (SELECT Automatisch
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'BemerkungZurVe' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS ZusaetzlicheInformationenAutomatischAnonymisiert,
    (SELECT Manuell
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'BemerkungZurVe' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS ZusaetzlicheInformationenManuellKorrigiert,

    (SELECT Original
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'VerwandteVE' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS VerwandteVEGemAIS,
    (SELECT Automatisch
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'VerwandteVE' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS VerwandteVEAutomatischAnonymisiert,
    (SELECT Manuell
        FROM dbo.ManuelleKorrekturFeld 
        WHERE Feldname = 'VerwandteVE' 
        AND ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId) AS VerwandteVEManuellKorrigiert
FROM dbo.ManuelleKorrektur;
GO
