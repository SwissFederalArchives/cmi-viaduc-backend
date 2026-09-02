import {Component, Input} from '@angular/core';

@Component({
    selector: 'cmi-spinner',
    templateUrl: 'spinner.component.html',
    styleUrls: ['./spinner.component.less'],
    standalone: false
})
export class SpinnerComponent {

	@Input()
	public hint: string;
}
