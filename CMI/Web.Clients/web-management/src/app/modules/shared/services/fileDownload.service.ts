import {Injectable} from '@angular/core';
import {CoreOptions, HttpService} from '@cmi/viaduc-web-core';
import {Observable} from 'rxjs';
import {map} from 'rxjs/operators';

declare const jQuery: any;
@Injectable()
export class FileDownloadService {

	private _apiUrl: string;

	constructor(private _options: CoreOptions, private _http: HttpService) {
		this._apiUrl = this._options.serverUrl + this._options.privatePort + '/api/File';
	}

	public downloadFile(id: number): Observable<void> {
		return this._getOneTimeToken(id).pipe(map(linkWithToken => this._downloadFileWithToken(linkWithToken, id)));
	}

	private _getOneTimeToken(id: number): Observable<any> {
		const url = `${this._apiUrl}/GetOneTimeToken?orderItemId=${id}`;
		return this._http.get<any>(url);
	}

	private _downloadFileWithToken(token:any, id: number): void {
		const form = [];
		form.push(
			'<form action="',
			this._options.serverUrl + this._options.privatePort + '/api/File/DownloadFile',
			'" ',
			'method="get">',
			this._createHtmlHiddenField('id', id),
			this._createHtmlHiddenField('token', token),
			'</form>'
		);

		jQuery(form.join('')).appendTo('body').submit().remove();
	}

	private _createHtmlHiddenField(parameter:string, value: any): string {
		return `<input type="hidden" name="${parameter}" value="${value}" />`;
	}
}
