import {Component, OnInit} from '@angular/core';
import {TranslationService} from '@cmi/viaduc-web-core';
import {UrlService} from '../../../shared/services';
import {MonitoringService} from '../../services';
import {MonitoringResult} from '../../model/monitoringResult';

@Component({
	selector: 'cmi-viaduc-monitoringPage',
	templateUrl: './monitoringPage.component.html',
	styleUrls: ['./monitoringPage.component.less']
})
export class MonitoringPageComponent implements  OnInit {
	public crumbs: any[] = [];
	public loadingWindowsServices: boolean = undefined;
	public loadingTests: boolean = undefined;
	public statuses: MonitoringResult[] = [];
	public testResults: MonitoringResult[] = [];

	constructor (private _txt: TranslationService, private _url:UrlService, private _monitoring: MonitoringService) {
	}

	public ngOnInit(): void {
		this._buildCrumbs();

		this.getMonitoredServices();
		this.getTestResults();
	}

	public onReload(): void {
		this.statuses = [];
		this.testResults = [];

		this.ngOnInit();
	}

	private _buildCrumbs(): void {
		this.crumbs = [];
		this.crumbs.push({iconClasses: 'glyphicon glyphicon-home', _url: this._url.getHomeUrl()});
		this.crumbs.push({
			label: this._txt.get('breadcrumb.Administration', 'Administration')
		});
		this.crumbs.push({
			url: this._url.getNormalizedUrl('/administration/systemstatus'),
			label: this._txt.get('breadcrumb.systemstatus', 'Systemstatus')
		});
	}

	private getMonitoredServices() {
		this.loadingWindowsServices = true;
		this._monitoring.getServicesStatus().then(response => {
			this.statuses = response;
			this.loadingWindowsServices = false;
		});
	}

	public async getTestResults() {
		this.loadingTests = true;
		this._monitoring.getTestStatus().then(response => {
			this.testResults = response;
			this.loadingTests = false;
		});
	}

	public get applicationStatus(): boolean {
		if (this.statuses && this.testResults) {
			if (this.statuses.length > 0 && this.testResults.length > 0) {
				for (const status of this.statuses) {
					if (status.status !== 'Ok') {
						return false;
					}
				}
				for (const status of this.testResults) {
					if (status.status !== 'Ok') {
						return false;
					}
				}
				return true;
			}
		}
		return false;
	}
}
