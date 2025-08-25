import {Component, Input} from '@angular/core';

@Component({
	selector: 'cmi-record-info',
	templateUrl: './record-info.component.html',
	styleUrls: ['./record-info.component.less']
})
export class RecordInfoComponent {
	@Input() public modifiedBy: string;
	@Input() public modifiedOn: string;
	@Input() public createdBy: string;
	@Input() public createdOn: string;
}
