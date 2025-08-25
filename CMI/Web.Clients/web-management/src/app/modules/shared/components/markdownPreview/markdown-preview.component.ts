import { Component, Input } from '@angular/core';

@Component({
	selector: 'cmi-markdown-preview',
	templateUrl: 'markdown-preview.component.html',
	styleUrls: ['./markdown-preview.component.less']
})

export class MarkdownPreviewComponent {
	public hideComponent = true;
	public vorschauAnzeigen = false;

	@Input()
	public set markdownText(val: string) {
		this._markdownText = val;

		if (!val || val.trim().length === 0) {
			this.hideComponent = true;
		} else {
			this.hideComponent = false;
		}
	}
	public get markdownText() {
		return this._markdownText;
	}

	private _markdownText: string;
}
