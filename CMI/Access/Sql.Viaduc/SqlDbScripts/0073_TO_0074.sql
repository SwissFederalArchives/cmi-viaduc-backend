/* ---------------------------------------------------------------------- */
/* Default von 180 auf 90 Tage ändern                                     */
/* ---------------------------------------------------------------------- */

GO

ALTER TABLE OrderItem DROP CONSTRAINT DF_OrderItem_Ausleihdauer
GO

ALTER TABLE OrderItem ADD CONSTRAINT DF_OrderItem_Ausleihdauer DEFAULT 90 FOR Ausleihdauer
GO

