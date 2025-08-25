import {Component, OnInit, ViewChild} from '@angular/core';
import {Router} from '@angular/router';
import {
	ClientContext, ConfigService, CoreOptions, TranslationService, CountriesService, CmiGridComponent
} from '@cmi/viaduc-web-core';
import {ErrorService, UrlService, UserService} from '../../../shared/services/index';
import {DataType, SortDescription} from '@mescius/wijmo';
import {UserListUserSettings} from '../../../shared/model';
import {ManagementUserSettings} from '../../../shared/model/managementUserSettings';
import {Column, DataMap} from '@mescius/wijmo.grid';
import {ODataCollectionView} from '@mescius/wijmo.odata';
import {DetailPagingService} from '../../../shared/services';
import {DateUtilityService} from '../../../shared/services/date-utility.service';

@Component({
	selector: 'cmi-viaduc-user-roles-page',
	templateUrl: 'userRolesPage.component.html',
	styleUrls: ['./userRolesPage.component.less']
})
export class UserRolesPageComponent implements OnInit {

	@ViewChild('flexGrid', { static: true })
	public flexGrid: CmiGridComponent;

	public loading: boolean;
	public error: any;
	public crumbs: any[] = [];
	public columns: any[] = [];
	public showColumnPicker = false;
	public valueFilters: any;
	public hiddenColumns: any[] = [];
	public visibleColumns: any[] = [];
	public visibleColumnsSelector: any[] = [];
	public userList: ODataCollectionView;

	private _language: any = [{name:'de', code:'de'}, {name:'fr', code:'fr'}, {name:'it', code:'it'}, {name:'en', code:'en'}];

	constructor(private _context: ClientContext,
				private _txt: TranslationService,
				private _url: UrlService,
				private _router: Router,
				private _usr: UserService,
				private _err: ErrorService,
				private _cfg: ConfigService,
				private _opt: CoreOptions,
				private _dps: DetailPagingService,
				private _countriesService: CountriesService,
				private _dateUtilityService: DateUtilityService) {
	}

	public get txt(): TranslationService {
		return this._txt;
	}

	public ngOnInit(): void {

		this._buildCrumbs();

		this._loadColumns();
		this._initTableView();
		this._refreshHiddenVisibleColumns();
	}

	public show(item: any): void {
		const index = this.userList.items.indexOf(item);
		this._dps.setCurrent(this.userList, index);

		const id = item ? item.id : 'new';
		this._router.navigate([this._url.getNormalizedUrl('/benutzerundrollen/benutzer') + '/' + id]);
	}

	public saveColumns() {
		const cols = [...this.columns];

		for (const c of cols) {
			const existing: Column = this.flexGrid.columns.filter(col => col.header === c.defaultLabel)[0];
			if (existing) {
				c.width = existing.renderWidth;
				c.format = existing.format;
				c.visible = existing.visible;
				c.index = existing.index;
			} else {
				c.visible = false;
				c.index = 999;
			}
		}

		const sortedArray = cols.sort((a, b) => {
			if (a.index > b.index) {
				return 1;
			}

			if (a.index < b.index) {
				return -1;
			}

			return 0;
		});

		this._saveColumnsAsUserSettings(sortedArray);
	}

	public refreshTable() {
		this.userList.load();
	}

	public resetColumnsToDefault() {
		this.columns = this._cfg.getSetting('user.userListColumns', {}).map(x => Object.assign({}, x));
		this._saveColumnsAsUserSettings(this.columns);
		this.ngOnInit();
		this._resetSorts();
		this.userList.load();

	}

	private _loadColumns(): void {
		const userSettings = this._cfg.getSetting('user.settings') as ManagementUserSettings;
		if (userSettings && userSettings.userListSettings && userSettings.userListSettings.columns) {
			this.columns = userSettings.userListSettings.columns;
		} else {
			this.resetColumnsToDefault();
		}
	}

	public showColumnPickerModal(): void {
		this._refreshHiddenVisibleColumns();
		this.showColumnPicker = true;
	}

	public showColumn(c: any) {
		this.columns.filter(col => col.key === c.key)[0].visible = true;
		this._refreshHiddenVisibleColumns();
	}

	public hideColumn(c: any) {
		this.columns.filter(col => col.key === c.key)[0].visible = false;
		this._refreshHiddenVisibleColumns();
	}

	private _getInitialSortDescriptions(): any[] {
		const sortCol = this.visibleColumns.find(c => c.key === 'familyName');
		if (sortCol) {
			return [new SortDescription(sortCol.key, true)];
		}

		return [];
	}

	private _resetSorts() {
		this.userList.sortDescriptions.clear();
		for (const sc of this._getInitialSortDescriptions()) {
			this.userList.sortDescriptions.push(sc);
		}
	}

	public resetSortsAndFilters() {
		this.flexGrid.filter.clear();
		this._resetSorts();
		this.userList.load();
	}

	public exportExcel() {
		this.flexGrid.exportToExcelOData('benutzer_liste.bar.ch.xlsx').subscribe(() => {
			// nothing
		}, (err) => {
			this._err.showError(err);
		});
	}

	private _saveColumnsAsUserSettings(cols) {
		const existingSettings = this._cfg.getUserSettings() as ManagementUserSettings;
		existingSettings.userListSettings = <UserListUserSettings> {
			columns: cols
		};
		this._usr.updateUserSettings(existingSettings);
	}

	private _refreshHiddenVisibleColumns() {
		this.visibleColumns = this.columns.filter(c => c.visible);
		this.visibleColumnsSelector = this.visibleColumns.filter(c => c.visible && !c.loadPerDefault);
		this.hiddenColumns = this.columns.filter(c => !c.visible);

		if (this.userList) {
			this.userList.fields = this._getFieldsToLoad();
			this.userList.load();
		}
	}

	private _getFieldsToLoad(): string[] {
		const fieldsToLoad = this.visibleColumns.filter(x => x.key !== 'unknown').map(x => x.key);
		const additionalFields = this.hiddenColumns.filter(x => x.loadPerDefault).map(x => x.key);

		// The following fields must be loaded in any case due to their data type, as it is important that the grid correctly initializes the filters and display type (e.g. checkbox)
		const mustFields: string[] = ['barInternalConsultation', 'researcherGroup', 'identifizierungsmittel', 'created', 'createdOn',  'modifiedOn', 'birthday', 'downloadLimitDisabledUntil'];

		additionalFields.forEach((f) => { fieldsToLoad.push(f); });
		mustFields.forEach((f) => { fieldsToLoad.push(f); });
		return fieldsToLoad;
	}

	private _buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({
			url: this._url.getNormalizedUrl('/benutzerundrollen'),
			label: this._txt.get('breadcrumb.usersAndRoles', 'Benutzer und Rollen')
		});
		crumbs.push({label: this._txt.get('breadcrumb.usersRoles', 'Benutzerverwaltung')});
	}

	private _initTableView() {
		this.loading = true;

		this._loadDataMaps();
		this._refreshHiddenVisibleColumns();
		this.userList = new ODataCollectionView(this._opt.odataUrl, 'UserOverview', {
			requestHeaders: { withCredentials: true },
			fields: this._getFieldsToLoad(),
			dataTypes: {
				created: DataType.Date,
				createdOn: DataType.Date,
				modifiedOn: DataType.Date,
				birthday: DataType.Date,
				downloadLimitDisabledUntil: DataType.Date,
				digitalisierungsbeschraenkungAufgehobenBis: DataType.Date
			},
			canFilter: true,
			canSort: true,
			sortOnServer: true,
			pageOnServer: true,
			filterOnServer: true,
			pageSize: 10
		});

		this.userList.error.addHandler((view, error) => {
			this._err.showOdataErrorIfNecessary(error);
		});

		this.userList.loading.addHandler(() => {
			this.loading = true;
		});
		this.userList.loaded.addHandler(() => {
			this.loading = false;
		});

		this.userList.onPageChanged();
	}

	private _loadDataMaps() {
		const countries = this._countriesService.getCountries(this._context.language);
		const countriesList = [];
		this._countriesService.sortCountriesByName(countries).forEach(c => countriesList.push({name: c.code}));
		const countrymap = new DataMap(countriesList, 'name', 'name');

		const maps: { [id: string]: DataMap; } = {};
		maps['language'] = new DataMap(this._language, 'code', 'name');
		maps['reasonForRejection'] = new DataMap(this._getListForRejection(), 'name', 'name');
		maps['rolePublicClient'] = new DataMap(this._getListRolePublicClient(), 'name', 'name');
		maps['eiamRoles'] = new DataMap(this._getListRoleManagementClient(), 'name', 'name');

		if (countrymap) {
			maps['countryCode'] = countrymap;
		}

		this.valueFilters = maps;
	}

	private _getListForRejection(): any {
		return [{ name: 'RejectionNoDataMatch'},
			{ name: 'RejectionNegativeProfiling'},
			{ name: 'RejectionFalseBirthdate'},
			{ name: 'RejectionManipulativeTry'},
			{ name: 'RejectionFalsePersAttributes'},
			{ name: 'RejectionFraudDocument'},
			{ name: 'RejectionMRZCheckDigitError'},
			{ name: 'RejectionDifferingAge'},
			{ name: 'RejectionAgain'},
			{ name: 'RejectionNoDataMatchAndManipulativeTry'},
			{ name: 'RejectionNoDataMatchAndFalsePersAttributes'},
			{ name: 'RejectionNoDataMatchAndNegativeProfiling'},
			{ name: 'RejectionNoDataMatchAndFalseBirthdate'}];
	}

	private _getListRolePublicClient(): any {
		return [{ name: 'Ö2'},
			{ name: 'Ö3'},
			{ name: 'BVW'},
			{ name: 'AS'},
			{ name: 'BAR'}];
	}

	private _getListRoleManagementClient(): any {
		return [{ name: 'APPO'}, { name: 'ALLOW'}];
	}

	public formatDate(date: Date, format: string): string {
		if(format=='dd.MM.yyyy HH:mm:ss') {
			return this._dateUtilityService.formatDate(date, 'DD.MM.YYYY HH:mm:ss');
		}

		return this._dateUtilityService.formatDate(date);
	}
}
