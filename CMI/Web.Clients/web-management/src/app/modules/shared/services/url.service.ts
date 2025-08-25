import {Injectable} from '@angular/core';
import {Utilities as _util, ClientContext, Routing, ConfigService} from '@cmi/viaduc-web-core';

@Injectable()
export class UrlService {
	constructor(private _context: ClientContext, private _cfg: ConfigService) {
	}

	public localizeUrl(lang: string, url: string): string {
		let localized = url;
		if (lang !== Routing.defaultLanguage) {
			localized = Routing.localizePath(lang, localized);
		}
		if (Routing.languagePrefixMatcher.test(localized)) {
			localized = localized.substring(Routing.languagePrefixLength);
		}
		return _util.addToString('/' + lang, '/', localized);
	}

	public normalizeUrl(lang: string, url: string): string {
		let normalized = url;
		if (lang !== Routing.defaultLanguage) {
			normalized = Routing.normalizePath(lang, normalized);
		}
		if (Routing.languagePrefixMatcher.test(normalized)) {
			normalized = normalized.substring(Routing.languagePrefixLength);
		}
		return _util.addToString('/' + Routing.defaultLanguage, '/', normalized);
	}

	public getNormalizedUrl(url: string): string {
		return this.localizeUrl(this._context.language, url);
	}

	public getHomeUrl(): string {
		return this.localizeUrl(this._context.language, '/home');
	}

	public getAuftragsuebersichtUrl(): string {
		return this.localizeUrl(this._context.defaultLanguage, '/auftragsuebersicht/auftraege');
	}

	public getEinsichtsgesucheUebersichtUrl(): string {
		return this.localizeUrl(this._context.defaultLanguage, '/auftragsuebersicht/einsichtsgesuche');
	}

	public getBenutzerUebersichtUrl(): string {
		return this.localizeUrl(this._context.defaultLanguage, '/benutzerundrollen/benutzer');
	}

	public getManuelleKorrekturenUrl(): string {
		return this.localizeUrl(this._context.defaultLanguage, '/anonymization/manuelleKorrekturen');
	}

	public getExternalHostUrl(): string {
		const scheme = window.location.protocol;
		let url = scheme + '//' + window.location.hostname;
		if (scheme === 'http:' && window.location.port !== '80' || scheme === 'https:' && window.location.port !== '443') {
			url = _util.addToString(url, ':', window.location.port);
		}
		return url;
	}

	public getPublicClientBaseURL(): string {
		const baseUrl = this._cfg.getSetting('publicClientUrl') as string;

		if (baseUrl.endsWith('/#')) {
			return baseUrl;
		} else if (baseUrl.endsWith('/')) {
			return baseUrl + '#';
		} else {
			return baseUrl + '/#';
		}
	}

	public getExternalBaseUrl(): string {
		return _util.addToString(this.getExternalHostUrl(), '/', window.location.pathname);
	}

	public setQuery(qs: string): void {
		const h = window.location.hash.split('?');
		window.location.hash = h[0] + '?' + qs;
	}

	public getErrorSmartcardUrl(): string {
		return this.localizeUrl(this._context.language, '/errorsmartcard');
	}

	public getErrorNewUser(): string {
		return this.localizeUrl(this._context.language, '/errornewuser');
	}

	public getErrorPermission(): string {
		return this.localizeUrl(this._context.language, '/errorpermission');
	}
}
