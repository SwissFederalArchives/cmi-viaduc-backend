import {	SynchronizationAddPageComponent, SynchronizationMonitorPageComponent} from "./components";
import {ApplicationFeatureGuard} from "../client";
import { ApplicationFeatureEnum } from "@cmi/viaduc-web-core";

export const ROUTES: any = [
	{
		path: 'add',
		component: SynchronizationAddPageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.SynchronizationHinzufuegenEinsehen] }
	},
	{
		path: 'monitor',
		component: SynchronizationMonitorPageComponent,
		canActivate: [ApplicationFeatureGuard],
		data: { applicationFeature: [ApplicationFeatureEnum.SynchronizationUeberwachenEinsehen] }
	}
];
