import {Component, OnInit} from '@angular/core';
import {AuthenticationService} from '../../modules/client/services/authentication.service';

@Component({
	selector: 'cmi-viaduc-auth-page',
	templateUrl: 'authPage.component.html'
})
export class AuthPageComponent implements OnInit {

	public success: boolean;

	constructor(private _authentication: AuthenticationService) {
	}

	public ngOnInit(): void {
		this._authentication.isSigningIn = true;
		this._authentication.activateSession().subscribe(
			r => {
				this._authentication.isSigningIn = false;
				this._authentication.onSignedIn.next(r);
				this.redirectToOriginBeforeLogin();
			},
			() => {
				this.success = false;
				this._authentication.isSigningIn = false;
				this._authentication.onSignedIn.next(false);
			}
		);
	}

	public redirectToOriginBeforeLogin(): void {
		this._authentication.redirectToOriginBeforeLogin();
	}
}
