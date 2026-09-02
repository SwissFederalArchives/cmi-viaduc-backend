import {Component, EventEmitter, Output} from '@angular/core';

@Component({
    template: '',
    standalone: false
})
export class CanDeactivateData {
	title: string;
	content: string;
	yesButtonText: string;
	noButtonText: string;
	@Output() result = new EventEmitter<boolean>();
	closeButtonText: string;
}
