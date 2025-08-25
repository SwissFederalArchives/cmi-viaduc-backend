import {AblieferndeStellePageComponent, TokenDetailPageComponent, TokenListPageComponent} from './components';
import {AblieferndeStelleDetailPageComponent} from './components/ablieferndeStelleDetailPage/ablieferndeStelleDetailPage.component';
import {ApplicationFeatureGuard} from '../client';
import {ApplicationFeatureEnum, CanDeactivateGuard} from '@cmi/viaduc-web-core';

export const ROUTES: any = [
	{
		path: 'token',
		component: TokenListPageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.BehoerdenzugriffAccessTokensEinsehen] },
	},
	{
		path: 'zustaendigestellen',
		component: AblieferndeStellePageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.BehoerdenzugriffZustaendigeStellenEinsehen] },
	},
	{
		path: 'tokendetail',
		component: TokenDetailPageComponent,
		canActivate: [ApplicationFeatureGuard],
		canDeactivate: [CanDeactivateGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.BehoerdenzugriffAccessTokensBearbeiten] },
	},
	{
		path: 'tokendetail/:id',
		component: TokenDetailPageComponent,
		canActivate: [ApplicationFeatureGuard],
		canDeactivate: [CanDeactivateGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.BehoerdenzugriffAccessTokensBearbeiten] },
	},
	{
		path: 'zustaendigestelledetail',
		component: AblieferndeStelleDetailPageComponent,
		canActivate: [ApplicationFeatureGuard],
		canDeactivate: [CanDeactivateGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.BehoerdenzugriffZustaendigeStellenBearbeiten] },
	},
	{
		path: 'zustaendigestelledetail/:id',
		component: AblieferndeStelleDetailPageComponent,
		canActivate: [ApplicationFeatureGuard],
		canDeactivate: [CanDeactivateGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.BehoerdenzugriffZustaendigeStellenBearbeiten] },
	}
];
