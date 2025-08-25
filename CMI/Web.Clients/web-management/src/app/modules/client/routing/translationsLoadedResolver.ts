import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Injectable } from '@angular/core';
import { ClientContext, PreloadService } from '@cmi/viaduc-web-core';
import { map, take } from 'rxjs/operators';
import { Observable } from 'rxjs';

@Injectable()
export class TranslationsLoadedResolver  {
	constructor(private _context: ClientContext, private _preloadService: PreloadService) {
	}
	// eslint-disable-next-line
	public resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> | Promise<any> | any {
		const language = this._context.loadingLanguage || this._context.language;
		if (this._preloadService.hasTranslationsFor(language)) {
			return true;
		} else {
			return this._preloadService.translationsLoaded.pipe(take(1)).pipe(map(res => {
				return (res && res.language === language);
			}));
		}
	}
}
