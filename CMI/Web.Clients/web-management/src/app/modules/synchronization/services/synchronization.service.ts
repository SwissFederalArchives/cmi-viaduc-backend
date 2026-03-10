import { Injectable } from '@angular/core';
import {CoreOptions, HttpService, SyncActionLogDto, SyncAction, SyncNumberPerHourDto} from '@cmi/viaduc-web-core';
import {map} from 'rxjs/operators';
import {Observable} from 'rxjs';

@Injectable()
export class SynchronizationService {
	private readonly _createBaseUrl: string;

	constructor(private _options: CoreOptions, private http: HttpService) {
		this._createBaseUrl = this._options.serverUrl + this._options.publicPort + '/api/Synchronisationen/';
	}

	public getLogData(syncActionId: string): Observable<SyncActionLogDto[] | null> {
		let url = this._createBaseUrl + 'LogData/{syncActionId}';
		url = url.replace('{syncActionId}', syncActionId);
		return this.http.get<SyncActionLogDto[]>(url, this.http.noCaching).pipe(map(arr => arr.map(item => SyncActionLogDto.fromJS(item))));
	}
	public getSyncData(filter:	number): Observable<SyncAction[] | null> {
		const url = this._createBaseUrl + 'SyncData/' + filter;
		return this.http.get<SyncAction[]>(url, this.http.noCaching).pipe(map(arr => arr.map(item => SyncAction.fromJS(item))));
	}

	public batchAddSyncActions(ids: string[], action: number): Observable<any> {
		const url = this._createBaseUrl + 'BatchAddSyncActions/' + '' + action;
		return this.http.post(url, ids, this.http.noCaching);
	}

	public syncNumberPerHour(days:	number): Observable<SyncNumberPerHourDto[] | null> {
		const url = this._createBaseUrl + 'SyncNumberPerHour/' + days;
		return this.http.get<SyncNumberPerHourDto[]>(url, this.http.noCaching).pipe(map(arr => arr.map(item => SyncNumberPerHourDto.fromJS(item))));
	}
}
