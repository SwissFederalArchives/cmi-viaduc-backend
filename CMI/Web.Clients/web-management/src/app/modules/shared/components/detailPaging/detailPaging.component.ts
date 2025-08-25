import {Component, Input} from '@angular/core';
import {Router} from '@angular/router';
import {DetailPagingService} from '../../services';
@Component({
	selector: 'cmi-viaduc-detail-paging',
	templateUrl: 'detailPaging.component.html',
	styleUrls: ['./detailPaging.component.less']
})
export class DetailPagingComponent {
	public loading = false;

	@Input()
	public detailUrl: string;

	@Input()
	public idProperty: string;

	@Input()
	public disableNavigation: boolean;

	constructor(private _detailPaging: DetailPagingService,
				private _router: Router) {
	}

	public goToNext(): void {
		if (!this.disableNavigation) {
			const item = this._detailPaging.goToNext();
			this._router.navigate([this.detailUrl + '/' + item[this.idProperty]]);
		}
	}

	public goToPrevious(): void {
		if (!this.disableNavigation) {
			this._detailPaging.goToPrevious().subscribe(item => {
				this._router.navigate([this.detailUrl + '/' + item[this.idProperty]]);
			});
		}
	}

	public goToLast(): void {
		if (!this.disableNavigation) {
			this._detailPaging.goToLast().subscribe(item => {
				this._router.navigate([this.detailUrl + '/' + item[this.idProperty]]);
			});
		}
	}

	public goToFirst(): void {
		if (!this.disableNavigation) {
			const item = this._detailPaging.goToFirst();
			this._router.navigate([this.detailUrl + '/' + item[this.idProperty]]);
		}
	}

	public hasNext(): boolean {
		return this._detailPaging.hasNext();
	}

	public hasPrevious(): boolean {
		return this._detailPaging.hasPrevious();
	}

	public getMax(): number {
		return this._detailPaging.getMax();
	}

	public getCurrentIndex(): number {
		return this._detailPaging ? this._detailPaging.getCurrentIndex() : -1;
	}

}
