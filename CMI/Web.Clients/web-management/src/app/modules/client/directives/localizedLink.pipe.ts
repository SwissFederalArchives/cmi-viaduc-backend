import {Directive, HostListener, Input} from '@angular/core';
import {Router} from '@angular/router';
import {ClientContext} from '@cmi/viaduc-web-core';
import {UrlService} from '../../shared/services';

@Directive({
	selector: '[localizedLink]'
})
export class LocalizedLinkDirective {

	constructor(private _context: ClientContext, private _router: Router, private _url: UrlService) {
	}

	@Input('localizedLink')
	public url: string;

	@HostListener('click')
	public onClick() {
		const localized = this._url.localizeUrl(this._context.language, this.url);
		this._router.navigate([localized]);
	}
}
