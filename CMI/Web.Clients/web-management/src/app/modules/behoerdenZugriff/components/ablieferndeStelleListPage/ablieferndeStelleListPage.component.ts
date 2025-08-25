import {Component, OnInit, ViewChild} from '@angular/core';
import {
	ApplicationFeatureEnum,
	CmiGridComponent,
	ConfigService,
	TranslationService,
	Utilities as _util
} from '@cmi/viaduc-web-core';
import {
	AblieferndeStelleService,
	UrlService,
	ErrorService,
	AuthorizationService,
	UserService
} from '../../../shared/services';
import {AblieferndeStelle} from '../../../shared/model/ablieferndeStelle';
import {Router} from '@angular/router';
import {CollectionView} from '@mescius/wijmo';
import {AblieferndeStelleSettings, ManagementUserSettings} from '../../../shared/model';
import {Column} from '@mescius/wijmo.grid';
import * as moment from 'moment';

@Component({
	selector: 'cmi-viaduc-ablieferndestelle-page',
	templateUrl: 'ablieferndeStelleListPage.component.html',
	styleUrls: ['./ablieferndeStelleListPage.component.less']
})
export class AblieferndeStellePageComponent implements OnInit {
	@ViewChild('flexGrid', { static: true })
	public flexGrid: CmiGridComponent;

	public loading: boolean;
	public crumbs: any[] = [];
	public ablieferndeStellen: CollectionView = new CollectionView();
	public showDeleteModal: boolean;
	public hiddenColumns: any[] = [];
	public visibleColumns: any[] = [];
	public showColumnPicker = false;
	public columns: any[];

	constructor(private _ablieferndeStelleService: AblieferndeStelleService,
				private _txt: TranslationService,
				private _url: UrlService,
				private _err: ErrorService,
				private _cfg: ConfigService,
				private _usr: UserService,
				private _router: Router,
				public _authorization: AuthorizationService) {
	}

	public ngOnInit(): void {
		this.buildCrumbs();
		this._loadColumns();
		this.loadAblieferndeStelleList();
	}

	public getUserAsString(item: any): string {
		if (item == null) {
			return '';
		}
		return item.applicationUserList.map(u => u.firstName + ' ' + u.familyName).join(', ');
	}

	public getTokenAsString(item: any): string {
		if (item == null) {
			return '';
		}
		return item.ablieferndeStelleTokenList.map(t => `${t.token} (${t.bezeichnung})`).join(', ');
	}

	public deleteCheckedAblieferndeStelle(): void {
		if (!this.flexGrid) {
			return;
		}

		const toDelete: number[] = this.flexGrid.checkedItems.map(s => s.ablieferndeStelleId);

		if (toDelete.length === 0) {
			return;
		}

		const promise: Promise<any> = this._ablieferndeStelleService.deleteAblieferndeStelle(toDelete);
		promise.then(() => {
				this.loadAblieferndeStelleList();
				this.showDeleteModal = false;
			},
			(error) => {
				console.error(error);
			});
	}

	public editAblieferndeStelle(item: any): void {
		const id = item ? item.ablieferndeStelleId : 'new';
		this._router.navigate([this._url.getNormalizedUrl('/behoerdenzugriff/zustaendigestelledetail') + '/' + id]);
	}

	public addNewAblieferndeStelle(): void {
		this._router.navigate([this._url.getNormalizedUrl('/behoerdenzugriff/zustaendigestelledetail')]);
	}

	public get deleteButtonDisabled(): boolean {
		if (!this.flexGrid) {
			return true;
		}

		return this.flexGrid.checkedItems.length <= 0;
	}

	public getElementToDelete(): string {
		if (this.getQuantityOfCheckedItmesToDelete() !== 1) {
			return '';
		}

		return this.flexGrid.checkedItems[0].bezeichnung;
	}

	public getQuantityOfCheckedItmesToDelete(): number {
		const counter = 0;
		if (!this.flexGrid) {
			return counter;
		}

		return this.flexGrid.checkedItems.length;
	}

	public toggleDeleteModal(): void {
		this.showDeleteModal = !this.showDeleteModal;
	}

	public exportExcel() {
		if (!this.flexGrid) {
			return;
		}

		const fileName = this._txt.get('behoerdenZugriff.ablieferndeStellenExportFileName', 'abliefernde.stellen.bar.ch.xlsx');
		this.flexGrid.exportToExcel(fileName).subscribe(() => {
			// nothing
		}, (err) => {
			this._err.showError(err);
		});
	}

	public goToUserDetail(id: any): void {
		this._router.navigate([this._url.getNormalizedUrl('/benutzerundrollen/benutzer') + '/' + id]);
	}

	private buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({label: this._txt.get('breadcrumb.behoerdenZugriff', 'Behörden-Zugriff')});
		crumbs.push({label: this._txt.get('breadcrumb.zusteandigestellen', 'Zuständige Stellen')});
	}

	private loadAblieferndeStelleList(): void {
		this.loading = true;
		this._ablieferndeStelleService.getAllAblieferndeStellen().subscribe(
			res => this.prepareResult(res),
			err => this._err.showError(err),
			() => {
				this.loading = false;
			});
	}

	private prepareResult(result: AblieferndeStelle[]) {
		if (!_util.isEmpty(result)) {
			for (const arrayItem of result) {
				arrayItem.applicationUserAsString = this.getUserAsString(arrayItem);
				arrayItem.ablieferndeStelleTokenAsString = this.getTokenAsString(arrayItem);
				arrayItem.kontrollstellenAsString = (arrayItem.kontrollstellen || []).join('; ');
				arrayItem.hasKontrollstelle = !_util.isEmpty(arrayItem.kontrollstellen);
				arrayItem.createdOn = this._fixDate(arrayItem.createdOn);
				arrayItem.modifiedOn = this._fixDate(arrayItem.modifiedOn);
			}
		}
		this.ablieferndeStellen = new CollectionView(result);
		this.ablieferndeStellen.pageSize = 10;
	}

	private _fixDate(dt): Date {
		return dt ? moment(dt).toDate() : null;
	}

	public get allowZustaendigeStellenBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BehoerdenzugriffZustaendigeStellenBearbeiten);
	}

	private _loadColumns(): void {
		this._usr.getUserSettings().then((settings) => {
			const userSettings = settings as ManagementUserSettings;
			if (userSettings.ablieferndeStelleSettings && userSettings.ablieferndeStelleSettings.columns) {
				this.columns = userSettings.ablieferndeStelleSettings.columns;
			} else {
				this.resetColumnsToDefault();
			}
			this.refreshHiddenVisibleColumns();
		});
	}

	private _saveColumnsAsUserSettings(cols) {
		const existingSettings = this._cfg.getUserSettings() as ManagementUserSettings;
		existingSettings.ablieferndeStelleSettings = <AblieferndeStelleSettings> {
			columns: cols
		};
		this._usr.updateUserSettings(existingSettings);
	}

	public resetColumnsToDefault() {
		this.columns = this._cfg.getSetting('ablieferndeStellen.listColumns', {}).map(x => Object.assign({}, x));
		this._saveColumnsAsUserSettings(this.columns);
		this.loadAblieferndeStelleList();
		this.refreshHiddenVisibleColumns();
		this.flexGrid.resetGridState();
	}

	public saveColumns() {
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

	public refreshHiddenVisibleColumns() {
		this.visibleColumns = this._getVisibleColumns();
		this.hiddenColumns = this._getHiddenColumns();
	}

	public showColumnPickerModal() {
		this.showColumnPicker = true;
	}
}
