export class ErrorMessage {
	constructor(
		public forControl: string,
		public forValidator: string,
		public text: string
	) { }
}

export const CollectionDetailPageErrorMessages = [
	new ErrorMessage('title', 'required', 'Es muss ein Titel angegeben werden.'),
	new ErrorMessage('title', 'maxlength', 'Text darf nicht länger als 255 Zeichen sein.'),
	new ErrorMessage('validFrom', 'required', 'Es muss ein Datum angegeben werden.'),
	new ErrorMessage('validFrom', 'validFromDateValueValidator', 'Das Datum "Gültig von" muss vor dem Datum "Gültig bis" liegen.'),
	new ErrorMessage('validTo', 'validToDateValueValidator', 'Das Datum "Gültig von" muss vor dem Datum "Gültig bis" liegen.'),
	new ErrorMessage('validTo', 'required', 'Es muss ein Datum angegeben werden.'),
	new ErrorMessage('sortOrder', 'min', 'Der Wert darf nicht kleiner als 0 sein.'),
	new ErrorMessage('sortOrder', 'max', 'Der Wert darf nicht grösser als 1 Mio. sein'),
	new ErrorMessage('descriptionShort', 'required', 'Es muss eine Kurzbeschreibung angegeben werden.'),
	new ErrorMessage('descriptionShort', 'maxlength', 'Die Kurzbeschreibung darf nicht länger als 512 Zeichen sein.'),
	new ErrorMessage('description', 'required', 'Es muss eine Beschreibung angegeben werden.'),
	new ErrorMessage('imageAltText', 'maxlength', 'Text darf nicht länger als 255 Zeichen sein.'),
	new ErrorMessage('link', 'minlength', 'Der Link muss mindestens 3 Zeichen umfassen.'),
	new ErrorMessage('link', 'maxlength', 'Der Link darf nicht länger als 4000 Zeichen sein.'),
	new ErrorMessage('link', 'required', 'Es muss ein Link angegeben werden.'),
	new ErrorMessage('language', 'required', 'Es muss eine Sprache gewählt werden.'),
];
