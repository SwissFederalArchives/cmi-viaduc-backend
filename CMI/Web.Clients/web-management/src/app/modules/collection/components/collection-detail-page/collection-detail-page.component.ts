import {Component, OnInit} from '@angular/core';
import {CollectionDto, ComponentCanDeactivate, TranslationService} from '@cmi/viaduc-web-core';
import {FormBuilder, FormGroup, ValidatorFn, Validators} from '@angular/forms';
import {ActivatedRoute, ParamMap, Router} from '@angular/router';
import {combineLatest, Observable, of} from 'rxjs';
import {switchMap} from 'rxjs/operators';
import {CollectionService} from '../../services';
import * as moment from 'moment';
import {formatDate} from '@angular/common';
import {ErrorService, UrlService} from '../../../shared';
import {CollectionDetailPageErrorMessages} from './collection-detail-page-error-messages';
import {ToastrService} from 'ngx-toastr';
import flatpickr from 'flatpickr';

import {FlatPickrOutputOptions} from 'angularx-flatpickr/lib/flatpickr.directive';
import {German} from 'flatpickr/dist/l10n/de';

@Component({
	selector: 'cmi-collection-detail-page',
	templateUrl: './collection-detail-page.component.html',
	styleUrls: ['./collection-detail-page.component.less']
})
export class CollectionDetailPageComponent extends ComponentCanDeactivate implements OnInit {
	public errors: { [key: string]: string } = {};
	public detailItem: CollectionDto;
	public myForm: FormGroup;
	public crumbs: any;
	public collectionTypes: Array<any> = [{collectionTypeId: 0, name: 'Sammlung'}, {collectionTypeId: 1, name: 'Themenblock'}];
	public languages: Array<any> = [{name: 'de'}, {name: 'fr'}, {name: 'it'}, {name: 'en'}];
	public allowedParents: Observable<any[]>;
	private isNew: boolean;
	private id: any;
	public pictureToBig = false;
	public reader = new FileReader();

	constructor(private _collectionService: CollectionService,
				private formbuilder: FormBuilder,
				private _url: UrlService,
				private _err: ErrorService,
				private _route: ActivatedRoute,
				private _router: Router,
				private _toastr: ToastrService,
				private _txt: TranslationService) {
		super();
		flatpickr.localize(German);
	}

	public ngOnInit(): void {
		const collection$ = this._route.paramMap.pipe(switchMap((params: ParamMap) => {
			this.id = params.get('id');
			this.allowedParents = this._collectionService.getAllowedParents(this.id === 'new' ? 0 : +this.id);
			if (this.id === 'new') {
				this.isNew = true;
				return of(CollectionDto.fromJS({
					collectionId: -1,
					validFrom: moment().startOf('day').toISOString(),
					validTo: moment().startOf('day').add(30, 'days').toDate().toISOString(),
					createdOn: moment().startOf('day').toISOString(),
					modifiedOn: moment().startOf('day').toISOString(),
					language: 'de',
					collectionTypeId: 0,
				}));
			}
			this.isNew = false;
			return this._collectionService.get(Number(this.id));
		}));

		combineLatest([collection$])
			.subscribe(([collection]) => {
				this.detailItem = collection;
				this.buildCrumbs();
				this.initForm();
			});
	}

	public canDeactivate(): boolean {
		return !this.myForm.dirty;
	}

	public promptForMessage(): false | 'question' | 'message' {
		return 'question';
	}

	public message(): string {
		return this._txt.get('hints.unsavedChanges', 'Sie haben ungespeicherte Änderungen. Wollen Sie die Seite tatsächlich verlassen?');
	}

	public loadImageFromFile(event) {
		const file = (event.target as HTMLInputElement).files[0];
		if (file && file.size < (1024 * 1024)) {
			this.pictureToBig = false;

			this.reader.onload = () => {
				const imageAsBase64 = this.reader.result as string;
				const parts = imageAsBase64.split(',');
				const imagePart = parts[1];
				const mimeType = parts[0].match(/[^:]\w+\/[\w-+\d.]+(?=;|,)/)[0];
				this.myForm.controls['image'].setValue(imagePart);
				this.myForm.controls['imageMimeType'].setValue(mimeType);
			};
			this.reader.readAsDataURL(file);
		} else {
			this.pictureToBig = true;
		}
	}

	public save() {
		let result: Observable<any>;
		const rawValue = this.myForm.getRawValue();
		const collection = CollectionDto.fromJS(rawValue);
		if (this.isNew) {
			result = this._collectionService.create(collection);
		} else {
			result = this._collectionService.update(this.id, collection);
		}
		result.subscribe((r) => {
				if (this.isNew) {
					this.myForm.reset();
					this._router.navigate([this._url.getNormalizedUrl('/collection/collection/' + r.collectionId)]);
				} else {
					this.reloadData(r);
				}
				this._toastr.success('Erfolgreich gespeichert');

			},
			(error) => {
				this._err.showError(error);
			});
	}

	public cancel() {
		this.gotoList();
	}

	public deleteImage() {
		this.pictureToBig = false;
		this.myForm.controls['image'].setValue(null);
		this.myForm.controls['imageMimeType'].setValue(null);
		this.myForm.controls['imageAltText'].setValue(null);
	}

	public collectionTypeChanged() {
		// Link field is required for "Sammlungen"
		const collectionTypeId = this.myForm.controls['collectionTypeId'].value;
		if (collectionTypeId === 0) {
			this.setValidator('link', [Validators.required, Validators.minLength(3), Validators.maxLength(4000)]);
		} else {
			this.setValidator('link', null);
		}
	}

	public getCollectionTypeName(collectionTypeId: number) {
		const item = this.collectionTypes.find(c => c.collectionTypeId === collectionTypeId);
		if (item) {
			return item.name;
		}
		return 'type not found';
	}

	private reloadData(detailItem: CollectionDto): void {
		// fetch latest data
		this._collectionService.get(detailItem.collectionId).subscribe(r => {
			this.detailItem = r;
			this.initForm();
		});
	}

	private gotoList(): void {
		this.myForm.reset();
		this._router.navigate([this._url.getNormalizedUrl('collection/collection')]);
	}

	private initForm() {
		this.myForm = this.formbuilder.group({
			collectionId: [this.detailItem.collectionId, Validators.required],
			language: [this.detailItem.language, Validators.required],
			title: [this.detailItem.title, [Validators.required, Validators.maxLength(255)]],
			collectionTypeId: [{value: this.detailItem.collectionTypeId, disabled: this.detailItem.childCollections?.length > 0}, [Validators.required, Validators.min(0), Validators.max(1)]],
			validFrom: [this.detailItem.validFrom, Validators.required],
			validTo: [this.detailItem.validTo, Validators.required],
			createdOn: [formatDate(this.detailItem.createdOn, 'yyyy-MM-ddTHH:mm', 'en')],
			createdBy: [this.detailItem.createdBy],
			modifiedOn: [formatDate(this.detailItem.modifiedOn ? this.detailItem.modifiedOn : new Date(), 'yyyy-MM-ddTHH:mm', 'en')],
			modifiedBy: [this.detailItem.modifiedBy],
			parentId: [this.detailItem.parentId],
			sortOrder: [this.detailItem.sortOrder, [Validators.min(0), Validators.max(1000000)]],
			descriptionShort: [this.detailItem.descriptionShort, [Validators.required, Validators.maxLength(512)]],
			description: [this.detailItem.description, [Validators.required]],
			image: [this.detailItem.image],
			imageMimeType: [this.detailItem.imageMimeType],
			collectionPath: [{value: this.detailItem.collectionPath, disabled: true}],
			thumbnail: [this.detailItem.thumbnail],
			imageAltText: [this.detailItem.imageAltText, [Validators.maxLength(255)]],
			link: [this.detailItem.link]
		});
		this.collectionTypeChanged();
		this.myForm.statusChanges.subscribe(() => this.updateErrorMessages());
	}

	private buildCrumbs(): void {
		const crumbs: any[] = this.crumbs = [];
		const collectionMenu = 'collection';
		const collection = 'collection';

		crumbs.push({iconClasses: 'glyphicon glyphicon-home', url: this._url.getHomeUrl()});
		crumbs.push({
			url: this._url.getNormalizedUrl(`/${collectionMenu}`),
			label: this._txt.get('breadcrumb.collectionMenu', 'Sammlungen')
		});
		crumbs.push({
			url: this._url.getNormalizedUrl(`/${collectionMenu}/${collection}`),
			label: this._txt.get('breadcrumb.collection', 'Virtuelle Sammlungen')
		});

		if (this.isNew) {
			crumbs.push({
				url: this._url.getNormalizedUrl(`/${collectionMenu}/${collection}/new`),
				label: this._txt.get('breadcrumb.create', 'Erfassen')
			});
		}

		if (!this.isNew) {
			crumbs.push({
				url: this._url.getNormalizedUrl(`/${collectionMenu}/${collection}/${this.detailItem.collectionId}`),
				label: this._txt.get('breadcrumb.edit', 'Bearbeiten')
			});
		}
	}

	private updateErrorMessages() {
		this.errors = {};
		for (const message of CollectionDetailPageErrorMessages) {
			const control = this.myForm.get(message.forControl);
			if (control &&
				control.dirty &&
				control.invalid &&
				control.errors[message.forValidator] &&
				!this.errors[message.forControl]) {
				this.errors[message.forControl] = message.text;
			}
		}
	}

	private setValidator(controlName: string, newValidator: ValidatorFn | ValidatorFn[] | null) {
		this.myForm.controls[controlName].clearValidators();
		this.myForm.controls[controlName].setValidators(newValidator);
		this.myForm.controls[controlName].updateValueAndValidity();
	}


	public dataPickerValueUpdateVon($event: FlatPickrOutputOptions) {
		if ($event.dateString === ''){
			this.detailItem.validFrom = null;
		} else {
			this.detailItem.validFrom = $event.selectedDates[0];
		}
	}


	public dataPickerValueUpdateBis($event: FlatPickrOutputOptions) {
		if ($event.dateString === ''){
			this.detailItem.validTo = null;
		} else {
			this.detailItem.validTo = $event.selectedDates[0];
		}
	}
}
