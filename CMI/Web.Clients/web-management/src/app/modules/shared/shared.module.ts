import {ModuleWithProviders, NgModule} from '@angular/core';
import {CommonModule} from '@angular/common';
import {ALL_COMPONENTS} from './components/_all';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {ALL_SERVICES} from './services/_all';
import {ALL_PIPES} from './pipes/_all';
import {WijmoModule, CoreModule} from '@cmi/viaduc-web-core';
import {MarkdownModule} from 'ngx-markdown';

@NgModule({
	declarations: [
		...ALL_COMPONENTS,
		...ALL_PIPES
	],
	imports: [
		CoreModule,
		WijmoModule,
		CommonModule,
		FormsModule,
		MarkdownModule,
		ReactiveFormsModule
	],
	exports: [
		CoreModule,
		WijmoModule,
		FormsModule,
		MarkdownModule,
		ReactiveFormsModule,
		...ALL_COMPONENTS,
		...ALL_PIPES
	],
})
export class SharedModule {
	public static forRoot(): ModuleWithProviders<SharedModule> {
		return {
			ngModule: SharedModule,
			providers: [
				...ALL_SERVICES
			]
		};
	}
}
