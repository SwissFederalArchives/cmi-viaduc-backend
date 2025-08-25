import {Component, EventEmitter, Input, Output} from '@angular/core';
import {OrderService} from '../../../services';
import {ToastrService} from 'ngx-toastr';
import {ErrorService} from '../../../../shared/services';

@Component({
	selector: 'cmi-viaduc-auftraege-ausleihen-modal',
	templateUrl: 'auftraegeAusleihenModal.component.html',
	styleUrls: ['./auftraegeAusleihenModal.component.less']
})
export class AuftraegeAusleihenModalComponent {

	@Input()
	public ids: number[] = [];
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

	public isLoading = false;

	private _open = true;

	constructor(private _ord: OrderService,
				private _err: ErrorService,
				private _toastr: ToastrService) {
	}

	public cancel() {
		this.open = false;
	}

	public ok() {
		this.isLoading = true;
		this._ord.auftraegeAusleihen(this.ids).subscribe(() => {
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
