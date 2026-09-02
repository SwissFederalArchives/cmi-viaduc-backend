import {PipeTransform, Pipe} from '@angular/core';
import {DomSanitizer} from '@angular/platform-browser';

@Pipe({
    name: 'safeHtml',
    standalone: false
})
export class SafeHtmlPipe implements PipeTransform {
	constructor(private _sanitizer: DomSanitizer) {
	}

	public transform(value: any) {
		return this._sanitizer.bypassSecurityTrustHtml(value);
	}
}
