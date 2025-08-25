import { Injectable } from '@angular/core';
import {CoreOptions, HttpService, ManuelleKorrekturDetailItem, ManuelleKorrekturDto} from '@cmi/viaduc-web-core';
import {Observable} from 'rxjs';
import {map} from 'rxjs/operators';

@Injectable()
export class ManuelleKorrekturenService {
	private readonly _createBaseUrl: string;

	constructor(private _options: CoreOptions, private http: HttpService) {
		this._createBaseUrl = this._options.serverUrl + this._options.publicPort + '/api/ManuelleKorrekturen/';
	}

	public async BatchAddManuelleKorrektur(veIds: string[]): Promise<Map<string, string>> {
		const url = this._createBaseUrl + 'BatchAddManuelleKorrektur';
		return this.http.post<Map<string, string>>(url, veIds, this.http.noCaching).toPromise();
	}

	public batchDelete(itemsToDelete: number[]): Observable<any> {
		const url = this._createBaseUrl + 'BatchDeleteManuelleKorrektur';
		if (itemsToDelete === undefined || itemsToDelete.length === 0) {
			throw new Error('The parameter ' + itemsToDelete + ' must be defined and contain values.');
		}
		return this.http.post(url, itemsToDelete, this.http.noCaching);
	}

	public delete(id: number): Observable<any> {
		let url = this._createBaseUrl + 'Delete/{id}';
		if (id === undefined || id === null) {
			throw new Error('The parameter ' + id + ' must be defined.');
		}
		url = url.replace('{id}', '' + id);
		return this.http.delete(url);
	}

	public update(value: ManuelleKorrekturDto | null): Observable<ManuelleKorrekturDto> {
		const url = this._createBaseUrl + 'InsertOrUpdateManuelleKorrektur';
		return this.http.post<ManuelleKorrekturDto>(url, value, this.http.noCaching);
	}

	public getManuelleKorrektur(id: number): Observable<ManuelleKorrekturDetailItem | null> {
		let url = this._createBaseUrl + 'GetManuelleKorrektur/{id}';
		if (id === undefined || id === null) {
			throw new Error('The parameter ' + id + ' must be defined.');
		}
		url = url.replace('{id}', '' + id);
		return this.http.get<ManuelleKorrekturDetailItem>(url, this.http.noCaching).pipe(map(r => {
			if (r === null) {
				return null;
			} else {
				return ManuelleKorrekturDetailItem.fromJS(r);

			}
		}));
	}

	public publizieren(id: number): Observable<ManuelleKorrekturDto> {
		let url = this._createBaseUrl + 'publizieren/{id}';
		if (id === undefined || id === null) {
			throw new Error('The parameter ' + id + ' must be defined.');
		}
		url = url.replace('{id}', '' + id);
		return this.http.get<ManuelleKorrekturDto>(url,  this.http.noCaching).pipe(map (r => ManuelleKorrekturDto.fromJS(r)));
	}
}
