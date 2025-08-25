import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Injectable } from '@angular/core';
import { PreloadService } from '@cmi/viaduc-web-core';
import { map, take } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { UserService } from '../../shared/services';

@Injectable()
export class UserSettingsResolver  {
	constructor(private _usrService: UserService, private _preloadService: PreloadService) {
	}
	// eslint-disable-next-line
	public resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> | Promise<any> | any {
		if (this._usrService.hasUserSettingsLoaded) {
			return true;
		} else {
			if (!this._preloadService.isPreloaded) {
				return this._preloadService.preloaded.pipe(take(1)).pipe(map((data) => {
					if (!data) {
						return false;
					}
					return this._usrService.userSettingsLoaded.pipe(take(1))
						.pipe(map((res) => {
							return res === true;
						}));
				}));
			} else {
				return this._usrService.userSettingsLoaded.pipe(take(1)).pipe(map((res) => {
					return (res === true);
				}));
			}
		}
	}
}
