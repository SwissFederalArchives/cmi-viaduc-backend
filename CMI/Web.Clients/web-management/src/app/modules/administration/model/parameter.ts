export interface Parameter {
	name: string;
	value: any;
	type: string;
	mandatory: boolean;
	description: string;
	regexValidation: string;
	errrorMessage: string;
	default: any;
}
