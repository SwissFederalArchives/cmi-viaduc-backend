import {ComponentFixture, TestBed, waitForAsync} from '@angular/core/testing';
import { ManuelleKorrekturDetailPageComponent } from './manuelle-korrektur-detail-page.component';
import {
	CoreModule,
	ArchiveRecordContextItem,
	ManuelleKorrekturDetailItem,
	ManuelleKorrekturDto,
	TranslationService,
	ManuelleKorrekturStatusHistoryDto,
	ManuelleKorrekturFeldDto
} from '@cmi/viaduc-web-core';
import {DetailPagingService, ErrorService, SharedModule, UrlService} from '../../../../shared';
import {ToastrTestingModule} from '../../../../collection/components/collection-list-page/mocks';
import {ManuelleKorrekturenService} from '../../../services/manuelleKorrekturen-services';
import {FormBuilder} from '@angular/forms';
import {ActivatedRoute, ParamMap, Router} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import {Observable, of} from 'rxjs';
import {MockUserSettingsParamMap} from '../../../../collection/components/collection-detail-page/mocks';
import * as moment from 'moment';
import {NO_ERRORS_SCHEMA} from '@angular/core';

describe('ManuelleKorrekturDetailPageComponent', () => {
	let sut: ManuelleKorrekturDetailPageComponent;
	let fixture: ComponentFixture<ManuelleKorrekturDetailPageComponent>;
	let translationService = <TranslationService>{
		get(key: string, defaultValue?: string, ...args): string {
			if (defaultValue) {
				return defaultValue;
			} else {
				return key;
			}
		},
		translate(text: string, key?: string, ...args): string {
			return text;
		}
	};
	let toastrService = <ToastrService>{};
	let urlService = <UrlService>{
		getNormalizedUrl(url: string): string {
			return url;
		},
		getHomeUrl(): string {
			return 'www.google.de';
		},
		localizeUrl(lang: string, url: string): string {
			return url;
		}
	};
	let activatedRoute = <ActivatedRoute>{
		get paramMap(): Observable<ParamMap> {
			return of ( new MockUserSettingsParamMap()).pipe();
		}
	};
	let dps = <DetailPagingService>{
		getCurrentIndex(): number {
			return -1;
		}
	};
	let errorService = <ErrorService>{};
	let manuelleKorrekturenService = <ManuelleKorrekturenService>{
		getManuelleKorrektur(id: number): Observable<ManuelleKorrekturDetailItem | null> {
			let manuelleKorrektur = ManuelleKorrekturDto.fromJS({
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
			manuelleKorrektur.manuelleKorrekturFelder  = [];
			let titelFeld = ManuelleKorrekturFeldDto.fromJS({
				manuelleKorrekturFelderId: 21,
				manuelleKorrekturId: 1245,
				feldname: 'Titel',
				original: 'Haus am See',
				automatisch: 'Haus am See',
				manuell: 'Haus am See'
			});

			let darinFeld = ManuelleKorrekturFeldDto.fromJS({
				manuelleKorrekturFelderId: 17,
				manuelleKorrekturId: 1245,
				feldname: 'Darin',
				original: 'Haus im See',
				automatisch: 'Haus am See',
				manuell: ''
			});

			let zusaetzlicheInformationenFeld = ManuelleKorrekturFeldDto.fromJS({
				manuelleKorrekturFelderId: 19,
				manuelleKorrekturId: 1245,
				feldname: 'BemerkungZurVe',
				original: 'Haus im See',
				automatisch: 'Haus am See',
				manuell: ''
			});

			let verwandteVeFeld = ManuelleKorrekturFeldDto.fromJS({
				manuelleKorrekturFelderId: 18,
				manuelleKorrekturId: 1245,
				feldname: 'VerwandteVe',
				original: 'Haus im See',
				automatisch: 'Haus am See',
				manuell: ''
			});

			let zusatzkomponenteFeld = ManuelleKorrekturFeldDto.fromJS({
				manuelleKorrekturFelderId: 20,
				manuelleKorrekturId: 1245,
				feldname: 'ZusatzkomponenteZac1',
				original: 'Haus im See',
				automatisch: 'Haus am See',
				manuell: ''
			});

			manuelleKorrektur.manuelleKorrekturFelder.push(darinFeld,  titelFeld, zusatzkomponenteFeld, zusaetzlicheInformationenFeld, verwandteVeFeld);
			let archiveRecordContextItem = ArchiveRecordContextItem.fromJS({
				titel: 'Test archiveRecordContextItem',
				archiveRecordId: 1236,
				signatur: 'E2020'
			});
			let archiveRecordContextItems: ArchiveRecordContextItem[] = new Array(ArchiveRecordContextItem[1]);
			archiveRecordContextItems[0] = ArchiveRecordContextItem.fromJS({
					titel: 'Test archiveRecordContextItem',
					archiveRecordId: 1236,
					referenceCode: 'REF2020'
				});

			let history: ManuelleKorrekturStatusHistoryDto[] = new Array(ManuelleKorrekturStatusHistoryDto[1]);
			history[0] = ManuelleKorrekturStatusHistoryDto.fromJS({

				manuelleKorrekturStatusHistoryId: 1,
				manuelleKorrekturId: 1,
				anonymisierungsstatus: 1,
				erzeugtAm:  moment(Date.now()).toDate(),
				erzeugtVon: 'PET',
			});
			manuelleKorrektur.manuelleKorrekturStatusHistories = history;

			let detailItem = ManuelleKorrekturDetailItem.fromJS({
				manuelleKorrektur: manuelleKorrektur,
				archivplanKontext: archiveRecordContextItems,
				untergeordneteVEs: archiveRecordContextItem,
				verweiseVEs: archiveRecordContextItem
			});

			return of(detailItem);
		}
	};
	let router = <Router>{};

	beforeEach(waitForAsync(async () => {
		await TestBed.configureTestingModule({
			imports: [SharedModule.forRoot(), CoreModule.forRoot(), ToastrTestingModule],
			declarations: [ManuelleKorrekturDetailPageComponent],
			schemas: [NO_ERRORS_SCHEMA],
			providers: [
				{provide: ManuelleKorrekturenService, useValue: manuelleKorrekturenService},
				{provide: FormBuilder, useValue: new FormBuilder()},
				{provide: UrlService, useValue: urlService},
				{provide: DetailPagingService, useValue: dps},
				{provide: ErrorService, useValue: errorService},
				{provide: ActivatedRoute, useValue: activatedRoute},
				{provide: ToastrService, useValue: toastrService},
				{provide: TranslationService, useValue: translationService },
				{provide: Router, useValue: router},
			]
		}).compileComponents();
	}));

	beforeEach(waitForAsync ( async () => {
		fixture = TestBed.createComponent(ManuelleKorrekturDetailPageComponent);
		sut = fixture.componentInstance;
		await sut.ngOnInit();
		await fixture.whenStable();
	}));
	it('should create', () => {
		expect(sut).toBeTruthy();
		expect(sut.myForm).toBeTruthy();
		expect(sut.myForm.controls['titel'].value === 'Test').toBeTruthy();
		expect(sut.sortedList[0].feldname === 'Titel').toBeTruthy() ;
		expect(sut.sortedList[1].feldname === 'Darin').toBeTruthy() ;
		expect(sut.sortedList[2].feldname === 'BemerkungZurVe').toBeTruthy() ;
		expect(sut.sortedList[3].feldname === 'VerwandteVe').toBeTruthy() ;
		expect(sut.sortedList[4].feldname === 'ZusatzkomponenteZac1').toBeTruthy() ;
	});
});
