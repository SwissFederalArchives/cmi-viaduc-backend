import {Component} from '@angular/core';
import {ClientContext, ApplicationFeatureEnum} from '@cmi/viaduc-web-core';
import {AuthorizationService} from '../../services';

@Component({
	selector: 'cmi-viaduc-left-navigation',
	templateUrl: 'leftNavigation.component.html',
	styleUrls: ['./leftNavigation.component.less']
})
export class LeftNavigationComponent{

	public get authenticated(): boolean {
		return this._context.authenticated;
	}

	public get authorized(): boolean {
		return this.authenticated && this._authorization.authorized();
	}

	constructor(private _context: ClientContext, private _authorization: AuthorizationService) {
	}

	public nyi(): void {
		window.alert('Diese Funktion wird zu einem späteren Zeitpunkt implementiert');
	}

	public canSeeOrders(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.AuftragsuebersichtAuftraegeView);
	}

	public canSeeOrdersEinsicht(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheView);
	}

	public canSeeDigipoolOrders(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.AuftragsuebersichtDigipoolView);
	}

	public canSeeBenutzerverwaltung(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRolleBenutzerverwaltungView) || this._authorization.hasRole(this._authorization.roles.APPO);
	}

	public canSeeRollenFunktionenMatrix(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BenutzerUndRollenRollenFunktionenMatrixEinsehen) || this._authorization.hasRole(this._authorization.roles.APPO);
	}

	public canSeeZustaendigeStellen(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BehoerdenzugriffZustaendigeStellenEinsehen);
	}

	public canSeeAccessTokens(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.BehoerdenzugriffAccessTokensEinsehen);
	}

	public canSeeEinstellungen(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.AdministrationEinstellungenEinsehen);
	}

	public canSeeNews(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.AdministrationNewsEinsehen);
	}

	public canSeeSystemstatus(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.AdministrationSystemstatusEinsehen);
	}

	public canSeeLoginformationen(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.AdministrationLoginformationenEinsehen);
	}

	public canSeeStatisticReportsViewReports(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.ReportingStatisticsReportsEinsehen);
	}

	public canSeeStatisticReportsViewConverterProgress(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.ReportingStatisticsConverterProgressEinsehen);
	}

	public canSeeCollection(): boolean {
		return this._authorization.hasApplicationFeature(ApplicationFeatureEnum.AdministrationSammlungenEinsehen);
	}

	public canSeeAnonymization(): boolean {
		return true;
	}
}
