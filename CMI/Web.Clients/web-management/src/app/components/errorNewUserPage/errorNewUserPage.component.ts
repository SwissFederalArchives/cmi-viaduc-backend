import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {ErrorInfo, TranslationService, Utilities as _util} from '@cmi/viaduc-web-core';
import {SessionStorageService} from '../../modules/client/services';

@Component({
	selector: 'cmi-viaduc-newusererror-page',
	templateUrl: 'errorNewUserPage.component.html'
})
export class ErrorNewUserPageComponent implements OnInit {

	public error: ErrorInfo;

	constructor(private _txt: TranslationService, private _route: ActivatedRoute, private _sessionStorage: SessionStorageService) {
	}

	public ngOnInit() {
		const errInfo = this.error = (this._route.snapshot.data['error'] || <ErrorInfo>{});
		if (_util.isEmpty(errInfo.title)) {
			errInfo.title = this._txt.get(
				'errors.newUserErrorTitle',
				'Zuerst im Public-Client des Online-Zugangs anmelden'
			);
		}
		if (_util.isEmpty(errInfo.message)) {
			errInfo.message = this._txt.get(
				'errors.newUserErrorMessage',
				'Um sich im Management-Client anmelden zu können, müssen Sie in der Benutzerverwaltung des Online-Zugangs ein Profil haben. ' +
				'Bitte melden Sie sich hierzu zuerst im <a href="' + this._sessionStorage.getItem('pcurl') + '">Public-Client des Online-Zugangs</a> an. ' +
				'Anschliessend können Sie sich im Management-Client ebenfalls anmelden.'
			);
		}
	}
}
