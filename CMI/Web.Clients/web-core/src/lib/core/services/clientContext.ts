import {DEFAULT_LANGUAGE} from '../model/translations';
import {ClientModel} from './clientModel';
import {Injectable} from '@angular/core';
import moment from 'moment';
import {Session} from '../model/session';
import * as wjCore from '@mescius/wijmo';
import {SearchState} from '../model/searchState';

@Injectable()
export class ClientContext {

	public get client(): ClientModel {
		return this._clientModel;
	}

	private _defaultLanguage: string = DEFAULT_LANGUAGE;
	private _currentLanguage: string = DEFAULT_LANGUAGE;
	private _loadingLanguage: string;

	private _currentSession: Session = <Session>{};
	private _searchState: SearchState = <SearchState>{};

	public lastSearchLink: string;

	constructor(private _clientModel: ClientModel) {
	}

	public get defaultLanguage(): string {
		return this._defaultLanguage || DEFAULT_LANGUAGE;
	}

	public set defaultLanguage(value: string) {
		this._defaultLanguage = value;
	}

	public get language(): string {
		return this._currentLanguage || DEFAULT_LANGUAGE;
	}

	public set language(lang: string) {
		this._currentLanguage = lang;
		moment.locale(lang);

		this.loadWijmoLocalizationFile(lang);
		wjCore.Control.invalidateAll();
	}

	public loadWijmoLocalizationFile(language: string) {
		if (language === 'de') {
			language = 'de-CH';
		}

		document
			.querySelectorAll<HTMLScriptElement>('script[src*="wijmo.culture"]')
			.forEach((el) => el.remove());

		const node = document.createElement('script');
		node.src = `client/wijmo.culture.${language}.js`;
		node.type = 'text/javascript';
		node.async = true;

		document.head.appendChild(node);
	}

	public get loadingLanguage(): string {
		return this._loadingLanguage;
	}

	public set loadingLanguage(value: string) {
		this._loadingLanguage = value;
	}

	public get authenticated(): boolean {
		return this._currentSession.authenticated === true;
	}

	public get currentSession(): Session {
		return this._currentSession || <Session>{};
	}

	public set currentSession(value: Session) {
		this._currentSession = value;
	}

	public get search(): SearchState {
		return this._searchState;
	}

	public set search(value: SearchState) {
		this._searchState = value;
	}
}
