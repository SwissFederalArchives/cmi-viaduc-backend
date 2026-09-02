import {Component, Input} from '@angular/core';
import {ErrorInfo} from '../../';

@Component({
    selector: 'cmi-error-content',
    templateUrl: 'errorContent.component.html',
    standalone: false
})
export class ErrorContentComponent {
	@Input()
	public error: ErrorInfo;
}
