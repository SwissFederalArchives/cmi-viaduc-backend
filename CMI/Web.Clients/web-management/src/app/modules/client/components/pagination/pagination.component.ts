import {Component, EventEmitter, Input, OnChanges, OnInit, Output} from '@angular/core';
import {ConfigService, Paging, Utilities as _util} from '@cmi/viaduc-web-core';

@Component({
	selector: 'cmi-viaduc-pagination',
	templateUrl: 'pagination.component.html',
	styleUrls: ['./pagination.component.less']
})
export class PaginationComponent implements OnInit, OnChanges {

	@Input()
	public paging: Paging;

	@Output()
	public onPaged: EventEmitter<Paging> = new EventEmitter<Paging>();

	public pagingSize: number;
	public possiblePagingSizes: number[];

	public items: any[] = [];

	constructor(private _cfg: ConfigService) {
	}

	public ngOnInit(): void {
		this.pagingSize = this._cfg.getValidPagingSize();
		this.possiblePagingSizes = this._cfg.getSetting('search.possiblePagingSizes');
		this._refresh();
	}

	// eslint-disable-next-line
	public ngOnChanges(changes: any) {
		this._refresh();
	}

	private _addItem(index: number, label?: string, active?: any, enabled?: any): any {
		let classes = '';
		active = !_util.isBoolean(active) ? (index === this.pageIndex) : (active === true);
		enabled = !_util.isBoolean(enabled) ? this.pageIndexIsNavigable(index) : (enabled === true);
		if (active) {
			classes = _util.addToString(classes, ' ', 'active');
			enabled = false;
		}
		if (!enabled) {
			classes = _util.addToString(classes, ' ', 'disabled');
		}
		const item = {
			index: index,
			label: _util.isEmpty(label) ? '' + (index + 1) : label,
			enabled: enabled,
			classes: classes
		};
		this.items.push(item);
		return item;
	}

	private _refresh(): void {
		const index = this.pageIndex;
		const ilast = this.pageCount - 1;
		const imax = Math.min(ilast, this._getNumberOfNavigableAndCompletelyFilledPages());

		this.items.splice(0, this.items.length);

		let ibase = Math.max(0, Math.min(ilast - 5, index - 2));

		if (ibase > 0) {
			this._addItem(0);
		}
		if (ibase > 1) {
			this._addItem(Math.max(1, ibase - 5), '...', false);
		}

		for (let i = 0; i < 5 && ibase <= ilast; i += 1, ibase += 1) {
			this._addItem(ibase);
		}
		ibase -= 1; // ibase is increment inside for

		if (ibase < ilast - 1) {
			this._addItem(Math.min(imax - 1, ibase + 5), '...', false);
		}
		if (ibase < ilast) {
			this._addItem(ilast);
		}
	}

	private _paging(): Paging {
		if (this.paging) {
			this.paging.skip = this.paging.skip || 0;
			this.paging.take = this.paging.take || this.pagingSize;

			/*
			 PV-514: Bei gleichzeitger Darstellung mehrerer Controls zur Einstellung der Anzahl Suchtreffer pro Seite
			 muss bei einer Aenderung auch noch this.pagingSize neu gesetzt werden
			 */
			this.pagingSize = this.paging.take;

			return this.paging;
		}
		return <Paging>{skip: 0, take: this.pagingSize};
	}

	public get pageIndex(): number {
		const p = this._paging();
		return _util.isNumber(p.take) && (p.take > 0) ? Math.floor(p.skip / p.take) : 0;
	}

	public get pageCount(): number {
		const p = this._paging();
		if (_util.isNumber(p.take) && _util.isNumber(p.total) && (p.take > 0)) {
			return Math.floor(p.total / p.take) + (p.total % p.take > 0 ? 1 : 0);
		}
		return 1;
	}

	public onChangePageSize() {
		this.paging.take = this.pagingSize;
		this.paging.skip = 0;
		this._refresh();
		this.onPaged.emit(this.paging);
	}

	public setPageIndex(i: number): void {
		const p = this._paging();
		if (i < 0) {
			i = 0;
		}
		const skippedHitsToReachLastPage = p.total - (p.total % p.take);
		p.skip = Math.min(skippedHitsToReachLastPage, i * p.take);
		this._refresh();
		this.onPaged.emit(p);
	}

	public pageIndexIsNavigable(i: number): boolean {
		return (i >= 0) && (i < this._getNumberOfNavigableAndCompletelyFilledPages());
	}

	private _getNumberOfNavigableAndCompletelyFilledPages(): number {
		const currentHitlistPageLimit = Math.ceil(this.paging.total / this.paging.take);
		return currentHitlistPageLimit;
	}
}
