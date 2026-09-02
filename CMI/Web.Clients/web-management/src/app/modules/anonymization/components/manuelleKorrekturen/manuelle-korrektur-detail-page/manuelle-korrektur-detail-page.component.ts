import {Component, HostListener, OnInit, ViewChild} from '@angular/core';
import {
	ComponentCanDeactivate,
	ManuelleKorrekturDetailItem,
	ManuelleKorrekturDto,
	TranslationService,
	ManuelleKorrekturFeldDto,
	ManuelleKorrekturStatusHistoryDto,
	CmiGridComponent
} from '@cmi/viaduc-web-core';
import {FormBuilder, FormControl, FormGroup, Validators} from '@angular/forms';
import {DetailPagingService, ErrorService, UrlService} from '../../../../shared';
import {ActivatedRoute, ParamMap} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import {switchMap} from 'rxjs/operators';
import {ManuelleKorrekturenService} from '../../../services/manuelleKorrekturen-services';
import {CollectionView} from '@mescius/wijmo';
import {DataMap} from '@mescius//wijmo.grid';
import moment from 'moment';
import {SortDescription, } from '@mescius/wijmo';

@Component({
    selector: 'cmi-manuelle-korrektur-detail-page',
    templateUrl: './manuelle-korrektur-detail-page.component.html',
    styleUrls: ['./manuelle-korrektur-detail-page.component.less'],
    standalone: false
})

export class ManuelleKorrekturDetailPageComponent extends ComponentCanDeactivate implements OnInit {

	@ViewChild('flexGridReferenzen', { static: true })
	public flexGridReferenzen: CmiGridComponent;

	public statusMap!: DataMap;
	public detailItem!: ManuelleKorrekturDetailItem;
	public myForm!: FormGroup;
	public crumbs: any;
	public isNavFixed: boolean = false;
	public detailPagingEnabled = false;
	public history!: CollectionView;
	public referenzen!: CollectionView;
	public verweise!: CollectionView;
	public editMode: boolean = false;
	public sortedList: ManuelleKorrekturFeldDto[] = [];
	public loading!: boolean;

	constructor(private _service: ManuelleKorrekturenService,
				private formbuilder: FormBuilder,
				private _url: UrlService,
				private _dps: DetailPagingService,
				private _err: ErrorService,
				private _route: ActivatedRoute,
				private _toastr: ToastrService,
				private _txt: TranslationService) {
		super();
	}

	public ngOnInit(): void {
		this.loading = true;
		const ManuelleKorrekturDetailItem$ = this._route.paramMap.pipe(switchMap((params: ParamMap) => {
			const id = Number(params.get('id'));
			this.detailPagingEnabled = this._dps.getCurrentIndex() > -1;
			return this._service.getManuelleKorrektur(id);
		}));
		ManuelleKorrekturDetailItem$.subscribe(manuelleKorrekturDetailItem => {

			this.detailItem = manuelleKorrekturDetailItem;
			this.loading = false;
			if (this.detailItem !== null) {
				this.sortFelder();
				this.initForm();
				this.buildCrumbs();
			}
		});
	}

	private sortFelder() {
		this.sortedList = [];
		let titel, darin, bemerkungZurVe, verwandteVe, zusatzkomponenteZac1: ManuelleKorrekturFeldDto;
		if ( this.detailItem?.manuelleKorrektur !== null &&
			this.detailItem.manuelleKorrektur.manuelleKorrekturFelder?.length > 0) {
			for (const feld of this.detailItem.manuelleKorrektur.manuelleKorrekturFelder) {
				if (feld.feldname === 'Titel') {
					titel = feld;
				} else if (feld.feldname === 'Darin') {
					darin = feld;
				} else if (feld.feldname === 'BemerkungZurVe') {
					bemerkungZurVe = feld;
				} else if (feld.feldname === 'VerwandteVe') {
					verwandteVe = feld;
				} else if (feld.feldname === 'ZusatzkomponenteZac1') {
					zusatzkomponenteZac1 = feld;
				}
			}
		}
		if (titel && darin && bemerkungZurVe && verwandteVe && zusatzkomponenteZac1) {
			this.sortedList.push(titel, darin, bemerkungZurVe, verwandteVe, zusatzkomponenteZac1);
		}
	}

	/* eslint-disable */
	@HostListener('window:scroll', ['$event'])
	public onScroll(event: any) {
		const verticalOffset = window.pageYOffset
			|| document.documentElement.scrollTop
			|| document.body.scrollTop || 0;

		if (verticalOffset >= 222) {
			// make nav fixed
			this.isNavFixed = true;
			return;
		}

		if (this.isNavFixed) {
			this.isNavFixed = false;
		}
	}

	public canDeactivate(): boolean {
		if (this.myForm) {
			return !this.myForm.dirty;
		}
		return true;
	}

	public promptForMessage(): false | 'question' | 'message' {
		return 'question';
	}

	public message(): string {
		return this._txt.get('hints.unsavedChanges', 'Sie haben ungespeicherte Änderungen. Wollen Sie die Seite tatsächlich verlassen?');
	}

	public save(statusChanged: boolean) {
		const rawValue = this.myForm.getRawValue();
		const manuelleKorrektur = ManuelleKorrekturDto.fromJS(rawValue);
		const result = this._service.update(manuelleKorrektur);
		result.subscribe((r) => {
				this.reloadData(r);
				if (statusChanged) {
					this._toastr.success('Anonymisierungsstatus geändert');
				} else {
					this._toastr.success('Erfolgreich gespeichert');
				}

			},
			(error) => {
				this._err.showError(error);
			});
	}

	private reloadData(detailItem: ManuelleKorrekturDto): void {
		// fetch latest data
		this._service.getManuelleKorrektur(detailItem.manuelleKorrekturId).subscribe(r => {
			if (r !== null) {
				this.detailItem = r;
			}
			this.sortFelder();
			this.initForm();
		});
	}

	private initForm() {
		const mk = this.detailItem?.manuelleKorrektur;
		const felder = mk?.manuelleKorrekturFelder ?? [];

		const getFeldOriginal = (name: string): string => {
			return felder.find(f => f.feldname === name)?.original ?? '';
		};

		const darin = getFeldOriginal('Darin');
		const bemerkung = getFeldOriginal('BemerkungZurVe');

		this.myForm = this.formbuilder.group({
			manuelleKorrekturId: new FormControl(mk?.manuelleKorrekturId, [Validators.required]),
			veId: new FormControl(mk?.veId),
			signatur: new FormControl(mk?.signatur),
			schutzfristende: new FormControl(mk?.schutzfristende),

			schutzfristendeText: new FormControl(
				moment(mk?.schutzfristende).format('DD.MM.YYYY')
			),

			titel: new FormControl(mk?.titel),
			darin: new FormControl(darin),
			zusätzlicheInformationen: new FormControl(bemerkung),

			erzeugtAm: new FormControl(mk?.erzeugtAm),
			erzeugtVon: new FormControl(mk?.erzeugtVon ?? ''),
			geändertAm: new FormControl(mk?.geändertAm ?? null),
			geändertVon: new FormControl(mk?.geändertVon ?? null),

			kommentar: new FormControl(mk?.kommentar),
			hierachiestufe: new FormControl(mk?.hierachiestufe),
			aktenzeichen: new FormControl(mk?.aktenzeichen),

			entstehungszeitraum: new FormControl(mk?.entstehungszeitraum ?? null),

			zugänglichkeitGemässBGA: new FormControl(mk?.zugänglichkeitGemässBGA),
			schutzfristverzeichnung: new FormControl(mk?.schutzfristverzeichnung),
			zuständigeStelle: new FormControl(mk?.zuständigeStelle),
			publikationsrechte: new FormControl(mk?.publikationsrechte),

			anonymisiertZumErfassungszeitpunk: new FormControl(
				mk?.anonymisiertZumErfassungszeitpunk
			),

			manuelleKorrekturFelder: new FormControl(felder),

			anonymisierungsstatus: new FormControl(mk?.anonymisierungsstatus),

			anonymisierungsstatusText: this.getAnonymisierungsStatus(
				mk?.anonymisierungsstatus ?? -1
			)
		});

		this.editMode = mk?.anonymisierungsstatus === 0;

		const sortedArray: ManuelleKorrekturStatusHistoryDto[] =
			(mk?.manuelleKorrekturStatusHistories ?? [])
				.slice()
				.sort((a, b) =>
					a.erzeugtAm > b.erzeugtAm ? -1 : a.erzeugtAm < b.erzeugtAm ? 1 : 0
				);

		this.history = new CollectionView(sortedArray);
		this.history.pageSize = 10;

		this.referenzen = new CollectionView(this.detailItem?.untergeordneteVEs ?? []);
		this.referenzen.pageSize = 10;
		this.referenzen.sortDescriptions.push(new SortDescription('referenceCode', true));

		this.verweise = new CollectionView(this.detailItem?.verweiseVEs ?? []);
		this.verweise.pageSize = 10;
		this.verweise.sortDescriptions.push(new SortDescription('referenceCode', true));

		this.statusMap = new DataMap([
			{ key: 2, name: 'Prüfung notwendig' },
			{ key: 0, name: 'In Bearbeitung' },
			{ key: 1, name: 'Publiziert' }
		], 'key', 'name');
	}
	private buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		const menu = 'anonymization';
		const manuelleKorrekturen = 'manuelleKorrekturen';
		const id = this.detailItem.manuelleKorrektur.manuelleKorrekturId;

		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({
			url: this._url.getNormalizedUrl(`/${menu}`),
			label: this._txt.get('breadcrumb.AnonymisierungMenu', 'Anonymisierung')
		});
		crumbs.push({
			url: this._url.getNormalizedUrl(`/${menu}/${manuelleKorrekturen}`),
			label: this._txt.get('breadcrumb.manuelleKorrekturen', 'Manuelle Korrekturen')
		});

		crumbs.push({
			url: this._url.getNormalizedUrl(`/${menu}/${manuelleKorrekturen}/${id}`),
			label: this.detailItem.manuelleKorrektur ? this.detailItem.manuelleKorrektur.titel : ''
		});
	}

	public reset() {
		if (this.detailItem.manuelleKorrektur) {
			this.reloadData(this.detailItem.manuelleKorrektur);
		}
	}

	public getDetailBaseUrl(): string {
		return this._url.getManuelleKorrekturenUrl();
	}

	public publizieren() {
		const result = this._service.publizieren(this.detailItem.manuelleKorrektur.manuelleKorrekturId);
		result.subscribe((r) => {
				this.detailItem.manuelleKorrektur = r;
				this.initForm();
				this._toastr.success('Erfolgreich publiziert');
				this.editMode = false;
			},
			(error) => {
				this._err.showError(error, 'Publizieren');
			});
	}

	public edit() {
		this.myForm.controls['anonymisierungsstatus'].setValue(0);
		this.save(true);
	}

	public feldTextChanged($event: string, feld: ManuelleKorrekturFeldDto) {
		this.detailItem.manuelleKorrektur.manuelleKorrekturFelder.find(x => x.manuelleKorrekturFelderId === feld.manuelleKorrekturFelderId).manuell = $event;
	}

	private getAnonymisierungsStatus(status: number): string {
		switch (status) {
			case 0:
				return 'In Bearbeitung';
			case 1:
				return 'Publiziert';
			case 2:
				return 'Prüfung notwendig';
			default:
				return '';
		}
	}
}
