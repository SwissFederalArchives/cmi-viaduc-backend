import {AblieferndeStelleToken} from './ablieferndeStelleToken';
import {User} from './user';

export class AblieferndeStelle {
	public ablieferndeStelleId: number;
	public bezeichnung: string;
	public kuerzel: string;

	public applicationUserList: User[];
	public ablieferndeStelleTokenList: AblieferndeStelleToken[];
	public kontrollstellen: string[];

	public createdOn: Date;
	public createdBy: string;
	public modifiedOn: Date;
	public modifiedBy: string;
	public createModifyData = '';

	// Helpers
	public deleteMe: boolean;

	public applicationUserAsString: string;
	public ablieferndeStelleTokenAsString: string;
	public kontrollstellenAsString: string;
	public hasKontrollstelle: boolean;

	public tokenIdList: number[];
}
