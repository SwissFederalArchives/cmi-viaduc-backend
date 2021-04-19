# Connection to the Archive Information System (AIS).

The AIS system can be replaced. The system is designed to create a generalized representation a unit of description record from the AIS data and then use it in the system. Of course, the current system is aligned with the scopeArchiv AIS system, and connecting to a scopeArchiv database should be possible via configuration file adjustments only and without changing the code.

> **Note**: Since scopeArchiv is a commercial product, we do not provide SQL statements in the source code that are used to access the database. This is for legal reasons. <br/>
Placeholders for all SQL statements can be found in the file `SqlStatements.cs` in the project `CMI.Access.Harvest`.


## Connection to scopeArchive
To connect the solution to another scopeArchive configuration, the following must be considered:

### Elasticsearch configuration.
The Elasticsearch server, respectively the index created in it, must be customized based on the scopeArchiv database configuration. It is a matter of determining which fields should be stored and in what way. Which fields should be copied to the "all" field, so that their content can be found in the search field. This configuration is stored in the file `ElasticRecordMapping.json`. 

This file must be maintained "manually" and the index must be created manually in the Elasticsearch server before a synchronization is started.

### Definition of the fields to be transferred
Depending on your wishes, maybe not all fields that are available in scopeArchive should be transferred. In the file `customFieldsConfig.json` you can define which fields should be exported. Fields that do not appear in this list will not be included in the synchronization process.

### Display forms
The display of the details of a archival unit in the web application is done according to a form definition. By default, the same form definition is used as it is defined in scopeArchive. However, this is not queried in realtime accessing the scopeArchiv database. There is however a file called `templates.json` which contains the different form definitions. For the generation of the file `templates.json` the console application `CMI.Utilities.FormTemplate.Helper` can be found within the code.

If desired, however, an independent form definition can be created, or depending upon your wishes certain fields can be hidden within a form. Which record uses which form is handled by the 'FormId' field. 


## Connection to other AIS

### Implementation of interfaces
A connection to another AIS is possible. The interfaces of the project `CMI.Contract.Harvest` are to be implemented in the data access component `CMI.Access.Harvest`. These are:
* IDbMetadataAccess
* IDbDigitizationOrderAccess
* IDbTestAccess

and if necessary

* IDbResyncAccess
* IDbMutationQueueAccess
* IDbStatusAccess

The latter interfaces relate directly to the way mutations are reported in scopeArchive. In an own implementation this can be solved similarly, or completely differently. Depending on the implementation these interfaces may or may not be needed.

It is important that the returned object for a UoD is created according to the schema 'ViaducDataSchema.xsd'. This schema defines the data structure `ArchiveRecord`, which has universal validity. If the AIS returns this data structure for a archive record to be synchronized, the task is completed.

### Elasticsearch Configuration
The configuration of the Elasticsearch index is identical to that of scopeArchive.
The Elasticsearch server, respectively the index created in it must be adjusted based on the configuration of the AIS. 
See the [chapter of the same name under "scopeArchiv"](###-Elasticsearch configuration).

### Display forms
Identical to what is described in the [scopeArchive / Display Forms](###-Display Forms) chapter, the form definitions must also be recorded in the `templates.json` file for another AIS. These are also created using the `FormId` fields of the template file and the `externalDisplayTemplateName` field of the `ArchiveRecord`.
If the AIS does not have a direct FormId, one must be defined and coded accordingly in the metadata.

