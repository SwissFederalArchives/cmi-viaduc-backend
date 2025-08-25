import {FilterExpression} from './filterExpression';
import {SortExpression} from './sortExpression';

export class PagingRequestParameter {
	public take: number;
	public skip: number;
	public filterExpressions: FilterExpression[];
	public sortExpressions: SortExpression[];
}
