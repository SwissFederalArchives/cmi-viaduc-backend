import {Component, Input} from '@angular/core';

@Component({
    selector: 'cmi-progressbar',
    templateUrl: './progressbar.component.html',
    styleUrls: ['./progressbar.component.less'],
    standalone: false
})
export class ProgressbarComponent {
	@Input()
	public percentage = 0;
}
