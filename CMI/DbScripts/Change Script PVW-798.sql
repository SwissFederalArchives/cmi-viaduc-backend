/*******************************************************************************
  Dieses Script ergänzt einen neuen Trigger für die Tabelle Behältnis.
  Dies ist notwendig, weil aktulell eine Änderung an einem Behältnis in 
  scopeArchiv keine Neusynchronisierung auslöst.
******************************************************************************/

create or replace PACKAGE PKK_VIADUC_KNST
/******************************************************************************
   NAME:     PKK_VIADUC_KNST
   PURPOSE:  Define several constants used in Viaduc  

   REVISIONS:
   Ver        Date        Author           Description
   ---------  ----------  ---------------  ------------------------------------
   1.0        20.02.2019  jlang            1. Created this package.
   1.1        05.06.2019  jlang            Removed constants that are no longer
                                           used
******************************************************************************/
IS
-- ****************************************************
-- ******************* Konstanten *********************
-- ****************************************************
ci_false                   CONSTANT INT := 0;
ci_true                    CONSTANT INT := 1;

/*-------------*/
/* Sync Status */
/*-------------*/
ci_aktn_stts_waiting       CONSTANT INT := 0;
ci_aktn_stts_inProgress    CONSTANT INT := 1;
ci_aktn_stts_completed     CONSTANT INT := 2;
ci_aktn_stts_failed        CONSTANT INT := 3;
ci_aktn_stts_aborted       CONSTANT INT := 4;

/*---------------*/
/* Can Sync      */
/*---------------*/
ci_sync_notAllowed         CONSTANT INT := 0;
ci_sync_allowed            CONSTANT INT := 1;

/*-----------------*/
/* Zugänglichkeit  */
/*-----------------*/
ci_zgnl_oeffnetlich        CONSTANT INT := 1;
ci_zgnl_gsprt              CONSTANT INT := 10000;
ci_zgnl_vrbtn              CONSTANT INT := 10001;

/*---------------*/
/* Datenelemente */
/*---------------*/
ci_de_idnt_dgtl_mgzn       CONSTANT INT := 10367;
ci_de_pblktn_recht         CONSTANT INT := 10438;
ci_de_zgnl_keit            CONSTANT INT := 10425;
ci_de_zstnd_stelle         CONSTANT INT := 10426;

/*--------------------------*/
/* Stufen nicht in PKK_KNST */
/*--------------------------*/
ci_entrg_typ_sub_dsr_id    CONSTANT INT := 10010;

/*--------------------------*/
/* Codewerte                */
/*--------------------------*/
ci_pblktn_recht_dritte      CONSTANT INT := 23087; 
ci_pblktn_recht_prfng_ntwnd CONSTANT INT := 23088; 
ci_pblktn_recht_unbknt      CONSTANT INT := 23089; 

ci_zgnl_keit_frei_zgnl      CONSTANT INT := 22918; 

/*---------------*/
/* Klassen       */
/*---------------*/
ci_gsft_obj_kls_vrzng_enht  CONSTANT INT := 9; 

/*----------------*/
/* Behältnistypen */
/*----------------*/
ci_bhltn_typ_msn_spchr      CONSTANT INT := 10087;
ci_bhltn_typ_dir            CONSTANT INT := 10093;

END PKK_VIADUC_KNST;
/

CREATE OR REPLACE TRIGGER TRK_U_TBS_BHLTN
BEFORE UPDATE
ON TBS_BHLTN 
REFERENCING NEW AS NEW OLD AS OLD
FOR EACH ROW
FOLLOWS TRG_BHLTN_UBR
DECLARE
i_bhltn_typ_id          INT := 0;
/******************************************************************************
   NAME:      TRK_U_TBS_BHLTN
   PURPOSE:

   REVISIONS:
   Ver        Date        Author           Description
   ---------  ----------  ---------------  ------------------------------------
   1.0        09.08.2021  jlang            1. Created this trigger.

******************************************************************************/
BEGIN

      Select gsft_obj_typ_id into i_bhltn_typ_id from tbs_gsft_obj where gsft_obj_id = :NEW.bhltn_id;

       -- Insert the VE's into the mutation table that are in status "Abgeschlossen" and that do not already exist in
       -- the mutation table and that are affected by an update of the Behältnis Record
       IF i_bhltn_typ_id <> PKK_VIADUC_KNST.ci_bhltn_typ_msn_spchr AND
          i_bhltn_typ_id <> PKK_VIADUC_KNST.ci_bhltn_typ_dir THEN
            INSERT INTO tbk_viaduc_mttn (gsft_obj_id, aktn)
               SELECT v.vrzng_enht_id, 'Update'
                 FROM tbs_vrzng_enht v,
                      tbs_bhltn_vrzng_enht bv
                WHERE     bv.bhltn_id = :new.bhltn_id
                      AND bv.vrzng_enht_id = v.vrzng_enht_id
                      AND v.vrzng_enht_brbtg_stts_id = PKA_KNST.ci_vrzng_enht_stts_absls_id
                      AND fk_viaduc_vrzng_enht_fltr(v.vrzng_enht_id) = PKK_VIADUC_KNST.ci_sync_allowed
                      AND NOT EXISTS
                                 (SELECT gsft_obj_id
                                    FROM tbk_viaduc_mttn m
                                   WHERE m.aktn = 'Update' AND aktn_stts = PKK_VIADUC_KNST.ci_aktn_stts_waiting AND m.gsft_obj_id = v.vrzng_enht_id);
       END IF;
       
EXCEPTION
   WHEN OTHERS
   THEN
      pka_error.raise_error (SQLCODE);
      RAISE;
END TRK_U_TBS_BHLTN;
/


--------------------------------------------------------
-- Create procedure for enabling/disabling the
-- custom VIADUC triggers
--------------------------------------------------------
CREATE OR REPLACE PROCEDURE SPK_VIADUC_FNKTN(pi_enable in INT) IS
/******************************************************************************
   NAME:      SPK_VIADUC_FNKTN
   PURPOSE:   Enables or disables all viaduc functionality
              if pi enable = 0 the functionality is disabled, else
              the functionality is enabled.

   REVISIONS:
   Ver        Date        Author           Description
   ---------  ----------  ---------------  ------------------------------------
   1.0        15.02.2017  jlang            1. Created this procedure.
   1.1        09.08.2021  jlang            2. Added new trigger TRK_U_TBS_BHLTN

******************************************************************************/
vc_action               VARCHAR2 ( 10 );

BEGIN
    
    if pi_enable = 0 then   
        vc_action:= 'DISABLE';
    else
        vc_action:= 'ENABLE';
    end if;
    
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_D_TBS_FRMLR_ENTRG ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_U_TBS_FRMLR_ENTRG ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_IU_TBS_VRZNG_ENHT ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_U_TBA_CD ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_U_TBS_DATEN_ELMNT ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_D_TBS_DATEI ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_IU_TBS_DATEI ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_IU_TBS_DATEI ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_U_TBS_ENTRG_TYP ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_D_TBS_BHLTN_VRZNG_ENHT ' || vc_action;
    EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_I_TBS_BHLTN_VRZNG_ENHT ' || vc_action;
	EXECUTE IMMEDIATE 'ALTER TRIGGER TRK_U_TBS_BHLTN ' || vc_action;

   EXCEPTION
     WHEN OTHERS THEN
      pka_error.raise_error (SQLCODE);
      RAISE;
END SPK_VIADUC_FNKTN;
/