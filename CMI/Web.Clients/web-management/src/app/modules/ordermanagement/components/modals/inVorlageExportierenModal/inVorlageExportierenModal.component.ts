import {Component, EventEmitter, Input, Output} from '@angular/core';
import {OrderService} from '../../../services';
import {ToastrService} from 'ngx-toastr';
import {ErrorService} from '../../../../shared/services';
import {EinsichtsGesuchEmailVorlage} from '../../../model';
import {ClientContext, Utilities as _util} from '@cmi/viaduc-web-core';

@Component({
	selector: 'cmi-viaduc-in-vorlage-exportieren-modal',
	templateUrl: 'inVorlageExportierenModal.component.html',
	styleUrls: ['./inVorlageExportierenModal.component.less']
})
export class InVorlageExportierenModalComponent {

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

	public get selectedVorlage(): EinsichtsGesuchEmailVorlage {
		return this._selectedVorlage;
	}
	public set selectedVorlage(val: EinsichtsGesuchEmailVorlage) {
		this._selectedVorlage = val;
	}

	public loading = false;
	public vorlagen: EinsichtsGesuchEmailVorlage[] = [];
	public isLoading = false;
	public sprache: string;
	public sprachen: string[] = ['de', 'fr', 'it', 'en'];

	private _open = true;
	private _selectedVorlage: EinsichtsGesuchEmailVorlage = null;

	constructor(private _ord: OrderService,
				private _err: ErrorService,
				private _ctx: ClientContext,
				private _toastr: ToastrService) {
		this.vorlagen = Object.keys(EinsichtsGesuchEmailVorlage)
			.filter(k => isNaN(parseInt(k, 10)))
			.map(k => EinsichtsGesuchEmailVorlage[k]);

		this.sprache = this._ctx.language;
	}

	public getVorlageDisplay(vorlage: EinsichtsGesuchEmailVorlage): string {
		return EinsichtsGesuchEmailVorlage[vorlage];
	}

	public cancel() {
		this.open = false;
	}

	public showSprache(): boolean {
		switch (this.selectedVorlage) {
			case EinsichtsGesuchEmailVorlage.Musterbrief_EG_J:
			case EinsichtsGesuchEmailVorlage.Weiterleitung_Entscheid_gemäss_Art_13_BGA:
			case EinsichtsGesuchEmailVorlage.Weiterleitung_Entscheid_gemäss_Art_15_BGA:
				return true;
			default:
				return false;
		}
	}

	public ok() {
		if (!_util.isNumber(this.selectedVorlage)) {
			return;
		}

		if (!this.showSprache()) {
			// einzelne Vorlagen sind nur auf Deutsch verfügbar
			this.sprache = 'de';
		}

		this.isLoading = true;
		this._ord.inVorlageExportieren(this.ids, this.selectedVorlage, this.sprache).subscribe(() => {
			const msg = this.ids.length === 1 ? 'Der Auftrag wurde erfolgreich exportiert' : 'Die Aufträge wurden erfolgreich exportiert';
			this._toastr.success(msg, 'Erfolgreich');
			this.open = false;
			this.onSubmitted.emit(true);
			this.isLoading = false;
		}, (e) => {
			this._err.showError(e);
			this.isLoading = false;
		});
	}
}
