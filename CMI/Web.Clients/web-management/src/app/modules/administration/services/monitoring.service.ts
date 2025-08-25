import {Injectable} from '@angular/core';
import {CoreOptions, HttpService} from '@cmi/viaduc-web-core';
import {MonitoringResult} from '../model/monitoringResult';

@Injectable()
export class MonitoringService {

	private _apiUrl: string;

	constructor(private _options: CoreOptions, private _http: HttpService) {
		this._apiUrl = this._options.serverUrl + this._options.privatePort + '/api/Monitoring';
	}

	public getServicesStatus(): Promise<MonitoringResult[]> {
		const url = this._apiUrl + '/GetServicesStatus';
		return this._http.get<MonitoringResult[]>(url, this._http.noCaching).toPromise();
	}

	public getTestStatus(): Promise<MonitoringResult[]> {
		const url = this._apiUrl + '/GetTestStatus';
		return this._http.get<MonitoringResult[]>(url, this._http.noCaching).toPromise();
	}
}
