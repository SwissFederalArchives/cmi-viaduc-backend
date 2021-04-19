UPDATE OrderItem Set ApproveStatus = 0 where ApproveStatus is null
GO
ALTER TABLE OrderItem ALTER COLUMN ApproveStatus integer not null
GO
ALTER TABLE OrderItem ADD CONSTRAINT DF_ApproveStatus DEFAULT 0 FOR ApproveStatus;
GO
UPDATE ApproveStatusHistory SET ApproveFromStatus=0 WHERE ApproveFromStatus IS NULL
GO
ALTER TABLE ApproveStatusHistory ALTER COLUMN ApproveFromStatus INTEGER NOT NULL
GO
UPDATE ApproveStatusHistory SET ApprovedTo='???' WHERE ApprovedTo IS NULL
GO
ALTER TABLE ApproveStatusHistory ALTER COLUMN ApprovedTo nvarchar(200) NOT NULL
GO
UPDATE ApproveStatusHistory SET ApprovedFrom='???' WHERE ApprovedFrom IS NULL
GO
ALTER TABLE ApproveStatusHistory ALTER COLUMN ApprovedFrom nvarchar(200) NOT NULL

