import {Component, OnInit} from '@angular/core';
import {ConverterProgressService} from '../../services';
import {UrlService} from '../../../shared';
import {TranslationService} from '@cmi/viaduc-web-core';
import {NavigationStart, Router} from '@angular/router';
import {filter} from 'rxjs/operators';
import {ProgressDetail} from '../../model/progressDetail';
import {ToastrService} from 'ngx-toastr';
import {formatDate} from '@angular/common';

@Component({
	selector: 'cmi-converter-progress',
	templateUrl: './converterProgress.component.html',
	styleUrls: ['./converterProgress.component.less']
})
export class ConverterProgressComponent implements OnInit {
	public crumbs: any[] = [];
	public loading: boolean;
	public progressTextExtraction: ProgressDetail[];
	public progressTransform: ProgressDetail[];
	public showDetails: boolean;

	constructor(private progressService: ConverterProgressService,
				private _txt: TranslationService,
				private _toastr: ToastrService,
				private _url: UrlService,
				_router: Router) {

		_router.events.pipe(
			filter(event => event instanceof NavigationStart))
			.subscribe(() => {
				progressService.pause();
			});
	}

	public ngOnInit(): void {
		this._buildCrumbs();
		this.loading = true;
		this.progressService.startOrResume();
		this.progressService.getCurrentConverterProgress().subscribe(r => {
			this.progressTransform = r.filter(f => f.processType === 'Rendering');
			this.progressTextExtraction = r.filter(f => f.processType === 'TextExtraction');
			this.loading = false;
		});
	}

	public removeItem(detailId: string) {
		this.progressService.removeItem(detailId).subscribe(() => {
			this._toastr.success(this._txt.get('converterProgress.removeItemSuccess', 'Eintrag gelöscht'));
		}, () => {
			this._toastr.error(this._txt.get('converterProgress.removeItemFailure', 'Eintrag konnte nicht gelöscht werden'));
		});
	}

	public removeAll() {
		this.progressService.clearProgressInfo().subscribe(() => {
			this._toastr.success(this._txt.get('converterProgress.removeAllSuccess', 'Einträge gelöscht'));
		}, () => {
			this._toastr.error(this._txt.get('converterProgress.removeAllFailure', 'Einträge konnten nicht gelöscht werden'));
		});
	}

	public getProgressbarClass(detail: ProgressDetail) {
		switch (detail.processState) {
			case 'warning':
				return 'progress-bar progress-bar-warning';
			case 'danger':
				return 'progress-bar progress-bar-danger';
			default:
				return detail.completed ? 'progress-bar progress-bar-success' : 'progress-bar';
		}
	}

	public getFormattedStartedOn(detail: ProgressDetail): string {
		return `Gestartet am: ${formatDate(detail.startedOn, 'dd.MM.yyyy HH:mm:ss', 'en')}`;
	}

	public toggleShowDetails() {
		this.showDetails = !this.showDetails;
	}

	private _buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({ iconClasses: 'glyphicon glyphicon-home', _url: this._url.getHomeUrl() });
		crumbs.push({
			label: this._txt.get('breadcrumb.Reporting', 'Reporting')
		});
		crumbs.push({
			url: this._url.getNormalizedUrl('reporting/converterProgress'),
			label: this._txt.get('breadcrumb.converterProgress', 'Abbyy Aktivitäten')
		});
	}
}
