import {OrdersListPageComponent, OrdersDetailPageComponent} from './components';
import {OrdersEinsichtListPageComponent, OrdersEinsichtDetailPageComponent} from './components';
import {DigipoolListPageComponent} from './components';
import {ApplicationFeatureGuard} from '../client';
import {ApplicationFeatureEnum, CanDeactivateGuard} from '@cmi/viaduc-web-core';

export const ROUTES: any = [
	{
		path: 'auftraege',
		component: OrdersListPageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AuftragsuebersichtAuftraegeView] },
	},
	{
		path: 'auftraege/:id',
		component: OrdersDetailPageComponent,
		canActivate: [ApplicationFeatureGuard],
		canDeactivate: [CanDeactivateGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AuftragsuebersichtAuftraegeView] },
	},
	{
		path: 'einsichtsgesuche',
		component: OrdersEinsichtListPageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheView] },
	},
	{
		path: 'einsichtsgesuche/:id',
		component: OrdersEinsichtDetailPageComponent,
		canActivate: [ApplicationFeatureGuard],
		canDeactivate: [CanDeactivateGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AuftragsuebersichtEinsichtsgesucheView] },
	},
	{
		path: 'digitalisierungsauftraege',
		component: DigipoolListPageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AuftragsuebersichtDigipoolView] }
	}
];
