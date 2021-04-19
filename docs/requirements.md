# Requirements

Viaduc is a software system that consists of various services and applications and is in turn integrated into various surrounding systems.

## Development environment

It is recommended to use Visual Studio 2019. The following components to be licensed are used:

* Devart dotConnect for Oracle
* Rebex SFTP Library
* Aspose Total for .NET (Imaging and PDF are used).

**Important information:** Without the licenses for these components, the solutions won't run successfully

## Services and components used

| Name | Description |
| --- | --- |
| [RabbitMq](https://www.rabbitmq.com/) | RabbitMq is an open source message bus. All services of the solution communicate via the message bus. |
| [Elastic Search](https://www.elastic.co/) | For full-text search and storage of data from the AIS/DIR |
| [MS SQL Server](https://www.microsoft.com/de-ch/sql-server/sql-server-downloads) | For the storage of users, orders, insight requests and other data. |
| [Abbyy](https://www.abbyy.com/ocr-sdk) | For OCR recognition of primary data. To convert TIFF/JP2 files to PDF with a text layer |
| Mail Server | For sending e-mails using an SMTP mail server.    |
| SFTP Server | To copy data exported from the digital repository (DIP) to other servers an SFTP server is required.     |

MS SQL Server and Abbyy are commercial products. For the licensing of these products, contact the respective manufacturer.

## External peripheral systems

| Name | Description |
| --- | --- |
| scopeArchiv (AIS) | The descriptive metadata of the archival records are kept in an AIS.<br/>The Swiss Federal Archives uses the commercial product *scopeArchiv* of the company *scope solutions*. This AIS can be configured dynamically, so that no customer installation is identical. <br/>If the system should also be connected to a scopeArchiv AIS, this can be done relatively easily by adjusting the configuration.<br/>**Note:** Since scopeArchiv is a commercial product, the SQL commands for accessing the data from the repository have been removed and replaced by placeholders. This is to ensure that no legal problems arise.<br/><br/>**Connection other AIS:** The code is abstracted in order to facilitate connection to a different AIS system.
| Digital Repository DIR) | The primary data is stored in the Swiss Federal Archives in a digital repository of the company Preservica. When a user requests access to the data, it is exported from the system, transformed according to archival standards, and converted into a so-called working copy. The access is done through a CMIS interface and thus the access to a CMIS compatible system can be similar. However, the code for creating the working copy is very specific to the SFA and certainly needs to be revised. |
| eIAM | The identification of users in the public, as well as in the internal management client is done via the identity access management solution of the federal government. This is a SAML-2 based system. A connection to another external identity management system is, of course, possible.    |

# Limitations

As already mentioned in the introduction, it is not possible to use the code 1:1 and compile it directly after forking. The code **MUST** be adapted to your own environment and systems. This includes (not exhaustive):

* Connection to your own AIS. Even if _scopeArchiv_ is used, various configurations must be adapted. E.g., the file `customFieldsConfig.json` and `templates.json`. For the generation of the file `templates.json` the console application `CMI.Utilities.FormTemplate.Helper` can be found within the repository.
* Connection to your own digital repository. The SFA uses  their own  metadata standard/schema called *Arelda*. The access, like the internal structure of the DIP has to be adapted according to your requirements.
* The Asset Manager service handles the conversion of the data exported from the DIR into a working copy (DIP). Depending on the metadata standard and requirements, the conversion process must be adapted.
* In some cases, commercial products are used. These must either be replaced, or also licensed. The license keys required for these products have been moved to configuration files and replaced by dummy entries.
  * Rebex SFTP Server (https://www.rebex.net/buru-sftp-server/)
  * Aspose (https://purchase.aspose.com/pricing/total/net)
  * Devart dotConnect for Oracle (https://www.devart.com/dotconnect/oracle/ordering.html)
  * Abbyy (https://www.abbyy.com/ocr-sdk/)
