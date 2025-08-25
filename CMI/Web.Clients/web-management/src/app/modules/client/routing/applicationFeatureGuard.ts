import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router } from '@angular/router';
import {ApplicationFeatureEnum} from '@cmi/viaduc-web-core';
import {AuthorizationService, UrlService} from '../../shared/services';

@Injectable()
export class ApplicationFeatureGuard  {
	constructor(private  _authorization: AuthorizationService, private _url: UrlService, private  _router: Router) {}

	public canActivate(route: ActivatedRouteSnapshot): boolean {
		const feature = route.data['applicationFeature'] as ApplicationFeatureEnum;
		if (!this._authorization.hasApplicationFeature(feature)) {
			this._router.navigate([this._url.getErrorPermission()]);
			return false;
		}
		return true;
	}
}
