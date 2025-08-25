import {Injectable} from '@angular/core';
import {ToastrService} from 'ngx-toastr';
import {UiService} from '@cmi/viaduc-web-core';

@Injectable()
export class UiServiceMC extends UiService{
	constructor(protected _toastr: ToastrService) {
		super(_toastr);
	}

	public detectInsideClick(event: any, element: any): boolean {
		let target = event.target;
		let inside = false;
		do {
			if (target === element) {
				inside = true;
			}
			target = target.parentNode;
		} while (target && !inside);
		return inside;
	}

	public showSuccess(message: string, title: string = null) {
		this._toastr.success(message, title);
	}

	public showWarning(message: string, title: string = null, options?: any) {
		this._toastr.warning(message, title, options);
	}

	public showInfo(message: string, title: string = null) {
		this._toastr.info(message, title);
	}

	public showError(message: string, title: any = null) {
		this._toastr.error(message, title, { disableTimeOut: true, closeButton: true});
	}

	public showHtmlInNewTab(html: string, title: string) {
		try {
			const wnd = window.open(`about:blank`, '_blank');
			if (!wnd || wnd.closed || typeof wnd.closed === 'undefined') {
				this._toastr.warning('Ihr Browser hat das neu geöffnete Fenster blockiert. Bitte erlauben Sie das Öffnen von Popups in Ihrem Browser (für diese Applikation) und versuchen Sie es erneut.', title, {
					disableTimeOut: true
				});
				return;
			}
			wnd.document.title = title;
			wnd.document.write(html);

			// force EDGE + IE to show correct title
			if (/MSIE|Trident|Edge/.test(navigator.userAgent)) {
				wnd.document.location.reload();
			}

		} catch (e) {
			this.showError('Beim Öffnen des neuen Fensters ist ein Fehler aufgetreten.');
			console.error(e);
		}
	}
}
