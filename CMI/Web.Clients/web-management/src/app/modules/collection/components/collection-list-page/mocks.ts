import { Injectable, NgModule } from '@angular/core';
import {IndividualConfig, ToastPackage, ToastRef, ToastrModule, ToastrService} from 'ngx-toastr';
import {
	AblieferndeStelleSettings,
	CollectionSettings,
	DigipoolUserSettings,
	EinsichtsgesuchUserSettings,
	ManagementUserSettings, ManuelleKorrekturSettings,
	OrderUserSettings,
	UserListUserSettings
} from '../../../shared';
import {BrowserAnimationsModule} from '@angular/platform-browser/animations';
@Injectable()
class Mocks extends ToastPackage {
	constructor() {
		const toastConfig = { toastClass: 'custom-toast' };
		super(1, <IndividualConfig>toastConfig, 'test message', 'test title', 'show', new ToastRef(null));
	}
}

const toastPackage = <Mocks>{};
@NgModule(
	{
	providers: [
		{ provide: ToastPackage, useValue:  toastPackage},
		{ provide: ToastrService, useClass: ToastrService }
	],
	imports: [
		ToastrModule.forRoot(), BrowserAnimationsModule
	],
	exports: [
		ToastrModule
	]
})
export class ToastrTestingModule {
}

export class MockCollectionSettings implements CollectionSettings {
	public columns: any;

	constructor() {
		this.columns = ['Test', 'Test1'];
	}
}

export class MockUserSettings implements ManagementUserSettings {
	constructor() {
		this.collectionSettings = new MockCollectionSettings();
	}

	public ablieferndeStelleSettings: AblieferndeStelleSettings;
	public collectionSettings: CollectionSettings;
	public digipoolSettings: DigipoolUserSettings;
	public einsichtsGesuchSettings: EinsichtsgesuchUserSettings;
	public orderSettings: OrderUserSettings;
	public pagingSize: any;
	public selectedSortingField: any;
	public showInfoWhenEmptySearchResult: boolean;
	public userListSettings: UserListUserSettings;
	public manuelleKorrekturSettings: ManuelleKorrekturSettings;
}
