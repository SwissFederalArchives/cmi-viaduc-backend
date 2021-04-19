========================================
This project is simply a helper utility
========================================

Purpose of this tool is to populate a digital repository supporting the CMIS interface with test data.
This test data is required in order to completely program and test the workflow.
As the DIR of the customer (BAR) is a closed software product, we use Alfresco as a mock replacement.

This utility conntects to the current AIS and checks how many AIP@DossierId entries are found.
Then it checks if for each of those entries a entry exists in Alfresco. If yes, everything is fine.
If no, it will randomly create directories and files that later on will build the "digial package"
for a given archive record.