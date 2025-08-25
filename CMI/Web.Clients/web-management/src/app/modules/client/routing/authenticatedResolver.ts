import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import {Injectable} from '@angular/core';
import {AuthenticationService} from '../services';
import {ClientContext} from '@cmi/viaduc-web-core';
import {skip, map} from 'rxjs/operators';
import {Observable, of} from 'rxjs';

@Injectable()
export class AuthenticatedResolver  {
	constructor(private _context: ClientContext, private _authService: AuthenticationService) {
	}

	// eslint-disable-next-line
	public resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> | Observable<boolean> {
		if (this._context.authenticated) {
			return of(true);
		} else {

			if (this._authService.isSigningIn) {
				// skipping first, because its a behaviour-subject
				return this._authService.onSignedIn
					.pipe(skip(1))
					.pipe(map(res => {
						return res;
					}));
			} else {
				return of(false);
			}
		}
	}
}
