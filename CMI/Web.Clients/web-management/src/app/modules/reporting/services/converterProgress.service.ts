import {Injectable, OnDestroy} from '@angular/core';
import {CoreOptions, HttpService} from '@cmi/viaduc-web-core';
import {Observable, Subject, timer} from 'rxjs';
import {ProgressDetail} from '../model/progressDetail';
import {retry, share, switchMap, takeUntil, takeWhile} from 'rxjs/operators';

@Injectable()
export class ConverterProgressService implements OnDestroy {

	private readonly progressDetails$: Observable<ProgressDetail[]>;
	private stopPolling = new Subject();
	private isPaused = false;
	private apiUrl: string;

	constructor(private _options: CoreOptions, private _http: HttpService) {
		this.apiUrl = this._options.serverUrl + this._options.privatePort + '/api/ConverterProgress';
		const url = this.apiUrl +  '/GetCurrentConverterProgress';

		this.progressDetails$ = timer(1, 5000).pipe(
			switchMap(() => this._http.get<ProgressDetail[]>(url)),
			retry(),
			share(),
			takeWhile(() => !this.isPaused),
			takeUntil(this.stopPolling)
		);
	}

	public getCurrentConverterProgress(): Observable<ProgressDetail[]> {
		return this.progressDetails$;
	}

	public ngOnDestroy(): void {
		this.stopPolling.next(true);
	}

	public pause() {
		this.isPaused = true;
	}

	public startOrResume() {
		this.isPaused = false;
	}

	public removeItem(detailId: string): Observable<any> {
		const postBody = {
			detailId: detailId
		};
		const url = this.apiUrl +  '/RemoveItemFromResults';
		return this._http.post(url, postBody);
	}

	public clearProgressInfo(): Observable<any> {
		const url = this.apiUrl +  '/ClearProgressInfo';
		return this._http.post(url, null);
	}
}
