import {Component, EventEmitter, Input, OnInit} from '@angular/core';
import {ParameterService} from '../../services';
import {Parameter} from '../../model/parameter';
import {AuthorizationService, UiServiceMC} from '../../../shared/services';
import {ApplicationFeatureEnum} from '@cmi/viaduc-web-core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';

@Component({
    selector: 'cmi-viaduc-parameter',
    templateUrl: './parameter.component.html',
    styleUrls: ['./parameter.component.less'],
    standalone: false
})
export class ParameterComponent implements OnInit {
	@Input()
	public parameter!: Parameter;
	@Input()
	public validationEvent: EventEmitter<void> = new EventEmitter<void>();
	@Input()
	public searchString!: string;

	public validationError!: boolean;
	public myForm!: FormGroup;

	get name(): string | undefined {
		return this.parameter.name.split('.').pop();
	}

	constructor (private _paramService: ParameterService, private _uiService: UiServiceMC, private _aut: AuthorizationService, private _formBuilder: FormBuilder) {
	}

	public ngOnInit() {
		this.validationEvent.subscribe(() => {
			if (this.myForm.controls['parameterValue'].value) {
				this.validationError = !this._validateString(this.myForm.controls['parameterValue'].value);
			} else {
				this.validationError = !this._isValid();
			}

		});

		this.buildForm();
	}

	public saveParameter() {
		this.validationError = !this._validateString(this.myForm.controls['parameterValue'].value);
		if (this.validationError === false) {
			if (this.parameter.type === 'textarea') {
				// historically textarea value property filters CRLF to LF only.
				// on a form post event, the CRLF are reintroduced, but we are not doing a post
				// so we do that manually
				this.parameter.value = this.myForm.controls['parameterValue'].value.replace(/\r?\n/g, '\r\n');
			} else {
				this.parameter.value = this.myForm.controls['parameterValue'].value;
			}
			this._paramService.saveParameter(this.parameter).then( error => {
				this.validationError = error !== null;
				if (error === null) {
					this.buildForm();
					this._uiService.showSuccess(`Parameter ${this.name} wurde erfolgreich gespeichert!`);
				} else {
					this._uiService.showError(`Parameter ${this.name} konnte nicht gespeichert werden!\nGrund: ${error}`);
				}
			});
		} else {
			this._uiService.showError(`Parameter ${this.name} konnte nicht gespeichert werden!\nGrund: ${this.parameter.errrorMessage}`);
		}
	}

	public cancelEdit() {
		this.myForm.patchValue({ parameterValue: this.parameter.value});
		this.myForm.markAsPristine();
	}

	private _isValid(): boolean {
		return this._validateString(this.parameter.value) === true ? true : false;
	}

	private _validateString(value: string): boolean {
		if (!value && this.parameter.mandatory === true) {
			return false;
		}
		if (this.parameter && this.parameter.regexValidation && value && typeof(value) === 'string') {
			const matches = value.match(this.parameter.regexValidation);
			return matches !== null && matches[0] !== null;
		} else {
			return true;
		}
	}

	public getErrorClass(): string {
		return this.validationError ? 'parameter-list row alert-danger' : 'parameter-list row';
	}

	public getInputClass(): string {
		if (this.parameter.value && this.searchString) {
			if (this.parameter.value.toString().toLowerCase().indexOf(this.searchString.toLowerCase()) !== -1) {
				return 'form-control highlighted';
			}
		}
		return 'form-control';
	}

	public get allowEinstellungenBearbeiten(): boolean {
		return this._aut.hasApplicationFeature(ApplicationFeatureEnum.AdministrationEinstellungenBearbeiten);
	}

	public get formControls(): any {
		return this.myForm['controls'];
	}

	private buildForm() {
		this.myForm = this._formBuilder.group({
			parameterValue: this.parameter.value,
			isDefault: this.parameter?.value?.toString() === this.parameter?.default?.toString()
		});

		if (this.parameter.regexValidation) {
			this.myForm.controls['parameterValue'].setValidators(Validators.pattern(this.parameter.regexValidation));
		}

		if (this.parameter.mandatory) {
			this.myForm.controls['parameterValue'].setValidators(Validators.required);
		}
	}

}
