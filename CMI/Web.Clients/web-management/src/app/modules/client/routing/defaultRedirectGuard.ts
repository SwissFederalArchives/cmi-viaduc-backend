
import {Observable} from 'rxjs';
import {Injectable, Injector} from '@angular/core';
import {ActivatedRouteSnapshot, Router} from '@angular/router';
import {ClientContext, Utilities as _util} from '@cmi/viaduc-web-core';
import {DefaultContextGuard} from './defaultContextGuard';
import {ContextService} from '../services';
import {UrlService} from '../../shared/services';

@Injectable()
export class DefaultRedirectGuard extends DefaultContextGuard {
	constructor(context: ClientContext,
				contextService: ContextService,
				private _url: UrlService,
				private _injector: Injector) {
		super(context, contextService);
	}

	public canActivate(route: ActivatedRouteSnapshot): Observable<boolean> {
		const can = super.canActivate(route);

		const url = route.url.reduce((l, s) => {
			l += '/' + s.path;
			return l;
		}, '');
		let redirectUrl = url;

		if (_util.isEmpty(url) || url === '/') {
			redirectUrl = this._url.getHomeUrl();
		} else {
			redirectUrl = this._url.localizeUrl(this.context.language, url);
		}

		if (redirectUrl !== url) {
			const router = this._injector.get(Router);
			router.navigate([redirectUrl], {queryParamsHandling: 'merge'}).then(res => {
				return res && can;
			});
		} else {
			return can;
		}
	}

}
