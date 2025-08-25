import {AfterViewInit, Component, OnInit, ViewChild, ViewEncapsulation} from '@angular/core';
import {ApplicationFeatureEnum, ConfigService, TranslationService} from '@cmi/viaduc-web-core';
import {AuthorizationService, ErrorService, UrlService, UserService} from '../../../shared/services';
import {OrderingFlatItem, SelectionPreFilter} from '../../model';
import {WjMenu} from '@mescius/wijmo.angular2.input';
import {EinsichtsgesuchUserSettings, ManagementUserSettings} from '../../../shared/model/managementUserSettings';
import {OrdersListComponent} from '../ordersList/ordersList.component';
import {ToastrService} from 'ngx-toastr';
import {SessionStorageService} from '../../../client/services';
import {NgForm} from '@angular/forms';

@Component({
	selector: 'cmi-viaduc-orders-einsichtsgesuche-list-page',
	templateUrl: 'ordersEinsichtListPage.component.html',
	encapsulation: ViewEncapsulation.None,
	styleUrls: ['./ordersEinsichtListPage.component.less']
})
export class OrdersEinsichtListPageComponent implements OnInit, AfterViewInit {
	@ViewChild('listMenu', { static: true })
	public listMenu: WjMenu;

	@ViewChild('preFilterMenu', { static: true })
	public preFilterMenu: WjMenu;

	@ViewChild(OrdersListComponent, { static: false })
	public ordersList: OrdersListComponent;

	@ViewChild(NgForm, { static: false })
	public formOrderEinsichtlist: NgForm;

	public detailRecords: OrderingFlatItem[];

	public crumbs: any[] = [];
	public columns: any[] = [];
	public hiddenColumns: any[] = [];
	public visibleColumns: any[] = [];
	public preFilter: SelectionPreFilter = SelectionPreFilter.NurPendente;
	public SelectionPreFilter = SelectionPreFilter;
	public showColumnPicker = false;
	public showEntscheidHinterlegen = false;
	public selectedUser: string;
	public showEinsichtsgesucheAbbrechen = false;
	public showAuftraegeZuruecksetzen = false;
	public showDigitalisierungAusloesen = false;
	public showInVorlageExportieren = false;
	public hasRight = false;

	private _previousPreFilter: SelectionPreFilter = null;

	constructor(private _txt: TranslationService,
				private _url: UrlService,
				private _err: ErrorService,
				private _storage: SessionStorageService,
				private _usr: UserService,
				private _toastr: ToastrService,
				private _aut: AuthorizationService,
				private _cfg: ConfigService) {
		this.hasRight = this._aut.hasApplicationFeature(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheView);
	}

	public ngOnInit(): void {
		this._buildCrumbs();
		this._loadColumns();
		this.refreshHiddenVisibleColumns();
	}

	public ngAfterViewInit(): void {
		setTimeout(() => {
			const filter = this._storage.getItem('OrderEinsichtList_preFilter') as SelectionPreFilter;
			if (filter !== null && filter !== undefined) {
				this._previousPreFilter = filter;
				this.preFilter = filter;
			} else {
				this._previousPreFilter = SelectionPreFilter.NurPendente;
				this.preFilter = SelectionPreFilter.NurPendente;
			}
		}, 0);
	}

	private _resetColumnsToDefault() {
		this.columns = this._cfg.getSetting('orders.ordersEinsichtListColumns', {}).map(x => Object.assign({}, x));
		this._saveColumnsAsUserSettings(this.columns);
	}

	private _loadColumns(): void {
		const userSettings = this._cfg.getSetting('user.settings') as ManagementUserSettings;
		if (userSettings.einsichtsGesuchSettings && userSettings.einsichtsGesuchSettings.columns) {
			this.columns = userSettings.einsichtsGesuchSettings.columns;
		} else {
			this._resetColumnsToDefault();
		}
	}

	public get checkedCount() {
		return this.ordersList ? this.ordersList.checkedRowsCount : 0;
	}

	public get checkedIds() {
		return this.ordersList?.checkedRowsCount ? this.ordersList.checkedRowsIds : [];
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
	}

	public listMenuItemClicked(menu: WjMenu) {
		const cmd = menu.selectedIndex;
		switch (cmd) {
			case 0:
				this.saveColumns();
				return;
			case 1:
				this.showColumnPickerModal();
				return;
			case 2:
				this.resetSortsAndFilters();
				return;
			case 3:
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
		if (!changedObj) {
			return;
		}

		if (this._previousPreFilter === this.preFilter) {
			return;
		}

		this._previousPreFilter = this.preFilter;
		this.preFilter = changedObj.value || changedObj;
		this._storage.setItem('OrderEinsichtList_preFilter', this.preFilter);

		switch (this.preFilter) {
			case SelectionPreFilter.NurPendente:
				this.ordersList.showPendente();
				return;
			case SelectionPreFilter.Alle:
				this.ordersList.showAll();
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

	private _saveColumnsAsUserSettings(cols) {
		const existingSettings = this._cfg.getUserSettings() as ManagementUserSettings;
		existingSettings.einsichtsGesuchSettings = <EinsichtsgesuchUserSettings> {
			columns: cols
		};

		this._usr.updateUserSettings(existingSettings);
	}

	public refreshHiddenVisibleColumns() {
		this.visibleColumns = this._getVisibleColumns();
		this.hiddenColumns = this._getHiddenColumns();

		if (this.ordersList) {
			this.ordersList.refreshHiddenVisibleColumns();
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

	public showEntscheidHinterlegenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheKannEntscheidGesuchHinterlegen)) {
			return;
		}

		if (this.ordersList.checkedRowsCount > 0) {
			this.detailRecords = this.ordersList.currentChecked;
			const users = this.detailRecords.map((i: OrderingFlatItem) => i.user);
			if (users && users.length > 0) {
				if (users.length > 1) {
					for (const c of users)	{
						if (c !== users[0]) {
							this._toastr.error('Ausgewählte Gesuche sind von unterschiedlichen Benutzern', 'Entscheid Gesuch hinterlegen');
							return;
						}
					}
				}

				this.selectedUser = users[0];
			}

			this.showEntscheidHinterlegen = true;
		}
	}

	public showInVorlageExportierenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheInVorlageExportieren)) {
			return;
		}

		if (this.ordersList.checkedRowsCount > 0) {
			this.detailRecords = this.ordersList.currentChecked;
			this.showInVorlageExportieren = true;
		}
	}

	public showDigitalisierungsauftragAusfuehren() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheDigitalisierungAusloesenAusfuehren)) {
			return;
		}

		const ids = this.checkedIds;
		if (ids.length !== 0) {
			this.showDigitalisierungAusloesen = true;
		}
	}

	public showEinsichtsgesucheAbbrechenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheKannAbbrechen)) {
			return;
		}

		this.showEinsichtsgesucheAbbrechen = true;
	}

	public resetSortsAndFilters() {
		this.ordersList.resetFilters();
		this.ordersList.ngOnInit();
		this.ordersList.resetSorts(); // Reihenfolge ist wichtig!
	}

	public showAuftraegeZuruecksetzenModal() {
		if (!this._err.verifyApplicationFeatureOrShowError(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheKannZuruecksetzen)) {
			return;
		}

		this.showAuftraegeZuruecksetzen = true;
	}

}
