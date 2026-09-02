import {Directive, ElementRef, Input} from '@angular/core';

@Directive({
    selector: '[autoFocus]',
    standalone: false
})
export class AutoFocusDirective {
	private _autoFocus: boolean;

	constructor(private _elementRef: ElementRef) {
	}

	public ngAfterViewInit() {
		if (this._autoFocus || typeof this._autoFocus === 'undefined') {
			this._elementRef.nativeElement.focus();
		}
	}

	@Input()
	set autoFocus(condition: boolean) {
		this._autoFocus = condition !== false;
	}
}
