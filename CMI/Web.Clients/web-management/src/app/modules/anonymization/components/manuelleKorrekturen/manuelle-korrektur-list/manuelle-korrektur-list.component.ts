import {Component, ElementRef, OnInit, ViewChild} from '@angular/core';
import {
	CmiGridComponent,
	ConfigService,
	CoreOptions, ManuelleKorrekturDto,
	TranslationService, Utilities as _util
} from '@cmi/viaduc-web-core';
import {ODataCollectionView} from '@mescius/wijmo.odata';
import {DetailPagingService, ErrorService, ManagementUserSettings, ManuelleKorrekturSettings, UiServiceMC, UrlService, UserService} from '../../../../shared';
import {Column, DataMap} from '@mescius//wijmo.grid';
import {WjMenu} from '@mescius/wijmo.angular2.input';
import {DataType, SortDescription, } from '@mescius/wijmo';
import {FormBuilder, FormGroup} from '@angular/forms';
import {ManuelleKorrekturenService} from '../../../services/manuelleKorrekturen-services';
import {Observable} from 'rxjs';
import {Router} from '@angular/router';
import {SessionStorageService} from '../../../../client';

@Component({
	selector: 'cmi-manuelle-korrektur-list',
	templateUrl: './manuelle-korrektur-list.component.html',
	styleUrls: ['./manuelle-korrektur-list.component.less']
})
export class ManuelleKorrekturListComponent implements OnInit {

	public manuelleKorrekturItems: ODataCollectionView;
	@ViewChild('flexGrid', { static: true })
	public flexGrid: CmiGridComponent;
	@ViewChild('preFilterMenu', { static: true })
	public preFilterMenu: WjMenu;
	@ViewChild('addModal')
	private searchElement: ElementRef;

	public columns: any[] = [];
	public hiddenColumns: any[] = [];
	public visibleColumns: any[] = [];
	public visibleColumnsSelector: any[] = [];
	public crumbs: any[] = [];
	public veIds = '';
	public baseFilterString = '';

	public loading: boolean;
	public allowManuelleKorrekturenBearbeiten = true;
	public showColumnPicker: boolean;
	public myForm: FormGroup;
	public showAddModal = false;
	public showDeleteModal = false;
	public valueFilters: any;
	private isInitializing = true;
	public preFilter: number = null;
	private previousPreFilter: number = null;

	constructor(private _opt: CoreOptions,
				private _txt: TranslationService,
				private _url: UrlService,
				private _usr: UserService,
				private _ui: UiServiceMC,
				private _err: ErrorService,
				private _dps: DetailPagingService,
				private _cfg: ConfigService,
				private _router: Router,
				private _storage: SessionStorageService,
				private _service: ManuelleKorrekturenService,
				private _formBuilder: FormBuilder) {
		this.isInitializing = true;
	}

	public ngOnInit(): void {
		this.buildCrumbs();
		this._initTableView();
		const maps: { [id: string]: DataMap; } = {};
		maps['anonymisierungsstatus'] = new DataMap([
			{key: 2, name: 'Prüfung notwendig'},
			{key: 0, name: 'In Bearbeitung'},
			{key: 1, name: 'Publiziert'}
		], 'key', 'name');
		this.valueFilters = maps;
		this.buildForm();
	}

	private _loadColumns(): void {
		this._usr.getUserSettings().then((settings) => {
			const userSettings = settings as ManagementUserSettings;
			if (userSettings.manuelleKorrekturSettings && userSettings.manuelleKorrekturSettings.columns) {
				this.columns = userSettings.manuelleKorrekturSettings.columns;
			} else {
				this._resetColumnsToDefault();
			}
			this.refreshHiddenVisibleColumns();
		});
	}

	public getVisibleColumns(): any[] {
		return this.columns.filter(c => c.visible);
	}

	public getAndSortCurrentColumns(): any[] {
		const cols = [...this.columns];

		for (const c of cols) {
			const existing: Column = this.flexGrid.columns.filter(col => col.header === c.defaultLabel)[0];
			if (existing) {
				c.width = existing.renderWidth;
				c.format = existing.format;
				c.visible = true;
				c.index = existing.index;
			} else {
				c.visible = false;
				c.index = 999;
			}
		}

		return cols.sort((a, b) => {
			if (a.index > b.index) {
				return 1;
			}

			if (a.index < b.index) {
				return -1;
			}

			return 0;
		});
	}

	public getHiddenColumns(): any [] {
		return this.columns.filter(c => !c.visible);
	}
	public refreshHiddenVisibleColumns() {
		this.visibleColumns = this.getVisibleColumns();
		this.visibleColumnsSelector = this.visibleColumns.filter(c => c.visible && !c.loadPerDefault);
		this.hiddenColumns = this.getHiddenColumns();
	}

	private _resetColumnsToDefault() {
		this.columns = this._cfg.getSetting('manuellekorrektur.listColumns').map(x => Object.assign({}, x));
		this._saveColumnsAsUserSettings(this.columns);
	}

	public saveColumns() {
		this._saveColumnsAsUserSettings(this.getAndSortCurrentColumns());
	}

	private _saveColumnsAsUserSettings(cols) {
		const existingSettings = this._cfg.getUserSettings() as ManagementUserSettings;
		existingSettings.manuelleKorrekturSettings = <ManuelleKorrekturSettings> {
			columns: cols
		};
		this._usr.updateUserSettings(existingSettings);
	}

	private _initTableView() {
		this._loadColumns();
		this.restorePreFilterButtonState();
		this.getDefaultView();
	}

	private getDefaultView() {
		this.manuelleKorrekturItems = new ODataCollectionView(this._opt.odataUrl, 'VManuelleKorrekturen', {
			requestHeaders:  { withCredentials: true },
			dataTypes: {
				signatur: DataType.String,
				anonymisierungsstatus: DataType.String,
				veId: DataType.String,
				erzeugtAm: DataType.Date
			},
			canFilter: true,
			canSort: true,
			sortOnServer: true,
			pageOnServer: true,
			filterOnServer: true,
			filterDefinition: this.preFilter === null || this.preFilter === 1  ? '(Anonymisierungsstatus ne 1)' : '',
			pageSize: 10
		});
		this.manuelleKorrekturItems.error.addHandler((view, error) => {
			this._err.showOdataErrorIfNecessary(error);
		});
		this.manuelleKorrekturItems.loading.addHandler(() => {
			this.loading = true;
		});
		this.manuelleKorrekturItems.loaded.addHandler(() => {
			this.loading = false;
		});
	}

	private restorePreFilterButtonState() {
		setTimeout(() => {
			const filter = this._storage.getItem('MK_PreFilterStringValue') as number;
			if (filter !== null && filter !== undefined) {
				this.preFilter = filter;
			} else {
				this.preFilter = 1;
			}

			this.isInitializing = false;
			this.baseFilterString = this.preFilter === 1  ? '(Anonymisierungsstatus ne 1)' : '';
			// Set Default Filter wenn keiner gesetzt ist
			if (this.manuelleKorrekturItems.filterDefinition === undefined || this.manuelleKorrekturItems.filterDefinition === '') {
				this.manuelleKorrekturItems.filterDefinition = this.baseFilterString;
			}

		},
		50);
	}

	private buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({label: this._txt.get('breadcrumb.mauelleKorrekturen', 'Mauelle Korrekturen')});
	}

	public openDetail(item: ManuelleKorrekturDto) {
		this._dps.setCurrent(this.manuelleKorrekturItems, this.manuelleKorrekturItems.items.indexOf(item));
		this._router.navigate([this._url.getNormalizedUrl('/anonymization/manuelleKorrekturen') + '/' + item.manuelleKorrekturId]);
	}

	public toggleAddModal() {
		this.showAddModal = !this.showAddModal;
		if (this.showAddModal) {
			this.buildForm();
			setTimeout(() => {
				this.searchElement.nativeElement.focus();
			}, 0);
		}
	}
	public  toggleDeleteModal() {
		this.showDeleteModal = !this.showDeleteModal;
	}

	public getQuantityOfCheckedItmesToDelete(): number {
		const counter = 0;
		if (!this.flexGrid) {
			return counter;
		}

		return this.flexGrid.checkedItems.length;
	}

	public getElementToDelete(): string {
		if (this.getQuantityOfCheckedItmesToDelete() !== 1) {
			return '';
		}

		return this.flexGrid.checkedItems[0].signatur;
	}

	public refreshTable() {
		this.manuelleKorrekturItems.load();
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

	public preFilterItemClicked(menu: WjMenu) {
		this.preFilterItemSet(Number(menu.value));
	}

	private preFilterItemSet(key: number) {
		if (!key || this.isInitializing) {
			return;
		}
		if (this.previousPreFilter === this.preFilter) {
			return;
		}

		// Setting a pre filter removes all current filters.
		if (this.flexGrid && this.flexGrid.filter) {
			this.flexGrid.filter.clear();
		}

		this.previousPreFilter = this.preFilter;
		this.preFilter = key;
		this.baseFilterString = this.preFilter === 1 ? '(Anonymisierungsstatus ne 1)' : '';
		this._storage.setItem('MK_PreFilterStringValue', this.preFilter );
		if (this.manuelleKorrekturItems) {
			// Überschreibt alle bisherigen Filter z. B. Titel Contains Land
			this.manuelleKorrekturItems.filterDefinition = this.baseFilterString;
		}
	}

	// eslint-disable-next-line
	public onFilterApplied(ev) {
		if (!_util.isEmpty(this.baseFilterString)) {
			const filterBefore = this.manuelleKorrekturItems.filterDefinition.toString();
			if (_util.isEmpty(filterBefore) || filterBefore.indexOf(this.baseFilterString) < 0) {
				let filter = this.baseFilterString;

				if (!_util.isEmpty(filterBefore)) {
					filter += ' and ' + filterBefore;
				}
				this.manuelleKorrekturItems.filterDefinition = filter;
			}
		}
	}

	public exportExcel() {
		if (!this.flexGrid) {
			return;
		}

		const fileName = this._txt.get('anonymization.manuelleKorrekturen', 'anonymisierung.manuelleKorrekturen.xlsx');
		this.flexGrid.exportToExcelOData(fileName).subscribe(() => {
			// nothing
		}, (err) => {
			this._err.showError(err);
		});
	}

	public showColumnPickerModal() {
		this.showColumnPicker = true;
	}

	public resetView() {
		this._resetColumnsToDefault();
		this.refreshHiddenVisibleColumns();
		this.manuelleKorrekturItems.sortDescriptions.clear();
		for (const sc of this._getInitialSortDescriptions()) {
			this.manuelleKorrekturItems.sortDescriptions.push(sc);
			this.flexGrid.defaultSortColumnKey = 'manuelleKorrekturId';
		}
		this.manuelleKorrekturItems.filterDefinition = this.baseFilterString;
	}

	public showColumn(c: any) {
		this.columns.filter(col => col.key === c.key)[0].visible = true;
		this.refreshHiddenVisibleColumns();
	}

	public hideColumn(c: any) {
		this.columns.filter(col => col.key === c.key)[0].visible = false;
		this.refreshHiddenVisibleColumns();
	}

	public async addNewManuelleKorrekturenConfirm() {
		this.showAddModal = false;
		this.veIds = this.myForm.controls['veIds'].value;
		const myArray = this.veIds .split('\n');
		this.loading = true;
		const map = await this._service.BatchAddManuelleKorrektur(myArray) as  Map<string, string>;
		let message = '' ;

		const maxLength = myArray.length;
		for (let i = 0; i < maxLength; i++) {
			if (map[i]) {
				message = message + map[i] + '<br/>' + (i === maxLength - 1 ? '' : '-----------<br/>');
			} else {
				break;
			}
		}

		if (message.length > 0) {
			this._ui.showWarning( message, null, { disableTimeOut: true, closeButton: true, enableHtml: true});
		} else {
			this._ui.showSuccess('Erfolgreich hinzugefügt');
		}

		this.refreshTable();
		this.loading = false;
	}

	private buildForm() {
		this.veIds  = '';
		this.myForm = this._formBuilder.group({
			veIds: this.veIds,
		});
	}

	public deleteModal(): void {
		if (!this.flexGrid) {
			return;
		}

		this.loading = true;
		this.showDeleteModal = false;
		const itemsToDelete: number[] = this.flexGrid.checkedItems.map(s => s.manuelleKorrekturId);

		let result: Observable<any>;
		if (itemsToDelete.length === 1) {
			result = this._service.delete(itemsToDelete[0]);
		} else {
			result = this._service.batchDelete(itemsToDelete);
		}
		result.subscribe(() => {
				this.refreshTable();
				this.loading = false;
				this._ui.showSuccess('Erfolgreich gelöscht');
			},
			(error) => {
				this._err.showError(error);
				this.loading = false;
			});
	}

	public deleteButtonDisabled(): boolean {
		return this.flexGrid.checkedItems.length < 1 ;
	}

	public resetSortsAndFilters() {
		this._loadColumns();
		this.refreshHiddenVisibleColumns();
		this.baseFilterString = this.preFilter === 1 ? '(Anonymisierungsstatus ne 1)' : '';
		this.manuelleKorrekturItems.sortDescriptions.clear();
		this.manuelleKorrekturItems.filterDefinition = this.baseFilterString;
		this.flexGrid.filter.clear();
		for (const sc of this._getInitialSortDescriptions()) {
			this.manuelleKorrekturItems.sortDescriptions.push(sc);
			this.flexGrid.defaultSortColumnKey = 'manuelleKorrekturId';
		}
		this.refreshTable();
	}

	private _getInitialSortDescriptions(): any[] {
		const firstCol = this.visibleColumns.find(c => c.key === 'manuelleKorrekturId');
		// is visisble then sort after this column
		if (firstCol) {
			return [new SortDescription('manuelleKorrekturId', false)];
		}

		return [];
	}
}
