import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {EntityDecoratorService, EntscheidGesuchStatus} from '@cmi/viaduc-web-core';
import {OrderService} from '../../../services';
import {ToastrService} from 'ngx-toastr';
import {ErrorService} from '../../../../shared/services';
import * as moment from 'moment';
import {OrderingFlatItem} from '../../../model';
import {forkJoin, Observable} from 'rxjs';

@Component({
	selector: 'cmi-viaduc-entscheid-hinterlegen-modal',
	templateUrl: 'entscheidHinterlegenModal.component.html',
	styleUrls: ['./entscheidHinterlegenModal.component.less']
})
export class EntscheidHinterlegenModalComponent implements OnInit {

	@Input()
	public items: OrderingFlatItem[] = [];

	@Input()
	public user: string;

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

	public get selectedEntscheid(): EntscheidGesuchStatus {
		return this._selectedEntscheid;
	}
	public set selectedEntscheid(val: EntscheidGesuchStatus) {
		this._selectedEntscheid = val;
	}

	public isValidDate = true;
	public isLoading = false;
	public entscheide = [];
	public datumEntscheid: string;
	public interneBemerkung: string;
	public stepNr = 1;
	public hint: string;
	public filteredItems: OrderingFlatItem[] = [];
	public showUnHideDataButton = true;
	private _open = true;

	private _selectedEntscheid: EntscheidGesuchStatus = null;

	constructor(private _dec: EntityDecoratorService,
				private _ord: OrderService,
				private _err: ErrorService,
				private _toastr: ToastrService) {
	}

	public ngOnInit(): void {
		this.entscheide = Object.keys(EntscheidGesuchStatus)
			.filter(k => isNaN(parseInt(k, 10)))
			.map(k => EntscheidGesuchStatus[k])
			.filter(k => k !== EntscheidGesuchStatus.NichtGeprueft);

		this.filteredItems = this.items;
	}

	public translateStatus(a: EntscheidGesuchStatus) {
		return this._dec.translateEntscheidGesuchStatus(a);
	}

	public cancel(event: any) {
		this.open = false;
		event.stopPropagation();
	}

	public checkDate(isValid) {
		this.isValidDate = isValid;
	}

	public goPrev(event: any) {
		this.stepNr = 1;
		event.stopPropagation();
	}
	public goNext(event: any) {
		this.stepNr = 2;
		event.stopPropagation();
	}

	public seeHiddenOrderDetail() {
		this.hint = 'Daten werden geladen...';
		this.isLoading = true;

		const observables:Observable<OrderingFlatItem>[] = [];
		for (const order of this.filteredItems) {
			const obs = this._ord.getUnHiddenOrderingDetail(order.itemId);
			observables.push(obs);
		}

		forkJoin(observables).subscribe(unhiddenItems => {
			this.filteredItems = unhiddenItems;
			this.isLoading = false;
			this.showUnHideDataButton = false;
		});
	}

	public ok() {
		if (!this.selectedEntscheid) {
			return;
		}

		const bewilligungsDatum = this.datumEntscheid ? moment.utc(this.datumEntscheid, 'DD.MM.YYYY').toDate() : null;
		if (!bewilligungsDatum) {
			return;
		}
		this.hint = 'Statusänderung wird durchgeführt...';
		this.isLoading = true;
		this._ord.einsichtsgesucheEntscheidFuerGesucheHinterlegen(this.items.map(i => i.itemId), this.selectedEntscheid, bewilligungsDatum, this.interneBemerkung).subscribe(() => {
			this._toastr.success('Statusänderung erfolgreich durchgeführt', 'Erfolgreich');
			this.open = false;
			this.onSubmitted.emit(true);
			this.isLoading = false;
		}, (e) => {
			this._err.showError(e);
			this.isLoading = false;
		});
	}
}
