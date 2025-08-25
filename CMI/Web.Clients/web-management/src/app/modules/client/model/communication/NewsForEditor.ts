import {News} from './News';

export class NewsForEditor implements News {
	public id: string;
	public fromDate: string;
	public toDate: string;
	public de: string;
	public en: string;
	public fr: string;
	public it: string;
	public deHeader: string;
	public enHeader: string;
	public frHeader: string;
	public itHeader: string;

	public hasChanged: boolean;
	public editMe: boolean;
	public deleteMe: boolean;

	constructor(private _parent: News) {
		this.transferValues();
	}

	public transferValues(): void {
		this.id = this._parent.id;
		this.fromDate = this._parent.fromDate;
		this.toDate = this._parent.toDate;
		this.de = this._parent.de;
		this.en = this._parent.en;
		this.fr = this._parent.fr;
		this.it = this._parent.it;
		this.deHeader = this._parent.deHeader;
		this.enHeader = this._parent.enHeader;
		this.frHeader = this._parent.frHeader;
		this.itHeader = this._parent.itHeader;

		this.hasChanged = false;
		this.editMe = false;
		this.deleteMe = false;
	}
}
