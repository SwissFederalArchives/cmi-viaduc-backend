import {NgModule} from '@angular/core';
import {ALL_COMPONENTS} from './components/_all';
import {ROUTES} from './routes';
import {RouterModule} from '@angular/router';
import {SharedModule} from '../shared';
import {ALL_SERVICES} from './services/_all';
import {CommonModule} from '@angular/common';
import {FlatpickrModule} from 'angularx-flatpickr';

@NgModule({
	imports: [
		SharedModule,
		RouterModule.forChild(ROUTES),
		/* eslint-disable */
		FlatpickrModule.forRoot({
			locale: "de",
			prevArrow: "<svg version='1.1\' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 17 17'><g></g><path d='M5.207 8.471l7.146 7.147-0.707 0.707-7.853-7.854 7.854-7.853 0.707 0.707-7.147 7.146z'></path></svg>",
			nextArrow: "<svg version='1.1\' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 17 17'><g></g><path d='M13.207 8.472l-7.854 7.854-0.707-0.707 7.146-7.146-7.146-7.148 0.707-0.707 7.854 7.854z'></path></svg>"
		}),
		CommonModule
	],
	providers: [ ... ALL_SERVICES ],
	exports: [ ...ALL_COMPONENTS ],
	declarations: [ ...ALL_COMPONENTS ]
})
export class CollectionModule {
}
