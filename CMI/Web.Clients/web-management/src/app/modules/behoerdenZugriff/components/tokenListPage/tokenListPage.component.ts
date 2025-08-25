import {Component, OnInit, ViewChild, ChangeDetectorRef} from '@angular/core';
import {TokenService} from '../../services';
import {ApplicationFeatureEnum, CmiGridComponent, TranslationService, Utilities as _util} from '@cmi/viaduc-web-core';
import {UrlService, ErrorService, AuthorizationService} from '../../../shared/services';
import {AsToken} from '../../model/asToken';
import {AblieferndeStelleToken} from '../../../shared/model/ablieferndeStelleToken';
import {Router} from '@angular/router';
import {CollectionView} from '@mescius/wijmo';

@Component({
	selector: 'cmi-viaduc-token-page',
	templateUrl: 'tokenListPage.component.html',
})
export class TokenListPageComponent implements OnInit {
	public loading: boolean;
	public crumbs: any[] = [];
	public tokenList: CollectionView;
	public showDeleteModal: boolean;

	public items: any[] = [];

	@ViewChild('flexGrid', { static: false })
	public flexGrid: CmiGridComponent;

	constructor(private _tokenService: TokenService,
				private _txt: TranslationService,
				private _url: UrlService,
				private _err: ErrorService,
				private _router: Router,
				private _cdRef: ChangeDetectorRef,
				public _authorization: AuthorizationService) {
	}

	public ngOnInit(): void {
		this.buildCrumbs();
		this.loadTokenList();
	}

	private loadTokenList(): void {
		this.loading = true;
		this._tokenService.getAllTokens().subscribe(
			res => this.prepareResult(res),
			err => this._err.showError(err),
			() => {
				this.loading = false;
			});
	}

	public getBehoerdenBeschreibung(item: any): string {
		if (item == null) {
			return '';
		}
		return item.ablieferndeStelleList.map(a => a.bezeichnung).join(', ');
	}

	public getBehoerdenKuerzel(item: any): string {
		if (item == null) {
			return '';
		}
		return item.ablieferndeStelleList.map(a => a.kuerzel).join(', ');
	}

	public deleteCheckedToken(): void {
		if (!this.flexGrid) {
			return;
		}

		const toDelete: number[] = this.flexGrid.checkedItems.map(t => t.tokenId);
		if (toDelete.length === 0) {
			return;
		}

		this._tokenService.deleteToken(toDelete).then(() => {
				this.loadTokenList();
				this._cdRef.detectChanges();
			},
			(error) => {
				this._err.showError(error);
			});
	}

	public editToken(item: any): void {
		const id = item ? item.tokenId : 'new';
		this._router.navigate([this._url.getNormalizedUrl('/behoerdenzugriff/tokendetail') + '/' + id]);
	}

	public addNewToken(): void {
		this._router.navigate([this._url.getNormalizedUrl('/behoerdenzugriff/tokendetail')]);
	}

	public get deleteButtonDisabled(): boolean {
		if (!this.flexGrid) {
			return true;
		}

		return this.flexGrid.checkedItems.length > 0 ? false : true;
	}

	public getElementToDelete(): string {
		if (this.getQuantityOfCheckedItemsToDelete() !== 1) {
			return '';
		}

		return this.flexGrid.checkedItems[0].token;
	}

	public getQuantityOfCheckedItemsToDelete(): number {
		const counter = 0;
		if (!this.flexGrid) {
			return counter;
		}

		return this.flexGrid.checkedItems.length;
	}

	public toggleDeleteModal(): void {
		this.showDeleteModal = !this.showDeleteModal;
	}

	public exportExcel() {
		const fileName = this._txt.get('behoerdenZugriff.tokenExportFileName', 'token.bar.ch.xlsx');
		this.flexGrid.exportToExcel(fileName).subscribe(() => {
			// nothing
		}, (err) => {
			this._err.showError(err);
		});
	}

	public isPosibleToDelete(item: AblieferndeStelleToken): boolean {
		if (!item) {
			return false;
		}

		return item.ablieferndeStelleList.length === 0;
	}

	private buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({
			label: this._txt.get('breadcrumb.behoerdenZugriff', 'Behörden-Zugriff')
		});
		crumbs.push({label: this._txt.get('breadcrumb.tokens', 'Access-Tokens')});
	}

	private prepareResult(result: AsToken) {
		if (!_util.isEmpty(result)) {
			for (const arrayItem of result.tokens) {
				arrayItem.ablieferndeStellenKuerzel = this.getBehoerdenKuerzel(arrayItem);
				arrayItem.ablieferndeStellenBezeichnung = this.getBehoerdenBeschreibung(arrayItem);
			}
		}
		this.tokenList = new CollectionView(result.tokens);
		this.tokenList.pageSize = 10;
	}

	public get allowAccessTokensBearbeiten(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BehoerdenzugriffAccessTokensBearbeiten);
	}
}
