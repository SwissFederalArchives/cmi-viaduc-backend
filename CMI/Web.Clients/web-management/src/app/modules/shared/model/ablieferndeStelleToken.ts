import {AblieferndeStelle} from './ablieferndeStelle';

export class AblieferndeStelleToken {
	public tokenId: number;
	public token: string;
	public bezeichnung: string;
	public ablieferndeStelleList: AblieferndeStelle[];

	public displayName: string;

	public deleteMe: boolean;
	public ablieferndeStellenKuerzel: string;
	public ablieferndeStellenBezeichnung: string;
}
