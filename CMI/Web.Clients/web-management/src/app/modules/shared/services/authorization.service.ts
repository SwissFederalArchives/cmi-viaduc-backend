import {Injectable} from '@angular/core';
import {ClientContext, ApplicationFeatureEnum, Session, Utilities as _util} from '@cmi/viaduc-web-core';

@Injectable()
export class AuthorizationService {
	// keep in-sync with CMI.Contract.Common.AccessRoles. Note: lower-cased
	public readonly roles: any = {
		Oe1: 'Ö1',
		Oe2: 'Ö2',
		Oe3: 'Ö3',
		BVW: 'BVW',
		AS: 'AS',
		BAR: 'BAR',
		ALLOW: 'ALLOW',
		APPO: 'APPO'
	};

	constructor(private _context: ClientContext) {
	}

	private _getApplicationFeatures(): { [key: string]: string; } {
		const dict: { [key: string]: string; } = { };
		const keys = Object.keys(ApplicationFeatureEnum).filter(value => typeof value === 'string');
		keys.forEach(k => dict[k] = k);
		return dict;
	}

	public setupSessionAuthorization(session: Session, identity?: any): void {
		session.roles = {};
		session.accessTokens = {};
		session.applicationRoles = {};
		session.applicationFeatures = {};

		session.authenticationRoles = this.roles;
		session.authorizationFeatures = this._getApplicationFeatures();

		if (!_util.isObject(identity)) {
			return;
		}

		if (_util.isArray(identity.roles)) {
			_util.forEach(identity.roles, t => {
				session.roles[t] = true;
			});
		}

		if (_util.isArray(identity.issuedAccessTokens)) {
			_util.forEach(identity.issuedAccessTokens, t => {
				session.accessTokens[t] = true;
			});
		}

		if (_util.isArray(identity.applicationRoles)) {
			_util.forEach(identity.applicationRoles, r => {
				session.applicationRoles[r.identifier] = true;
			});
		}

		if (_util.isArray(identity.applicationFeatures)) {
			_util.forEach(identity.applicationFeatures, f => {
				session.applicationFeatures[f.identifier] = true;
			});
		}
	}

	public hasRole(key: string): boolean {
		const session = this._context.currentSession || <Session>{};
		return _util.isObject(session.roles) && (session.roles[key] === true);
	}

	public hasApplicationRole(identifier: string): boolean {
		const session = this._context.currentSession || <Session>{};
		return _util.isObject(session.applicationRoles) && (session.applicationRoles[identifier] === true);
	}

	public hasApplicationFeature(identifier: ApplicationFeatureEnum): boolean {
		const key: string = ApplicationFeatureEnum[identifier];
		const session = this._context.currentSession || <Session>{};
		return _util.isObject(session.applicationFeatures) && (session.applicationFeatures[key] === true);
	}

	public authorized(): boolean {
		return this.hasRole(this.roles.ALLOW) || this.hasRole(this.roles.APPO);
	}
}
