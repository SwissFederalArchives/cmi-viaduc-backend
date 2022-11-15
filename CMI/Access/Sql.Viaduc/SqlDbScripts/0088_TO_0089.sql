/* ---------------------------------------------------------------------- */
/* Add table "dbo.v_ManuelleKorrektur"                                             */
/* ---------------------------------------------------------------------- */
GO

CREATE  OR ALTER View v_ManuelleKorrektur
AS
SELECT    ManuelleKorrekturId, VeId, Signatur, Schutzfristende, Titel, ErzeugtAm, ErzeugtVon, GeändertAm, GeändertVon, Anonymisierungsstatus, Kommentar, Hierachiestufe, Aktenzeichen, Entstehungszeitraum, 
                      ZugänglichkeitGemässBGA, Schutzfristverzeichnung, ZuständigeStelle, Publikationsrechte, AnonymisiertZumErfassungszeitpunk,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_1
                            WHERE      (Feldname = 'Titel') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS TitelGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_2
                            WHERE      (Feldname = 'Titel') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS TitelAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_3
                            WHERE      (Feldname = 'Titel') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS TitelManuellKorrigiert,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_4
                            WHERE      (Feldname = 'Darin') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS DarinGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_5
                            WHERE      (Feldname = 'Darin') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS DarinAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_6
                            WHERE      (Feldname = 'Darin') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS DarinManuellKorrigiert,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_7
                            WHERE      (Feldname = 'Zusatzkomponente') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusatzkomponenteGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_8
                            WHERE      (Feldname = 'Zusatzkomponente') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusatzkomponenteAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_9
                            WHERE      (Feldname = 'Zusatzkomponente') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusatzkomponenteManuellKorrigiert,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_10
                            WHERE      (Feldname = 'ZusätzlicheInformationen') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusaetzlicheInformationenGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_11
                            WHERE      (Feldname = 'ZusätzlicheInformationen') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusaetzlicheInformationenAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_12
                            WHERE      (Feldname = 'ZusätzlicheInformationen') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusaetzlicheInformationenManuellKorrigiert,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_13
                            WHERE      (Feldname = 'VerwandteVE') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS VerwandteVEGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_14
                            WHERE      (Feldname = 'VerwandteVE') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS VerwandteVEAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_15
                            WHERE      (Feldname = 'VerwandteVE') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS VerwandteVEManuellKorrigiert
FROM         dbo.ManuelleKorrektur

