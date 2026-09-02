import {Component, OnInit, ViewChild, ViewEncapsulation} from '@angular/core';
import {ApplicationFeatureEnum, ConfigService, InternalStatus, ShippingType, TranslationService, Utilities as _util} from '@cmi/viaduc-web-core';
import {AuthorizationService, ErrorService, UiServiceMC, UrlService, UserService} from '../../../shared/services';
import {OrderingFlatItem, SelectionPreFilter} from '../../model';
import {WjMenu} from '@mescius/wijmo.angular2.input';
import {ManagementUserSettings, OrderUserSettings, User} from '../../../shared/model';
import {OrderService} from '../../services';
import {OrdersListComponent} from '../ordersList/ordersList.component';
import moment from 'moment';
import {SessionStorageService} from '../../../client/services';

@Component({
    selector: 'cmi-viaduc-orders-list-page',
    templateUrl: 'ordersListPage.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['./ordersListPage.component.less'],
    standalone: false
})
export class OrdersListPageComponent implements OnInit {
	@ViewChild('listMenu', { static: false })
	public listMenu: WjMenu;

	@ViewChild('preFilterMenu', { static: false })
	public preFilterMenu: WjMenu;

	@ViewChild('orderslist', { static: false })
	public ordersList: OrdersListComponent;

	public crumbs: any[] = [];
	public columns: any[] = [];
	public hiddenColumns: any[] = [];
	public visibleColumns: any[] = [];
	public preFilter!: SelectionPreFilter;
	public SelectionPreFilter = SelectionPreFilter;
	public showColumnPicker = false;
	public showEntscheidHinterlegen = false;
	public showAuftraegeAbschliessen = false;
	public selectedInternalComment!: string;
	public selectedrolePublicClient!: string;
	public selectedBewilligungsDate?: Date;
	public showAuftraegeAbbrechen = false;
	public showAuftraegeZuruecksetzen = false;
	public showAuftraegeAusleihen = false;
	public showAuftraegeReponieren = false;
	public showAuftraegeMahnungVersenden = false;
	public showAuftraegeErinnerungVersenden = false;
	public showBarCode = false;
	public hasRight = false;
	public barcodeSet: boolean = false;

	private _previousPreFilter!: SelectionPreFilter;
	private _isInitializing = true;
	public isBusy = false;

	constructor(private _txt: TranslationService,
				private _url: UrlService,
				private _usr: UserService,
				private _ord: OrderService,
				private _ui: UiServiceMC,
				private _storage: SessionStorageService,
				private _err: ErrorService,
				private _aut: AuthorizationService,
				private _cfg: ConfigService) {
		this.hasRight = this._aut.hasApplicationFeature(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeView);
		this._isInitializing = true;
	}

	public ngOnInit(): void {
		this._buildCrumbs();
		this._loadColumns();
		this._initTableView();
	}

	private _resetColumnsToDefault() {
		this.columns = this._cfg.getSetting('orders.ordersListColumns', {}).map((x: any) => Object.assign({}, x));
		this._saveColumnsAsUserSettings(this.columns);
	}

	private _loadColumns(): void {
		const userSettings = this._cfg.getSetting('user.settings') as ManagementUserSettings;
		if (userSettings.orderSettings && userSettings.orderSettings.columns) {
			this.columns = userSettings.orderSettings.columns;
		} else {
			this._resetColumnsToDefault();
		}
	}

	private _initTableView() {
		this.refreshHiddenVisibleColumns();
		this._restorePreFilterButtonState();
	}

	private _restorePreFilterButtonState() {
		setTimeout(() => {
			const filter = this._storage.getItem('AL_PreFilterButton') as SelectionPreFilter;
			if (filter !== null && filter !== undefined) {
				if (filter === SelectionPreFilter.Barcode) {
					this.barcodeSet = true;
				} else {
					this._previousPreFilter = filter;
				}
				setTimeout(() => {
					this.preFilter = filter;
					this._isInitializing = false;
				}, 50);
			} else {
				this._isInitializing = false;
				this.preFilterItemClicked(SelectionPreFilter.NurPendente );
			}
		}, 0);
	}

	private _buildCrumbs(): void {
		this.crumbs = [];
		this.crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		this.crumbs.push({
			url: this._url.getAuftragsuebersichtUrl(),
			label: this._txt.get('breadcrumb.orderings', 'Auftragsübersicht')
		});
	}

	public get checkedCount() {
		return this.ordersList ? this.ordersList.checkedRowsCount : 0;
	}

	public get checkedIds() {
		return this.ordersList?.checkedRowsCount ? this.ordersList.checkedRowsIds : [];
	}

	public listMenuItemClicked(menu: WjMenu) {
		const cmd = menu.selectedIndex;
		switch (cmd) {
			case 0:
				this.saveColumns();
				return;
			case 1:
				this.exportExcel();
				return;
			case 2:
				this.showColumnPickerModal();
				return;
			case 3:
				this.resetSortsAndFilters();
				return;
			case 4:
				this.resetView();
				return;
			default:
				this._notImplementedYet();
				return;
		}
	}

	private _notImplementedYet() {
		alert('not implemented yet');
	}

	public preFilterItemClicked(changedObj:number | any) {
		if (!changedObj || this._isInitializing) {
			return;
		}
		if (this._previousPreFilter === this.preFilter) {
			return;
		}

		this._previousPreFilter = this.preFilter;
		this.preFilter = changedObj.value || changedObj;
		this._storage.setItem('AL_PreFilterButton', this.preFilter);

		switch (this.preFilter) {
			case SelectionPreFilter.NurPendente:
				this.barcodeSet = false;
				this.ordersList.showPendente();
				return;
			case SelectionPreFilter.Alle:
				this.barcodeSet = false;
				this.ordersList.showAll();
				return;
			case SelectionPreFilter.Barcode:
				this.barcodeSet = true;
				return;
			default:
				this._notImplementedYet();
				return;
		}
	}

	public _getVisibleColumns(): any[] {
		return this.columns.filter(c => c.visible);
	}

	public _getHiddenColumns(): any [] {
		return this.columns.filter(c => !c.visible);
	}

	public showColumn(c: any) {
		this.columns.filter(col => col.key === c.key)[0].visible = true;
		this.refreshHiddenVisibleColumns();
	}

	public hideColumn(c: any) {
		this.columns.filter(col => col.key === c.key)[0].visible = false;
		this.refreshHiddenVisibleColumns();
	}

	public refreshTable() {
		this.ordersList.refreshTable();
	}

	public saveColumns() {
		const cols = this.ordersList.getCurrentColumns();
		this._saveColumnsAsUserSettings(cols);
	}

	private _saveColumnsAsUserSettings(cols: any) {
		const existingSettings = this._cfg.getUserSettings() as ManagementUserSettings;
		existingSettings.orderSettings = <OrderUserSettings> {
			columns: cols
		};
		this._usr.updateUserSettings(existingSettings);
	}

	public refreshHiddenVisibleColumns() {
		this.visibleColumns = this._getVisibleColumns();
		this.hiddenColumns = this._getHiddenColumns();

		if (this.ordersList) {
			this.ordersList.refreshTable();
		}
	}

	public exportExcel() {
		this.ordersList.exportExcel();
	}

	public showColumnPickerModal() {
		this.showColumnPicker = true;
	}

	public resetView() {
		this._resetColumnsToDefault();
		this.refreshHiddenVisibleColumns();
		this.ordersList.ngOnInit();
		this.ordersList.resetSorts();
	}

	public showAushebungsAuftrag() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeKannAushebungsauftraegeDrucken)) {
			return;
		}
		this.isBusy = true;
		const checkedItemids = this.ordersList.checkedRowsIds;
		this._ord.getAushebungsAuftragHtml(checkedItemids).subscribe(html => {
			this.isBusy = false;
			this._ui.showHtmlInNewTab(html, this._txt.get('aushebungsauftrag', 'Aushebungsauftrag'));
		}, (error) => {
			this.isBusy = false;
			this._err.showError(error);
		});
	}

	public showVersandkontrolle() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeVersandkontrolleAusfuehren)) {
			return;
		}
		this.isBusy = true;
		const checkedItemids = this.ordersList.checkedRowsIds;
		this._ord.getVersandkontrolleHtml(checkedItemids).subscribe(html => {
			this.isBusy = false;
			this._ui.showHtmlInNewTab(html, this._txt.get('versandkontrolle', 'Versandkontrolle'));
		}, (error) => {
			this.isBusy = false;
			this._err.showError(error);
		});
	}

	public resetSortsAndFilters() {
		this.ordersList.resetFilters();
		this.ordersList.ngOnInit();
		this.ordersList.resetSorts(); // Reihenfolge ist wichtig!
	}

	public showEntscheidHinterlegenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeKannEntscheidFreigabeHinterlegen)) {
			return;
		}

		this.selectedBewilligungsDate = undefined;
		this.selectedInternalComment = '';

		if (this.ordersList.checkedRowsCount > 0) {
			const checkedItems = Array.from(this.ordersList.currentChecked.values());

			const bewilligungDates = checkedItems.map((i: OrderingFlatItem) => i.bewilligungsDatum);
			if (bewilligungDates && bewilligungDates.length > 0) {

				if (bewilligungDates.length > 1) {
					for (const d of bewilligungDates) {
						if (!bewilligungDates[0] && d) {
							this._ui.showError('Ausgewählte Aufträge haben unterschiedliche Bewilligungsdaten', 'Freigabekontrolle');
							return;
						}

						if (!bewilligungDates[0] && !d) {
							continue;
						}

						if (!moment(d).isSame(moment(bewilligungDates[0]), 'day')) {
							this._ui.showError('Ausgewählte Aufträge haben unterschiedliche Bewilligungsdaten', 'Freigabekontrolle');
							return;
						}
					}
				}

				this.selectedBewilligungsDate = bewilligungDates[0];
			}

			const internalComments = checkedItems.map((i: OrderingFlatItem) => i.internalComment);
			if (internalComments && internalComments.length > 0) {
				if (internalComments.length > 1) {
					for (const c of internalComments) {
						if (c !== internalComments[0]) {
							this._ui.showError('Ausgewählte Aufträge haben unterschiedliche interne Bemerkungen', 'Freigabekontrolle');
							return;
						}
					}
				}

				this.selectedInternalComment = internalComments[0];
			}

			const userIds = checkedItems.map((i: OrderingFlatItem) => i.userId);
			this._usr.getUsers(userIds).then((users) => {
				if (users) {
					const rolePublicClients = users.map((u: User) => u.rolePublicClient);
					if (rolePublicClients && rolePublicClients.length > 0) {
						if (rolePublicClients.length > 1) {
							const oe2Count = rolePublicClients.filter((r: any) => r === 'Ö2').length;
							if (oe2Count !== 0 && oe2Count !== rolePublicClients.length) {
								this._ui.showError('Registrierte und identifizierte BenutzerInnen vorhanden.', 'Freigabekontrolle');
								return;
							}
						}
					}

					this.selectedrolePublicClient = rolePublicClients[0];
					this.showEntscheidHinterlegen = true;
				}
			});
		}
	}

	public showAuftraegeAbschliessenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeKannAbschliessen)) {
			return;
		}

		this.showAuftraegeAbschliessen = true;
	}

	public showAuftraegeAbbrechenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeKannAbbrechen)) {
			return;
		}

		this.showAuftraegeAbbrechen = true;
	}

	public showAuftraegeAusleihenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeKannAuftraegeAusleihen)) {
			return;
		}

		this.showAuftraegeAusleihen = true;
	}

	public showAuftraegeReponierenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeKannReponieren)) {
			return;
		}

		this.showAuftraegeReponieren = true;
	}

	public showAuftraegeZuruecksetzenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeKannZuruecksetzen)) {
			return;
		}

		this.showAuftraegeZuruecksetzen = true;
	}

	public showMahnungVersendenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeMahnungVersenden)) {
			return;
		}

		if (this.ordersList.checkedRowsCount > 0) {
			const checkedItems = Array.from(this.ordersList.currentChecked.values());

			const filtered = checkedItems.filter((i: OrderingFlatItem) =>
				i.status !== InternalStatus.Ausgeliehen ||
				!(i.orderingType === ShippingType.Lesesaalausleihen || i.orderingType === ShippingType.Verwaltungsausleihe)
			);

			if (filtered.length > 0) {
				this._ui.showError(`Mindestens ein markierter Auftrag ist nicht vom Typ «Verwaltungsausleihen» oder «Lesesaalausleihen»
				und/oder er ist nicht im  internen Status «Ausgeliehen». Der erste fehlerhafte Auftrag hat die ID ${filtered[0].itemId}`,
					'Versand nicht erlaubt');
				return;
			}
		}

		this.showAuftraegeMahnungVersenden = true;
	}

	public showErinnerungVersendenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeErinnerungVersenden)) {
			return;
		}

		if (this.ordersList.checkedRowsCount > 0) {
			const checkedItems = Array.from(this.ordersList.currentChecked.values());

			const filtered = checkedItems.filter((i: OrderingFlatItem) =>
				i.status !== InternalStatus.Ausgeliehen ||
				i.orderingType !== ShippingType.Lesesaalausleihen);

			if (filtered.length > 0) {
				this._ui.showError(`Mindestens ein markierter Auftrag ist nicht vom Typ «Lesesaalausleihen»
				und/oder er ist nicht im internen Status «Ausgeliehen». Der erste fehlerhafte Auftrag hat die ID ${filtered[0].itemId}`,
					'Versand nicht erlaubt');
				return;
			}

		this.showAuftraegeErinnerungVersenden = true;
		}
	}

	public showBarCodeModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeBarcodesEinlesenAusfuehren)) {
			return;
		}

		this.showBarCode = true;
	}

	public barcodesChanged(value: string[]) {
		this.showBarCode = false;

		if (_util.isEmpty(value)) {
			this._ui.showWarning('Es wurde keine Filterung vorgenommen, da keine Werte eingegeben wurden');
			return;
		}

		this.barcodeSet = true;

		setTimeout(() => {
			this.ordersList.barCodesFilter(value);
			this.preFilter = SelectionPreFilter.Barcode;
			this.preFilterItemClicked(SelectionPreFilter.Barcode);
			this._ui.showSuccess('Die Aufträge wurden erfolgreich anhand der Barcodes gefiltert');
		}, 50);
	}

	protected showBehaeltnisInhalt() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeKannBehaeltnisInhaltDrucken)) {
			return;
		}
		this.isBusy = true;
		const checkedItemIds = this.ordersList.currentChecked;
		const behaeltnisNummern = checkedItemIds.map(
			(item: OrderingFlatItem) => item.behaeltnisNummer
		);
		this._ord.getBehaeltnisInhaltHtml(behaeltnisNummern).subscribe(html => {
			this.isBusy = false;
			this._ui.showHtmlInNewTab(html, this._txt.get('behaeltnisInhalt', 'BehaeltnisInhalt'));
		}, (error) => {
			this.isBusy = false;
			this._err.showError(error);
		});
	}
}
