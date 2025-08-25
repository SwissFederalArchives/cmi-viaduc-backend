import {MonitoringPageComponent, ParameterPageComponent, LogInformationenPageComponent} from './components';
import {ApplicationFeatureGuard} from '../client';
import {ApplicationFeatureEnum, CanDeactivateGuard} from '@cmi/viaduc-web-core';

export const ROUTES: any = [
	{
		path: 'einstellungen',
		component: ParameterPageComponent,
		canActivate: [ApplicationFeatureGuard],
		canDeactivate: [CanDeactivateGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AdministrationEinstellungenEinsehen] },
	},
	{
		path: 'logInformationen',
		component: LogInformationenPageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AdministrationLoginformationenEinsehen] },
	},
	{
		path:'systemstatus',
		component: MonitoringPageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AdministrationSystemstatusEinsehen] },
	}
];
