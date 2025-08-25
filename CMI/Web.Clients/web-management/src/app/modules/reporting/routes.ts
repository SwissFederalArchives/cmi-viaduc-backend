import {StatisticsReportsComponent} from './components/statisticsReports/statisticsReports.component';
import {ApplicationFeatureGuard} from '../client';
import {ApplicationFeatureEnum} from '@cmi/viaduc-web-core';
import {ConverterProgressComponent} from './components';

export const ROUTES: any = [
	{
		path: 'statisticsReports',
		_localize: {'fr': 'reports', 'it': 'reports', 'en': 'reports'},
		component: StatisticsReportsComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.ReportingStatisticsReportsEinsehen] }
	},
	{
		path: 'converterProgress',
		_localize: {'fr': 'Abbyy activity', 'it': 'Abbyy activity', 'en': 'Abbyy activity'},
		component: ConverterProgressComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.ReportingStatisticsConverterProgressEinsehen] }
	}
];
