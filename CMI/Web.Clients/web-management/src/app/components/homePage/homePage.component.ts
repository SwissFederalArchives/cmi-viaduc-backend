import {Component, OnInit} from '@angular/core';
import {ClientContext} from '@cmi/viaduc-web-core';
import {AuthenticationService} from '../../modules/client/services/authentication.service';
import {AuthorizationService, UrlService} from '../../modules/shared/services';

@Component({
	selector: 'cmi-viaduc-home-page',
	templateUrl: 'homePage.component.html',
	styleUrls: ['./homePage.component.less']
})
export class HomePageComponent implements OnInit {

	public crumbs: any[] = [];

	public get authenticated(): boolean {
		return this._context.authenticated;
	}

	public get authorized(): boolean {
		return this.authenticated && this._authorization.authorized();
	}

	private _authenticating = false;
	public get authenticating(): boolean {
		return this._authenticating;
	}

	constructor(private _context: ClientContext,
				private _authentication: AuthenticationService,
				private _authorization: AuthorizationService,
				private _url: UrlService) {
	}

	private _buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
	}

	public ngOnInit(): void {
		this._buildCrumbs();

		if (!this.authenticated) {
			this._authenticating = true;
			this._authentication.isSigningIn = true;
			this._authentication.activateSession().subscribe(
				r => {
					if (r) {
						this._authentication.isSigningIn = false;
						this._authenticating = false;					
						this._authentication.onSignedIn.next(r);
						this.redirectToOriginBeforeLogin();
					} else {
						this._authenticating = false;
						this._authentication.login();
					}					
				},
				() => {					
					this._authentication.isSigningIn = false;
					this._authenticating = false;
					this._authentication.onSignedIn.next(false);
				}
			);
		}
	}

	public redirectToOriginBeforeLogin(): void {
		this._authentication.redirectToOriginBeforeLogin();
	}

	public login(): void {
		this._authentication.login();
	}

}
