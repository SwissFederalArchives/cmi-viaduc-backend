import {Component, OnInit, ViewChild} from '@angular/core';
import { CmiGridComponent, ComponentCanDeactivate, TranslationService, SyncAction } from '@cmi/viaduc-web-core';
import {UrlService} from "../../../shared";
import {SynchronizationService} from "../../services";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";

@Component({
  selector: 'cmi-synchronization-add-page',
  templateUrl: './synchronization-add-page.component.html',
  styleUrls: ['./synchronization-add-page.component.less']
})
export class SynchronizationAddPageComponent extends ComponentCanDeactivate implements OnInit {

	@ViewChild('flexGrid', { static: true })
	public flexGrid: CmiGridComponent;

	public crumbs: any[] = [];
	public loading: boolean;
	public syncActionItems: SyncAction[];

	public beforeSyncs: any = [{name:'1 Stunde', code:'1'}, {name:'2 Stunden', code:'2'}, {name:'12 Stunden', code:'3'}, {name:'1 Tag', code:'4'}];
	public myForm: FormGroup;
	public myForm2: FormGroup;
	public actions: any = [{name:'Update', code:'1'}, {name:'Delete', code:'2'}];

	constructor(public _url: UrlService,
				private _sys: SynchronizationService,
				private _txt: TranslationService,
				private formBuilder: FormBuilder) {
		super();
	}

	public ngOnInit(): void {
		this.buildCrumbs();
		this.loading = true;
		this._sys.getSyncData(1).subscribe(r => {
			this.syncActionItems = r;
			this.loading = false;
			this.initForm();
			if (this.syncActionItems?.length > 0 && this.flexGrid) {
				this.flexGrid.itemsSource = this.syncActionItems;
				this.flexGrid.refresh();
			}
		});
	}
	public updateTable() {
		this._sys.getSyncData(this.myForm2.controls.beforeSync.value).subscribe(r => {
			this.syncActionItems = r;
			this.flexGrid.itemsSource = this.syncActionItems;
			this.flexGrid.refresh();
		});
	}

	private initForm() {
		this.myForm = this.formBuilder.group({
			newIds: [null, [Validators.required, Validators.minLength(1)]],
			actions: {
				value: '1',
				disabled: false
			}
		});

		this.myForm2 = this.formBuilder.group({
			beforeSync: {
				value: '1',
				disabled: false
			}
		});
	}
	public canDeactivate(): boolean {
		return true;
    }
	public promptForMessage(): false | 'question' | 'message' {
		return false;
    }
	public message(): string {
       return 'Method not implemented.';
    }

	private buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({label: this._txt.get('breadcrumb.synchronization', 'Synchronisation')});
		crumbs.push({label: this._txt.get('breadcrumb.synchronizationadd', 'Hinzufügen')});
	}

	public addSyncOrders() {
		const ids = this.myForm.controls.newIds.value.split("\n");
		this._sys.batchAddSyncActions(ids, this.myForm.controls.actions.value).subscribe(() => {
			this.myForm.controls.newIds.setValue(null);
			this.updateTable();
	});
	}
}
