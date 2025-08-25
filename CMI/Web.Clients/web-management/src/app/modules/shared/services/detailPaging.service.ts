
import {of as observableOf, Observable} from 'rxjs';
import {Injectable} from '@angular/core';
import {CollectionView} from '@mescius/wijmo';

@Injectable()
export class DetailPagingService {
	private _collectionView: CollectionView;

	public setCurrent(view: CollectionView, currentPosition: number) {
		this._collectionView = view;
		this._collectionView.currentPosition = Math.max(currentPosition, 0);
	}

	public getCurrent(): any {
		return this._collectionView ? this._collectionView.currentItem : null;
	}

	private _pageForwardIfNecessary(): boolean {
		if (this._collectionView.currentPosition === this._collectionView.itemCount - 1) {
			if (this._collectionView.pageIndex < this._collectionView.pageCount) {
				return this._collectionView.moveToNextPage();
			}
		}
		return false;
	}

	private _pageBackwardsIfNecessary():  boolean {
		if (this._collectionView.currentPosition === 0) {
			if (this._collectionView.pageIndex > 0) {
				return this._collectionView.moveToPreviousPage();
			}
		}
		return false;
	}

	public goToNext(): any {
		if (!this._pageForwardIfNecessary()) {
			this._collectionView.moveCurrentToNext();
		}
		return this.getCurrent();
	}

	public goToPrevious(): Observable<any> {
		if (!this._pageBackwardsIfNecessary()) {
			this._collectionView.moveCurrentToPrevious();
			return observableOf(this.getCurrent());
		} else {
			return this._delayMoveToLastAndReturnCurrent();
		}
	}

	public goToLast(): Observable<any> {
		this._collectionView.moveToLastPage();
		return this._delayMoveToLastAndReturnCurrent();
	}

	public goToFirst(): any {
		this._collectionView.moveToFirstPage();
		this._collectionView.moveCurrentToFirst();
		return this.getCurrent();
	}

	public hasNext(): boolean {
		return this.getCurrentIndex() + 1 < this.getMax();
	}

	public hasPrevious(): boolean {
		return this.getCurrentIndex() > 0;
	}

	public getMax(): number {
		return this._collectionView.totalItemCount;
	}

	public getCurrentIndex(): number {
		if (!this._collectionView) {
			return -1;
		}

		const p = this._collectionView.pageIndex;
		const t = this._collectionView.pageSize;

		const basis = p * t;
		return basis + this._collectionView.currentPosition;
	}

	private _delayMoveToLastAndReturnCurrent(): Observable<any> {
		// delay, because paging is async and moveCurrentToLast must happen after it
		// Wijmo does not provide Promise or Observable for that event to wait for it
		return new Observable((obs) => {
			setTimeout(() => {
				this._collectionView.moveCurrentToLast();
				obs.next(this.getCurrent());
				obs.complete();
			}, 300);
		});
	}
}
