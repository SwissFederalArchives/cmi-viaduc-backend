import {NgModule} from '@angular/core';
import '@mescius/wijmo.touch';
import * as wjcCore from '@mescius/wijmo';
import * as wjcGrid from '@mescius/wijmo.angular2.grid';
import { WjGridModule } from '@mescius/wijmo.angular2.grid';
import { WjGridGrouppanelModule } from '@mescius/wijmo.angular2.grid.grouppanel';
import { WjGridFilterModule } from '@mescius/wijmo.angular2.grid.filter';
import { WjGridSearchModule } from '@mescius/wijmo.angular2.grid.search';
import { WjInputModule, WjAutoComplete } from '@mescius/wijmo.angular2.input';
import {WjCoreModule} from '@mescius/wijmo.angular2.core';
import {ALL_SERVICES} from './services/_all';
import {ALL_COMPONENTS} from './components/_all';

import {CommonModule} from '@angular/common';
import {WIJMO_LICENSEKEY} from './wijmo.licensekey';

@NgModule({
	declarations: [ALL_COMPONENTS],
	imports: [
		CommonModule,
		WjInputModule,
		WjGridFilterModule,
		WjGridModule,
		WjGridSearchModule
	],
	exports: [
		WjInputModule,
		WjCoreModule,
		WjGridModule,
		WjGridFilterModule,
		WjGridGrouppanelModule,
		WjAutoComplete,
		WjGridFilterModule,
		wjcGrid.WjFlexGridCellTemplate,
		...ALL_COMPONENTS
	],
	providers: [
		...ALL_SERVICES
	]
})

export class WijmoModule {
	constructor() {
		wjcCore.setLicenseKey(WIJMO_LICENSEKEY);
	}
}
