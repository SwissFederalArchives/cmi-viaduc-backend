import {ComponentFixture, TestBed, waitForAsync} from '@angular/core/testing';
import {CollectionListItemDto, UiService, ClientContext} from '@cmi/viaduc-web-core'; // ClientContext Import hinzugefügt
import { provideRouter, Router} from '@angular/router';
import {CollectionService} from '../../../modules/client/services/collection.service';
import {LocalizeLinkPipe, UrlService} from '../../../modules/client';
import {Observable, of} from 'rxjs';
import {CollectionOverviewComponent} from './collection-overview.component';
import moment from 'moment';
import {By} from '@angular/platform-browser';
import {NO_ERRORS_SCHEMA, Pipe, PipeTransform} from '@angular/core';
import {ToastrService} from "ngx-toastr";

@Pipe({
	name: 'translate'
})
class MockTranslatePipe implements PipeTransform {
	transform(value: string): string {
		return value;
	}
}

describe('auto generate CollectionOverviewComponent', () => {
	// 1. Variablen-Deklarationen (nur einmal zentral definiert)
	let fixture: ComponentFixture<CollectionOverviewComponent>;
	let sut: CollectionOverviewComponent;

	const toastrService = <ToastrService>{};
	const uiService = new UiService(toastrService);

	const collectionItems: CollectionListItemDto[] = [
		CollectionListItemDto.fromJS({
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
		}),
		CollectionListItemDto.fromJS({
			collectionId: 32,
			title: ' Titel Blau',
			validFrom: moment(Date.now()).toDate(),
			validTo: moment(Date.now()).toDate(),
			createdOn: moment(Date.now()).toDate(),
			modifiedOn: moment(Date.now()).toDate(),
			collectionTypeId: 0,
			descriptionShort: 'Short kurz',
			description: 'Test Lang',
			imageMimeType: 'png',
			link: 'www.yahoo.de'
		}),
		CollectionListItemDto.fromJS({
			collectionId: 13,
			title: '3 Titel',
			validFrom: moment(Date.now()).toDate(),
			validTo: moment(Date.now()).toDate(),
			createdOn: moment(Date.now()).toDate(),
			modifiedOn: moment(Date.now()).toDate(),
			collectionTypeId: 0,
			descriptionShort: 'Short short',
			description: 'Test kind',
			imageMimeType: 'jpg',
			link: 'www.bing.de'
		})
	];

	const collectionService = <CollectionService>{
		getActiveCollections(_parentId: number | null): Observable<CollectionListItemDto[] | null> {
			return of(collectionItems);
		},
		getSizedImageURL(collectionId: number): string {
			return '/api/Collections/GetSizedImage/' + collectionId + '?mimeType=image/png&width=400&height=282';
		},
		getImageURL(collectionId: number): string {
			return '/api/Collections/GetImage/' + collectionId + '?usePrecalculatedThumbnail=false';
		}
	};

	const localizeLinkPipe = <LocalizeLinkPipe>{
		transform(_value: any, ..._args: any[]): string {
			return 'any';
		}
	};

	const urlService = <UrlService>{
		getHomeUrl(): string {
			return 'suche/einfach';
		},
		localizeUrl(_lang: string, url: string): string {
			return url;
		}
	};

	const router = <Router>{};

	// Mock-Konstrukt für den ClientContext
	const clientContextMock = {
		language: () => 'de',
		authenticated: true
	};

	beforeEach(waitForAsync(async() => {
		await TestBed.configureTestingModule({
			imports: [
				MockTranslatePipe // Verhindert NG0302 Fehler
			],
			providers: [
				provideRouter([]), // Verhindert den NG0203 duplicate forRoot guard Fehler restlos
				{provide: CollectionService, useValue: collectionService},
				{provide: Router, useValue: router},
				{provide: UrlService, useValue: urlService},
				{provide: UiService, useValue: uiService},
				{provide: LocalizeLinkPipe, useValue: localizeLinkPipe},
				// FIX: Beseitigt den NG0201 'No provider found for _ClientContext' Fehler
				{provide: ClientContext, useValue: clientContextMock}
			],
			declarations: [
				LocalizeLinkPipe,
				CollectionOverviewComponent
			],
			schemas: [NO_ERRORS_SCHEMA]
		}).compileComponents();

		fixture = TestBed.createComponent(CollectionOverviewComponent);
		sut = fixture.componentInstance;
		sut.parentId = 1;

		fixture.detectChanges(); // Startet den Lifecycle (inkl. ngOnInit)
		await fixture.whenStable();
	}));

	// 3. Unittests
	it('should create an instance', () => {
		expect(sut).toBeTruthy();
	});

	it('should create Data', () => {
		expect(sut.collections).toBeTruthy();
		expect(sut.collections.length).toBe(3);
	});

	it('should the imageminetype was set image appears', async () => {
		await fixture.whenStable().then(() => {
			fixture.detectChanges();
			const elementArray = fixture.debugElement.queryAll(By.css('.card-img-top'));
			expect(elementArray.length).toBeGreaterThan(0);
		});
	});
});
