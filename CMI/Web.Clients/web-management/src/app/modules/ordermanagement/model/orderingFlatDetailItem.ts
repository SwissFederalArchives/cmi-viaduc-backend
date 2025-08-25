import {OrderingFlatItem} from './orderingFlatItem';
import {StatusHistory} from './statusHistory';
import {Bestellhistorie} from './bestellhistorie';
import {ArchiveRecordContextItem} from '@cmi/viaduc-web-core';

export class OrderingFlatDetailItem extends OrderingFlatItem {
	public statusHistory: StatusHistory[];
	public orderingHistory: Bestellhistorie[];
	public hasMoreOrderingHistory: boolean;
	public archivplanKontext: ArchiveRecordContextItem[];
}
