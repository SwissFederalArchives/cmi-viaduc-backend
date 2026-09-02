import {Utilities as _util} from '../includes/utilities';

export interface CookieOptionsArgs {
	path?: string;
	domain?: string;
	expires?: string | Date;
	secure?: boolean;
}

export class CookieOptions {
	public path?: string;
	public domain?: string;
	public expires?: string | Date;
	public secure?: boolean;

	constructor({path, domain, expires, secure}: CookieOptionsArgs = {}) {
		this.path = !_util.isEmpty(path) ? path as string : undefined;
		this.domain = !_util.isEmpty(domain) ? domain as string : undefined;
		this.expires = !_util.isEmpty(expires) ? expires as string : undefined;
		this.secure = !_util.isEmpty(secure) ? secure as boolean : false;
	}

	public merge(options?: CookieOptionsArgs): CookieOptions {
		return new CookieOptions(<CookieOptionsArgs>{
			path: _util.isObject(options) && !_util.isEmpty((options as CookieOptionsArgs).path) ?
				(options as CookieOptionsArgs).path : this.path,
			domain: _util.isObject(options) && !_util.isEmpty((options as CookieOptionsArgs).domain) ?
				(options as CookieOptionsArgs).domain : this.domain,
			expires: _util.isObject(options) && !_util.isEmpty((options as CookieOptionsArgs).expires) ?
				(options as CookieOptionsArgs).expires : this.expires,
			secure: _util.isObject(options) && !_util.isEmpty((options as CookieOptionsArgs).secure) ?
				(options as CookieOptionsArgs).secure : this.secure
		});
	}
}
