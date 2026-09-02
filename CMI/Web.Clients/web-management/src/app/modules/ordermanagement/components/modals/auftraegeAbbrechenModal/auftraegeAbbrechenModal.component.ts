import {Component, EventEmitter, Input, Output} from '@angular/core';
import {OrderService} from '../../../services';
import {ToastrService} from 'ngx-toastr';
import {ErrorService} from '../../../../shared/services';
import {
	Abbruchgrund,
	EntityDecoratorService,
} from '@cmi/viaduc-web-core';

@Component({
    selector: 'cmi-viaduc-auftraege-abbrechen-modal',
    templateUrl: 'auftraegeAbbrechenModal.component.html',
    styleUrls: ['./auftraegeAbbrechenModal.component.less'],
    standalone: false
})
export class AuftraegeAbbrechenModalComponent {

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

	public get selectedAbbruchgrund(): Abbruchgrund {
		return this._selectedAbbruchgrund;
	}
	public set selectedAbbruchgrund(val: Abbruchgrund) {
		this._selectedAbbruchgrund = val;
	}

	public bemerkungDossier!: string;
	public interneBemerkung!: string;
	public loading = false;
	public gruende!:  Abbruchgrund[];
	public isLoading = false;

	private _open = true;
	private _selectedAbbruchgrund!: Abbruchgrund;

	constructor(private _dec: EntityDecoratorService,
				private _ord: OrderService,
				private _err: ErrorService,
				private _toastr: ToastrService) {

		this.gruende = Object.values(Abbruchgrund)
			.filter(k =>
				typeof k === 'number' &&
				k !== Abbruchgrund.NichtGesetzt &&
				k !== Abbruchgrund.ZurueckgewiesenEinsichtsbewilligungNoetig &&
				k !== Abbruchgrund.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenInSchutzfrist &&
				k !== Abbruchgrund.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenFreiBewilligung &&
				k !== Abbruchgrund.ZurueckgewiesenFormularbestellungNichtErlaubt &&
				k !== Abbruchgrund.ZurueckgewiesenDossierangabenUnzureichend &&
				k !== Abbruchgrund.ZurueckgewiesenTeilbewilligungVorhanden
			) as Abbruchgrund[];
	}
	public translateStatus(a: Abbruchgrund) {
		return this._dec.translateAbbruchgrund(a);
	}

	public cancel() {
		this.open = false;
	}

	public ok() {
		if (!this.selectedAbbruchgrund) {
			return;
		}

		this.isLoading = true;
		this._ord.abbrechen(this.ids, this.selectedAbbruchgrund, this.bemerkungDossier, this.interneBemerkung).subscribe(() => {
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
