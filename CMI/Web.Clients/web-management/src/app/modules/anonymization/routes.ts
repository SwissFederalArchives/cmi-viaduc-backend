import {ManuelleKorrekturListComponent} from './components/manuelleKorrekturen/manuelle-korrektur-list/manuelle-korrektur-list.component';
import {ApplicationFeatureGuard} from '../client';
import {ApplicationFeatureEnum, CanDeactivateGuard} from '@cmi/viaduc-web-core';
import {ManuelleKorrekturDetailPageComponent} from './components';

export const ROUTES: any = [
	{
		path: 'manuelleKorrekturen',
		component: ManuelleKorrekturListComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AnonymisierungManuelleKorrekturenEinsehen] }
	},
	{
		path: 'manuelleKorrekturen/:id',
		component: ManuelleKorrekturDetailPageComponent,
		canActivate: [ApplicationFeatureGuard],
		canDeactivate: [CanDeactivateGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.AnonymisierungManuelleKorrekturenBearbeiten] },
	}
];
