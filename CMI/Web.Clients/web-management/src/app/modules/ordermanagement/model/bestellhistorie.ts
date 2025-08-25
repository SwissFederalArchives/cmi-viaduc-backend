import {
	ApproveStatus,
	EntscheidGesuchStatus,
	ShippingType
} from '@cmi/viaduc-web-core';

export class Bestellhistorie {
	public auftragsId: number;
	public auftragsTyp: ShippingType;
	public freigabestatus: ApproveStatus;
	public datumDerFreigabe: Date;
	public entscheidGesuch: EntscheidGesuchStatus;
	public datumDesEntscheids: Date;
	public besteller: string;
	public interneBemerkung: string;
	public sachbearbeiter: string;
}
