export class ErrorMessage {
	constructor(
		public forControl: string,
		public forValidator: string,
		public key: string,
		public text: string
	) { }
}

export const UserRolesDetailPageErrorMessages = [
	new ErrorMessage('familyName', 'required', 'main.errorMandatoryField', 'Dieses Feld muss ausgefüllt werden.'),
	new ErrorMessage('familyName', 'maxlength', 'main.errorMaxLength200', 'Text darf nicht länger als 200 Zeichen sein.'),
	new ErrorMessage('firstName', 'required',  'main.errorMandatoryField', 'Dieses Feld muss ausgefüllt werden.'),
	new ErrorMessage('firstName', 'maxlength', 'main.errorMaxLength200', 'Text darf nicht länger als 200 Zeichen sein.'),
	new ErrorMessage('organization', 'maxlength', 'main.errorMaxLength200', 'Text darf nicht länger als 200 Zeichen sein.'),
	new ErrorMessage('organization', 'required', 'main.errorMandatoryField', 'Dieses Feld muss ausgefüllt werden.'),

	new ErrorMessage('birthday', 'invalidDate', 'main.errorDateFormat', 'Das Datum liegt nicht im Format dd.mm.jjjj vor.'),
	new ErrorMessage('downloadLimitDisabledUntil', 'invalidDate', 'main.errorDateFormat', 'Das Datum liegt nicht im Format dd.mm.jjjj vor.'),
	new ErrorMessage('downloadLimitDisabledUntil', 'invalidDateRange', 'main.errorDateRange', 'Das Datum muss innerhalb der nächsten 30 Tage liegen.'),
	new ErrorMessage('digitalisierungsbeschraenkungAufgehobenBis', 'invalidDate', 'main.errorDateFormat', 'Das Datum liegt nicht im Format dd.mm.jjjj vor.'),
	new ErrorMessage('digitalisierungsbeschraenkungAufgehobenBis', 'invalidDateRange', 'main.errorDateRange', 'Das Datum muss innerhalb der nächsten 30 Tage liegen.'),

	new ErrorMessage('street', 'required',  'main.errorMandatoryField', 'Dieses Feld muss ausgefüllt werden.'),
	new ErrorMessage('street', 'maxlength', 'main.errorMaxLength200', 'Text darf nicht länger als 200 Zeichen sein.'),
	new ErrorMessage('streetAttachment', 'maxlength', 'main.errorMaxLength200', 'Text darf nicht länger als 200 Zeichen sein.'),
	new ErrorMessage('zipCode', 'maxlength', 'main.errorMaxLength200', 'Text darf nicht länger als 200 Zeichen sein.'),
	new ErrorMessage('zipCode', 'required',  'main.errorMandatoryField', 'Dieses Feld muss ausgefüllt werden.'),
	new ErrorMessage('town', 'required',  'main.errorMandatoryField', 'Dieses Feld muss ausgefüllt werden.'),
	new ErrorMessage('town', 'maxlength', 'main.errorMaxLength200', 'Text darf nicht länger als 200 Zeichen sein.'),
	new ErrorMessage('countryCode', 'required',  'main.errorMandatoryField', 'Dieses Feld muss ausgefüllt werden.'),

	new ErrorMessage('mobileNumber', 'pattern', 'main.errorPhone', 'Telefonnummern können mit einem "+" beginnen sowie Zahlen, Leerzeichen und "-" ' +
		'enthalten und müssen zwischen 6 und 20 Zeichen lang sein'),
	new ErrorMessage('phoneNumber', 'pattern', 'main.errorPhone', 'Telefonnummern können mit einem "+" beginnen sowie Zahlen, Leerzeichen und "-" ' +
		'enthalten und müssen zwischen 6 und 20 Zeichen lang sein'),
	new ErrorMessage('emailAddress', 'email', 'main.errorEmailFormat', 'Dieses Feld muss ausgefüllt werden und muss eine gültige E-Mail sein'),
	new ErrorMessage('emailAddress', 'maxlength', 'main.errorMaxLength200', 'Text darf nicht länger als 200 Zeichen sein.'),
	new ErrorMessage('emailAddress', 'required',  'main.errorMandatoryField', 'Dieses Feld muss ausgefüllt werden und muss eine gültige E-Mail sein'),
];
