import {ModuleWithProviders, NgModule} from '@angular/core';
import {RouterModule} from '@angular/router';
import {HttpClientModule} from '@angular/common/http';
import {ALL_COMPONENTS} from './components/_all';
import {ALL_DIRECTIVES} from './directives/_all';
import {ALL_SERVICES} from './services/_all';
import {ALL_GUARDS, ALL_RESOLVERS} from './routing/_all';
import {BrowserAnimationsModule} from '@angular/platform-browser/animations';
import {AdminModel} from './model';
import {SharedModule} from '../shared';
import {FlatpickrModule} from 'angularx-flatpickr';

export function AdminModelFactory() {
	return new AdminModel();
}

@NgModule({
	declarations: [
		...ALL_COMPONENTS,
		...ALL_DIRECTIVES,
	],
	exports: [
		...ALL_COMPONENTS,
		...ALL_DIRECTIVES,
		RouterModule,
		BrowserAnimationsModule
	],
	imports: [
		SharedModule,
		RouterModule,
		HttpClientModule,
		BrowserAnimationsModule,
		/* eslint-disable */
		FlatpickrModule.forRoot({
			locale: "de",
			prevArrow: "<svg version='1.1\' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 17 17'><g></g><path d='M5.207 8.471l7.146 7.147-0.707 0.707-7.853-7.854 7.854-7.853 0.707 0.707-7.147 7.146z'></path></svg>",
			nextArrow: "<svg version='1.1\' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 17 17'><g></g><path d='M13.207 8.472l-7.854 7.854-0.707-0.707 7.146-7.146-7.146-7.148 0.707-0.707 7.854 7.854z'></path></svg>"
		})
	]
})

export class ClientModule {

	public static forRoot(): ModuleWithProviders<ClientModule> {
		return {
			ngModule: ClientModule,
			providers: [
				...ALL_SERVICES,
				...ALL_GUARDS,
				...ALL_RESOLVERS,
				{ provide: AdminModel, useFactory: AdminModelFactory }
			]
		};
	}
}
