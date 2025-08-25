import {NgModule} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';
import {Router, RouterModule} from '@angular/router';
import {RootComponent} from './components/root/root.component';
import {ALL_COMPONENTS} from './components/_all';
import {ROUTES, initRoutes} from './routes';
import {SharedModule} from './modules/shared';
import {ClientModule} from './modules/client';
import {ClientContext, CoreModule} from '@cmi/viaduc-web-core';
import {ToastrModule} from 'ngx-toastr';
import {AuthenticationService} from './modules/client/services';
import {MarkdownModule} from 'ngx-markdown';
import {FormsModule} from '@angular/forms';
import {HTTP_INTERCEPTORS} from '@angular/common/http';
import {AuthInterceptor} from './interceptors/authInterceptor';
import {FlatpickrModule} from 'angularx-flatpickr';

export const toastrOptions = {
	timeOut: 3000,
	positionClass: 'toast-top-center', // PVW-92
	preventDuplicates: true
};

initRoutes(ROUTES);

export function tryActivateExistingSession(authentication: AuthenticationService) {
	const x = () => authentication.activateSession();
	return x;
}

@NgModule({
	imports: [
		BrowserModule,
		FormsModule,
		CoreModule.forRoot(),
		SharedModule.forRoot(),
		ClientModule.forRoot(),
		RouterModule.forRoot(ROUTES, { useHash: true }),
		ToastrModule.forRoot(toastrOptions),
		MarkdownModule.forRoot(),
		/* eslint-disable */
		FlatpickrModule.forRoot({
			locale: "de",
			prevArrow: "<svg version='1.1\' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 17 17'><g></g><path d='M5.207 8.471l7.146 7.147-0.707 0.707-7.853-7.854 7.854-7.853 0.707 0.707-7.147 7.146z'></path></svg>",
			nextArrow: "<svg version='1.1\' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 17 17'><g></g><path d='M13.207 8.472l-7.854 7.854-0.707-0.707 7.146-7.146-7.146-7.148 0.707-0.707 7.854 7.854z'></path></svg>"
		})
	],
	providers: [
		{
		provide: HTTP_INTERCEPTORS,
		useFactory: function(auth: AuthenticationService, context: ClientContext) {
			return new AuthInterceptor(auth, context);
		},
		multi: true,
		deps: [AuthenticationService, ClientContext]
		}
		],
	bootstrap: [RootComponent],
	declarations: [
		...ALL_COMPONENTS
	]
})
export class AppModule {
	constructor(_router: Router) {
		_router.resetConfig(ROUTES);
	}
}
