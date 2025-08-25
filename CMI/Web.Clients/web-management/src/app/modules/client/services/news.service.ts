import {Injectable} from '@angular/core';
import {CoreOptions, HttpService} from '@cmi/viaduc-web-core';
import {News} from '../model/communication/News';

@Injectable()
export class NewsService {
	constructor(private _options: CoreOptions,
				private _http: HttpService) {
	}

	public async getAllNewsForManagementClient(): Promise<News[]> {
		const url = this._createBaseUrl() + '/GetAllNewsForManagementClient';
		return await this._http.get<News[]>(url, this._http.noCaching).toPromise();
	}

	public async deleteNews(idsToDelete: any): Promise<any> {
		const url = this._createBaseUrl() + '/DeleteNews';
		return await this._http.post(url, idsToDelete, this._http.noCaching).toPromise();
	}

	public async getSingleNews(id: string): Promise<any> {
		const url = this._createBaseUrl() + '/GetSingleNews/' + id;
		return await this._http.get<News>(url, this._http.noCaching).toPromise();
	}

	private _createBaseUrl(): string {
		return this._options.serverUrl + this._options.publicPort + '/api/News';
	}

	public async insertOrUpdateNews(news: any): Promise<any> {
		const url = this._createBaseUrl() + '/InsertOrUpdateNews';
		return await this._http.post(url, news, this._http.noCaching).toPromise();
	}
}
