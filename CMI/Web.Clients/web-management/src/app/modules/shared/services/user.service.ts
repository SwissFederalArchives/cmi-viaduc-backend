import {Injectable} from '@angular/core';
import {User} from '../model/user';
import {Subject, Observable} from 'rxjs';
import {ConfigService, CoreOptions, HttpService} from '@cmi/viaduc-web-core';
import * as moment from 'moment/moment';

@Injectable()
export class UserService {
	private _userSettingsLoaded = false;
	private _isLoadingUserSettings = false;

	public userSettingsLoaded: Subject<boolean>;

	constructor(private _options: CoreOptions,
				private _http: HttpService,
				private _cfg: ConfigService) {
		this.userSettingsLoaded = new Subject<boolean>();
	}

	public async getUser(): Promise<User> {
		const url = this._createBaseUrl() + 'GetUser';
		return this._http.get<User>(url, this._http.noCaching).toPromise();
	}

	public async getUsers(userIds: string[]): Promise<User[]> {
		const queryParams = userIds.map(i => i).join('&userIds=');
		const url = this._createBaseUrl() + 'GetUsers?userIds=' + queryParams;
		return this._http.get<User[]>(url, this._http.noCaching).toPromise();
	}

	public async getUserSettings(): Promise<any> {
		const url = this._createBaseUrl() + 'GetUserSettings';
		return this._http.get<any>(url, this._http.noCaching).toPromise();
	}

	public get hasUserSettingsLoaded(): boolean {
		return this._userSettingsLoaded;
	}

	public get isLoadingUserSettings(): boolean {
		return this._isLoadingUserSettings;
	}

	public initUserSettings(): void {
		if (this._userSettingsLoaded) {
			return;
		}

		this._isLoadingUserSettings = true;

		this.getUserSettings().then((settings) => {
			if (settings) {
				this._cfg.setSetting('user.settings', settings);
				this._userSettingsLoaded = true;
				this._isLoadingUserSettings = false;
				this.userSettingsLoaded.next(true);
			}
		});
	}

	public async updateUserSettings(settings: any): Promise<void> {
		const url = this._createUrl('UpdateUserSettings');
		await this._http.post<string>(url, settings, this._http.noCaching).toPromise();
	}

	public cleanAndAddAblieferndeStelle(userId: string, ablieferndeStelleIds: number[]): Observable<any> {
		const urlParameter = `?userId=${userId}`;
		const url = this._createUrl('CleanAndAddAblieferndeStelle' + urlParameter);
		return this._http.post<string>(url, {ablieferndeStelleIds: ablieferndeStelleIds}, this._http.noCaching);
	}

	public updateAllUserData(userItem: any): Observable<any> {
		const url = this._createUrl('UpdateUser');
		return this._http.post<string>(url, this._mapUserPostParameter(userItem), this._http.noCaching);
	}

	public getIdentifizierungsmittelPdfUrl(userId: string): string {
		const urlParameter = `?userId=${userId}`;
		const url = this._createUrl('GetIdentifizierungsmittelPdf' + urlParameter);
		return url;
	}

	public setIdentifizierungsmittelPdf(userId: string, rolePublicClient: string, identifizierungsmittel: FormData): Observable<any> {
		const urlParameter = `?userId=${userId}&rolePublicClient=${rolePublicClient}`;
		const url = this._createUrl('SetIdentifizierungsmittelPdf' + urlParameter);
		return this._http.post<string>(url, identifizierungsmittel, this._http.noCaching);
	}

	private _createUrl(methodName: string): string {
		return this._createBaseUrl() + methodName;
	}

	private _createBaseUrl(): string {
		return this._options.serverUrl + this._options.publicPort + '/api/User/';
	}

	private _mapUserPostParameter(userItem: any): any {
		userItem.birthdayString = this.getFormattedDateWithoutTime(userItem.birthday);
		userItem.downloadLimitDisabledUntilString = this.getFormattedDateWithoutTime(userItem.downloadLimitDisabledUntil);
		userItem.digitalisierungsbeschraenkungString = this.getFormattedDateWithoutTime(userItem.digitalisierungsbeschraenkungAufgehobenBis);
		return userItem;
	}

	private getFormattedDateWithoutTime(dt: Date | string) {
		return dt ? moment(dt).format('DD.MM.YYYY') : '';
	}

}
