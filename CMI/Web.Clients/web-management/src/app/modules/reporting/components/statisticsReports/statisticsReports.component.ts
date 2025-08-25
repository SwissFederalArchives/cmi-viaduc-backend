import { Component, OnInit } from '@angular/core';
import {UiServiceMC, UrlService} from '../../../shared/index';
import * as moment from 'moment';
import {TranslationService} from '@cmi/viaduc-web-core';
import {HttpErrorResponse, HttpEventType} from '@angular/common/http';
import * as fileSaver from 'file-saver';
import { StatisticReportService } from '../../services';

@Component({
	selector: 'cmi-statistics-reports',
	templateUrl: './statisticsReports.component.html',
	styleUrls: ['./statisticsReports.component.less']
})
export class StatisticsReportsComponent implements OnInit {
	public crumbs: any[] = [];
	public startDate: Date;
	public endDate: Date;
	public startDateDownload: Date;
	public endDateDownload: Date;
	public loading: boolean;

	constructor(private  statisticService: StatisticReportService,
				private _txt: TranslationService,
				private _url: UrlService,
				private _ui: UiServiceMC) {
		this.endDateDownload = this.endDate = moment.utc(new Date()).toDate();
		this.startDateDownload = this.startDate = moment.utc(new Date()).add(-24, 'h').toDate();
	}

	public ngOnInit(): void {
		this._buildCrumbs();
	}

	public doExport() {
		this.loading = true;
		this.statisticService.getStatisticReportData(this.startDate, this.endDate).subscribe(
			event => {
				if (event.type === HttpEventType.Response) {
					try {
						const contentDisposition: string = event.headers.get('content-disposition');
						const filename = contentDisposition.substring(contentDisposition.indexOf('filename=') + 10, contentDisposition.length - 1);
						const blob = event.body;
						this.loading = false;
						fileSaver.saveAs(blob, filename, { autoBom: false });
						this._ui.showSuccess(this._txt.get('StatisticsReport.downloadSuccess', 'Die Statistikreport werden gespeichert.'));
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

	public doExportDownload() {
		this.loading = true;
		this.statisticService.getStatisticReportDownloadData(this.startDateDownload, this.endDateDownload).subscribe(
			event => {
				if (event.type === HttpEventType.Response) {
					try {
						const contentDisposition: string = event.headers.get('content-disposition');
						const filename = contentDisposition.substring(contentDisposition.indexOf('filename=') + 10, contentDisposition.length - 1);
						const blob = event.body;
						this.loading = false;
						fileSaver.saveAs(blob, filename, { autoBom: false });
						this._ui.showSuccess(this._txt.get('StatisticsReport.downloadSuccess', 'Die Statistikreport werden gespeichert.'));
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
				this._ui.showError(errorInfo.message, this._txt.get('StatisticsReport.downloadFail', 'Statistikreport konnten nicht heruntergeladen werden.'));
			});
			reader.readAsText(err.error);
		} else {
			this._ui.showError(this._txt.get('StatisticsReport.downloadFail', 'Statistikreport konnten nicht heruntergeladen werden.'), err.message);
		}
	}

	private _buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({ iconClasses: 'glyphicon glyphicon-home', _url: this._url.getHomeUrl() });
		crumbs.push({
			label: this._txt.get('breadcrumb.Reporting', 'Reporting')
		});
		crumbs.push({
			url: this._url.getNormalizedUrl('reporting/statisticsReports'),
			label: this._txt.get('breadcrumb.statisticsReports', 'Statistiken und Reports')
		});
	}
}
