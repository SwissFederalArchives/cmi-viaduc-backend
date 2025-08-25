import {Component, OnInit, ViewChild} from '@angular/core';
import {Router} from '@angular/router';
import {ApplicationFeatureEnum, CmiGridComponent, TranslationService} from '@cmi/viaduc-web-core';
import {News, NewsForEditor} from '../../../modules/client/model';
import {NewsService} from '../../../modules/client/services';
import {AuthorizationService, ErrorService, UrlService} from '../../../modules/shared/services';
import {CollectionView} from '@mescius/wijmo';
import {ToastrService} from 'ngx-toastr';

@Component({
	selector: 'cmi-news-management-page',
	templateUrl: 'newsManagementPage.component.html',
	styleUrls: ['./newsManagementPage.component.less']
})

export class NewsManagementPageComponent implements OnInit {
	public crumbs: any[] = [];
	public newsForEditor: CollectionView;

	@ViewChild('flexGrid', { static: true })
	public flexGrid: CmiGridComponent;

	constructor(private _newsService: NewsService,
				private _url: UrlService,
				private _err: ErrorService,
				private _toastr: ToastrService,
				private _txt: TranslationService,
				private _router: Router,
				private _aut: AuthorizationService) {
	}

	public ngOnInit(): void  {
		this._buildCrumbs();
		this._getAndStoreNews();
	}

	private _buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		const communication = 'kommunikation';
		crumbs.push({ iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl() });
		crumbs.push({ url: this._url.getNormalizedUrl(`/${communication}`), label: this._txt.get('breadcrumb.communication', 'Kommunikation') });
		crumbs.push({ url: this._url.getNormalizedUrl(`/${communication}/news`), label: this._txt.get('breadcrumb.news', 'News') });
	}

	public editNews(item: NewsForEditor):void {
		this._router.navigate([this._url.getNormalizedUrl('kommunikation/news/bearbeiten') + '/' + item.id]);
	}

	public addNews(): void {
		this._router.navigate([this._url.getNormalizedUrl('kommunikation/news/erfassen')]);
	}

	public refresh(item: NewsForEditor):void {
		item.transferValues();
	}

	public get deleteButtonDisabledText(): string {
		const disabled = 'disabled';

		if (!this.flexGrid) {
			return disabled;
		}

		return this.flexGrid.checkedItems.length > 0 ? null : disabled;
	}

	public deleteChecked():void {
		if (!this.flexGrid) {
			return;
		}

		const toDelete: string[] = this.flexGrid.checkedItems.map(n => n.id);
		if (toDelete.length === 0) {
			return;
		}

		this._newsService.deleteNews(toDelete).then(() => {
			this._getAndStoreNews();
			this._toastr.success('Erfolgreich gelöscht');
		},
		(error) => {
			this._err.showError(error);
		});
	}

	private _getAndStoreNews(): void {
		this._newsService.getAllNewsForManagementClient().then((data: News[]) => {
				const news = [];
				for (const n of data) {
					news.push(new NewsForEditor(n));
				}
				this.newsForEditor = new CollectionView(news);
			},
			(error) => {
				this._err.showError(error);
			});
	}

	public get allowNewsBearbeiten(): boolean {
		return this._aut.hasApplicationFeature(ApplicationFeatureEnum.AdministrationNewsBearbeiten);
	}
}
