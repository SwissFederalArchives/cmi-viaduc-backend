import { Component, OnInit, ViewChild, ChangeDetectorRef, AfterViewChecked } from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {WjListBox} from '@mescius//wijmo.angular2.input';
import {ApplicationFeatureEnum, ClientContext, ComponentCanDeactivate, CountriesService, HttpService, TranslationService, Utilities as _util} from '@cmi/viaduc-web-core';
import {
	AblieferndeStelleService, AuthorizationService, DetailPagingService, ErrorService, UiServiceMC, UrlService,
	UserService
} from '../../../shared/services';
import {AblieferndeStelle, DetailResult} from '../../../shared/model';
import {RoleService} from '../../services';
import * as fileSaver from 'file-saver';
import {HttpEventType} from '@angular/common/http';

import {AbstractControl, FormBuilder, FormControl, FormGroup, Validators} from '@angular/forms';
import flatpickr from 'flatpickr';
import {German} from 'flatpickr/dist/l10n/de';
import {UserRolesDetailPageErrorMessages} from './userRolesDetailPageErrorMessages';

@Component({
    selector: 'cmi-viaduc-user-roles-detail-page',
    templateUrl: 'userRolesDetailPage.component.html',
    styleUrls: ['./userRolesDetailPage.component.less'],
    standalone: false
})
export class UserRolesDetailPageComponent extends ComponentCanDeactivate implements OnInit, AfterViewChecked {
	public errors: { [key: string]: string } = {};
	public loading: boolean = true;
	public crumbs!: any[];
	public stillSelectedRoles: any;
	public stillAvailableRoles: any;
	private allRoles: any;
	private initialeRoles: any;
	public detail!: DetailResult<any>;
	public selectedIdentifizierungsmittel: any;

	public language: any = [{name:'Deutsch', code:'de'}, {name:'Französisch', code:'fr'}, {name:'Italienisch', code:'it'}, {name:'Englisch', code:'en'}];
	private countries: any;

	public showModal!: boolean;
	public showVerifyModal!: boolean;

	@ViewChild('listExcluded', { static: false })
	public listExcluded!: WjListBox;
	@ViewChild('listIncluded', { static: false })
	public listIncluded!: WjListBox;
	public myForm!: FormGroup;
	public ablieferndeStelleAllList!: AblieferndeStelle[];
	private stillSelectedAblieferndeStelleList: AblieferndeStelle[];
	private initialeAblieferndeStelleList: AblieferndeStelle[];
	private rolesIsDirty!: boolean;
	public today = new Date();
	public maxDate = new Date();

	public clientRoles = ['Ö2','Ö3','BVW','AS','BAR'];

	constructor(private _context: ClientContext, public _authorization: AuthorizationService, private _roleService: RoleService, private _txt: TranslationService,
				private _url: UrlService,
				private _route: ActivatedRoute,
				private _router: Router,
				private _ui: UiServiceMC,
				private _ablieferndeStelleService: AblieferndeStelleService,
				private _userService: UserService, private _countriesService: CountriesService,
				private _http: HttpService,
				private _dps: DetailPagingService,
				private _errService: ErrorService,
				private _changeDetectionRef: ChangeDetectorRef,
				private formbuilder: FormBuilder) {
		super();
		flatpickr.localize(German);
	}

	public ngOnInit(): void {
		this.today.setHours(0, 0, 0, 0);
		this.maxDate.setDate( this.today.getDate() + 30 );
		this.maxDate.setHours(23, 59, 59, 99);
		this._route.params.subscribe(params => this._load(params['id']));
		this.loading = true;
		this._ablieferndeStelleService.getAllAblieferndeStellen().subscribe(
			res => this.ablieferndeStelleAllList = res,
			err => {
				this.loading = false;
				this._ui.showError(err);
			},
			() => this.loading = false);

		const countries = this._countriesService.getCountries(this._context.language);
		this.countries = this._countriesService.sortCountriesByName(countries);
	}

	public include(): void {
		this.rolesIsDirty = true;
		const role = this.listExcluded.selectedItem;
		this.detail.item.roles.push(role);
		this._remove(this.stillAvailableRoles, role);
		this.listExcluded.refresh();
		this.listIncluded.refresh();
	}

	public exclude(): void {
		this.rolesIsDirty = true;
		const role = this.listIncluded.selectedItem;
		this._eiamRoleHandlingWarnungen(role.identifier);
		this.stillAvailableRoles.push(role);
		this._remove(this.detail.item.roles, role);
		this.listExcluded.refresh();
		this.listIncluded.refresh();
	}

	public ngAfterViewChecked(): void {
		this._changeDetectionRef.detectChanges();
	}

	public async saveUser(): Promise<void> {
		this.loading = true;
		this.reassembleDatatype();
		this._userService.updateAllUserData(this.detail?.item).subscribe(
			async () => {
				this._ui.showSuccess(this._txt.get('userAndRoles.userSuccessfullySaved', 'Benutzerdaten erfolgreich gespeichert'));
				await this._reload();
			},
			err => {
				this.loading = false;
				this._errService.showError(err);
			},
			() => {
				this.loading = false;
			}
		);
	}

	public async saveRoles(): Promise<void> {
		this.loading = true;

		const roleIds = [];
		for (let i = 0; i < this.detail?.item?.roles.length; i += 1) {
			roleIds.push(this.detail?.item?.roles[i].id);
		}

		this._roleService.setUserRoles(this.detail?.item?.id, roleIds).then(
			async () => {
				this.loading = false;
				this._ui.showSuccess(this._txt.get('userAndRoles.userroleSuccessfullySaved', 'Benutzerrollen erfolgreich gespeichert'));
				this.stillSelectedRoles = null;
				this.rememberSelectedAblieferndeStelle();
				await this._reload();
				this.rolesIsDirty = false;
			},
			err => {
				this.loading = false;
				this._ui.showError(this._txt.get('userAndRoles.userroleCouldNotBeSaved', 'Benutzerrollen konnte nicht gespeichert werden'), err.message);
			}
		);
	}

	public async saveAblieferndeStellen(): Promise<void> {
		this.loading = true;
		this.stillSelectedAblieferndeStelleList = null;
		const ablieferndeStelleIds = this.detail?.item?.ablieferndeStelleList != null ? this.detail?.item?.ablieferndeStelleList.map((as: any) => as.ablieferndeStelleId) : [];
		this._userService.cleanAndAddAblieferndeStelle(this.detail?.item?.id, ablieferndeStelleIds).subscribe(
			async () => {
			const access = this.detail?.item?.access || {};
				this.detail.item.tokens = access.asTokens || [];
				this._ui.showSuccess(this._txt.get('userAndRoles.assignedAblieferndeStellenSuccessfullySaved', 'Zuständige Stellen erfolgreich gespeichert'));
				this.rememberSelectedRoles();
				await this._reload();
			},
			err => {
				this.loading = false;
				this._ui.showError(this._txt.get('userAndRoles.assignedAblieferndeStellenNotBeSaved', 'Zuständige Stellen konnte nicht gespeichert werden'), err.message);
			},
			() => {
				this.loading = false;
			}
		);
	}

	public resetUserData(): void {
		this.myForm.reset();
		this.myForm.patchValue({
			familyName: this.detail.item.familyName,
			firstName: this.detail.item.firstName,
			organization: this.detail.item.organization,
			street: this.detail.item.street,
			streetAttachment: this.detail.item.streetAttachment,
			zipCode: this.detail.item.zipCode,
			town: this.detail.item.town,
			countryCode: this.detail.item.countryCode,
			phoneNumber: this.detail.item.phoneNumber,
			mobileNumber: this.detail.item.mobileNumber,
			birthday: this.detail.item.birthday,
			emailAddress: this.detail.item.emailAddress,
			fabasoftDossier: this.detail.item.fabasoftDossier,
			id: this.detail.item.id,
			userExtId: this.detail.item.userExtId,
			language: this.detail.item.language,
			downloadLimitDisabledUntil: this.detail.item.downloadLimitDisabledUntil,
			digitalisierungsbeschraenkungAufgehobenBis: this.detail.item.digitalisierungsbeschraenkungAufgehobenBis,
			createModifiyData: this.detail.item.createModifiyData,
			rolePublicClient: this.detail.item.rolePublicClient,
			researcherGroup: this.detail.item.researcherGroup,
			homeName: this.detail.item.homeName,
			qoAValue: this.detail.item.qoAValue,
			isIdentifiedUser: this.detail.item.isOe3OrHigher,
			lastLoginDate: this.detail.item.lastLoginDate,
			barInternalConsultation: this.detail.item.barInternalConsultation,
		});
		this.recalculateResearcherGroup();
		this.updateValidators();
	}

	public resetRoleManagement (): void {
		this.rolesIsDirty = false;
		this.detail.item.roles = [];
		this.stillAvailableRoles = [];
		this.distributeAssignedRoles(this.initialeRoles, this.detail?.item?.roles );
		this.listIncluded.refresh();
		this.listExcluded.refresh();
	}

	public resetAccessTokens(): void {
		this.detail.item.ablieferndeStelleList = [];
		if (this.initialeAblieferndeStelleList) {
			this.initialeAblieferndeStelleList.forEach((ablieferndeStelle: any) => {
				this.detail.item.ablieferndeStelleList.push(ablieferndeStelle);
			});
		}
	}

	public goToBenutzerList(): void {
		// BT Abbrechen
		this._router.navigate([this._url.getNormalizedUrl('/benutzerundrollen/benutzer')]);
	}

	public get languageDependantCountries(): any {
		return this.countries;
	}

	public canManageManagementClientRole(eiamRoles: string): boolean {
		if (eiamRoles === null || eiamRoles === undefined) {
			return false;
		}
		return this._authorization.hasRole(this._authorization.roles.APPO) &&
			(eiamRoles.indexOf(this._authorization.roles.APPO) >= 0 || eiamRoles.indexOf(this._authorization.roles.ALLOW) >= 0 );
	}

	public researcherGroupDdsModalAnswer(value: boolean) {
		this.myForm.controls['researcherGroup'].setValue(value);
		this.myForm.controls['researcherGroup'].markAsDirty();
	}

	public oeDreiDisabled(): boolean {
		return !this.allowBenutzerrollePublicClientBearbeiten ||
			this._authorization.roles.Oe2 === this.myForm?.controls.rolePublicClient.value ||
			this._authorization.roles.BAR === this.myForm?.controls.rolePublicClient.value||
			this.isFEDLogin();
	}
	public oeZweiDisabled(): boolean {
		return !this.allowBenutzerrollePublicClientBearbeiten || this.isFEDLogin() ||
			this._authorization.roles.BAR === this.myForm?.controls.rolePublicClient.value;
	}

	public asTokenManageDisabled(): boolean {
		return !this.detail?.item?.rolePublicClient || this.detail?.item?.rolePublicClient.indexOf(this._authorization.roles.AS) === -1;
	}

	public isRoleOptionDisabled(role: string): boolean | null {
		switch (role) {
			case this._authorization.roles.Oe2:
				if (this.oeZweiDisabled()) {
					return true;
				}
				return null;
			case this._authorization.roles.Oe3:
				if (this.oeDreiDisabled()) {
					return true;
				}
				return null;
			case this._authorization.roles.BVW:
				if (this.allowBenutzerrollePublicClientBearbeiten && this.qoaValue() > 39) {
					return null;
				} else {
					return true;
				}
			case this._authorization.roles.AS:
				if (this.allowBenutzerrollePublicClientBearbeiten && (this.qoaValue() > 49 || (this.detail?.item?.rolePublicClient == this._authorization.roles.BVW && this.qoaValue() > 39))) {
					return null;
				} else {
					return true;
				}
			case this._authorization.roles.BAR:
				if (this.allowBenutzerrollePublicClientBearbeiten && this.isFEDLogin() && ((this.qoaValue() > 59 ) || (this.detail?.item?.rolePublicClient == this._authorization.roles.BVW && this.qoaValue() > 39))) {
					return null;
				} else {
					return true;
				}
			default:
				return true;
		}
	}

	public getCheckedStatusForRolePublic(value: string): boolean {
		return value.indexOf(this.myForm.controls['rolePublicClient'].value ) === 0;
	}


	public toggelSelectedResearcherGroup(e: MouseEvent): void {
		if (this.myForm.controls['researcherGroup'].value === false) {
			this.showVerifyModal = true;
			e.preventDefault();
		}
	}

	public onChangedentIfierungsmittel(event: any) {
		if (event.target.files.length === 0) {
			return;
		}
		const formData = new FormData();
		for (const file of event.target.files) {
			formData.append(file.name, file);
		}
		this.selectedIdentifizierungsmittel = formData;
	}

	public downloadIdentifierungsmittel() {
		this.loading = true;
		const url = this._userService.getIdentifizierungsmittelPdfUrl(this.detail.item.id);
		this._http.download(url).subscribe(
			event => {
				if (event.type === HttpEventType.Response) {
					try {
						this.loading = false;
						const blob = event.body;
						fileSaver.saveAs(blob, this.detail.item.id + '.pdf', { autoBom: false });
					} catch (ex) {
						console.error(ex);
					}
				}
			},
			(error) => {
				this.loading = false;
				this._ui.showError(this._txt.get('userAndRoles.downloadfail', 'Identifizierungsmittel konnte nicht geladen werden'), error.message);
			},
			() => {
				this.loading = false;
			});
	}

	public uploadIdentifierungsmittel() {
		this.loading = true;
		this._userService.setIdentifizierungsmittelPdf(this.detail?.item?.id, this._authorization.roles.Oe3, this.selectedIdentifizierungsmittel).subscribe(
			() => {
				this._ui.showSuccess(this._txt.get('userAndRoles.uploadsuccess', 'Identifizierungsmittel erfolgreich gespeichert'));
			},
			err => {
				this.loading = false;
				const detailErrorMessage = err.error && err.error.message ? this._txt.get('userAndRoles.uploadfail', 'Identifizierungsmittel konnte nicht gespeichert werden') +
					' (' + err.error.message + ')' :
					this._txt.get('userAndRoles.uploadfail', 'Identifizierungsmittel konnte nicht gespeichert werden');

				this._ui.showError(detailErrorMessage,  err.message);
			},
			() => {
				this.detail.item.hasIdentifizierungsmittel = true;
				this.myForm.controls.rolePublicClient.setValue(this._authorization.roles.Oe3);
				this.updateValidators();
				this.recalculateResearcherGroup();
				this.selectedIdentifizierungsmittel = null;
				this.loading = false;
			}
		);
	}

	public removeIdentifizierungsmittel() {
		this.loading = true;
		this._userService.setIdentifizierungsmittelPdf(this.detail?.item?.id, this._authorization.roles.Oe2, null).subscribe(
			() => {
				this._ui.showSuccess(this._txt.get('userAndRoles.downgradsuccess', 'Identifizierungsmittel erfolgreich gelöscht'));
				this.detail.item.hasIdentifizierungsmittel = null;
			},
			err => {
				this.loading = false;
				this._ui.showError(this._txt.get('userAndRoles.downgradfail', 'Identifizierungsmittel konnte nicht gelöscht werden'), err.message);
			},
			() => {
				this.loading = false;
			}
		);
	}

	private downgradeToOe2() {
		this.detail.item.hasIdentifizierungsmittel = false;
		this.selectedIdentifizierungsmittel = null;
		this.recalculateResearcherGroup();
	}

	public onCancelClick(): void {
		this.myForm.controls.rolePublicClient.reset();
		this.myForm.controls.rolePublicClient.setValue(this.detail?.item?.rolePublicClient);
		this.showModal = false;
	}

	// eslint-disable-next-line
	public onOkClick(event: any): void {
		this.showModal = false;

		// Forschungsgruppe DDS darf nicht für Ö2 gewählt werden
		if (this.myForm.controls.rolePublicClient.value  === this._authorization.roles.Oe2) {
			this.myForm.controls['researcherGroup'].setValue(false);
		}

		switch (this.myForm.controls.rolePublicClient.value) {
			case this._authorization.roles.Oe2:
				if (this.detail?.item?.hasIdentifizierungsmittel) {
					this.removeIdentifizierungsmittel();
					this.downgradeToOe2();
				} else {
					this.downgradeToOe2();
				}
				break;
			case this._authorization.roles.Oe3:
				if (this.detail?.item?.rolePublicClient === this._authorization.roles.Oe2){
					// Es kann nicht von Oe2 zu Oe3 gewechselt werden. Dies muss ueber ein PDF upload gemacht werden.
					this.uploadIdentifierungsmittel();
				}
				break;
		}

		this.updateValidators();
		this.updateErrorMessages();
	}

	public GetModalMessage(): string {
		switch (this.detail?.item?.rolePublicClient) {
			case this._authorization.roles.BAR:
				return this._txt.get('user.upload.lessRightMessage', 'Der Benutzer verliert dadurch seine erweiterten Rechte. Möchten Sie fortfahren?');
			case this._authorization.roles.AS:
				if (this.myForm.controls.rolePublicClient.value === this._authorization.roles.BAR) {
					return this._txt.get('user.upload.asToBarMessage', 'Die dem Benutzer zugewiesenen Verlinkungen auf die zuständigen Stellen werden dauerhaft gelöscht. ' +
						'Der Benutzer erhält durch den Rollenwechsel erweiterte Rechte. Möchten Sie fortfahren?');
				} else  {
					return this._txt.get('user.upload.asToBvwMessage', 'Die dem Benutzer zugewiesenen Verlinkungen auf die zuständigen Stellen werden dauerhaft gelöscht. ' +
						'Der Benutzer verliert dadurch seine erweiterten Rechte. Wollen Sie fortfahren?');
				}
				break;
			case this._authorization.roles.BVW:
				if (this.myForm.controls.rolePublicClient.value === this._authorization.roles.BAR ||this.myForm.controls.rolePublicClient.value === this._authorization.roles.AS) {
					return this._txt.get('user.upload.moreRightMessage', 'Der Benutzer erhält durch den Rollenwechsel erweiterte Rechte. Möchten Sie fortfahren?');
				} else  {
					return this._txt.get('user.upload.lessRightMessage', 'Der Benutzer verliert dadurch seine erweiterten Rechte. Möchten Sie fortfahren?');
				}
			case this._authorization.roles.Oe3:
				if (this.myForm.controls.rolePublicClient.value === this._authorization.roles.BAR ||this.myForm.controls.rolePublicClient.value === this._authorization.roles.AS || this.myForm.controls.rolePublicClient.value === this._authorization.roles.BVW) {
					return this._txt.get('user.upload.moreRightMessage', 'Der Benutzer erhält durch den Rollenwechsel erweiterte Rechte. Möchten Sie fortfahren?');
				} else  {
					return this._txt.get('user.upload.oe3ToOe2Message', 'Die Identifizierungsausweise werden dauerhaft gelöscht. Möchten Sie fortfahren?');
				}
			case this._authorization.roles.Oe2:
				if (this._authorization.roles.Oe3 === this.myForm.controls.rolePublicClient.value) {
					return this._txt.get('user.upload.oe2ToOe3Message', 'Nach erfolgtem Upload des Identifizierungsnachweises als PDF-Datei ' +
						'erfolgt ein automatischer Rollenwechsel durch das System. Der Benutzer erhält durch den Rollenwechsel erweiterte Rechte. Möchten Sie fortfahren?');
				} else
					return this._txt.get('user.upload.moreRightMessage', 'Der Benutzer erhält durch den Rollenwechsel erweiterte Rechte. Möchten Sie fortfahren?');
			default:

				return 'Kein Text';
		}
	}

	public GetModalTitel(): string {
		switch (this.detail?.item?.rolePublicClient) {
			case this._authorization.roles.BAR:
				return this._txt.get('user.upload.lessRightTitel', 'Rolle zu {0} zurückstufen', this.myForm.controls.rolePublicClient.value);
			case this._authorization.roles.AS:
				if (this.myForm.controls.rolePublicClient === this._authorization.roles.BAR) {
					return this._txt.get('user.upload.moreRightTitel', 'Rollenwechsel erweiterte Rechte');
				} else  {
					return this._txt.get('user.upload.lessRightTitel', 'Rolle zu {0} zurückstufen', this.myForm.controls.rolePublicClient.value);
				}
			case this._authorization.roles.BVW:
				if (this.myForm.controls.rolePublicClient === this._authorization.roles.BAR ||this.myForm.controls.rolePublicClient.value === this._authorization.roles.AS) {
					return this._txt.get('user.upload.moreRightTitle', 'Rollenwechsel erweiterte Rechte');
				} else  {
					return this._txt.get('user.upload.lessRightTitle', 'Rolle zu {0} zurückstufen', this.myForm.controls.rolePublicClient.value);
				}
			case this._authorization.roles.Oe3:
				if (this.myForm.controls.rolePublicClient === this._authorization.roles.BAR ||this.myForm.controls.rolePublicClient.value === this._authorization.roles.AS || this.myForm.controls.rolePublicClient.value === this._authorization.roles.BVW) {
					return this._txt.get('user.upload.moreRightTitle', 'Rollenwechsel erweiterte Rechte');
				} else  {
					return this._txt.get('user.upload.lessRightTitle', 'Rolle zu {0} zurückstufen', this.myForm.controls.rolePublicClient.value);
				}
			case this._authorization.roles.Oe2:
				if (this._authorization.roles.Oe3 === this.myForm.controls.rolePublicClient) {
					return this._txt.get('user.upload.toOe3Title', 'Upload Identifizierungsmittel');
				} else
					return this._txt.get('user.upload.moreRightTitle', 'Rollenwechsel erweiterte Rechte');
			default:
				return 'Kein Titel';
		}
	}

	public detailPagingEnabled(): boolean {
		return this._dps.getCurrentIndex() > -1;
	}

	public getDetailBaseUrl(): string {
		return this._url.getBenutzerUebersichtUrl();
	}

	public canDeactivate(): boolean {
		return !this.getFormIsDirty();
	}

	public promptForMessage(): false | 'question' | 'message' {
		return  'question';
	}

	public message(): string {
		return this._txt.get('hints.unsavedChanges', 'Sie haben ungespeicherte Änderungen. Wollen Sie die Seite tatsächlich verlassen?');
	}

	public getFormIsDirty():boolean {

		if (this.rolesIsDirty) {
			return true;
		}
		if (this.hasAblieferndeStelleListChanged()) {
			return true;
		}
		if (this.myForm) {
			return this.myForm.dirty;
		} else {
			return false;
		}
	}

	private async _reload(): Promise<void> {
		await this._load(this.detail?.item?.id);
	}

	private _remove(items: any[], item: any): void {
		_util.remove(items, (i: any) => i.id === item.id);
	}

	private _buildCrumbs(): void {
		this.crumbs = [];
		this.crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		this.crumbs.push({
			url: this._url.getNormalizedUrl('/benutzerundrollen'),
			label: this._txt.get('breadcrumb.usersAndRoles', 'Benutzer und Rollen')
		});
		this.crumbs.push({
			url: this._url.getNormalizedUrl('/benutzerundrollen/benutzer'),
			label: this._txt.get('breadcrumb.usersRoles', 'Benutzerverwaltung')
		});
		if (this.detail?.item) {
			this.crumbs.push({label: this.detail.item.emailAddress});
		}
	}

	private _prepareResult(result: DetailResult<any>) {
		this.detail = result;
		this.stillAvailableRoles = [];
		/// User was saved without roles and roles were already changed
		if (this.stillSelectedRoles && this.initialeRoles) {
			this.detail.item.roles = [];
			this.distributeAssignedRoles(this.stillSelectedRoles, this.detail?.item?.roles);
		} else {
			this.initialeRoles = [];
			this.allRoles  = (this.detail as any).roles;
			this.distributeAssignedRoles(this.detail?.item?.roles, this.initialeRoles);
		}
		const access = result.item.access || {};
		result.item.tokens = access.asTokens || [];
		if (this.stillSelectedAblieferndeStelleList) {
			this.detail.item.ablieferndeStelleList = [];
			_util.forEach(this.stillSelectedAblieferndeStelleList, (ablieferndeStelleList: AblieferndeStelle) => {
				this.detail?.item?.ablieferndeStelleList.push(ablieferndeStelleList);
			});
		} else {
			this.initialeAblieferndeStelleList = [];
			_util.forEach(this.detail?.item?.ablieferndeStelleList, (ablieferndeStelleList: AblieferndeStelle) => {
				this.initialeAblieferndeStelleList.push(ablieferndeStelleList);
			});
		}
		this.detail.item.birthday = result.item.birthday ? new Date(result.item.birthday) : null;
		this.detail.item.downloadLimitDisabledUntil = result.item.downloadLimitDisabledUntil && this.isInDateRange(new Date(this.detail?.item?.downloadLimitDisabledUntil))
			? new Date(this.detail?.item?.downloadLimitDisabledUntil)	: null;
		this.detail.item.digitalisierungsbeschraenkungAufgehobenBis  = result.item.digitalisierungsbeschraenkungAufgehobenBis  &&
		this.isInDateRange(new Date(this.detail.item.digitalisierungsbeschraenkungAufgehobenBis))
			? new Date(this.detail.item.digitalisierungsbeschraenkungAufgehobenBis) : null;

		this.initForm();
		this._buildCrumbs();
	}

	private distributeAssignedRoles(source: any, target: any) {
		if (source) {
			_util.forEach(source, (role: any) => {
				target.push(role);
			});
		}
		if (this.allRoles) {
			_util.forEach(this.allRoles, (role: any) => {
				this.stillAvailableRoles.push(role);
			});
		}
		if (source) {
			_util.forEach(source, (role: any) => {
				this._remove(this.stillAvailableRoles, role);
			});
		}
	}

	private updateErrorMessages(): void {
		this.errors = {};

		for (const message of UserRolesDetailPageErrorMessages) {
			const control = this.myForm.get(message.forControl);
			if (control &&
				control.dirty &&
				control.invalid &&
				control.errors?.[message.forValidator] &&
				!this.errors[message.forControl]
			) {
				this.errors[message.forControl] = message.text;
			}
		}
	}

	private initForm(): void {
		// is required to reset disabled controls when switching through the entries
		this.myForm?.controls.familyName.enable();
		this.myForm?.controls.firstName.enable();
		this.myForm?.controls.emailAddress.enable();
		this.myForm?.controls.birthday.enable();
		this.myForm = this.formbuilder.group({
			familyName: new FormControl({
					value: this.detail.item.familyName,
					disabled: (!this.allowBereichBenutzerdatenBearbeiten || this.detail.item.rolePublicClient !== this._authorization.roles.Oe2)
				},
				[Validators.required, Validators.maxLength(200)]),
			firstName: new FormControl({
					value: this.detail.item.firstName,
					disabled: (!this.allowBereichBenutzerdatenBearbeiten || this.detail.item.rolePublicClient !== this._authorization.roles.Oe2)
				},
				[Validators.required, Validators.maxLength(200)]),
			organization: new FormControl({value: this.detail.item.organization, disabled: (!this.allowBereichBenutzerdatenBearbeiten)},
				this.isInternalUser ? [Validators.required, Validators.maxLength(200)] : [Validators.maxLength(200)]),
			street: new FormControl({value: this.detail.item.street, disabled: !this.allowBereichBenutzerdatenBearbeiten},
				[Validators.required, Validators.maxLength(200)]),
			streetAttachment: new FormControl({value: this.detail.item.streetAttachment, disabled: !this.allowBereichBenutzerdatenBearbeiten},
				[Validators.maxLength(200)]),
			zipCode: new FormControl({value: this.detail.item.zipCode, disabled: !this.allowBereichBenutzerdatenBearbeiten},
				[Validators.required, Validators.maxLength(200)]),
			town: new FormControl({value: this.detail.item.town, disabled: !this.allowBereichBenutzerdatenBearbeiten},
				[Validators.required, Validators.maxLength(200)]),
			countryCode: new FormControl({value: this.detail.item.countryCode, disabled: !this.allowBereichBenutzerdatenBearbeiten}),
			phoneNumber: new FormControl({value: this.detail.item.phoneNumber, disabled: !this.allowBereichBenutzerdatenBearbeiten},
				[Validators.pattern('^[()+]?([0-9][\\s()-]*){6,20}$')]),
			mobileNumber: new FormControl({value: this.detail.item.mobileNumber, disabled: !this.allowBereichBenutzerdatenBearbeiten},
				[Validators.pattern('^[()+]?([0-9][\\s()-]*){6,20}$')]),
			emailAddress: new FormControl({value: this.detail.item.emailAddress, disabled: (!this.allowBereichBenutzerdatenBearbeiten || this.isInternalUser)},
				[Validators.required, Validators.email, Validators.maxLength(200)]),
			fabasoftDossier: new FormControl({value: this.detail.item.fabasoftDossier, disabled: !this.allowBereichBenutzerdatenBearbeiten}),
			id: [this.detail.item.id],
			userExtId: [this.detail.item.userExtId],
			qoAValue: new FormControl({value: this.detail.item.qoAValue, disabled: true}),
			homeName: new FormControl({value: this.detail.item.homeName, disabled: true}),
			isIdentifiedUser: new FormControl({value: this.detail.item.isOe3OrHigher, disabled: true}),
			lastLoginDate: new FormControl({value: this.detail.item.lastLoginDate, disabled: true}),
			downloadLimitDisabledUntil: new FormControl(this.detail.item.downloadLimitDisabledUntil ? new Date(this.detail.item.downloadLimitDisabledUntil) : null, [this.dateRangeValidator.bind(this)]),
			digitalisierungsbeschraenkungAufgehobenBis: new FormControl(this.detail.item.digitalisierungsbeschraenkungAufgehobenBis ? new Date(this.detail.item.digitalisierungsbeschraenkungAufgehobenBis) : null, [this.dateRangeValidator.bind(this)]),
			language: [{value: this.detail.item.language, disabled: !this.allowBereichBenutzerdatenBearbeiten}],
			createModifiyData: [this.detail.item.createModifiyData],
			rolePublicClient: new FormControl({value: this.detail.item.rolePublicClient, disabled: !this.allowBereichBenutzerdatenBearbeiten}),
			birthday: [{value: this.detail.item.birthday, disabled: (!this.allowBereichBenutzerdatenBearbeiten || this.detail.item.rolePublicClient === this._authorization.roles.Oe3)}],
			researcherGroup: new FormControl({
				value: this.detail.item.researcherGroup,
				disabled: (this.detail.item.rolePublicClient !== this._authorization.roles.Oe3 || !this.detail.item.emailAddress.endsWith('@dodis.ch') || !this.allowForschungsgruppeDdsBearbeiten)
			}),
			barInternalConsultation: new FormControl({
				value: this.detail.item.barInternalConsultation,
				disabled: !this.allowBarInterneKonsultationBearbeiten || this.detail.item.qoAValue < 60
			})
		});

		this.myForm.statusChanges.subscribe(() => this.updateErrorMessages());
	}

	private reassembleDatatype() {
		const detailItem = this.myForm.getRawValue();
		this.detail.item.familyName = detailItem.familyName;
		this.detail.item.firstName = detailItem.firstName;
		this.detail.item.organization = detailItem.organization;
		this.detail.item.street = detailItem.street;
		this.detail.item.streetAttachment = detailItem.streetAttachment;
		this.detail.item.zipCode = detailItem.zipCode;
		this.detail.item.town = detailItem.town;
		this.detail.item.countryCode = detailItem.countryCode;
		this.detail.item.phoneNumber = detailItem.phoneNumber;
		this.detail.item.mobileNumber = detailItem.mobileNumber;
		this.detail.item.birthday = detailItem.birthday;
		this.detail.item.emailAddress = detailItem.emailAddress;
		this.detail.item.fabasoftDossier = detailItem.fabasoftDossier;
		this.detail.item.id = detailItem.id;
		this.detail.item.userExtId = detailItem.userExtId;
		this.detail.item.downloadLimitDisabledUntil = this.isInDateRange(detailItem.downloadLimitDisabledUntil) ? detailItem.downloadLimitDisabledUntil : null;
		this.detail.item.digitalisierungsbeschraenkungAufgehobenBis = this.isInDateRange(detailItem.digitalisierungsbeschraenkungAufgehobenBis) ?
			detailItem.digitalisierungsbeschraenkungAufgehobenBis : null;
		this.detail.item.language = detailItem.language;
		this.detail.item.rolePublicClient = detailItem.rolePublicClient;
		this.detail.item.researcherGroup = detailItem.researcherGroup;
		this.detail.item.barInternalConsultation = detailItem.barInternalConsultation;
		this.detail.item.qoAValue = detailItem.qoAValue;
		this.detail.item.isOe3OrHigher = detailItem.isOe3OrHigher;
		this.detail.item.homeName = detailItem.homeName;
		this.detail.item.lastLoginDate = detailItem.lastLoginDate;
		this.rememberSelectedRoles();
		this.rememberSelectedAblieferndeStelle();
		this.detail.item.roles = this.initialeRoles;
		this.detail.item.ablieferndeStelleList = this.initialeAblieferndeStelleList;
	}

	private isInDateRange(checkDate: Date) {
		if (!checkDate) {
			return false;
		}
		if (checkDate >= this.today && checkDate <= this.maxDate) {
			return true;
		}

		return false;
	}

	private rememberSelectedRoles() {
		this.stillSelectedRoles = [];
		_util.forEach(this.detail.item.roles, (role: any) => {
			this.stillSelectedRoles.push(role);
		});
	}

	private rememberSelectedAblieferndeStelle() {
		this.stillSelectedAblieferndeStelleList = [];
		_util.forEach(this.detail.item.ablieferndeStelleList, (ablieferndeStelleList: any) => {
			this.stillSelectedAblieferndeStelleList.push(ablieferndeStelleList);
		});
	}

	private async _load(id: string): Promise<void> {
		this.loading = true;
		try {
			await this._roleService.getUserInfo(id).then((res: any) => {
					this._prepareResult(res);
				});
		} catch (err: any) {
			this._ui.showError(err);
		}

		this.loading = false;
	}

	private _eiamRoleHandlingWarnungen(roleIdentifier:any) {
		// Falls ApplicationOwner oder Standard Rolle entfernt wird muss eine Meldung gezeigt werden, denn die werden beim Login automatisch wieder ergänzt
		if (roleIdentifier === 'Standard' && this.detail?.item?.eiamRoles === this._authorization.roles.ALLOW) {
			this._ui.showWarning(this._txt.get('usersAndRoles.warning.remove.standard', 'Achtung! Die «Standardrolle» wird dem Benutzer der Applikation automatisch wieder zugewiesen, ' +
				'solange in eIAM für den Management-Client die Rolle «ALLOW» hinterlegt ist. ' +
				'Falls Sie dem Benutzer die «Standardrolle» entziehen möchten, entfernen Sie bitte in der eIAM-Rollenverwaltung die Rolle «ALLOW»'));
		}
	}

	public dateRangeValidator(control: AbstractControl): any | null {
		if (!control.value || control.value === '' )  {
			if (control.invalid) {
				control.setErrors(null);
				this.updateErrorMessages();
			}
			return null;
		}

		if (control.value <= this.maxDate && control.value >= this.today) {
			return null;
		}

		// return error object
		return {'invalidDateRange': {'value': control.value}};
	}

	public get allowBarInterneKonsultationBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRollenBenutzerverwaltungFeldBarInterneKonsultationBearbeiten);
	}

	public get allowDownloadbeschraenkungBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRollenBenutzerverwaltungFeldDownloadbeschraenkungBearbeiten);
	}

	public get allowDigitalisierungsbeschraenkungBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRollenBenutzerverwaltungFeldDigitalisierungsbeschraenkungBearbeiten);
	}

	public get allowUploadAusfuehren(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRollenBenutzerverwaltungUploadAusfuehren);
	}

	public get allowHerunterladenAusfuehren(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRollenBenutzerverwaltungHerunterladenAusfuehren);
	}

	public get allowBenutzerrollePublicClientBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRollenBenutzerverwaltungFeldBenutzerrollePublicClientBearbeiten);
	}

	public get allowBereichBenutzerdatenBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten);
	}

	public get allowZustaendigeStelleBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRolleBenutzerverwaltungZustaendigeStelleEdit);
	}

	public get allowForschungsgruppeDdsBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRollenBenutzerverwaltungFeldForschungsgruppeDdsBearbeiten);
	}

	public get editUserHasOe3Role(): boolean {
		return this.myForm.controls.rolePublicClient.value === this._authorization.roles.Oe3;
	}

	public qoaValue(): number {
		return this.detail?.item?.qoAValue
	}

	public isFEDLogin(): boolean {
		return 'E-ID FED-LOGIN' === this.myForm?.controls.homeName.value
	}

	protected hasAblieferndeStelleListChanged(): boolean {
		const ablieferndeStelleIds = this.detail?.item?.ablieferndeStelleList != null ? this.detail?.item?.ablieferndeStelleList.map((as: AblieferndeStelle) => as.ablieferndeStelleId) : [];
		if (this.initialeAblieferndeStelleList?.length > 0 && this.initialeAblieferndeStelleList?.length === this.detail?.item?.ablieferndeStelleList?.length) {
			const length = this.initialeAblieferndeStelleList.length;
			for (let i = 0; i < length; i++) {
				if (ablieferndeStelleIds.indexOf(this.initialeAblieferndeStelleList[i].ablieferndeStelleId) < 0) {
					return true;
				}
			}
 		} else {
			return this.initialeAblieferndeStelleList?.length !== this.detail?.item?.ablieferndeStelleList?.length;
		}
		return false;
	}

	public recalculateResearcherGroup() {
		if (!this.myForm.controls['emailAddress'].value.endsWith('@dodis.ch')) {
			// Forschungsgruppe DDS muss @dodis.ch Endung haben
			this.myForm.controls['researcherGroup'].setValue(false);
			this.myForm.controls['researcherGroup'].disable();
		} else if (this.editUserHasOe3Role && this.allowForschungsgruppeDdsBearbeiten) {
			this.myForm.controls['researcherGroup'].enable();
		} else {
			this.myForm.controls['researcherGroup'].disable();
		}
	}

	public dataPickerValueUpdate($event: any, controlName: string) {
		if ($event.dateString === '') {
			this.myForm.controls[controlName].setValue(null);
		}
	}

	public get isInternalUser() {
		if (this._authorization.roles.Oe3 ===  this.detail?.item?.rolePublicClient  || this._authorization.roles.Oe2 === this.detail?.item?.rolePublicClient )  {
			return false;
		}

		return true;
	}

	public rolePublicClientChange() {
		this.showModal = true;
	}

	public rolePublicClientChangeOe3() {
		this.myForm.controls.rolePublicClient.setValue(this._authorization.roles.Oe3);
		this.myForm.controls.rolePublicClient.markAsDirty();
		this.showModal = true;
	}

	private updateValidators() {
		if (this.myForm.controls.rolePublicClient.value === this._authorization.roles.Oe3 || this.myForm.controls.rolePublicClient.value === this._authorization.roles.Oe2) {
			this.myForm.controls.organization.removeValidators(Validators.required);
			this.myForm.controls.emailAddress.enable();
		} else {
			this.myForm.controls.organization.addValidators(Validators.required);
			if (!this.myForm.controls.organization.value || this.myForm.controls.organization.value === ''){
				// Damit der Bearbeiter weiss warum nicht gespeichert werden kann
				this.myForm.controls.organization.markAsDirty();
			}
			this.myForm.controls.emailAddress.disable();
		}
		if (this.allowBereichBenutzerdatenBearbeiten && this.myForm.controls.rolePublicClient.value !== this._authorization.roles.Oe3){
			this.myForm.controls.birthday.enable();
		} else {
			this.myForm.controls.birthday.disable();
		}

		if (this.allowBereichBenutzerdatenBearbeiten && this.myForm.controls.rolePublicClient.value === this._authorization.roles.Oe2){
			this.myForm.controls.familyName.enable();
			this.myForm.controls.firstName.enable();
		} else {
			this.myForm.controls.familyName.disable();
			this.myForm.controls.firstName.disable();
		}

		this.myForm.controls.birthday.updateValueAndValidity();
		this.myForm.controls.familyName.updateValueAndValidity();
		this.myForm.controls.organization.updateValueAndValidity();
		this.myForm.controls.firstName.updateValueAndValidity();
	}
}
