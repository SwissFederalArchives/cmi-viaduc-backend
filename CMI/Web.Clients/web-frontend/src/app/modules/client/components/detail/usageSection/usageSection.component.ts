import {Component, Input} from '@angular/core';

@Component({
    selector: 'cmi-viaduc-usage-section',
    templateUrl: 'usageSection.component.html',
    styleUrls: ['./usageSection.component.less'],
    standalone: false
})
export class UsageSectionComponent {

	@Input()
	public text: string;
}
