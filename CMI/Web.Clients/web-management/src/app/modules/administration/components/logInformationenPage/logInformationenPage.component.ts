import { Component, OnInit } from '@angular/core';
import { TranslationService } from '@cmi/viaduc-web-core';
import { UrlService, UiServiceMC } from '../../../shared/services';
import { LogService } from '../../services';
import * as moment from 'moment';
import * as fileSaver from 'file-saver';
import { HttpEventType, HttpErrorResponse } from '@angular/common/http';

@Component({
	selector: 'cmi-viaduc-logInformationenPage',
	templateUrl: './logInformationenPage.component.html',
	styleUrls: ['./logInformationenPage.component.less']
})
export class LogInformationenPageComponent implements OnInit {
	public crumbs: any[] = [];
	public startDate: Date;
	public endDate: Date;
	public loading: boolean;

	constructor(private _logService: LogService,
		private _txt: TranslationService,
		private _url: UrlService,
		private _ui: UiServiceMC) {
		this.endDate = moment.utc(new Date()).toDate();
		this.startDate = moment.utc(new Date()).add(-24, 'h').toDate();
	}

	public ngOnInit(): void {
		this._buildCrumbs();
	}

	public doExport() {
		this.loading = true;
		this._logService.getLogData(this.startDate, this.endDate).subscribe(
			event => {
				if (event.type === HttpEventType.Response) {
					try {
						const contentDisposition: string = event.headers.get('content-disposition');
						const filename = contentDisposition.substring(contentDisposition.indexOf('filename=') + 10, contentDisposition.length - 1);
						const blob = event.body;
						this.loading = false;
						fileSaver.saveAs(blob, filename, { autoBom: false });
						this._ui.showSuccess(this._txt.get('logInformationen.downloadSuccess', 'Die Loginformationen werden gespeichert.'));
					} catch (ex) {
						console.error(ex);
					}
				}
			},
			(error) => {
				this.loading = false;
				this.handleError(error);
			},
			() => {
				this.loading = false;
			});
	}

	private handleError(err: HttpErrorResponse): any {
		if (err.headers.get('content-type').startsWith('application/json')) {
			const reader = new FileReader();
			reader.addEventListener('loadend', (e) => {
				const errorInfo = JSON.parse(e.target.result.toString());
				this._ui.showError(errorInfo.message, this._txt.get('logInformationen.downloadFail', 'Loginformationen konnten nicht heruntergeladen werden.'));
			});
			reader.readAsText(err.error);
		} else {
			this._ui.showError(this._txt.get('logInformationen.downloadFail', 'Loginformationen konnten nicht heruntergeladen werden.'), err.message);
		}
	}

	private _buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({ iconClasses: 'glyphicon glyphicon-home', _url: this._url.getHomeUrl() });
		crumbs.push({
			label: this._txt.get('breadcrumb.Administration', 'Administration')
		});
		crumbs.push({
			url: this._url.getNormalizedUrl('/administration/logInformationen'),
			label: this._txt.get('breadcrumb.logInformationen', 'Loginformationen')
		});
	}
}
