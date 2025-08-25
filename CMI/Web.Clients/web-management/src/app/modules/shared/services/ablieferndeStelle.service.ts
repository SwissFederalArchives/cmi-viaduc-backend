import {Injectable} from '@angular/core';
import {CoreOptions, HttpService} from '@cmi/viaduc-web-core';
import {Observable} from 'rxjs';
import {AblieferndeStelle} from '../model/ablieferndeStelle';

@Injectable()
export class AblieferndeStelleService {

	private _apiUrl: string;

	constructor(private _options: CoreOptions, private _http: HttpService) {
		this._apiUrl = this._options.serverUrl + this._options.privatePort + '/api/AblieferndeStelle';
	}

	public getAllAblieferndeStellen(): Observable<AblieferndeStelle[]> {
		const url = `${this._apiUrl}/GetAllAblieferndeStellen`;
		return this._http.get<AblieferndeStelle[]>(url, this._http.noCaching);
	}

	public async deleteAblieferndeStelle(ablieferndeStelleIds: number[]): Promise<any> {
		const url = `${this._apiUrl}/DeleteAblieferndeStelle`;
		return await this._http.post(url, ablieferndeStelleIds, this._http.noCaching).toPromise();
	}

	public getAblieferndeStelle(ablieferndeStelleId: number): Observable<AblieferndeStelle> {
		const url = `${this._apiUrl}/GetAblieferndeStelle?ablieferndeStelleId=${ablieferndeStelleId}`;
		return this._http.get<AblieferndeStelle>(url, this._http.noCaching);
	}

	public async createAblieferndeStelle(ablieferndeStelle: AblieferndeStelle): Promise<any> {
		const url = `${this._apiUrl}/CreateAblieferndeStelle`;
		return await this._http.post(url, ablieferndeStelle, this._http.noCaching).toPromise();
	}

	public async updateAblieferndeStelle(ablieferndeStelle: AblieferndeStelle): Promise<any> {
		const url = `${this._apiUrl}/UpdateAblieferndeStelle`;
		return await this._http.post(url, ablieferndeStelle, this._http.noCaching).toPromise();
	}
}
