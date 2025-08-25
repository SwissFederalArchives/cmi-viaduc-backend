import {Injectable} from '@angular/core';
import {CoreOptions, HttpService} from '@cmi/viaduc-web-core';
import * as moment from 'moment';

@Injectable()
export class LogService {

	private _apiUrl: string;

	constructor(private _options: CoreOptions, private _http: HttpService) {
		this._apiUrl = this._options.serverUrl + this._options.privatePort + '/api/Log';
	}

	public getLogData(startDate: Date, endDate: Date): any {
		let url = this._apiUrl + '/GetElasticLogRecords';
		url = url + `?startDate=${moment(startDate).toISOString()}&endDate=${moment(endDate).toISOString()}`;
		return this._http.download(url);
	}
}
