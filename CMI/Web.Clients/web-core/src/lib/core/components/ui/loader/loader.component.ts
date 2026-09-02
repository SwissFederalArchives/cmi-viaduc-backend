import {Component, Input} from '@angular/core';

@Component({
    selector: 'cmi-loader',
    templateUrl: 'loader.component.html',
    styleUrls: ['./loader.component.less'],
    standalone: false
})
export class LoaderComponent {

	@Input()
	public options: any = {};
}
