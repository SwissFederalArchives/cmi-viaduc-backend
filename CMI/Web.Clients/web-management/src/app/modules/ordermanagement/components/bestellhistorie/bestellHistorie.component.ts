import {Component, Input} from '@angular/core';
import {
	ApproveStatus,
	EntityDecoratorService, EntscheidGesuchStatus, ShippingType,
} from '@cmi/viaduc-web-core';
import {Bestellhistorie, StatusHistory} from '../../model';
import * as moment from 'moment';

@Component({
	selector: 'cmi-viaduc-bestell-historie',
	templateUrl: 'bestellHistorie.component.html',
	styleUrls: ['./bestellHistorie.component.less']
})
export class BestellHistorieComponent {

	@Input()
	public historyItems: Bestellhistorie[];

	constructor(private _dec: EntityDecoratorService) {

	}

	public getOrderType(type: ShippingType): string {
		return this._dec.translateOrderingType(type);
	}

	public getApproveStatus(status: ApproveStatus): string {
		return this._dec.translateApproveStatus(status);
	}

	public getFormattedStatusHistoryDate(h: StatusHistory): string {
		return moment.utc(h.statusChangeDate).format('DD.MM.YYYY, HH:mm:ss');
	}

	public getFormattedDate(dt: Date | string) {
		return dt ? moment.utc(dt).format('DD.MM.YYYY, HH:mm:ss') : '';
	}

	public getDateAsString(field: any): string {
		if (field) {
			const val = moment.utc(field).format('DD.MM.YYYY');
			return (val === '01.01.0001') ? null : val;
		}
		return null;
	}

	public getEntscheidGesuch(entscheid: EntscheidGesuchStatus) {
		return this._dec.translateEntscheidGesuchStatus(entscheid);
	}
}
