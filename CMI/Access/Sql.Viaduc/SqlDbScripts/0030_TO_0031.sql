
-- Bereinigung von Aufträgen, die bei noch nicht kompletter Implementierung erstellt wurden 		

UPDATE OrderItem
SET Status = 5         
WHERE Status = 6 AND 
		(TerminDigitalisierung IS NULL OR DigitalisierungsKategorie IS NULL)