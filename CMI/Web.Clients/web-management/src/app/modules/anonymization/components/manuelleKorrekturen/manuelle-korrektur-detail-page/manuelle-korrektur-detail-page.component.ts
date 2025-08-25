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
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {combineLatest} from 'rxjs';
import {DetailPagingService, ErrorService, UrlService} from '../../../../shared';
import {ActivatedRoute, ParamMap} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import {switchMap} from 'rxjs/operators';
import {ManuelleKorrekturenService} from '../../../services/manuelleKorrekturen-services';
import {CollectionView} from '@mescius/wijmo';
import {DataMap} from '@mescius//wijmo.grid';
import * as moment from 'moment';
import {SortDescription, } from '@mescius/wijmo';

@Component({
	selector: 'cmi-manuelle-korrektur-detail-page',
	templateUrl: './manuelle-korrektur-detail-page.component.html',
	styleUrls: ['./manuelle-korrektur-detail-page.component.less']
})

export class ManuelleKorrekturDetailPageComponent extends ComponentCanDeactivate implements OnInit {

	@ViewChild('flexGridReferenzen', { static: true })
	public flexGridReferenzen: CmiGridComponent;

	public statusMap: DataMap;
	public detailItem: ManuelleKorrekturDetailItem;
	public myForm: FormGroup;
	public crumbs: any;
	public isNavFixed = false;
	public detailPagingEnabled = false;
	public history: CollectionView;
	public referenzen: CollectionView;
	public verweise: CollectionView;
	public editMode = false;
	public sortedList: ManuelleKorrekturFeldDto[] = [];
	public loading: boolean;

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
		combineLatest([ManuelleKorrekturDetailItem$])
			.subscribe(([manuelleKorrekturDetailItem]) => {
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

		this.sortedList.push(titel, darin, bemerkungZurVe, verwandteVe, zusatzkomponenteZac1);
	}

	/* eslint-disable */
	@HostListener('window:scroll', ['$event'])
	public onScroll(event) {
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
			this.detailItem = r;
			this.sortFelder();
			this.initForm();
		});
	}

	private initForm() {
		this.myForm = this.formbuilder.group({
			manuelleKorrekturId: [this.detailItem.manuelleKorrektur.manuelleKorrekturId, Validators.required],
			veId:[this.detailItem.manuelleKorrektur.veId],
			signatur: [this.detailItem.manuelleKorrektur.signatur],
			schutzfristende: this.detailItem.manuelleKorrektur.schutzfristende,
			schutzfristendeText: [moment(this.detailItem.manuelleKorrektur.schutzfristende).format('DD.MM.YYYY')],
			titel: [this.detailItem.manuelleKorrektur.titel],
			darin: this.detailItem.manuelleKorrektur.manuelleKorrekturFelder.find(f => f.feldname === 'Darin').original,
			zusätzlicheInformationen: this.detailItem.manuelleKorrektur.manuelleKorrekturFelder.find(f => f.feldname === 'BemerkungZurVe').original,
			erzeugtAm: [this.detailItem.manuelleKorrektur.erzeugtAm],
			erzeugtVon: [this.detailItem.manuelleKorrektur.erzeugtVon],
			geändertAm: [this.detailItem.manuelleKorrektur.geändertAm],
			geändertVon: [this.detailItem.manuelleKorrektur.geändertVon],
			kommentar: [this.detailItem.manuelleKorrektur.kommentar],
			hierachiestufe:[this.detailItem.manuelleKorrektur.hierachiestufe],
			aktenzeichen:[this.detailItem.manuelleKorrektur.aktenzeichen],
			entstehungszeitraum:[this.detailItem.manuelleKorrektur.entstehungszeitraum],
			zugänglichkeitGemässBGA:[this.detailItem.manuelleKorrektur.zugänglichkeitGemässBGA],
			schutzfristverzeichnung:[this.detailItem.manuelleKorrektur.schutzfristverzeichnung],
			zuständigeStelle:[this.detailItem.manuelleKorrektur.zuständigeStelle],
			publikationsrechte:[this.detailItem.manuelleKorrektur.publikationsrechte],
			anonymisiertZumErfassungszeitpunk:[this.detailItem.manuelleKorrektur.anonymisiertZumErfassungszeitpunk],
			manuelleKorrekturFelder:[this.detailItem.manuelleKorrektur.manuelleKorrekturFelder],
			anonymisierungsstatus: [this.detailItem.manuelleKorrektur.anonymisierungsstatus],
			anonymisierungsstatusText: this.getAnonymisierungsStatus(this.detailItem.manuelleKorrektur.anonymisierungsstatus)
		});
		this.editMode = this.detailItem.manuelleKorrektur.anonymisierungsstatus === 0;
		const sorrtedArray:Array<ManuelleKorrekturStatusHistoryDto> =
			this.detailItem.manuelleKorrektur.manuelleKorrekturStatusHistories.sort((mksh1, mksh2) => {
				if (mksh1.erzeugtAm > mksh2.erzeugtAm) {
					return -1;
				}
				if (mksh1.erzeugtAm < mksh2.erzeugtAm) {
					return 1;
				}

				return 0;
			});
		this.history = new CollectionView(sorrtedArray);
		this.history.pageSize = 10;
		this.referenzen = new CollectionView(this.detailItem.untergeordneteVEs );
		this.referenzen.pageSize = 10;
		this.referenzen.sortDescriptions.push(new SortDescription('referenceCode', true));
		this.verweise = new CollectionView(this.detailItem.verweiseVEs);
		this.verweise.pageSize = 10;
		this.verweise.sortDescriptions.push(new SortDescription('referenceCode', true));
		const maps: { [id: string]: DataMap; } = {};
		maps['history.anonymisierungsstatus'] = new DataMap([
			{ key: 2, name: 'Prüfung notwendig'},
			{ key: 0, name: 'In Bearbeitung'},
			{ key: 1, name: 'Publiziert'}
		], 'key', 'name');
		this.statusMap = maps['history.anonymisierungsstatus'];
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
			label: this.detailItem.manuelleKorrektur.titel
		});
	}

	public reset() {
		this.reloadData(this.detailItem.manuelleKorrektur);
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
		}
	}
}
