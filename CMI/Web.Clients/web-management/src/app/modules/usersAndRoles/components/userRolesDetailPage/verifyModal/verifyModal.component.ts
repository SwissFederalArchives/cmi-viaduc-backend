import {Component, EventEmitter, Input, Output} from '@angular/core';
import {TranslationService} from '@cmi/viaduc-web-core';

@Component({
	selector: 'cmi-viaduc-verify-modal',
	templateUrl: 'verifyModal.component.html',
	styleUrls: ['./verifyModal.component.less']
})
export class VerifyModalComponent {
	@Output()
	public onSubmitted: EventEmitter<boolean> = new EventEmitter<boolean>();
	@Output()
	public openChange: EventEmitter<boolean> = new EventEmitter<boolean>();
	private _open: boolean;

	public get open(): boolean {
		return this._open;
	}
	@Input()
	public set open(val: boolean) {
		this._open = val;
		this.openChange.emit(val);
	}

	public constructor(private _txt: TranslationService) {
	}

	public cancel() {
		this.onSubmitted.emit(false);
		this.open = false;
	}

	public ok() {
		this.onSubmitted.emit(true);
		this.open = false;
	}

	public getTitle(): string {
		return this._txt.translate('DDS Zugang ändern?', 'verifyModal.modalTitle');
	}
}
