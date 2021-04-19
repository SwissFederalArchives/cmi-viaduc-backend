# Viaduc Oracle Database Objects

This folder contains the scripts that create the database objects that are required for the synchronisation of a scopeArchiv Oracle database.

As scopeArchiv is a commercial system, we ommit the actual scripts for legal reasons.

## Required tables

The synchronisation relies on a mutation table, that contains the Id numbers of the archival records that either must be updated or deleted. The DataFeed service queries this table to find the records that must be synced.

A second table, basically a log table, is used to record status change information for each mutation record and detailed error information in case of a syncronisation failure.


## SQL Starting point
Below you find the DDL statements to create the necessary tables upon which the webOZ relies. 

What we omit are the triggers that we have created in the existing scopeArchiv database. These triggers detect when a record needs to be updated and then creates an entry in the mutation table (tbk_viaduc_mttn). This work needs to be done by yourself...

```

-- !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
-- IMPORTANT
-- Replace <APPSCHEMA>, <TS_DATA>, <TS_IDX>, <TS_TEMP>, <RL_ARCHV_USR> in this script 
-- with the names of the schema user or tablespaces you want to use.
-- !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

--------------------------------------------------------
-- Delete objects if they exist
--------------------------------------------------------
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE TBK_VIADUC_MTTN_AKTN';
EXCEPTION
   WHEN OTHERS THEN
      NULL;
END;
/

BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE TBK_VIADUC_MTTN';
EXCEPTION
   WHEN OTHERS THEN
      NULL;
END;
/

BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE SQK_VIADUC_MTTN_AKTN';
EXCEPTION
   WHEN OTHERS THEN
      NULL;
END;
/

BEGIN
   EXECUTE IMMEDIATE 'DROP SEQUENCE SQK_VIADUC_MTTN';
EXCEPTION
   WHEN OTHERS THEN
      NULL;
END;
/


--------------------------------------------------------
-- Create Sequences for auto incrementing identifiers
--------------------------------------------------------

CREATE SEQUENCE SQK_VIADUC_MTTN
  START WITH 1
  MAXVALUE 999999999999999999999999999
  MINVALUE 1
  NOCYCLE
  CACHE 100
  ORDER;

  
CREATE SEQUENCE SQK_VIADUC_MTTN_AKTN
  START WITH 1
  MAXVALUE 999999999999999999999999999
  MINVALUE 1
  NOCYCLE
  CACHE 100
  ORDER;  

--------------------------------------------------------
-- Create tables
--------------------------------------------------------

CREATE TABLE TBK_VIADUC_MTTN
(
  MTTN_ID         NUMBER(10)                    NOT NULL,
  GSFT_OBJ_ID     NUMBER(10)                    NOT NULL,
  AKTN            VARCHAR2(10 CHAR)             NOT NULL,
  AKTN_STTS       NUMBER(2)                     DEFAULT 0                     NOT NULL,
  SYNC_ANZ_VRSCH  NUMBER(5)                     DEFAULT 0                     NOT NULL,
  ERFSG_DT        DATE                          DEFAULT sysdate               NOT NULL,
  MTTN_DT         DATE                          DEFAULT sysdate               NOT NULL
)
TABLESPACE <TS_DATA>
PCTUSED    0
PCTFREE    10
INITRANS   1
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
LOGGING 
NOCOMPRESS 
NOCACHE
NOPARALLEL
MONITORING;

CREATE TABLE TBK_VIADUC_MTTN_AKTN
(
  MTTN_AKTN_ID    NUMBER(10)                    NOT NULL,
  MTTN_ID         NUMBER(10)                    NOT NULL,
  AKTN_STTS_DT    DATE                          DEFAULT sysdate               NOT NULL,
  AKTN_STTS_HIST  VARCHAR2(200 CHAR),
  ERROR_GRND      VARCHAR2(4000 CHAR)
)
TABLESPACE <TS_DATA>
PCTUSED    0
PCTFREE    10
INITRANS   1
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
LOGGING 
NOCOMPRESS 
NOCACHE
NOPARALLEL
MONITORING;


--------------------------------------------------------
-- Create primary keys
--------------------------------------------------------

CREATE UNIQUE INDEX CPK_VIADUC_MTTN ON TBK_VIADUC_MTTN
(MTTN_ID)
NOLOGGING
TABLESPACE <TS_IDX>
PCTFREE    10
INITRANS   2
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
NOPARALLEL;

CREATE UNIQUE INDEX CPK_VIADUC_MTTN_AKTN ON TBK_VIADUC_MTTN_AKTN
(MTTN_AKTN_ID)
NOLOGGING
TABLESPACE <TS_IDX>
PCTFREE    10
INITRANS   2
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
NOPARALLEL;

ALTER TABLE TBK_VIADUC_MTTN ADD (
  CONSTRAINT CPK_VIADUC_MTTN_PK
 PRIMARY KEY
 (MTTN_ID));
 
 ALTER TABLE TBK_VIADUC_MTTN_AKTN ADD (
  CONSTRAINT CPK_VIADUC_MTTN_AKTN
 PRIMARY KEY
 (MTTN_AKTN_ID)
    USING INDEX 
    TABLESPACE <TS_IDX>
    PCTFREE    10
    INITRANS   2
    MAXTRANS   255
    STORAGE    (
                INITIAL          64K
                MINEXTENTS       1
                MAXEXTENTS       UNLIMITED
                PCTINCREASE      0
               ));


--------------------------------------------------------
-- Add foreign key constraints
--------------------------------------------------------			   
			   
ALTER TABLE TBK_VIADUC_MTTN_AKTN ADD (
  CONSTRAINT FK_VIADUC_MTTN 
 FOREIGN KEY (MTTN_ID) 
 REFERENCES TBK_VIADUC_MTTN (MTTN_ID)
    ON DELETE CASCADE); 
	
	
--------------------------------------------------------
-- Add indexes
--------------------------------------------------------

CREATE INDEX IDX_VIADUC_MTTN_1 ON TBK_VIADUC_MTTN
(GSFT_OBJ_ID, AKTN_STTS, AKTN)
LOGGING
TABLESPACE <TS_IDX>
PCTFREE    10
INITRANS   2
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
NOPARALLEL;


CREATE INDEX IDX_VIADUC_MTTN_2 ON TBK_VIADUC_MTTN
(GSFT_OBJ_ID, AKTN_STTS)
LOGGING
TABLESPACE <TS_IDX>
PCTFREE    10
INITRANS   2
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
NOPARALLEL;

CREATE INDEX IDX_VIADUC_MTTN_3 ON TBK_VIADUC_MTTN
(AKTN_STTS)
LOGGING
TABLESPACE <TS_IDX>
PCTFREE    10
INITRANS   2
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
NOPARALLEL;

CREATE INDEX IDX_VIADUC_MTTN_4 ON TBK_VIADUC_MTTN 
(MTTN_DT, AKTN_STTS) 
LOGGING
TABLESPACE <TS_IDX>
PCTFREE    10
INITRANS   2
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
NOPARALLEL;

CREATE INDEX IDX_VIADUC_MTTN_5 ON TBK_VIADUC_MTTN 
(MTTN_DT) 
LOGGING
TABLESPACE <TS_IDX>
PCTFREE    10
INITRANS   2
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
NOPARALLEL;


CREATE INDEX IDX_VIADUC_MTTN ON TBK_VIADUC_MTTN_AKTN
(MTTN_ID)
LOGGING
TABLESPACE <TS_IDX>
PCTFREE    10
INITRANS   2
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
NOPARALLEL;

CREATE INDEX IDX_VIADUC_MTTN_AKTN_1 ON TBK_VIADUC_MTTN_AKTN 
(AKTN_STTS_DT, AKTN_STTS_HIST) 
LOGGING
TABLESPACE <TS_IDX>
PCTFREE    10
INITRANS   2
MAXTRANS   255
STORAGE    (
            INITIAL          64K
            MINEXTENTS       1
            MAXEXTENTS       UNLIMITED
            PCTINCREASE      0
            BUFFER_POOL      DEFAULT
           )
NOPARALLEL;

-- ToDo: Implement triggers that fill the mutation tables...

```