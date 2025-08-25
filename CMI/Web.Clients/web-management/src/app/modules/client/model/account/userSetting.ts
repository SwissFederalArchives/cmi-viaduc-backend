import {Utilities as _util} from '@cmi/viaduc-web-core';

export class UserSetting {
	constructor(private _captionDefault: string,
				private _captionTranslation: string,
				public value: string,
				private _isReadOnly: boolean,
				private _isRequired: boolean,
				private _regExPattern: string,
				private _errorDefault: string,
				private _errorTranslation: string) {
	}

	public calculateInvalidRegex(): boolean {
		if (this._isNullOrEmpty(this._regExPattern)) {
			this.regexIsInvalid = false;
			return;
		}

		if (this._isNullOrEmpty(this.value)) {
			this.regexIsInvalid = false;
			return;
		}

		const regexp = new RegExp(this._regExPattern);

		this.regexIsInvalid = !regexp.test(this.value);
	}

	private _isNullOrEmpty(value: string): boolean {
		return _util.isEmpty(value);
	}

	public regexIsInvalid = false;

	get captionDefault(): string {
		return this._captionDefault;
	}

	get errorDefault(): string {
		return this._errorDefault;
	}

	get errorTranslation(): string {
		return this._errorTranslation;
	}

	get captionTranslation(): string {
		return this._captionTranslation;
	}

	get isReadOnly(): boolean {
		return this._isReadOnly;
	}

	get isRequired(): boolean {
		return this._isRequired;
	}

	get elementId(): string {
		return this.captionDefault;
	}

	get regExPattern(): string {
		return this._regExPattern;
	}

	get hasRegExPattern(): boolean {
		return !_util.isEmpty(this._regExPattern);
	}

	get hasValue(): boolean {
		return !_util.isEmpty(this.value);
	}

	get isInvalid(): boolean {
		if (this.hasRegExPattern) {
			return this.regexIsInvalid;
		}

		if (this.isRequired) {
			return !this.hasValue;
		}

		return false;
	}
}
