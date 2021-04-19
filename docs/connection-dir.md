# Connection to the Digital Information Repository (DIR).

The commercial system Preservica is used in the Swiss Federal Archive. This system is custom tailored for the Federal Archives.
Large parts of the given codebase are trimmed to this system/configuration and cannot be used directly. 

It is mandatory to make adjustments in the following projects.
* CMI.Access.Repository
* CMI.Engine.PackageMetadata
* CMI.Manager.Repository


Access to the repository is made through a [CMIS interface](https://en.wikipedia.org/wiki/Content_Management_Interoperability_Services). 
For local development, the open-source CMS system [Alfresco](https://www.alfresco.com/) can be used. Not all use cases can be implemented with this, but it can at least be used to get started.

If the repository you use also provides a CMIS interface, then it is sufficient to adapt the existing code in the necessary places. The biggest changes should be in the project `CMI.Engine.PackageMetadata` where the metadata is read and prepared according to the Arelda standard.

If the repository used does not offer a CMIS interface, the present projects can serve as examples. The same actions must be reprogrammed analogously. It is important to keep in mind that the same messages must be sent via the bus at the end.