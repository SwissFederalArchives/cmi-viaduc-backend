import {Component, HostListener, ViewChild, ViewEncapsulation} from '@angular/core';
import {
	EntityDecoratorService,
	TranslationService,
	ApplicationFeatureEnum,
	EntscheidGesuchStatus,
	Abbruchgrund, StammdatenService, ArtDerArbeit, ShippingType, ApproveStatus, ComponentCanDeactivate
} from '@cmi/viaduc-web-core';
import {AuthorizationService, DetailPagingService, ErrorService, UrlService} from '../../../shared/services';
import {Bestellhistorie, OrderingFlatDetailItem, OrderingFlatItem, StatusHistory} from '../../model';
import {OrderService} from '../../services';
import {ActivatedRoute} from '@angular/router';
import * as moment from 'moment';
import {ToastrService} from 'ngx-toastr';
import {NgForm} from '@angular/forms';

@Component({
	selector: 'cmi-viaduc-orders-einsichtsgesuche-detail-page',
	templateUrl: 'ordersEinsichtDetailPage.component.html',
	encapsulation: ViewEncapsulation.None,
	styleUrls: ['./ordersEinsichtDetailPage.component.less']
})
export class OrdersEinsichtDetailPageComponent  extends ComponentCanDeactivate {
	@ViewChild('formEinsichtDetail', {static: false})
	public formEinsichtDetail: NgForm;

	public loading: boolean;
	public crumbs: any[] = [];
	public detailRecord: OrderingFlatDetailItem;

	public artDerArbeiten: ArtDerArbeit[] = [];
	public fieldInfos: any[];
	public showEntscheidHinterlegen: boolean;
	public showEinsichtsgesucheAbbrechen: boolean;
	public showAuftraegeZuruecksetzen: boolean;
	public showInVorlageExportieren: boolean;
	public showDigitalisierungAusloesen: boolean;
	public showOrderHistoryModal = false;

	public historyItems: Bestellhistorie[];
	public detailPagingEnabled: boolean;
	public isNavFixed = false;

	private _recordId: number;

	constructor(private _aut: AuthorizationService,
				private _dec: EntityDecoratorService,
				private _ord: OrderService,
				private _url: UrlService,
				private _dps: DetailPagingService,
				private _toastr: ToastrService,
				private _stm: StammdatenService,
				private _txt: TranslationService,
				private _err: ErrorService,
				private _route: ActivatedRoute) {

		super();
		this._route.params.subscribe(params => {
			this._recordId = params['id'];
			this.init();
		});
		this._stm.getArtDerArbeiten().subscribe((arbeiten) => {
			this.artDerArbeiten = arbeiten;
		});


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

	public init(): void {
		this.loading = true;
		this.detailPagingEnabled = this._dps.getCurrentIndex() > -1;
		this._ord.getOrderingDetail(this._recordId).subscribe(r => {
			this.loading = false;
			this.detailRecord = r;
			this.formEinsichtDetail.resetForm();
			this._buildCrumbs();
			this._ord.getEinsichtsgesuchOrderingDetailFields().subscribe(fields => {
				this.fieldInfos = fields;
			});
		});

	}

	private _buildCrumbs(): void {
		this.crumbs = [];
		this.crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		this.crumbs.push({
			url: this._url.getAuftragsuebersichtUrl(),
			label: this._txt.get('breadcrumb.orderings', 'Auftragsübersicht')
		});
		this.crumbs.push({
			url: this._url.getEinsichtsgesucheUebersichtUrl(),
			label: this._txt.get('breadcrumb.orderingsEinsicht', 'Einsichtsgesuche')
		});
		this.crumbs.push({
			url: this._url.getEinsichtsgesucheUebersichtUrl() + '/' + this.detailRecord.itemId,
			label: this.detailRecord.dossiertitel
		});
	}

	public hasRight(): boolean {
		return this._aut.hasApplicationFeature(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheView);
	}

	public getExternalStatus(): string {
		return this._dec.translateExternalStatus(this.detailRecord.externalStatus);
	}

	public getInternalStatus(): string {
		return this._dec.translateInternalStatus(this.detailRecord.status);
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

	public getAbbruchgrund(type: Abbruchgrund): string {
		return this._dec.translateAbbruchgrund(type);
	}

	public getEingangsart(art: number): string {
		return art === 0 ? 'Durch Kunde erfasst' : 'Durch BAR erfasst';
	}

	public getFormattedDate(dt: Date | string) {
		return dt ? moment.utc(dt).format('DD.MM.YYYY, HH:mm:ss') : '';
	}

	public getFormattedDateWithoutTime(dt: Date | string) {
		return dt ? moment.utc(dt).format('DD.MM.YYYY') : '';
	}

	public getFormattedStatusHistoryToStatus(h: StatusHistory): string {
		return this._dec.translateInternalStatus(h.toStatus);
	}

	public getEntscheidGesuch(entscheid: EntscheidGesuchStatus) {
		return this._dec.translateEntscheidGesuchStatus(entscheid);
	}

	public loadOrderHistoryModal(): void {
		this._ord.getOrderingHistoryForVe(this.detailRecord.veId).subscribe(r => {
			this.historyItems = r;
			this.showOrderHistoryModal = true;
		});
	}

	public updateOrderDetail() {
		this._ord.einsichtsgesuchUpdateOrderingDetail(this.detailRecord as OrderingFlatItem).subscribe(() => {
			this.reset();
			this._toastr.success(this._txt.get('ordersDetailPage.updateSuccess', 'Erfolgreich gespeichert'));
		}, () => {
			this._toastr.error(this._txt.get('ordersDetailPage.updateError', 'Fehler beim Speichern'));
		});
	}

	public reset() {
		this.init();
	}

	public getDateAsString(field: any): string {
		if (field) {
			const val = moment.utc(field).format('DD.MM.YYYY');
			return (val === '01.01.0001') ? null : val;
		}
		return null;
	}

	public getDetailBaseUrl(): string {
		return this._url.getEinsichtsgesucheUebersichtUrl();
	}

	public isFieldReadonly(field: string) {
		const info = this.fieldInfos.filter(i => i.name.toLowerCase() === field.toLowerCase())[0];
		if (info) {
			return info.isReadonly;
		}
		return true;
	}

	public showEntscheidHinterlegenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheKannEntscheidGesuchHinterlegen)) {
			return;
		}

		this.showEntscheidHinterlegen = true;
	}

	public showDigitalisierungsauftragAusfuehren() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheDigitalisierungAusloesenAusfuehren)) {
			return;
		}

		if (this._recordId !== 0) {
			this.showDigitalisierungAusloesen = true;
		}
	}

	public seeHiddenOrderDetail() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheViewNichtSichtbar)) {
			return;
		}

		this._ord.getUnHiddenOrderingDetail(this._recordId).subscribe(item => {
			this.detailRecord = item;
			this._toastr.success(this._txt.get('order.seeHiddenOrderDetail', 'Die Daten wurden erfolgreich aufgedeckt.'),
				this._txt.get('', 'Daten einsehen'));
			this._buildCrumbs();
		});
	}

	public showEinsichtsgesucheAbbrechenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheKannAbbrechen)) {
			return;
		}

		this.showEinsichtsgesucheAbbrechen = true;
	}

	public showAuftraegeZuruecksetzenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheKannZuruecksetzen)) {
			return;
		}

		this.showAuftraegeZuruecksetzen = true;
	}

	public showInVorlageExportierenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheInVorlageExportieren)) {
			return;
		}

		this.showInVorlageExportieren = true;
	}

	public canDeactivate(): boolean {
		return !this.formEinsichtDetail.dirty;
	}

	public promptForMessage(): false | 'question' | 'message' {
		return  'question';
	}

	public message(): string {
		return this._txt.get('hints.unsavedChanges', 'Sie haben ungespeicherte Änderungen. Wollen Sie die Seite tatsächlich verlassen?');
	}

	public getFormIsDirty():boolean {
		if (this.formEinsichtDetail) {
			return this.formEinsichtDetail.dirty;
		}
		return false;
	}
}
