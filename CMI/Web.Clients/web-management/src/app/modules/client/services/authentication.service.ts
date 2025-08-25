import {Injectable, Injector} from '@angular/core';
import {Router} from '@angular/router';
import {Observable, BehaviorSubject, of} from 'rxjs';
import {catchError, map} from 'rxjs/operators';
import {ContextService} from './context.service';
import {
	ConfigService,
	CoreOptions,
	HttpService,
	PreloadService,
	Session,
	Utilities as _util
} from '@cmi/viaduc-web-core';

import {SessionStorageService} from './sessionStorage.service';
import {UserService} from '../../shared/services/user.service';
import {AuthorizationService, UrlService} from '../../shared/services';
import {take} from 'rxjs/operators';
import {AuthStatus} from '../model';

const currentSessionKey = 'viaduc_management_auth_session';
const authReturnUrlKey = 'viaduc_management_auth_return_url';

@Injectable()
export class AuthenticationService {
	public isSigningIn = false;
	public onSignedIn: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);

	constructor(private _options: CoreOptions,
				private _config: ConfigService,
				private _http: HttpService,
				private _contextService: ContextService,
				private _authorizationService: AuthorizationService,
				private _sessionStorage: SessionStorageService,
				private _preloadService: PreloadService,
				private _userService: UserService,
				private _injector: Injector,
				private _urlService: UrlService) {
	}

	public login(idp: string = null): void {

		const callbackUrl = `${window.location.pathname}Auth/ExternalSignIn`; // relative url

		const returnUrl = window.location.hash.replace('#', '');
		let idpQueryParam = '';
		if (idp)
		{
			idpQueryParam = `&idp=${idp}`
		}

		const loginUrl = _util.addToString(this._options.serverUrl + this._options.publicPort, '/', 'AuthServices/SignIn?ReturnUrl=' + encodeURIComponent(callbackUrl) + idpQueryParam);
		this._sessionStorage.setUrl(authReturnUrlKey, returnUrl);
		window.location.assign(loginUrl);
	}

	public logout(): void {
		const callbackUrl = `${window.location.pathname}Auth/ExternalSignOut`; // relative url
		this.clearCurrentSession();

		const logoutUrl = _util.addToString(this._options.serverUrl + this._options.publicPort, '/', 'AuthServices/Logout?ReturnUrl=' + encodeURIComponent(callbackUrl));
		window.location.assign(logoutUrl);
	}

	public setDefaultRedirectUrl(): void {
		const homeUrl = this._urlService.getHomeUrl();
		this._sessionStorage.setUrl(authReturnUrlKey, homeUrl);
	}

	public redirectToOriginBeforeLogin(): void {
		const returnUrl = this._sessionStorage.getUrl(authReturnUrlKey);

		this.setDefaultRedirectUrl();
		if (returnUrl && returnUrl !== '') {
			const router = this._injector.get(Router);
			router.navigate([returnUrl]);
		}
	}

	public setCurrentSession(session: Session): void {
		this._sessionStorage.setItem(currentSessionKey, session);
		this._contextService.updateSession(session);
	}

	public clearCurrentSession(): void {
		this._config.removeSetting('user.settings');
		this.setCurrentSession(<Session>{});
	}

	private _initSession(identity?: any): void {
		const session = <Session>{
			inited: new Date().getTime()
		};

		this._authorizationService.setupSessionAuthorization(session, identity);

		let claims;
		if (_util.isObject(identity) && _util.isArray(identity.issuedClaims)) {
			claims = identity.issuedClaims;
		}

		if (_util.isArray(claims)) {
			session.authenticated = true;
			let matches = claims.filter(c => c.type.indexOf('/identity/claims/e-id/userExtId') >= 0);
			if (!_util.isEmpty(matches)) {
				session.userid = matches[0].value;
			}
			matches = claims.filter(c => c.type.indexOf('/identity/claims/displayName') >= 0);
			if (!_util.isEmpty(matches)) {
				session.username = matches[0].value;
			}
			matches = claims.filter(c => c.type.indexOf('/identity/claims/surname') >= 0);
			if (!_util.isEmpty(matches)) {
				session.lastname = matches[0].value;
			}
			matches = claims.filter(c => c.type.indexOf('/identity/claims/givenname') >= 0);
			if (!_util.isEmpty(matches)) {
				session.firstname = matches[0].value;
			}
			matches = claims.filter(c => c.type.indexOf('/identity/claims/emailaddress') >= 0);
			if (!_util.isEmpty(matches)) {
				session.emailaddress = matches[0].value;
			}
			matches = claims.filter(c => c.type.indexOf('/identity/claims/e-id/userExtId') >= 0);
			if (!_util.isEmpty(matches)) {
				session.userExtId = matches[0].value;
			}

			matches = claims.filter(c => c.type.indexOf('/identity/claims/authenticationmethod') >= 0);
			if (!_util.isEmpty(matches)) {
				session.isKerberosAuthentication = matches[0].value.toLowerCase().indexOf('kerberos') >= 0;
			}
		}

		this.setCurrentSession(session);

		if (!_util.isEmpty(identity)) {
			if (!this._userService.hasUserSettingsLoaded && !this._userService.isLoadingUserSettings) {
				if (!this._preloadService.isPreloaded) {
					this._preloadService.settingsloaded.pipe(take(1)).subscribe(() => {
						this._userService.initUserSettings();
					});
				} else {
					this._userService.initUserSettings();
				}
			}
		}
	}

	private _getIdentity(): Observable<any> {
		const baseUrl = this._options.serverUrl + this._options.publicPort;
		const claimsUrl = baseUrl + '/api/Auth/GetIdentity';
		return this._http.get<any>(claimsUrl);
	}

	public activateSession(): Observable<boolean> {
		this._initSession();
		return this._getIdentity().pipe(map((r) => {
				return this.handleIdentityResponse(r);
			}), catchError(() => {
				this.clearCurrentSession();
				return of(false);
			})
		);
	}

	private _getSessionFromStorage(): Session {
		return this._sessionStorage.getItem<Session>(currentSessionKey);
	}

	public async tryActivateExistingSession(): Promise<any> {
		const session = this._getSessionFromStorage();
		if (session != null && session.authenticated) {
			this._initSession();

			return this._getIdentity().toPromise().then(response_claims => {
				return this.handleIdentityResponse(response_claims);
			}, err => {
				console.error(err);
				this.clearCurrentSession();
				throw err;
			});
		}
	}

	private handleIdentityResponse(response: any) {
		const router = this._injector.get(Router);
		switch (response.authStatus) {
			case AuthStatus.ok:
				this._initSession(response);
				return true;
			case AuthStatus.keineKerberosAuthentication:
				router.navigate([this._urlService.getErrorSmartcardUrl()]);
				return true;
			case AuthStatus.neuerBenutzer:
				this._sessionStorage.setItem('pcurl', response.redirectUrl);
				router.navigate([this._urlService.getErrorNewUser()]);
				return true;
			case AuthStatus.requiresElevatedCheck:
				this.login('level-60');
				return true;
			default:
				console.error('Keine definierter AuthStatus!');
				break;
		}
	}

}
