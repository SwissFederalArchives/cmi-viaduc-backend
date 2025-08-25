import {AfterViewInit, Component, ElementRef} from '@angular/core';
import {AuthenticationService} from '../../../services';
import {ClientContext, TranslationService, Utilities as _util} from '@cmi/viaduc-web-core';

@Component({
	selector: 'cmi-viaduc-header-content',
	templateUrl: 'headerContent.component.html'
})
export class HeaderContentComponent implements AfterViewInit {
	private _elem: any;
	private _languages: any[];

	constructor(private _context: ClientContext,
				private _txt: TranslationService,
				private _elemRef: ElementRef,
				private _authentication: AuthenticationService) {
		this._elem = this._elemRef.nativeElement;
	}

	public ngAfterViewInit(): void {
		_util.initJQForElement(this._elem);
	}

	private refresh(): void {
		const ls = this._languages = (this._languages || []);
		if (_util.isEmpty(ls)) {
			for (let i = 0; i < this._txt.supportedLanguages.length; i += 1) {
				const l = {...this._txt.supportedLanguages[i]};
				ls.push(l);
			}
		}
		for (let i = 0; i < this._languages.length; i += 1) {
			const l = this._languages[i];
			l.active = (l.key === this._context.language);
			l.label = l.active === true ? this._txt.get('languages.' + l.key + '.labelActive', l.key) : l.name;
		}
	}

	public get versionInfo(): string {
		const v = this._context.client.version;
		return v ? `${v.major}.${v.minor}.${v.revision}.${v.build}` : void 0;
	}

	public get languages(): any[] {
		this.refresh();
		return this._languages;
	}

	public get language(): string {
		return this._context.language;
	}

	public login(): void {
		this._authentication.login();
	}

	public logout(): void {
		this._authentication.logout();
	}

	public get authenticated(): boolean {
		return this._context.authenticated;
	}

	public get username(): string {
		return this._context.currentSession.username;
	}
}
