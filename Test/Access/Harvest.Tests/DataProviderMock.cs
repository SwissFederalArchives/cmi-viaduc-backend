using System;
using System.Collections.Generic;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Access.Harvest.ScopeArchiv.DataSets;
using CMI.Contract.Common;
using Moq;

namespace CMI.Access.Harvest.Tests
{
    internal class DataProviderMock
    {
        private readonly ArchivePlanInfoDataSet archivePlanInfoDataSet;
        private readonly ArchiveRecordDataSet archiveRecordDataSet;
        private readonly ContainerDataSet containerDataSet;
        private readonly DescriptorDataSet descriptorDataSet;
        private readonly DetailDataDataSet detailDataSet;
        private readonly NodeInfoDataSet nodeInfoDataSet;
        private readonly ReferencesDataSet referencesDataSet;

        public DataProviderMock()
        {
            detailDataSet = new DetailDataDataSet();
            CreateNewTextRow(1, ScopeArchivGeschaeftsObjektKlasse.Verzeichnungseinheiten, ScopeArchivDatenElementId.Titel,
                ScopeArchivDatenElementTyp.Text, "Title of Archive Record", 1);
            CreateNewTextRow(2, ScopeArchivGeschaeftsObjektKlasse.Verzeichnungseinheiten, ScopeArchivDatenElementId.Signatur,
                ScopeArchivDatenElementTyp.Text, "Reference Code", 1);
            CreateNewTextRow(3, ScopeArchivGeschaeftsObjektKlasse.Verzeichnungseinheiten, ScopeArchivDatenElementId.Signatur,
                ScopeArchivDatenElementTyp.Text, "Reference Code Second Line", 2);
            CreateNewTextRow(4, ScopeArchivGeschaeftsObjektKlasse.Verzeichnungseinheiten, ScopeArchivDatenElementId.Darin,
                ScopeArchivDatenElementTyp.Memo, "Some long text ", 1);
            CreateNewTextRow(5, ScopeArchivGeschaeftsObjektKlasse.Verzeichnungseinheiten, ScopeArchivDatenElementId.Darin,
                ScopeArchivDatenElementTyp.Memo, "that continues on several lines ", 2);
            CreateNewTextRow(6, ScopeArchivGeschaeftsObjektKlasse.Verzeichnungseinheiten, ScopeArchivDatenElementId.Darin,
                ScopeArchivDatenElementTyp.Memo, "to be stiched together ", 3);
            CreateNewDateRangeRow(7, ScopeArchivGeschaeftsObjektKlasse.Verzeichnungseinheiten, ScopeArchivDatenElementId.Entstehungszeitraum,
                ScopeArchivDatenElementTyp.Datumsbereich, ScopeArchivDateOperator.FromTo, "+1940", "+1950", new DateTime(1940, 1, 1),
                new DateTime(1950, 12, 31), false, true, 1);
            CreateNewTextRow(8, ScopeArchivGeschaeftsObjektKlasse.Verzeichnungseinheiten, ScopeArchivDatenElementId.AblieferungLink,
                ScopeArchivDatenElementTyp.Verknüpfung, "1950 Ablieferung Link", 1);

            containerDataSet = new ContainerDataSet();
            var containerRow = containerDataSet.StorageContainer.NewStorageContainerRow();
            containerRow.BHLTN_TYP_NM = "Box";
            containerRow.BHLTN_DEF_STAND_ORT_CD = "1/5.03";
            containerRow.GSFT_OBJ_KURZ_NM = "Box 1/5.03";
            containerDataSet.StorageContainer.AddStorageContainerRow(containerRow);

            referencesDataSet = new ReferencesDataSet();
            var referenceRow = referencesDataSet.References.NewReferencesRow();
            referenceRow.GSFT_OBJ_ID = 9999;
            referenceRow.GSFT_OBJ_BZHNG_ROLLE_NM = "see also";
            referenceRow.GSFT_OBJ_KURZ_NM = "Archive Record Title";
            referenceRow.PRTCTD = 0;
            referencesDataSet.References.AddReferencesRow(referenceRow);

            descriptorDataSet = new DescriptorDataSet();
            var descriptorRow = descriptorDataSet.Descriptor.NewDescriptorRow();
            descriptorRow.DSKRP_BENUTZT_FUER_LISTE = "used for 1; used for 2";
            descriptorRow.DSKRP_BSR = "Just some description";
            descriptorRow.DSKRP_FREMD_SPR_NM = "Basle";
            descriptorRow.DSKRP_ID = 9999;
            descriptorRow.DSKRP_NM = "Basel";
            descriptorRow.GSFT_OBJ_BZHNG_ROLLE_NM = null;
            descriptorRow.GSFT_OBJ_KURZ_NM = "Basel (Place)";
            descriptorRow.THSRS_NM = "Place";
            descriptorDataSet.Descriptor.AddDescriptorRow(descriptorRow);

            nodeInfoDataSet = new NodeInfoDataSet();
            nodeInfoDataSet.NodeInfo.AddNodeInfoRow(800, "000000000100000008000000001000", 1, 0, 12);

            archiveRecordDataSet = new ArchiveRecordDataSet();
            var ar = archiveRecordDataSet.ArchiveRecord.NewArchiveRecordRow();
            ar.VRZNG_ENHT_ID = 1000;
            ar.AKTIV_IND = 1;
            ar.ERFSG_DT = DateTime.Today;
            ar.ERFSG_USR_CD = "Me";
            ar.TST = DateTime.Today;
            ar.VRZNG_ENHT_BRBTG_STTS_ID = 1;
            ar.VRZNG_ENHT_BWLG_TYP_ID = 1;
            ar.VRZNG_ENHT_ENTRG_TYP_ID = 1;
            ar.VRZNG_ENHT_INHLT_ID = 1;
            ar.VRZNG_ENHT_SCHTZ_FRIST_ID = 1;
            ar.VRZNG_ENHT_ZGNGL_ID = 1;
            ar.BILD_ANST_IND = 1;
            ar.BILD_VRSCH_IND = 1;
            ar.BLKRT_IND = 0;
            ar.FIND_MTL_IND = 0;
            ar.VRZNG_ENHT_ENTRG_TYP_NM = "Dossier";
            ar.VRZNG_ENHT_BRBTG_STTS_NM = "Abgeschlossen";
            ar.HRCH_PFAD = "000000000100000008000000001000";
            ar.VRZNG_ENHT_BSTLG_IND = 0;
            ar.SUCH_FRGB_IND = 0;
            ar.VRZNG_ENHT_BNTZB_ID = 1;
            ar.SCHTZ_PRSN_IND = 0;
            ar.VRZNG_ENHT_INHLT_NM = "Art. 9.1 BGA";
            ar.VRZNG_ENHT_SCHTZ_FRIST_NM = "Creation Period End";
            ar.SCHTZ_FRIST_DAUER = 50;
            ar.SCHTZ_FRIST_BIS_DT = DateTime.Today;
            ar.SCHTZ_FRIST_MIN_IND = 0;
            ar.SCHTZ_FRIST_MTTN_IND = 0;
            ar.SCHTZ_FRIST_NTZ = "Just a note";
            ar.VRZNG_ENHT_BWLG_TYP_NM = "None";
            ar.VRZNG_ENHT_BNTZB_NM = "Unrestricted";
            ar.VRZNG_ENHT_ZGNGL_NM = "Public";
            ar.BNTZG_HNWS_TXT = null;
            ar.ANST_FRMLR_ID = 1;
            ar.ANST_FRMLR_NM = "Test";
            ar.BRBTG_FRMLR_ID = 1;
            ar.BRBTG_FRMLR_NM = "Test";
            archiveRecordDataSet.ArchiveRecord.AddArchiveRecordRow(ar);

            archivePlanInfoDataSet = new ArchivePlanInfoDataSet();
            archivePlanInfoDataSet.ArchivePlanItem.AddArchivePlanItemRow("RefCode 1", "Just a string", "1940 - 1950", 1, "Series", 1, 1);
            archivePlanInfoDataSet.ArchivePlanItem.AddArchivePlanItemRow("RefCode 2", "Another string", "1945 - 1949", 1, "File", 800, 0);
            archivePlanInfoDataSet.ArchivePlanItem.AddArchivePlanItemRow("RefCode 3", "Document Title", "1947", 1, "Document", 1000, 0);
        }

        public void CreateNewTextRow(int gsftObjDtlId, ScopeArchivGeschaeftsObjektKlasse gsftObjKls, ScopeArchivDatenElementId elementId,
            ScopeArchivDatenElementTyp elementTyp, string text,
            int sequence)
        {
            var newRow = CreateBasicRow(gsftObjDtlId, gsftObjKls, elementId, elementTyp, sequence);
            newRow.MEMO_TXT = text;
            detailDataSet.DetailData.AddDetailDataRow(newRow);
        }

        public void CreateNewDateRangeRow(int gsftObjDtlId, ScopeArchivGeschaeftsObjektKlasse gsftObjKls, ScopeArchivDatenElementId elementId,
            ScopeArchivDatenElementTyp elementTyp, ScopeArchivDateOperator dateOperator, string bgnDtStnd, string endDtStnd, DateTime bgnDt,
            DateTime endDt, bool bgnApprox, bool endApprox,
            int sequence)
        {
            var newRow = CreateBasicRow(gsftObjDtlId, gsftObjKls, elementId, elementTyp, sequence);
            newRow.BGN_DT_STND = bgnDtStnd;
            newRow.END_DT_STND = endDtStnd;
            newRow.BGN_DT = bgnDt;
            newRow.END_DT = endDt;
            newRow.BGN_CIRCA_IND = bgnApprox ? 1 : 0;
            newRow.END_CIRCA_IND = endApprox ? 1 : 0;
            newRow.DT_OPRTR_ID = (int) dateOperator;
            detailDataSet.DetailData.AddDetailDataRow(newRow);
        }

        private DetailDataDataSet.DetailDataRow CreateBasicRow(int gsftObjDtlId, ScopeArchivGeschaeftsObjektKlasse gsftObjKls,
            ScopeArchivDatenElementId elementId,
            ScopeArchivDatenElementTyp elementTyp, int sequence)
        {
            var newRow = detailDataSet.DetailData.NewDetailDataRow();
            newRow.GSFT_OBJ_DTL_ID = gsftObjDtlId;
            newRow.GSFT_OBJ_KLS_ID = (int) gsftObjKls;
            newRow.DATEN_ELMNT_ID = (int) elementId;
            newRow.DATEN_ELMNT_TYP_ID = (int) elementTyp;
            newRow.GSFT_OBJ_ID = 1000;
            newRow.ELMNT_SQNZ_NR = sequence;
            newRow.VOLL_TXT_SRCHBL_IND = 1;
            newRow.ZGRF_BRTG_STUFE_ID = (int) DataElementVisibility.@public;
            newRow.TITEL = Enum.GetName(typeof(ScopeArchivDatenElementId), elementId);
            return newRow;
        }

        public IAISDataProvider GetMock()
        {
            var moq = new Mock<IAISDataProvider>();
            moq.Setup(t => t.LoadDetailData(1000)).Returns(detailDataSet);
            moq.Setup(t => t.LoadContainers(1000)).Returns(containerDataSet);
            moq.Setup(t => t.GetArchiveRecordRow(1000)).Returns((ArchiveRecordDataSet.ArchiveRecordRow) archiveRecordDataSet.ArchiveRecord.Rows[0]);
            moq.Setup(t => t.LoadNodeContext(1000))
                .Returns(new NodeContext
                {
                    ArchiveRecordId = "1000",
                    ParentArchiveRecordId = "800",
                    FirstChildArchiveRecordId = "1100",
                    NextArchiveRecordId = "1001",
                    PreviousArchiveRecordId = "999"
                });
            moq.Setup(t => t.LoadNodeInfo(1000)).Returns(nodeInfoDataSet);
            moq.Setup(t => t.LoadDescriptors(1000)).Returns(descriptorDataSet);
            moq.Setup(t => t.LoadReferences(1000)).Returns(referencesDataSet);
            moq.Setup(t => t.LoadArchivePlanInfo(new[] {1L, 800L, 1000L})).Returns(archivePlanInfoDataSet);
            moq.Setup(t => t.LoadMetadataSecurityTokens(1000)).Returns(new List<string> {"Ö1"});
            moq.Setup(t => t.LoadPrimaryDataSecurityTokens(1000))
                .Returns(new PrimaryDataSecurityTokenResult
                {
                    DownloadAccessTokens = new List<string> {"Ö1"},
                    FulltextAccessTokens = new List<string> {"Ö1"}
                });
            moq.Setup(t => t.LoadFieldSecurityTokens(1000)).Returns(new List<string> { "BAR" });

            return moq.Object;
        }
    }
}