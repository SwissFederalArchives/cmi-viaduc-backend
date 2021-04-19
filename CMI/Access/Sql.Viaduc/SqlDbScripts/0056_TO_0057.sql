ALTER TABLE ApplicationUser
DROP COLUMN AsTokens
GO
DELETE FROM ApproveStatusHistory WHERE  OrderItemId In 
( SELECT ID FROM OrderItem
  WHERE Status=6 
  AND ((TerminDigitalisierung is null) OR (DigitalisierungsKategorie <= 0))
)
GO
DELETE FROM StatusHistory WHERE  OrderItemId In 
( SELECT ID FROM OrderItem
  WHERE Status=6 
  AND ((TerminDigitalisierung is null) OR (DigitalisierungsKategorie <= 0))
)
GO
DELETE FROM OrderItem
  WHERE Status=6 
  AND ((TerminDigitalisierung is null) OR (DigitalisierungsKategorie <= 0))
GO
ALTER TABLE dbo.OrderItem ADD CONSTRAINT
	CK_OrderItem_ManadatoryFieldsWhenDigitalisierungBereit CHECK (Status <> 6 OR ((TerminDigitalisierung is not null ) AND (DigitalisierungsKategorie > 0 )))
GO

