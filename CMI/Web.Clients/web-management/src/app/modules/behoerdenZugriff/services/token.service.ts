import {Injectable} from '@angular/core';
import {CoreOptions, HttpService} from '@cmi/viaduc-web-core';
import {AsToken} from '../model/asToken';
import {Observable} from 'rxjs';
import {AblieferndeStelleToken} from '../../shared/model/ablieferndeStelleToken';

@Injectable()
export class TokenService {

	private _apiUrl: string;

	constructor(private _options: CoreOptions, private _http: HttpService) {
		this._apiUrl = this._options.serverUrl + this._options.privatePort + '/api/Token';
	}

	public getAllTokens(): Observable<AsToken> {
		const url = `${this._apiUrl}/GetAllTokens`;
		return this._http.get<AsToken>(url, this._http.noCaching);
	}

	public getToken(tokenId: number): Observable<AsToken> {
		const url = `${this._apiUrl}/GetToken?tokenId=${tokenId}`;
		return this._http.get<AsToken>(url, this._http.noCaching);
	}

	public async deleteToken(tokenIds: number[]): Promise<any> {
		const url = `${this._apiUrl}/DeleteToken`;
		return await this._http.post(url, tokenIds, this._http.noCaching).toPromise();
	}

	public async createToken(token: AblieferndeStelleToken): Promise<any> {
		const url = `${this._apiUrl}/CreateToken`;
		return await this._http.post(url, token, this._http.noCaching).toPromise();
	}

	public async updateToken(token: AblieferndeStelleToken): Promise<any> {
		const url = `${this._apiUrl}/UpdateToken`;
		return await this._http.post(url, token, this._http.noCaching).toPromise();
	}
}
