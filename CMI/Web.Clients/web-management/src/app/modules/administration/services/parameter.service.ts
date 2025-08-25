import {Injectable} from '@angular/core';
import {Parameter} from '../model/parameter';
import {CoreOptions, HttpService} from '@cmi/viaduc-web-core';

@Injectable()
export class ParameterService {

	private _apiUrl: string;

	constructor(private _options: CoreOptions, private _http: HttpService) {
		this._apiUrl = this._options.serverUrl + this._options.privatePort + '/api/Parameter';
	}

	public async getAllParameters() {
		const url = this._apiUrl + '/GetAllParameters';
		return await this._http.get<Parameter[]>(url, this._http.noCaching).toPromise();
	}

	public async saveParameter(param: Parameter) {
		const url = this._apiUrl + '/SaveParameter';
		return await this._http.post<string>(url, param, this._http.noCaching).toPromise();
	}
}
