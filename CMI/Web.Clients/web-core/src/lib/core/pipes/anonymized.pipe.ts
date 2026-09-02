import {Pipe, PipeTransform, SecurityContext} from '@angular/core';
import {TranslationService} from '../services/translation.service';
import {DomSanitizer} from '@angular/platform-browser';

@Pipe({
    name: 'anonymizedHtml',
    standalone: false
})

export class AnonymizedHtmlPipe implements PipeTransform {
	constructor(private translationService: TranslationService,
				private sanitizer: DomSanitizer) {}

	public transform(value: string, cssClassName = 'text-anonymized', tooltip = 'Aus Datenschutzgründen anonymisiert.', pattern = '███', container: string = '') {
		const text = this.translationService.translate(tooltip, 'anonymized.Tooltip');

		let html = `<span class=${cssClassName} data-toggle="tooltip" title="${text}" >${pattern}</span>`;
		if (container === 'body') {
			html =  `<span class=${cssClassName} data-toggle="tooltip" title="${text}" data-container="body">${pattern}</span>`;
		}
		if (value) {
			const sanitizedValue = this.sanitizer.sanitize(SecurityContext.HTML, value);
			const sanitizedPattern = this.sanitizer.sanitize(SecurityContext.HTML, pattern);
			if (sanitizedPattern && sanitizedValue) {
				return this.sanitizer.bypassSecurityTrustHtml(sanitizedValue.split(sanitizedPattern).join(html));
			}

		}
		return value;
	}
}
