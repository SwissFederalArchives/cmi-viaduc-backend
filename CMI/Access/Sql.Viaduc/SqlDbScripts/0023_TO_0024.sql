ALTER TABLE [dbo].[Ordering] ADD [BegruendungAngegeben] NVARCHAR(2000) NULL
ALTER TABLE [dbo].[OrderItem] ADD [HasEigenePersonendaten] BIT DEFAULT(0) NOT NULL