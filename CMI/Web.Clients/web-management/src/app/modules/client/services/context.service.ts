import {Injectable} from '@angular/core';
import {Subject, BehaviorSubject} from 'rxjs';
import {
	ClientContext,
	CookieOptions,
	CookieService, DEFAULT_LANGUAGE,
	PreloadService,
	Session,
	TranslationService,
	Utilities as _util
} from '@cmi/viaduc-web-core';
import {SeoService} from './seo.service';

const languageKey = 'viaduc_language';

@Injectable()
export class ContextService {

	public context: Subject<ClientContext>;

	private _languageCookieOptions: CookieOptions = new CookieOptions({
		path: '/',
		expires: new Date(new Date().getDate() + 365)
	});

	constructor(private _context: ClientContext,
				private _cookieService: CookieService,
				private _preloadService: PreloadService,
				private _txt: TranslationService,
				private _seo: SeoService) {
		const language = this._cookieService.get(languageKey);
		if (!_util.isEmpty(language)) {
			this._context.language = language;
		}

		// Fix auf Deutsch - Falls Multilanguage gefordert wird einfach nachfolgende Zeile löschen
		this._context.language = DEFAULT_LANGUAGE;

		this._context.loadingLanguage = this._context.language;

		this._preloadService.translationsLoaded.subscribe(translations => {
			if (translations && translations.language === this._context.loadingLanguage) {
				this._onLanguageUpdated(this._context.loadingLanguage);
			}
		});

		this.context = new BehaviorSubject<ClientContext>(this._context);
	}

	public updateLanguage(language: string): Promise<void> {
		// Fix auf Deutsch - Falls Multilanguage gefordert wird einfach nachfolgende Zeile löschen
		language = DEFAULT_LANGUAGE;

		this._cookieService.put(languageKey, language, this._languageCookieOptions);
		this._seo.setLanguageInfo(language);

		if (language !== this._context.language) {
			this._context.loadingLanguage = language;
			if (this._preloadService.hasTranslationsFor(language)) {
				this._onLanguageUpdated(language);
			} else {
				return this._preloadService.loadTranslationsFor(language); // will trigger _translationsLoadedSubscription
			}
		}
	}

	public updateSession(session: Session): void {
		this._context.currentSession = session;
		this.context.next(this._context);
	}

	private _onLanguageUpdated(language: string): void {
		this._context.language = language;
		this._context.loadingLanguage = undefined;
		this._txt.update();
		this._seo.updatePageInfo(null);
		this.context.next(this._context);
	}

}
