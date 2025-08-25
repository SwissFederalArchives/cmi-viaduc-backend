import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {ApproveStatus, EntityDecoratorService} from '@cmi/viaduc-web-core';
import {OrderService} from '../../../services';
import {ToastrService} from 'ngx-toastr';
import {ErrorService} from '../../../../shared/services';
import {FlatPickrOutputOptions} from 'angularx-flatpickr/lib/flatpickr.directive';
import * as moment from 'moment';

@Component({
	selector: 'cmi-viaduc-freigabekontrolle-modal',
	templateUrl: 'freigabeKontrolleModal.component.html',
	styleUrls: ['./freigabeKontrolleModal.component.less']
})
export class FreigabeKontrolleModalComponent implements  OnInit{

	@Input()
	public ids: number[] = [];
	@Input()
	public datumBewilligung:  Date;

	@Input()
	public interneBemerkung: string;
	@Input()
	public currentRolePublicClient: string;
	@Input()
	public set open(val: boolean) {
		this._open = val;
		this.openChange.emit(val);
	}

	public get open(): boolean {
		return this._open;
	}

	@Output()
	public openChange: EventEmitter<boolean> = new EventEmitter<boolean>();
	@Output()
	public onSubmitted: EventEmitter<boolean> = new EventEmitter<boolean>();

	public get selectedEntscheid(): ApproveStatus {
		return this._selectedEntscheid;
	}
	public set selectedEntscheid(val: ApproveStatus) {
		this._selectedEntscheid = val;
		this.haveToEnterBewilligungsDatum = (val && val === ApproveStatus.FreigegebenInSchutzfrist);
	}
	public isValidDate = false;
	public isLoading = false;
	public entscheide = [];
	public haveToEnterBewilligungsDatum = false;
	public isOe2UserConfirmed: boolean;

	private _open = true;
	private _selectedEntscheid: ApproveStatus = null;

	constructor(private _dec: EntityDecoratorService,
				private _ord: OrderService,
				private _err: ErrorService,
				private _toastr: ToastrService) {
		this.entscheide = Object.keys(ApproveStatus)
			.filter(k => isNaN(parseInt(k, 10)))
			.map(k => ApproveStatus[k])
			.filter(k => k !== ApproveStatus.NichtGeprueft && k !== ApproveStatus.FreigegebenDurchSystem);
	}

	public ngOnInit(): void {
		this.convertDatumBewilligungFromStringToDate();
	}

	public translateStatus(a: ApproveStatus) {
		return this._dec.translateApproveStatus(a);
	}

	public cancel() {
		this.open = false;
	}

	public checkDate($event: FlatPickrOutputOptions) {
		const isValid = $event.dateString !== '' && $event.dateString;
		if (isValid) {
			this.datumBewilligung = $event.selectedDates[0];
			this.isValidDate = true;
		} else {
			this.isValidDate = false;
		}
	}

	public ok() {
		if (!this.selectedEntscheid && !this.isValidDate) {
			return;
		}

		// Wir wandeln nach UTC um, behalten aber den Zeitteil. So wird aus 20.12.2022 00:00:00 GMT+1 nicht 19.12.2022 23:00:00Z sondern
		// wird zu 20.12.2022 00:00:00Z
		const bewilligungsDatum = this.haveToEnterBewilligungsDatum ? moment(this.datumBewilligung).utc(true).toDate() : null;
		if (!bewilligungsDatum && this.haveToEnterBewilligungsDatum) {
			return;
		}

		this.isLoading = true;
		this._ord.auftraegeEntscheidFreigabeHinterlegen(this.ids, this.selectedEntscheid, bewilligungsDatum , this.interneBemerkung).subscribe(() => {
			this._toastr.success('Statusänderung erfolgreich durchgeführt', 'Erfolgreich');
			this.open = false;
			this.onSubmitted.emit(true);
			this.isLoading = false;
		}, (e) => {
			this._err.showError(e);
			this.isLoading = false;
		});
	}

	public convertDatumBewilligungFromStringToDate() {
		this.isValidDate = false;
		if (this.datumBewilligung) {
			// Das im @Input übergebene Datum ist in Wahrheit ein string, obwohl der Datentyp ein Date ist. (2022-12-22T00:00:00)
			// Javascript akzeptiert dies. Mit der Umwandlung über moment, erhalten wir ein gültiges Datum
			this.datumBewilligung = moment(this.datumBewilligung).toDate();
			this.isValidDate = true;
		}
	}
}
