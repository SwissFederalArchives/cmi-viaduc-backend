import {Injectable} from '@angular/core';
import {ClientContext, CoreOptions, HttpService, Paging} from '@cmi/viaduc-web-core';
import {DetailResult, PagedResult} from '../../shared/model/apiModels';
import {Observable} from 'rxjs';

@Injectable()
export class RoleService {

	private _apiUrl: string;

	constructor(private _options: CoreOptions, private _http: HttpService, private _context: ClientContext) {
		this._apiUrl = this._options.serverUrl + this._options.privatePort + '/api/Role';
	}

	// region Roles

	public getRoleInfos(paging: Paging = null): Observable<PagedResult<any>> {
		let queryString = `?language=${this._context.language}`;
		if (paging) {
			queryString += '&paging=' + encodeURIComponent(JSON.stringify(paging));
		}

		const url = `${this._apiUrl}/GetRoleInfos${queryString}`;
		return this._http.get<PagedResult<any>>(url, this._http.noCaching);
	}

	public getRoleInfo(roleId: string): Promise<DetailResult<any>> {
		const queryString = `?roleId=${roleId}&language=${this._context.language}`;

		const url = `${this._apiUrl}/GetRoleInfo${queryString}`;
		return this._http.get<DetailResult<any>>(url, this._http.noCaching).toPromise();
	}

	public getRoles(): Promise<PagedResult<any>> {
		const url = `${this._apiUrl}/GetRoles`;
		return this._http.get<PagedResult<any>>(url, this._http.noCaching).toPromise();
	}

	public setRoleFeatures(roleId: any, featureIds: any[]): Promise<void> {
		const queryString = `?roleId=${roleId}`;
		const url = `${this._apiUrl}/SetRoleFeatures${queryString}`;
		return this._http.post<void>(url, {featureIds: featureIds}, this._http.noCaching).toPromise();
	}

	// endregion

	// region Users
	public getUserInfo(userId: string): Promise<DetailResult<any>> {
		const queryString = `?userId=${userId}&language=${this._context.language}`;
		const url = `${this._apiUrl}/GetUserInfo${queryString}`;
		return this._http.get<DetailResult<any>>(url, this._http.noCaching).toPromise();
	}

	public setUserRoles(userId: string, roleIds: any[]): Promise<void> {
		const queryString = `?userId=${userId}`;
		const url = `${this._apiUrl}/SetUserRoles${queryString}`;
		return this._http.post<void>(url, {roleIds: roleIds}, this._http.noCaching).toPromise();
	}

	// endregion

}
