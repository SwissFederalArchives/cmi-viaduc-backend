import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {ErrorInfo, TranslationService, Utilities as _util} from '@cmi/viaduc-web-core';

@Component({
	selector: 'cmi-viaduc-errorpermission-page',
	templateUrl: 'errorPermissionPage.component.html'
})
export class ErrorPermissionPageComponent implements OnInit {

	public error: ErrorInfo;

	constructor(private _txt: TranslationService, private _route: ActivatedRoute) {
	}

	public ngOnInit() {
		const errInfo = this.error = (this._route.snapshot.data['error'] || <ErrorInfo>{});
		if (_util.isEmpty(errInfo.title)) {
			errInfo.title = this._txt.get(
				'errors.errorPermissionTitle',
				'Keine Berechtigung'
			);
		}
		if (_util.isEmpty(errInfo.message)) {
			errInfo.message = this._txt.get(
				'errors.errorMessagePermission',
				'Sie sind nicht berechtigt auf die gewünschte Seite zuzugreifen. Wenden Sie sich bitte an den Systemadministrator.'
			);
		}
	}
}
