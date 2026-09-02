import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ManuelleKorrekturDetailPageComponent } from './manuelle-korrektur-detail-page.component';
import {
	ArchiveRecordContextItem,
	ManuelleKorrekturDetailItem,
	ManuelleKorrekturDto,
	TranslationService,
	ManuelleKorrekturStatusHistoryDto,
	ManuelleKorrekturFeldDto,
	ComponentCanDeactivate
} from '@cmi/viaduc-web-core';
import {DetailPagingService, ErrorService, UrlService} from '../../../../shared';
import {ToastrTestingModule} from '../../../../collection/components/collection-list-page/mocks';
import {ManuelleKorrekturenService} from '../../../services/manuelleKorrekturen-services';
import {FormBuilder, FormsModule, ReactiveFormsModule} from '@angular/forms';
import {ActivatedRoute, provideRouter} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import { of} from 'rxjs';
import moment from 'moment';
import {NO_ERRORS_SCHEMA, Pipe, PipeTransform} from '@angular/core'; // Pipe & PipeTransform hinzugefügt

// FIX 1: Lokale Mock-Pipe für das Template erstellen
@Pipe({
	name: 'translate'
})
class MockTranslatePipe implements PipeTransform {
	transform(value: string): string {
		return value;
	}
}

// FIX 2: Ivy-Host-Bindings der Basisklasse aushebeln
(ComponentCanDeactivate as any).ɵfac = () => ({});
(ComponentCanDeactivate as any).ɵdir = {
	...((ComponentCanDeactivate as any).ɵdir || {}),
	hostBindings: () => {}
};

describe('ManuelleKorrekturDetailPageComponent', () => {
	let sut: ManuelleKorrekturDetailPageComponent;
	let fixture: ComponentFixture<ManuelleKorrekturDetailPageComponent>;

	const translationService = {
		get: (key: string, defaultValue?: string) => defaultValue ?? key,
		translate: (text: string) => text
	};

	const toastrService = {
		success: jasmine.createSpy(),
		error: jasmine.createSpy(),
		info: jasmine.createSpy(),
		warning: jasmine.createSpy()
	};

	const urlService = {
		getNormalizedUrl: (url: string) => url,
		getHomeUrl: () => 'www.google.de',
		localizeUrl: (_lang: string, url: string) => url,
		getPublicClientBaseURL: () => 'www.google.de'
	};

	const activatedRoute = {
		paramMap: of({
			get: () => '123'
		})
	};

	const dps = {
		getCurrentIndex: () => -1
	};

	const errorService = {};

	const manuelleKorrekturenService = {
		getManuelleKorrektur: (_id: number) => {
			const manuelleKorrektur = ManuelleKorrekturDto.fromJS({
				titel: 'Test',
				manuelleKorrekturId: 123,
				veId: '45698',
				signatur: 'E0815',
				schutzfristende: moment(Date.now()).toDate(),
				erzeugtAm: moment(Date.now()).toDate(),
				erzeugtVon: 'Peter',
				geändertAm: moment(Date.now()).toDate(),
				geändertVon: 'Peter',
				anonymisierungsstatus: 0,
				kommentar: 'Keiner',
				hierachiestufe: 'Serie',
				aktenzeichen: 'xy0815',
				entstehungszeitraum: '1982-2089',
				zugänglichkeitGemässBGA: 'BAR',
				schutzfristverzeichnung: 'xyASTa',
				zuständigeStelle: 'AK'
			});

			manuelleKorrektur.manuelleKorrekturFelder = [
				ManuelleKorrekturFeldDto.fromJS({
					feldname: 'Darin',
					original: 'Haus im See',
					automatisch: 'Haus am See',
					manuell: ''
				}),
				ManuelleKorrekturFeldDto.fromJS({
					feldname: 'Titel',
					original: 'Haus am See',
					automatisch: 'Haus am See',
					manuell: 'Haus am See'
				}),
				ManuelleKorrekturFeldDto.fromJS({
					feldname: 'ZusatzkomponenteZac1',
					original: 'x',
					automatisch: 'x',
					manuell: ''
				}),
				ManuelleKorrekturFeldDto.fromJS({
					feldname: 'BemerkungZurVe',
					original: 'x',
					automatisch: 'x',
					manuell: ''
				}),
				ManuelleKorrekturFeldDto.fromJS({
					feldname: 'VerwandteVe',
					original: 'x',
					automatisch: 'x',
					manuell: ''
				})
			];

			const archiveRecordContextItems = [
				ArchiveRecordContextItem.fromJS({
					titel: 'Test archiveRecordContextItem',
					archiveRecordId: 1236,
					referenceCode: 'REF2020'
				})
			];

			const history = [
				ManuelleKorrekturStatusHistoryDto.fromJS({
					manuelleKorrekturStatusHistoryId: 1,
					manuelleKorrekturId: 1,
					anonymisierungsstatus: 1,
					erzeugtAm: moment(Date.now()).toDate(),
					erzeugtVon: 'PET'
				})
			];

			manuelleKorrektur.manuelleKorrekturStatusHistories = history;

			const detailItem = ManuelleKorrekturDetailItem.fromJS({
				manuelleKorrektur,
				archivplanKontext: archiveRecordContextItems,
				untergeordneteVEs: archiveRecordContextItems,
				verweiseVEs: archiveRecordContextItems
			});

			return of(detailItem);
		}
	};

	beforeEach(waitForAsync(async () => {
		await TestBed.configureTestingModule({
			imports: [
				ToastrTestingModule,
				FormsModule,
				ReactiveFormsModule,
				MockTranslatePipe // FIX 3: Als Standalone-Pipe hier in die Imports einfügen!
			],
			declarations: [ManuelleKorrekturDetailPageComponent],
			schemas: [NO_ERRORS_SCHEMA],
			providers: [
				provideRouter([]),
				{ provide: ManuelleKorrekturenService, useValue: manuelleKorrekturenService },
				{ provide: FormBuilder, useClass: FormBuilder },
				{ provide: UrlService, useValue: urlService },
				{ provide: DetailPagingService, useValue: dps },
				{ provide: ErrorService, useValue: errorService },
				{ provide: ActivatedRoute, useValue: activatedRoute },
				{ provide: ToastrService, useValue: toastrService },
				{ provide: TranslationService, useValue: translationService }
			]
		}).compileComponents();

		fixture = TestBed.createComponent(ManuelleKorrekturDetailPageComponent);
		sut = fixture.componentInstance;

		fixture.detectChanges();
		await fixture.whenStable();
	}));

	it('should create', async () => {
		await fixture.whenStable();
		fixture.detectChanges();

		expect(sut).toBeTruthy();
		expect(sut.myForm).toBeTruthy();
		expect(sut.myForm.controls['titel'].value).toBe('Test');

		expect(sut.sortedList[0]?.feldname).toBe('Titel');
		expect(sut.sortedList[1]?.feldname).toBe('Darin');
		expect(sut.sortedList[2]?.feldname).toBe('BemerkungZurVe');
		expect(sut.sortedList[3]?.feldname).toBe('VerwandteVe');
		expect(sut.sortedList[4]?.feldname).toBe('ZusatzkomponenteZac1');
	});
});
