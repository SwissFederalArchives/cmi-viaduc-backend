import {Component, EventEmitter, Input, Output} from '@angular/core';
import {Utilities as _util} from '@cmi/viaduc-web-core';

@Component({
	selector: 'cmi-viaduc-barcode-modal',
	templateUrl: 'barCodeModal.component.html',
	styleUrls: ['./barCodeModal.component.less']
})
export class BarCodeModalComponent {

	@Output()
	public onSubmitted: EventEmitter<string[]> = new EventEmitter<string[]>();
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

	public barcodes: string;

	public expandTextarea(event: any) {
		if (event && event.target) {
			event.target.style.cssText = 'height:auto;';
			event.target.style.cssText = 'height:' + (event.target.scrollHeight + 15) + 'px';
		}
	}

	public cancel() {
		this.open = false;
	}

	private _distinctSplit (barcodes: string): string[] {
		const list: string[] = [];

		barcodes.split('\n').forEach(b => {
			const barcode = b.trim();
			if (!_util.isEmpty(barcode) && list.indexOf(barcode) < 0) {
				list.push(barcode);
			}
		});

		return list;
	}

	public ok() {
		if (this.barcodes) {
			this.onSubmitted.emit(this._distinctSplit(this.barcodes));
		} else {
			this.onSubmitted.emit(null);
		}
	}
}
