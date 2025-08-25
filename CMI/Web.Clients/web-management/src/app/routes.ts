import {Route, Routes} from '@angular/router';
import {AuthPageComponent} from './components/authPage/authPage.component';
import {HomePageComponent} from './components/homePage/homePage.component';
import {ErrorPageComponent} from './components/errorPage/errorPage.component';
import {NewsManagementPageComponent} from './components/communication/newsManagementPage/newsManagementPage.component';
import {Utilities as _util, Routing, CountriesResolver, ApplicationFeatureEnum, CanDeactivateGuard} from '@cmi/viaduc-web-core';
import {NewsManagementDetailsPageComponent} from './components/communication/newsManagementDetailPage/newsManagementDetailsPage.component';
import {
	DefaultContextGuard, DefaultRedirectGuard, PreloadedResolver, ApplicationFeatureGuard, AuthGuard // , DefaultAuthenticatedGuard,
} from './modules/client';
import {UserSettingsResolver} from './modules/client/routing/userSettingsResolver';
import {ErrorSmartcardPageComponent} from './components/errorSmartcardPage/errorSmartcardPage.component';
import {ErrorNewUserPageComponent} from './components/errorNewUserPage/errorNewUserPage.component';
import {ErrorPermissionPageComponent} from './components/errorPermission/errorPermissionPage.component';

// region Template for localizable routes.
const defaultRouteChildren: any = <Routes>[
	{
		path: '',
		redirectTo: 'home',
		pathMatch: 'full'
	},
	{
		path: 'home',
		_localize: {'fr': 'home', 'it': 'home', 'en': 'home'},
		component: HomePageComponent
	},
	{
		path: 'administration',
		loadChildren:  () => import('./modules/administration/administration.module').then(m => m.AdministrationModule),
		canActivateChild: [AuthGuard],
		resolve: {preloaded: PreloadedResolver},
	},
	{
		path: 'collection',
		loadChildren:  () => import('./modules/collection/collection.module').then(m => m.CollectionModule),
		canActivateChild: [AuthGuard],
		resolve: {preloaded: PreloadedResolver},
	},
	{
		path: 'anonymization',
		loadChildren:  () => import('./modules/anonymization/anonymization.module').then(m => m.AnonymizationModule),
		canActivateChild: [AuthGuard],
		resolve: {preloaded: PreloadedResolver},
	},
	{
		path: 'reporting',
		loadChildren:  () => import('./modules/reporting/reporting.module').then(m => m.ReportingModule),
		canActivateChild: [AuthGuard],
		resolve: {preloaded: PreloadedResolver},
	},
	{
		path: 'benutzerundrollen',
		_localize: {'fr': 'usersandroles', 'it': 'usersandroles', 'en': 'usersandroles'},
		loadChildren:  () => import('./modules/usersAndRoles/usersAndRoles.module').then(m => m.UsersAndRolesModule),
		canActivateChild: [AuthGuard],
		resolve: {preloaded: UserSettingsResolver, countries: CountriesResolver}
	},
	{
		path: 'auftragsuebersicht',
		loadChildren:  () => import('./modules/ordermanagement/ordermanagement.module').then(m => m.OrderManagementModule),
		canActivateChild: [AuthGuard],
		resolve: {user: UserSettingsResolver}
	},
	{
		path: 'behoerdenzugriff',
		loadChildren:  () => import('./modules/behoerdenZugriff/behoerdenZugriff.module').then(m => m.BehoerdenZugriffModule),
		canActivateChild: [AuthGuard],
		resolve: {preloaded: PreloadedResolver}
	},
	{
		path: 'kommunikation',
		_localize: {'fr': 'communication', 'it': 'communication', 'en': 'communication'},
		canActivateChild: [AuthGuard],
		children: [
			{
				path: '',
				redirectTo: 'news',
				pathMatch: 'full'
			},
			{
				path: 'news',
				children: [
					{
						path: '',
						_localize: {'fr': 'news', 'it': 'news', 'en': 'news'},
						component: NewsManagementPageComponent,
						canActivate: [ApplicationFeatureGuard],
						data: { applicationFeature: [ApplicationFeatureEnum.AdministrationNewsEinsehen] }
					},
					{
						path: 'erfassen',
						_localize: {'fr': 'create', 'it': 'create', 'en': 'create'},
						component: NewsManagementDetailsPageComponent,
						canActivate: [ApplicationFeatureGuard],
						canDeactivate: [CanDeactivateGuard],
						data: { applicationFeature: [ApplicationFeatureEnum.AdministrationNewsBearbeiten] }
					},
					{
						path: 'bearbeiten/:id',
						_localize: {'fr': 'edit', 'it': 'edit', 'en': 'edit'},
						component: NewsManagementDetailsPageComponent,
						canActivate: [ApplicationFeatureGuard],
						canDeactivate: [CanDeactivateGuard],
						data: { applicationFeature: [ApplicationFeatureEnum.AdministrationNewsBearbeiten] }
					},
				],
			},
		],
	},
	{
		path:'errorsmartcard',
		_localize: {'fr': 'fr-errorsmartcard', 'it': 'it-errorsmartcard', 'en': 'en-errorsmartcard'},
		component: ErrorSmartcardPageComponent
	},
	{
		path:'errornewuser',
		_localize: {'fr': 'fr-errornewuser', 'it': 'it-errornewuser', 'en': 'en-errornewuser'},
		component: ErrorNewUserPageComponent
	},
	{
		path:'errorpermission',
		_localize: {'fr': 'fr-errorpermission', 'it': 'it-errorpermission', 'en': 'en-errorpermission'},
		component: ErrorPermissionPageComponent
	}
];

// endregion

const langMatcher = /^(de|fr|it|en)$/;

export const ROUTES: any = [
	{
		path: 'auth/success',
		component: AuthPageComponent,
		canActivate: [DefaultContextGuard],
		resolve: {preloaded: PreloadedResolver}
	},
	{
		path: 'de',
		children: defaultRouteChildren, // keep here, so lazy modules are recognized by AOT build
		localizeWithDefault: true,
	},
	{
		path: 'fr',
		children: [],
	},
	{
		path: 'it',
		children: [],
	},
	{
		path: 'en',
		children: [],
	},
	{
		path: ':lang',
		children: [],
		canActivate: [DefaultRedirectGuard]
	},
	{
		path: '',
		pathMatch: 'full',
		children: [],
		canActivate: [DefaultRedirectGuard]
	},

	{
		path: '**',
		component: ErrorPageComponent
	}
];

function assertRoutes(routes: any[]) {
	Routing.assertRoutes(routes, {
		assertRoute: (route: Route) => {
			if (!_util.isEmpty(route.component) && _util.isEmpty(route.canActivate)) {
				route.canActivate = [DefaultContextGuard];
			}
			if (!_util.isEmpty(route.component)) {
				const resolve = route.resolve = _util.isObject(route.resolve) ? route.resolve : {};
				resolve['preloaded'] = PreloadedResolver;
			}
		}
	});

	Routing.collectLocalizations(Routing.supportedLanguages, routes);
}

let initialized = false;
export function initRoutes(routes: any[]) {
	if (initialized) {
		return;
	}
	initialized = true;

	assertRoutes(defaultRouteChildren);

	routes.forEach((route) => {
		if (route.localizeWithDefault || (_util.isArray(route.children) && _util.isEmpty(route.children))) {
			if (_util.isString(route.path) && route.path.match(langMatcher)) {
				route.children = Routing.localizeRoutes(route.path, defaultRouteChildren);
			} else {
				route.children = defaultRouteChildren;
			}
		}
	});
}
