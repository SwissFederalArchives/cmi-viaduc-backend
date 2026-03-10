import {NgModule} from '@angular/core';
import {ALL_COMPONENTS} from './components/_all';
import {ROUTES} from './routes';
import {RouterModule} from '@angular/router';
import {SharedModule} from '../shared';
import {CommonModule} from "@angular/common";
import {ALL_SERVICES} from "./services/_all";
import {FlatpickrDirective} from "angularx-flatpickr";

@NgModule({
    imports: [
        SharedModule,
        RouterModule.forChild(ROUTES),
        CommonModule,
        FlatpickrDirective
    ],
	providers: [ ... ALL_SERVICES ],
	exports: [ ...ALL_COMPONENTS ],
	declarations: [ ...ALL_COMPONENTS ]
})
export class SynchronizationModule {
}
