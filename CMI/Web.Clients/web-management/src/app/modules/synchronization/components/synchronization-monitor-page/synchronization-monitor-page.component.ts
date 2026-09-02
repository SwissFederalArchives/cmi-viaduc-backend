import {Component, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {CmiGridComponent, ConfigService, CoreOptions, SyncActionLogDto, SyncNumberPerHourDto, TranslationService} from '@cmi/viaduc-web-core';
import {SortDescription, DataType} from '@mescius/wijmo';
import {WjMenu} from '@mescius/wijmo.angular2.input';
import {ODataCollectionView} from '@mescius/wijmo.odata';
import {ErrorService, ManagementUserSettings, UrlService, UserService} from "../../../shared";
import {SynchronizationService} from "../../services";
import flatpickr from "flatpickr";
import {German} from "flatpickr/dist/l10n/de";
import {FormBuilder, FormControl, FormGroup} from "@angular/forms";
import moment from 'moment';
import { CellRange } from '@mescius/wijmo.grid';


@Component({
    selector: 'cmi-synchronization-monitor-page',
    templateUrl: './synchronization-monitor-page.component.html',
    styleUrls: ['./synchronization-monitor-page.component.less'],
    standalone: false
})
export class SynchronizationMonitorPageComponent implements OnInit, OnDestroy {

	private _destroyed = false;

	@ViewChild('flexGrid', {static: true })
	public flexGrid: CmiGridComponent;

	@ViewChild('flexGridLog', {static: true})
	public flexGridLog: CmiGridComponent;

	@ViewChild('flexGridSyncPerHour', {static: true})
	public flexGridSyncPerHour: CmiGridComponent;


	@ViewChild('preFilterMenu', {static: true})
	public preFilterMenu: WjMenu;
	public synchronisationItems!: ODataCollectionView;
	public columns: any[] = [];
	public hiddenColumns: any[] = [];
	public visibleColumns: any[] = [];
	public visibleColumnsSelector: any[] = [];
	public crumbs: any[] = [];
	public valueFilters: any;
	public gridFilters: any;
	public loading!: boolean;
	public syncActionLogItems!: SyncActionLogDto[];
	public syncNumberPerHourItems!: SyncNumberPerHourDto[];
	public myForm?: FormGroup = undefined;

	public countDays = 5;

	constructor(private _opt: CoreOptions,
				public _url: UrlService,
				private _cfg: ConfigService,
				private _sys: SynchronizationService,
				private _err: ErrorService,
				private _usr: UserService,
				private formBuilder: FormBuilder,
				public _txt: TranslationService) {
		flatpickr.localize(German);
	}

	public ngOnInit(): void {
		this.buildCrumbs();
		this._initTableView();
	}

	public ngOnDestroy(): void {
		this._destroyed = true;
	}


	public refreshHiddenVisibleColumns() {
		this.visibleColumns = this.getVisibleColumns();
		this.visibleColumnsSelector = this.visibleColumns.filter(c => c.visible && !c.loadPerDefault);
		this.hiddenColumns = this.getHiddenColumns();
	}

	public getHiddenColumns(): any [] {
		return this.columns.filter(c => !c.visible);
	}

	public exportExcel() {
		const formated  = moment(Date.now()).format('DD.MM.YYYY_HH:mm:ss');
		const fileName = this._txt.get('synchronizationMonitor.synchronizationExcelFilename', 'Synchronisation_')
		+ formated  + '.xlsx';

		this.flexGrid.exportToExcelOData(fileName).subscribe(() => {
			// nothing
		}, (err) => {
			this._err.showError(err);
		});
	}

	private buildCrumbs() : void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({label: this._txt.get('breadcrumb.synchronization', 'Synchronisation')});
		crumbs.push({label: this._txt.get('breadcrumb.synchronizationMonitor', 'Überwachung')});
	}


	private _initTableView() {
		this.columns = this._cfg.getSetting('synchronizationMonitor.listColumns');
		this._loadColumns();
		this.restorePreFilterButtonState();
		this.getDefaultView();
		this.updateSyncNumberPerHourTable();
	}

	private restorePreFilterButtonState() {
		this.myForm = this.formBuilder.group({
			veIds:{
				value:'',
				disabled:false
			},
			vonDatumFilter:new FormControl(moment.utc(Date.now()).add(-2, 'd').toDate()),
			bisDatumFilter:new FormControl(moment.utc(Date.now()).toDate())
		});
		setTimeout(() => {
				if (this._destroyed) { return; }
				this.refreshTable();
			},
			50);
	}

	private _loadColumns(): void {
		this._usr.getUserSettings().then((settings) => {
			const userSettings = settings as ManagementUserSettings;
			if (userSettings.synchronizationMonitorSettings && userSettings.synchronizationMonitorSettings.columns) {
				this.columns = userSettings.synchronizationMonitorSettings.columns;
			}
			this.refreshHiddenVisibleColumns();
		});
	}

	public getVisibleColumns(): any[] {
		return this.columns.filter(c => c.visible);
	}

	private getDefaultView() {
		this.synchronisationItems = new ODataCollectionView(this._opt.odataUrl, 'VSynchronisationen', {
			requestHeaders: {withCredentials: true},
			dataTypes: {
				syncActionId: DataType.String,
				archiveRecordId: DataType.String,
				actionType: DataType.String,
				actionStatus: DataType.String,
				numberOfTries: DataType.Number,
				createdOn: DataType.Date,
				modifiedOn: DataType.Date,
				logDate: DataType.Date,
				errorReason: DataType.String,
				actionStatusHistory: DataType.String
			},
			canFilter: true,
			canSort: true,
			sortOnServer: true,
			pageOnServer: true,
			filterOnServer: true,
			filterDefinition: this.combineFilter(this.setFilter()),
			pageSize: 10
		});
		this.synchronisationItems.error.addHandler((view, error) => {
			this._err.showOdataErrorIfNecessary(error);
		});
		this.synchronisationItems.loading.addHandler(() => {
			if (this._destroyed) { return; }
			this.loading = true;
		});
		this.synchronisationItems.loaded.addHandler(() => {
			if (this._destroyed) { return; }
			this.loading = false;
			const cell: number | CellRange = new CellRange(0, 0);
			// Beim Neuladen erste Zeile auswählen (wenn vorhanden)
			if (this.synchronisationItems.items.length > 0 && this.flexGrid.itemsSource) {
					this.flexGrid.select(cell, true);
				this.updateLogViewForSelectedItem(); // Log dazu anzeigen
			} else {
				this.updateLogViewForSelectedItem(); // Leert Log wenn nichts da
			}
			this.onGridFilterApplied(null);
		});
	}

	private updateLogViewForSelectedItem(): void {
		if (this._destroyed) { return; }
		const selected = this.flexGrid?.selectedItems?.[0];
		if (selected) {
			// Logs zurücksetzen
			this.syncActionLogItems = [];
			if (this.flexGridLog) {
				this.flexGridLog.itemsSource = [];
			}

			if (selected?.syncActionId) {

				const cleanedId = selected.syncActionId?.replace(/[’']/g, '').trim();
				this._sys.getLogData(cleanedId).subscribe({
					next: r => {
						if (this._destroyed) { return; }
						if (r !== null) {
							this.syncActionLogItems = r;
						}
						if (this.flexGridLog) {
							this.flexGridLog.itemsSource = this.syncActionLogItems;
							this.flexGridLog.refresh();
						}
					},
					error: () => {
						// Nach 0.95 Sekunde erneut versuchen
						setTimeout(() => {
							if (this._destroyed) { return; }
							this._sys.getLogData(cleanedId).subscribe({
								next: retryData => {
									if (this._destroyed) { return; }
									if (retryData !== null) {
										this.syncActionLogItems = retryData;
									}
									if (this.flexGridLog) {
										this.flexGridLog.itemsSource = this.syncActionLogItems;
										this.flexGridLog.refresh();
									}
								},
								error: secondError => {
									console.error('Retry for getLogData failed', secondError);
									this._err.showOdataErrorIfNecessary(secondError);
								}
							});
						}, 950);
					}
				});
			} else {
					if (this.flexGridLog) {
						this.flexGridLog.refresh();
					}
				}
		}
	}

	public flexInitialized(flexgrid: any) {
		flexgrid.selectionChanged.addHandler(() => {
			this.updateLogViewForSelectedItem();
		});
	}


	public refreshTable() {
		const formFilter = this.setFilter(); // ✅ Base form filter

		if(this.flexGrid?.filter?.filterDefinition !== this.gridFilters){
			this.flexGrid.filter.clear();
			this.flexGrid.filter.filterDefinition = this.gridFilters;
		}

		if (this.synchronisationItems?.sortDescriptions?.length === 0){
			this.synchronisationItems.sortDescriptions.push(new SortDescription('syncActionId', false));
		}

		this.synchronisationItems.filterDefinition = this.combineFilter(formFilter);
		this.synchronisationItems.load();
	}


	private combineFilter(formFilter: string) {
		let combinedFilter = '';
		try {
			const parsed = typeof this.gridFilters === 'string'
				? JSON.parse(this.gridFilters)
				: this.gridFilters;

			const gridFilter = this.convertWijmoFilterToOData(parsed); // ✅ No removal — grid filters apply on subset

			combinedFilter = formFilter && gridFilter
				? `(${formFilter}) and (${gridFilter})`
				: formFilter || gridFilter || '';
		} catch (e) {
			console.warn('Failed to parse grid filter:', e);
			combinedFilter = formFilter;
			this.flexGrid?.filter?.clear();
		}

		return combinedFilter;
	}

	public dataPickerValueUpdate($event: any) {
		const isValid = $event.dateString !== '' && $event.dateString;
		if (isValid) {
			this.refreshTable();
		}
	}

	// eslint-disable-next-line
	public onGridFilterApplied(_: any): void {
		if (this.gridFilters !== this.flexGrid?.filter?.filterDefinition) {
			if(!this.gridFilters ||this.gridFilters === '')	{
				this.flexGrid.filter.filterDefinition = this.gridFilters = '';
				this.flexGrid.refresh();
			}

			this.gridFilters = this.flexGrid?.filter?.filterDefinition;
			this.refreshTable();
		}
	}

	private convertWijmoFilterToOData(filterDefinition: any): string {
		if (!filterDefinition?.filters?.length) return '';

		const operatorMap: Record<number, string>  = {
			0: 'eq', 1: 'ne', 2: 'gt', 3: 'ge',
			4: 'lt', 5: 'le', 6: 'startswith',
			7: 'endswith', 8: 'contains'
		};

		return filterDefinition.filters
			.map((f: any) => {
				const col = f.binding;
				const op: string = operatorMap[f.condition1?.operator];
				const val = f.condition1?.value;

				if (!col || op == null || val === undefined || val === '') return '';

				let value: string;

				// Match date string (ISO 8601)
				const isDateString = typeof val === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z?$/.test(val);

				if (val instanceof Date) {
					value = val.toISOString(); // no quotes
				} else if (isDateString) {
					value = val; // no quotes
				} else if (typeof val === 'boolean') {
					value = val ? 'true' : 'false';
				} else if (typeof val === 'number') {
					value = val.toString();
				} else {
					value = `'${val}'`; // assume string
				}

				if (['contains', 'startswith', 'endswith'].includes(op)) {
					return `${op}(${col},${value})`;
				}

				return `${col} ${op} ${value}`;
			})
			.filter((p: any) => p !== '')
			.join(' and ');
	}

	public searchVeIds() {
		this.refreshTable();
	}

	private setFilter(): string {
		return this.includingArchiveRecordIdFilter(this.includingCreatedOnFilter());
	}

	private includingCreatedOnFilter(): string {
		let filter = '';
		if (this.myForm.controls['vonDatumFilter'].value) {
			filter = `(CreatedOn gt ${this.myForm.controls['vonDatumFilter'].value.toISOString()} or modifiedOn gt ${this.myForm.controls['vonDatumFilter'].value.toISOString()})`;
		}

		if (filter === '') {
			if (this.myForm.controls['bisDatumFilter'].value) {
				filter = `(CreatedOn lt ${this.myForm.controls['bisDatumFilter'].value.toISOString()} or modifiedOn lt ${this.myForm.controls['bisDatumFilter'].value.toISOString()})`;
			}
		} else {
			if (this.myForm.controls['bisDatumFilter'].value) {
				filter = `(${filter} and (CreatedOn lt ${this.myForm.controls['bisDatumFilter'].value.toISOString()} or modifiedOn lt ${this.myForm.controls['bisDatumFilter'].value.toISOString()}))`
			}
		}
		return filter;
	}

	private includingArchiveRecordIdFilter(filter: string): string {
		const veIdsRaw = this.myForm.controls.veIds.value;
		if (veIdsRaw !== null && veIdsRaw.length > 3) {
			if (veIdsRaw.includes(';')) {
				const ids = veIdsRaw.split(';');
				let archiveRecordIdFilter = ''
				ids.forEach((id: any)=> {
					if (archiveRecordIdFilter === '') {
						archiveRecordIdFilter = '(archiveRecordId eq \'' + decodeURI(id) + '\')';
					} else {
						archiveRecordIdFilter += ' or (archiveRecordId eq \'' + decodeURI(id) + '\')';
					}
				});
				if (filter === '') {
					filter = '(' + archiveRecordIdFilter + ')';
				} else {
					filter = '((' + filter + ') and (' + archiveRecordIdFilter + '))';
				}

				if (ids.length === 0) {
					if (filter !== '') {
						filter = '((' + filter + ') and archiveRecordId eq \'' +  decodeURI(this.myForm.controls.veIds.value ) + '\')';
					} else {
						filter = '(archiveRecordId eq \'' +  decodeURI(this.myForm.controls.veIds.value ) + '\')';
					}
				}
			} else {
				if (filter !== '') {

					filter = '((' + filter + ') and archiveRecordId eq \'' + decodeURI(this.myForm.controls.veIds.value ) + '\')';
				} else {
					filter = '(archiveRecordId eq \'' +  decodeURI(this.myForm.controls.veIds.value ) + '\')';
				}
			}
		}
		return filter;
	}


	public updateSyncNumberPerHourTable() {
		this._sys.syncNumberPerHour(this.countDays).subscribe(r => {
			if (this._destroyed) { return; }
			if (r !== null) {
				this.syncNumberPerHourItems = r;
				if (this.flexGridSyncPerHour) {
					this.flexGridSyncPerHour.itemsSource = this.syncNumberPerHourItems;
				}
				if (this.flexGridLog) {
					this.flexGridLog.refresh();
				}
			}
		});
	}

	public countDaysClick($event: any) {
		const newValue = $event.target.valueAsNumber;
		if (!isNaN(newValue)) {
			this.countDays = newValue;
			this.updateSyncNumberPerHourTable();
		}
	}
}
