import {Component, OnInit, ViewChild} from '@angular/core';
import {CollectionView} from '@mescius/wijmo';
import {
	ApplicationFeatureEnum,
	CmiGridComponent,
	CollectionDto, CollectionListItemDto,
	ConfigService,
	TranslationService
} from '@cmi/viaduc-web-core';
import {AuthorizationService, CollectionSettings, ErrorService, ManagementUserSettings, UrlService, UiServiceMC,  UserService} from '../../../shared';
import {Router} from '@angular/router';
import {CollectionService} from '../../services';
import {Observable} from 'rxjs';
import {DataMap, Column} from '@mescius/wijmo.grid';

@Component({
	selector: 'cmi-collection-list-page',
	templateUrl: './collection-list-page.component.html',
	styleUrls: ['./collection-list-page.component.less']
})
export class CollectionListPageComponent implements OnInit {
	@ViewChild('flexGrid', {static: true})
	public flexGrid: CmiGridComponent;
	public showColumnPicker = false;
	public valueFilters: any;
	public collectionTypes = [{collectionTypeId: 0, name: 'Sammlung'}, {collectionTypeId: 1, name: 'Themenblock'}];
	public showDeleteModal: boolean;
	public loading: boolean;
	public collections: CollectionView = new CollectionView();
	public hiddenColumns: any[] = [];
	public visibleColumns: any[] = [];
	public visibleColumnsSelector: any[] = [];
	public columns: any[];
	public crumbs: any[] = [];

	constructor(private _collectionService: CollectionService,
				private _txt: TranslationService,
				private _url: UrlService,
				private _err: ErrorService,
				private _cfg: ConfigService,
				private _usr: UserService,
				private _ui: UiServiceMC,
				private _router: Router,
				private _authorization: AuthorizationService) {
	}

	public ngOnInit(): void {
		// To prevent filtering icon on image column
		this.flexGrid.filter.filterColumns = ['title', 'description', 'descriptionShort', 'validFrom', 'validTo', 'imageAltText', 'createdOn',
			'createdBy', 'modifiedOn', 'modifiedBy', 'link', 'parent', 'childCollections', 'language'];
		this.buildCrumbs();
		this.loadColumns();
		this.loadCollectionList();
		const maps: { [id: string]: DataMap; } = {};
		maps['collectionTypeId'] = new DataMap(this.collectionTypes, 'collectionTypeId', 'name');

		this.valueFilters = maps;
	}

	public get allowCollectionBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.AdministrationSammlungenBearbeiten);
	}

	public addNewCollection(): void {
		this._router.navigate([this._url.getNormalizedUrl('/collection/collection/new')]);
	}

	public getVisibleColumns(): any[] {
		return this.columns.filter(c => c.visible);
	}

	public getHiddenColumns(): any [] {
		return this.columns.filter(c => !c.visible);
	}

	public refreshHiddenVisibleColumns() {
		this.visibleColumns = this.getVisibleColumns();
		this.visibleColumnsSelector = this.visibleColumns.filter(c => c.visible && !c.loadPerDefault);
		this.hiddenColumns = this.getHiddenColumns();
	}

	public editCollection(item: CollectionDto): void {
		const id = item ? item.collectionId : 'new';
		this._router.navigate([this._url.getNormalizedUrl('/collection/collection') + '/' + id]);
	}

	public toggleDeleteModal(): void {
		const hasItemsWithChildren = this.flexGrid.checkedItems.map(s => s.childCollections).filter(children => children !== null);
		if (hasItemsWithChildren.length !== 0) {
			this._ui.showInfo('Bitte zuerst die enthaltenen Sammlungen und Themenblöcke löschen');
		} else {
			this.showDeleteModal = !this.showDeleteModal;
		}
	}

	public deleteCheckedCollection(): void {
		if (!this.flexGrid) {
			return;
		}

		const itemsToDelete: number[] = this.flexGrid.checkedItems.map(s => s.collectionId);

		let result: Observable<any>;
		if (itemsToDelete.length === 1) {
			result = this._collectionService.delete(itemsToDelete[0]);
		} else {
			result = this._collectionService.batchDelete(itemsToDelete);
		}

		result.subscribe(() => {
				this.loadCollectionList();
				this.showDeleteModal = false;
			},
			(error) => {
				this._err.showError(error);
			});
	}

	public getQuantityOfCheckedItmesToDelete(): number {
		const counter = 0;
		if (!this.flexGrid) {
			return counter;
		}

		return this.flexGrid.checkedItems.length;
	}

	public get deleteButtonDisabled(): boolean {
		if (!this.flexGrid) {
			return true;
		}

		return this.flexGrid.checkedItems.length <= 0;
	}

	public showColumnPickerModal(): void {
		this.refreshHiddenVisibleColumns();
		this.showColumnPicker = true;
	}

	public showColumn(c: any) {
		this.columns.filter(col => col.key === c.key)[0].visible = true;
		this.refreshHiddenVisibleColumns();
	}

	public hideColumn(c: any) {
		this.columns.filter(col => col.key === c.key)[0].visible = false;
		this.refreshHiddenVisibleColumns();
	}

	public getElementToDelete(): string {
		if (this.getQuantityOfCheckedItmesToDelete() !== 1) {
			return '';
		}

		return this.flexGrid.checkedItems[0].title;
	}

	public resetSortsAndFilters() {
		this.flexGrid.filter.clear();
		this.resetColumnsToDefault();
	}

	public resetColumnsToDefault() {
		this.columns = this._cfg.getSetting('collections.listColumns');
		this.saveColumnsAsUserSettings(this.columns);
		this.loadCollectionList();
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

		this.saveColumnsAsUserSettings(sortedArray);
	}

	public onImageLoaded() {
		this.flexGrid.invalidate();
	}

	private buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({label: this._txt.get('breadcrumb.collection', 'Sammlungen')});
		crumbs.push({label: this._txt.get('breadcrumb.collection', 'Virtuelle Sammlungen')});
	}

	private loadColumns(): void {
		this._usr.getUserSettings().then((settings) => {
			const userSettings =  settings as ManagementUserSettings;
			if (userSettings.collectionSettings && userSettings.collectionSettings.columns) {
				this.columns = userSettings.collectionSettings.columns;
			} else {
				console.log('hätte hier nicht rein sollen!!!');
				this.resetColumnsToDefault();
			}
			this.refreshHiddenVisibleColumns();
		});
	}

	private prepareResult(result: CollectionListItemDto[]) {
		this.loading = false;
		this.collections = new CollectionView(result);
		this.collections.pageSize = 10;
	}

	private loadCollectionList(): void {
		this.loading = true;
		this._collectionService.getAll().subscribe(
			res => this.prepareResult(res),
			err => this._err.showError(err));
	}

	private saveColumnsAsUserSettings(cols) {
		const existingSettings = this._cfg.getUserSettings() as ManagementUserSettings;
		existingSettings.collectionSettings = <CollectionSettings>{
			columns: cols
		};
		this._usr.updateUserSettings(existingSettings);
	}
}
