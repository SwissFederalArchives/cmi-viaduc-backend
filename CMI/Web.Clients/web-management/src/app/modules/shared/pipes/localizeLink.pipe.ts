import {Pipe, PipeTransform} from '@angular/core';
import {UrlService} from '../services';
import {Utilities as _util, ClientContext, Routing} from '@cmi/viaduc-web-core';

@Pipe({
	name: 'localizeLink'
})
export class LocalizeLinkPipe implements PipeTransform {
	constructor(private _context: ClientContext, private _url: UrlService) {
	}

	public transform(value: any, ...args: any[]): string {
		let language = this._context.language;
		if (args.length > 0 && _util.isString(args[0]) && Routing.languageMatcher.test(args[0])) {
			language = args[0];
		}
		return this._url.localizeUrl(language, value);
	}
}
