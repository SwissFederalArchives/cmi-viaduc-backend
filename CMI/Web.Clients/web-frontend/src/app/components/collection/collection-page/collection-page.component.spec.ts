import {ComponentFixture, TestBed, waitForAsync} from '@angular/core/testing';
import {CollectionDto, TranslationService, UiService} from '@cmi/viaduc-web-core';
import {ActivatedRoute, ParamMap, provideRouter, Router} from '@angular/router';
import {CollectionPageComponent} from './collection-page.component';
import {CollectionService} from '../../../modules/client/services/collection.service';
import {SeoService, UrlService} from '../../../modules/client';
import {Observable, of} from 'rxjs';
import {CollectionItemResult} from '../../../modules/client/model/collection/collectionItemResult';
import moment from 'moment';
import {By, Title} from '@angular/platform-browser';
import {MockUserSettingsParamMap, Mocks} from '../mocks';
import {NO_ERRORS_SCHEMA, Pipe, PipeTransform} from '@angular/core';
import {ToastPackage, ToastrService} from "ngx-toastr";

// FIX 1: Lokale Mock-Pipe für Template-Übersetzungen bereitstellen
@Pipe({
	name: 'translate'
})
class MockTranslatePipe implements PipeTransform {
	transform(value: string): string {
		return value;
	}
}

describe('auto generate CollectionPageComponent', () => {
	let fixture: ComponentFixture<CollectionPageComponent>;
	let sut: CollectionPageComponent;
	let uiService: UiService;

	const activatedRoute = <ActivatedRoute>{
		get paramMap(): Observable<ParamMap> {
			return of(new MockUserSettingsParamMap()).pipe();
		}
	};

	const router = <Router>{};
	const urlService = <UrlService>{
		getHomeUrl(): string {
			return 'suche/einfach';
		}
	};

	const _txt = <TranslationService>{
		get(_key: string, defaultValue?: string, ..._args: any[]): string {
			return defaultValue || '';
		},
		translate(text: string, _key?: string, ..._args: any[]): string {
			return text;
		}
	};

	// FIX: Nutze jasmine.createSpyObj um den Title-Service fehlerfrei zu faken
	let testTitleStore = '';
	const _title = jasmine.createSpyObj<Title>('Title', ['getTitle', 'setTitle']);
	_title.getTitle.and.callFake(() => testTitleStore);
	_title.setTitle.and.callFake((newTitle: string) => { testTitleStore = newTitle; });

	const toastrService = <ToastrService>{};
	uiService = new UiService(toastrService);

	const _seoService = new SeoService(_title, _txt);

	const collectionItemResult = CollectionItemResult.fromJS({
		item: CollectionDto.fromJS({
			collectionId: 3,
			title: 'Test Titel',
			validFrom: moment(Date.now()).toDate(),
			validTo: moment(Date.now()).toDate(),
			createdOn: moment(Date.now()).toDate(),
			modifiedOn: moment(Date.now()).toDate(),
			collectionTypeId: 0,
			descriptionShort: 'Short short',
			description: 'Test kind',
			imageMimeType: 'png',
			link: 'www.google.de'
		})
	});

	const collectionService = <CollectionService>{
		get(_id: number): Observable<CollectionItemResult | null> {
			return of(collectionItemResult);
		},
		getSizedImageURL(collectionId: number): string {
			return '/api/Collections/GetSizedImage/' + collectionId + '?mimeType=image/png&width=400&height=282';
		},
		getImageURL(collectionId: number): string {
			return '/api/Collections/GetImage/' + collectionId + '?usePrecalculatedThumbnail=false';
		}
	};

	beforeEach(waitForAsync(async() => {
		await TestBed.configureTestingModule({
			imports: [
				MockTranslatePipe // Ersetzt CoreModule.forRoot() -> Verhindert NG0203
			],
			providers: [
				provideRouter([]), // Ersetzt das fehlerhafte RouterTestingModule vollständig
				{provide: Router, useValue: router},
				{provide: UrlService, useValue: urlService},
				{provide: UiService, useValue: uiService},
				{provide: CollectionService, useValue: collectionService},
				{provide: TranslationService, useValue: _txt},
				{provide: ActivatedRoute, useValue: activatedRoute},
				{provide: SeoService, useValue: _seoService},
				{provide: ToastPackage, useClass: Mocks},
				{provide: ToastrService, useClass: ToastrService}
			],
			declarations: [
				CollectionPageComponent
			],
			schemas: [NO_ERRORS_SCHEMA]
		}).compileComponents();

		fixture = TestBed.createComponent(CollectionPageComponent);
		sut = fixture.componentInstance;

		fixture.detectChanges(); // Startet Lifecycle und verarbeitet ngOnInit automatisch
		await fixture.whenStable();
	}));

	it('should create an instance', () => {
		expect(sut).toBeTruthy();
	});

	it('should create Data', () => {
		expect(sut).toBeTruthy();
		expect(sut.detailItem).toBeTruthy();
		expect(sut.detailItem.collectionId).toBe(3);
		expect(sut.isValid).toBeTruthy();
	});

	it('should the title was completed with the collection name', () => {
		expect(sut).toBeTruthy();
		const title = _seoService.getTitle();
		expect(title === collectionItemResult.item.title + ' - ' + _txt.get('header.title', 'Online-Zugang zum Bundesarchiv')).toBeTruthy();
	});

	it('should the imageminetype was set image appears', () => {
		const imageContainer = fixture.debugElement.query(By.css('.image-container')).nativeElement;
		expect(imageContainer).toBeTruthy();
		fixture.componentInstance.detailItem.imageMimeType = undefined;
		fixture.detectChanges();
		expect(fixture.debugElement.query(By.css('.image-container'))).toBeFalsy();
	});

	it('should the link was set button appears', () => {
		fixture.autoDetectChanges(true);
		const button = fixture.debugElement.query(By.css('.link')).nativeElement;
		expect(button).toBeTruthy();
		fixture.componentInstance.detailItem.link = undefined;
		fixture.detectChanges();
		expect(fixture.debugElement.query(By.css('.link'))).toBeFalsy();
	});
});
