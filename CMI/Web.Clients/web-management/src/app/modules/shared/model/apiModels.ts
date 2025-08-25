import {Paging} from '@cmi/viaduc-web-core';

export interface PagedResult<T> {
	items: T[];
	paging: Paging;

	dynamicColumns: any[];
}

export interface DetailResult<T> {
	item: T;
}
