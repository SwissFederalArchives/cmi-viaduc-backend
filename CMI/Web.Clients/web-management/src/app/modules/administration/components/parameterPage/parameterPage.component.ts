import {Component, EventEmitter, OnInit, QueryList, ViewChildren} from '@angular/core';
import {Parameter} from '../../model/parameter';
import {ParameterService} from '../../services';
import {ComponentCanDeactivate, TranslationService} from '@cmi/viaduc-web-core';
import {UrlService} from '../../../shared/services';
import {ParameterComponent} from '../parameter/parameter.component';

@Component({
	selector: 'cmi-viaduc-parameterPage',
	templateUrl: './parameterPage.component.html',
	styleUrls: ['./parameterPage.component.less']
})
export class ParameterPageComponent extends ComponentCanDeactivate implements OnInit {

	@ViewChildren(ParameterComponent)
	public paramComponents: QueryList<ParameterComponent>;

	public loading = true;
	public filteredParameters: Parameter[] = [];
	private _allParameters: Parameter[] = [];
	public validationEvent: EventEmitter<void> = new EventEmitter<void>();
	public searchString = '';
	public searchedStringUpToDate: boolean;
	public crumbs: any[] = [];

	constructor(private _params: ParameterService, private _txt: TranslationService, private _url: UrlService) {
		super();
		this.getAllParameters();
	}

	public filteredParameterForGroup(group: string): Parameter[] {
		return this.filteredParameters.filter(p => p.name.indexOf(group) !== -1);
	}

	get parameterGroupNames(): string[] {
		let names = this.filteredParameters.map(p => p.name.slice(0, p.name.lastIndexOf('.')));
		names = names.sort((a, b) => (a > b ? 1 : -1));
		return Array.from(new Set(names));
	}

	public async getAllParameters() {
		this._params.getAllParameters().then(response => {
			this._allParameters = response;
			this.filteredParameters = this._allParameters;
			this.loading = false;
		});
	}

	public ngOnInit(): void {
		this._buildCrumbs();
	}

	public onValueChanged(event: any) {
		this.searchString = event.target.value;
		if (this.searchString) {
			this.searchedStringUpToDate = false;
		}
	}

	public emitValidationEvent() {
		this.validationEvent.emit();
	}

	public searchParam() {
		this.filteredParameters = [];
		this.searchedStringUpToDate = true;
		if (this.searchString !== '') {
			this.filteredParameters = this._allParameters.filter((param) =>
				param.name.toLowerCase().indexOf(this.searchString.toLowerCase()) !== -1 || param.value && param.value.toString().toLowerCase().indexOf(this.searchString.toLowerCase()) !== -1
			);
		} else {
			this.filteredParameters = this._allParameters;
		}
	}

	public GetIdByName(name: string): string {
		const re = /\./gi;
		const result = name.replace(re, '_');

		return result;
	}

	public canDeactivate(): boolean {
		return !(this.paramComponents.filter(parameter => parameter.myForm.dirty).length > 0);
	}

	public promptForMessage(): false | 'question' | 'message' {
		return 'question';
	}

	public message(): string {
		return this._txt.get('hints.unsavedChanges', 'Sie haben ungespeicherte Änderungen. Wollen Sie die Seite tatsächlich verlassen?');
	}

	private _buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', _url: this._url.getHomeUrl()});
		crumbs.push({
			label: this._txt.get('breadcrumb.Administration', 'Administration')
		});
		crumbs.push({
			url: this._url.getNormalizedUrl('/administration/einstellungen'),
			label: this._txt.get('breadcrumb.einstellungen', 'Einstellungen')
		});
	}
}
