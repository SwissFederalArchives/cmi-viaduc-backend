import {Component, OnInit, ViewChild} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {CmiGridComponent, ComponentCanDeactivate, TranslationService, Utilities as _util} from '@cmi/viaduc-web-core';
import {UrlService, AuthorizationService, ErrorService, UiServiceMC} from '../../../shared/services';
import {PagedResult} from '../../../shared/model/apiModels';
import {RoleService} from '../../services/role.service';
import {FlexGridFilter} from '@mescius//wijmo.grid.filter';

@Component({
    selector: 'cmi-viaduc-roles-features-page',
    templateUrl: 'roleFeaturesPage.component.html',
    styleUrls: ['./roleFeaturesPage.component.less'],
    standalone: false
})
export class RoleFeaturesPageComponent extends ComponentCanDeactivate implements OnInit {

	@ViewChild('flexGrid', { static: true })
	public flexGrid: CmiGridComponent;
	@ViewChild('filter', { static: true })
	public filter!: FlexGridFilter;
	public loading: boolean = true;
	public crumbs: any[] = [];
	public list: PagedResult<any>;
	public allowEdit!: boolean;
	public isDirty!: boolean;
	public roleFeaturesList: Map<string, string[]> = new Map<string, string[]>();
	public roleFeaturesForSaveList: Map<string, string[]> = new Map<string, string[]>();

	constructor(private _authorization: AuthorizationService,
				private _roleService: RoleService,
				private _txt: TranslationService,
				private _url: UrlService,
				private _err: ErrorService,
				private _router: Router,
				private _route: ActivatedRoute,
				private _ui: UiServiceMC) {
		super();
	}

	public get txt(): TranslationService {
		return this._txt;
	}

	private _buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({
			url: this._url.getNormalizedUrl('/benutzerundrollen'),
			label: this._txt.get('breadcrumb.usersAndRoles', 'Benutzer und Rollen')
		});
		crumbs.push({label: this._txt.get('breadcrumb.roles', 'Rollen')});
	}

	private _prepareResult(result: PagedResult<any>) {
		const columns: any[] = [];
		// Alle selektierten Features ermitteln
		for (const item of result.items) {
			const featureList = [];
			for (const feature of item.features) {
				featureList.push(feature.id);
			}
			this.roleFeaturesList.set(item.id.toString(), featureList);
			this.roleFeaturesForSaveList = this.roleFeaturesList;
		}


		// Alle Rollen Mappen
		_util.forEach(result.items, (applicationrole: any) => {
			columns.push({key: applicationrole.id.toString(), label: applicationrole.name, id: applicationrole.id});
		});

		// Rollen zu Benutzer-Rollen Mappen
		(result as any)['features'].forEach((feature: any) => {
			_util.forEach(result.items, (applicationrole: any) => {
				if (!applicationrole.id) {
					return;
				}

				const key = applicationrole.id.toString();
				const featureList = this.roleFeaturesList.get(key);

				feature[key] = featureList
					? featureList.indexOf(feature.id) >= 0
					: false;
			});
		});

		result.dynamicColumns = columns;
		this.list = result;

		this.flexGrid.itemsSource = (result as any).features;
		this.flexGrid.refresh();
		this._buildCrumbs();
	}

	private async _loadList(): Promise<void> {
		this.loading = true;
		try {
			this._roleService.getRoleInfos().subscribe(
				res => {
					this._prepareResult(res);
					this.loading = false;
				},
				err => {
					this._err.showError(err);
					this.loading = false;
				}
			);
		} catch (err) {
			this._err.showError(err);
		}
	}

	public isChecked(featureId: string, roleId: string): boolean {
		if (!this.list || !this.roleFeaturesList) {
			return false;
		}
		const role = this.roleFeaturesList.get(roleId);
		if (!role) {
			return false;
		}
		return role.indexOf(featureId) >= 0;
	}
	public onCheckboxClick(featureId: string, roleId: string): void {
		if (this.roleFeaturesForSaveList.has(roleId)) {
			const a: string[] | undefined = this.roleFeaturesForSaveList.get(roleId);
			if (a) {
				const index = a.indexOf(featureId);
				if (index >= 0) {
					a.splice(index, 1);
				} else {
					a.push(featureId);
				}
			}
			this.isDirty = this.allowEdit;
		}
	}

	public editMode(): void {
		this.allowEdit = !this.allowEdit && this._authorization.hasRole(this._authorization.roles.APPO);
	}

	public saveGridChanges(): void {
		this.loading = true;
		this.allowEdit = false;
		this.isDirty = false;
		this.roleFeaturesForSaveList.forEach((value: string[], key: string) => {
				this._roleService.setRoleFeatures(key, value).then(
					() => {
						this.loading = false;
						this._loadList();
						this._ui.showSuccess(this._txt.get('roleFeature.successfullySaved', 'Rollen-Funktionen-Matrix erfolgreich gespeichert'));
					},
					err => {
						this._err.showError(err);
					}
				);
		});
	}

	public cancelGridChanges(): void {
		this.ngOnInit();
	}

	public exportGrid(): void {
		const fileName = this.txt.get('role.benutzer.exportfilename', 'rollen-funktiononen.bar.ch.xlsx');

		this.flexGrid.exportToExcel(fileName).subscribe(() => {
			// nothing
		}, (err) => {
			this._err.showError(err);
		});
	}

	public ngOnInit(): void {
		this._buildCrumbs();
		this._route.params.subscribe(() => this._loadList());
		this.allowEdit = false;
		this.isDirty = false;
	}

	public show(item: any): void {
		const id = item ? item.id : 'new';
		this._router.navigate([this._url.getNormalizedUrl('/benutzerundrollen/rollen') + '/' + id]);
	}

	public canDeactivate(): boolean {
		return !this.isDirty;
	}

	public promptForMessage(): false | 'question' | 'message' {
		return  'question';
	}

	public message(): string {
		return this._txt.get('hints.unsavedChanges', 'Sie haben ungespeicherte Änderungen. Wollen Sie die Seite tatsächlich verlassen?');
	}
}
