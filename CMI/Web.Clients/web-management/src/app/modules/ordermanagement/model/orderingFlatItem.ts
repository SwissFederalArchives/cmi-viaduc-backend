import {
	ApproveStatus,
	ExternalStatus,
	InternalStatus,
	ShippingType,
	DigitalisierungsKategorie, EntscheidGesuchStatus, Abbruchgrund, GebrauchskopieStatus
} from '@cmi/viaduc-web-core';
export class OrderingFlatItem {
	public orderingType: ShippingType;
	public orderingComment: string;
	public orderingArtDerArbeit: string;
	public orderingArtDerArbeitId: number;
	public orderingLesesaalDate: Date;
	public orderingDate: Date;
	public user: string;
	public userId: string;
	public itemId: number;
	public veId: number;
	public itemComment: string;
	public orderId: number;
	public bewilligungsDatum: Date;
	public hasPersonendaten: boolean;
	public reason: string;
	public reasonId: number;
	public bestand: string;
	public ablieferung: string;
	public behaeltnisNummer: string;
	public dossiertitel: string;
	public zeitraumDossier: string;
	public standort: string;
	public signatur: string;
	public darin: string;
	public zusaetzlicheInformationen: string;
	public hierarchiestufe: string;
	public schutzfristverzeichnung: string;
	public zugaenglichkeitGemaessBga: string;
	public publikationsrechte: string;
	public behaeltnistyp: string;
	public zustaendigeStelle: string;
	public identifikationDigitalesMagazin: string;
	public archivNummer: string;
	public approveStatus: ApproveStatus;
	public status: InternalStatus;
	public externalStatus: ExternalStatus;
	public terminDigitalisierung: Date;
	public internalComment: string;
	public digitalisierungsKategorie: DigitalisierungsKategorie;
	public aktenzeichen: string;
	public eingangsart: number;
	public ausleihdauer: number;
	public ausgabedatum: Date;
	public abschlussdatum: Date;
	public mahndatumInfo: string;
	public anzahlMahnungen: number;
	public entscheidGesuch: EntscheidGesuchStatus;
	public datumDesEntscheids: Date;
	public begruendungEinsichtsgesuch: string;
	public unterlagenDieNutzerSelberBetreffen: boolean;
	public personenbezogeneNachforschung: boolean;
	public abbruchgrund: Abbruchgrund;
	public erwartetesRueckgabeDatum: Date;
	public benutzungskopie: boolean;
	public rolePublicClient: string;
	public gebrauchskopieStatus: GebrauchskopieStatus;
}
