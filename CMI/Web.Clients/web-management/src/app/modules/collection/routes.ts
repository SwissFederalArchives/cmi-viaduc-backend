import {ApplicationFeatureGuard} from '../client';
import {ApplicationFeatureEnum, CanDeactivateGuard} from '@cmi/viaduc-web-core';
import {CollectionDetailPageComponent, CollectionListPageComponent} from './components';

export const ROUTES: any = [
	{
		path: 'collection',
		component: CollectionListPageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AdministrationSammlungenEinsehen] }
	},
	{
		path: 'collection/:id',
		component: CollectionDetailPageComponent,
		canActivate: [ApplicationFeatureGuard],
		canDeactivate: [CanDeactivateGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AdministrationSammlungenBearbeiten] },
	}
];
