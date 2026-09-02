import {Injectable} from '@angular/core';
import {ToastrService} from 'ngx-toastr';
import {HttpErrorResponse} from '@angular/common/http';
import {AuthorizationService} from './authorization.service';
import {ApplicationFeatureEnum} from '@cmi/viaduc-web-core';

@Injectable({
	providedIn: 'root'
})
export class ErrorService {

	constructor(private _toastr: ToastrService, private _auth: AuthorizationService) {
	}

	public showError(e: any, title = '') {
		title = title.trim().length === 0 ? 'Fehler' : title;

		if (e && (e instanceof HttpErrorResponse || e.status !== undefined)) {
			let msg = e.error?.exceptionMessage || e.message || 'Ein Netzwerkfehler ist aufgetreten.';

			const index = msg.indexOf('faulted:');
			if (index > 0) {
				msg = msg.substring(index + 'faulted:'.length);
			}
			this._toastr.error(msg, title, { disableTimeOut: true, closeButton: true });

			return;
		}
		if (e) {
			this._toastr.error(e.message || e, title, { disableTimeOut: true, closeButton: true});
			return;
		}

		this._toastr.error('Es ist ein unerwarteter Fehler aufgetreten. Bitte versuchen Sie es später erneut', 'Fehler', { disableTimeOut: true, closeButton: true});
	}

	public showOdataErrorIfNecessary(error: any) {
		if (!(error.request && error.request.status > 399 && error.request.status < 500)) {
			return;
		}

		error.cancel = true;
		let msg = 'Bitte überprüfen Sie die gesetzten Filter. Versuchen Sie die Filter zu reduzieren und grenzen Sie die Resultate anschliessend via Excel ein.';
		if (error.message) {
			msg += ` Fehlerdetail: ${error.message}`;
		}
		this._toastr.error(msg, 'Unzulässige Filterung', { disableTimeOut: true });
	}

	public verifyApplicationFeatureOrShowError(identifier:ApplicationFeatureEnum): boolean {
		if (!this._auth.hasApplicationFeature(identifier)) {
			this._toastr.error(`Um diese Aktion durchzuführen benötigen Sie das Feature '${ApplicationFeatureEnum[identifier]}'`, 'Fehlende Berechtigung', { tapToDismiss:true});
			return false;
		}
		return true;
	}

}
