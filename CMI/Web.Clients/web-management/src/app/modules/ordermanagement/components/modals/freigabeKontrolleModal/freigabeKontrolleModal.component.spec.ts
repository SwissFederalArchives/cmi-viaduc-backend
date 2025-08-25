import {FreigabeKontrolleModalComponent} from './freigabeKontrolleModal.component';
import { ComponentFixture, TestBed,  waitForAsync } from '@angular/core/testing';
import {ApproveStatus, EntityDecoratorService, TranslationService, CoreModule, UiService} from '@cmi/viaduc-web-core';
import {OrderService} from '../../../services';
import {ErrorService, SharedModule} from '../../../../shared';
import {ToastrService} from 'ngx-toastr';
import {By} from '@angular/platform-browser';
import {NO_ERRORS_SCHEMA} from '@angular/core';

describe('FreigabeKontrolleModalPage', () => {
	let fixture: ComponentFixture<FreigabeKontrolleModalComponent>;
	let sut: FreigabeKontrolleModalComponent;
	let txt: TranslationService;
	let orderService: OrderService;
	let toastrService: ToastrService;
	let errorService: ErrorService;
	let entityDecoratorService: EntityDecoratorService;
	let uiService: UiService;

	txt = <TranslationService>{
		translate(text: string, key?: string, ...args): string {
			return text;
		},
		get(key: string, text?: string, ...args): string {
			return text;
		}
	};

	entityDecoratorService  = new EntityDecoratorService(txt);

	orderService = <OrderService>{

	};

	errorService = <ErrorService>{

	};

	toastrService = <ToastrService>{

	};

	uiService = new UiService(toastrService);

	beforeEach( waitForAsync(async() => {
		TestBed.configureTestingModule({
			imports:[CoreModule, SharedModule],
			declarations: [FreigabeKontrolleModalComponent],
			schemas: [NO_ERRORS_SCHEMA],
			providers: [
				{provide: EntityDecoratorService, useValue: entityDecoratorService},
				{provide: OrderService, useValue: orderService},
				{provide: ErrorService, useValue: errorService},
				{provide: ToastrService, useValue: toastrService },
				{provide: UiService, useValue: uiService },
				{ provide: TranslationService, useValue: txt }
			]
		}).compileComponents();
	}));

	beforeEach(waitForAsync(async() => {
		fixture = TestBed.createComponent(FreigabeKontrolleModalComponent);
		sut = fixture.componentInstance;
		sut.ngOnInit();
		await fixture.whenStable();
	}));

	it('should create an instance',  waitForAsync(async() => {
		fixture.detectChanges();
		fixture.whenStable().then(() => {
			expect(sut).toBeTruthy();
		});
	}));

	describe('when user is Ö2 and ApproveStatus is FreigegebenInSchutzfrist', () => {
		beforeEach(waitForAsync(async () => {
			fixture = TestBed.createComponent(FreigabeKontrolleModalComponent);
			sut = fixture.componentInstance;
			await sut.ngOnInit();
			sut.currentRolePublicClient = 'Ö2';
			sut.selectedEntscheid = ApproveStatus.FreigegebenInSchutzfrist;
			fixture.detectChanges();
			await fixture.whenStable();
		}));

		it('it should show warn text and show input for bewilligungsDatum', () => {
			const chkConfirmOe2userInput = fixture.debugElement.query(By.css('input[name="chkConfirmOe2user"]')).nativeElement as HTMLElement;
			const datumBewilligungInput = fixture.debugElement.query(By.css('input[name="datumBewilligung"]')).nativeElement as HTMLElement;

			expect(datumBewilligungInput.hasAttribute('disabled')).toBeFalse();
			expect(sut.haveToEnterBewilligungsDatum).toBeTruthy();
			expect(chkConfirmOe2userInput.hasAttribute('disabled')).toBeFalse();
		});

		it('when user is Ö2 and ApproveStatus is FreigegebenInSchutzfrist and User has warn text confirmed',
			() => {
			const chkConfirmOe2userInput = fixture.debugElement.query(By.css('input[name="chkConfirmOe2user"]')).nativeElement as HTMLElement;
			chkConfirmOe2userInput['checked'] = 'true';
			fixture.detectChanges();
			const datumBewilligungInput = fixture.debugElement.query(By.css('input[name="datumBewilligung"]')).nativeElement as HTMLElement;

			expect(datumBewilligungInput.hasAttribute('disabled')).toBeFalse();
			expect(sut.haveToEnterBewilligungsDatum).toBeTrue();
			expect(chkConfirmOe2userInput.hasAttribute('disabled')).toBeFalse();
			expect(sut.isOe2UserConfirmed = true).toBeTrue();
		});

		it('when user is Ö2 and ApproveStatus is FreigegebenInSchutzfris and change ApproveStatus' +
			'warn text not registered user should hide', () => {
			sut.selectedEntscheid = ApproveStatus.FreigegebenAusserhalbSchutzfrist;
			fixture.detectChanges();
			expect(sut.haveToEnterBewilligungsDatum).toBeFalse();

			expect(fixture.debugElement.query(By.css('input[name="chkConfirmOe2user"]'))).toBeNull();
			expect(fixture.debugElement.query(By.css('input[name="datumBewilligung"]'))).toBeNull();
		});
	});

	describe('when user is Ö3 and ApproveStatus is FreigegebenInSchutzfris ' +
		'field haveToEnterBewilligungsDatum appear and warn text not registered user should hide', () => {
		beforeEach(waitForAsync(async () => {
			fixture = TestBed.createComponent(FreigabeKontrolleModalComponent);
			sut = fixture.componentInstance;
			await sut.ngOnInit();
			sut.currentRolePublicClient = 'Ö3';
			sut.datumBewilligung = new Date('2025-08-26');
			sut.selectedEntscheid = ApproveStatus.FreigegebenInSchutzfrist;
			fixture.detectChanges();
			await fixture.whenStable();

		}));

		it('test if the date is converted correctly'
			,() => {
				expect(sut.datumBewilligung).toEqual(new Date('2025-08-26'));
			});

		it('when user is Ö3 and ApproveStatus is FreigegebenInSchutzfris ' +
			'field haveToEnterBewilligungsDatum appear and warn text not registered user should hide', () => {
			const datumBewilligungInput = fixture.debugElement.query(By.css('input[name="datumBewilligung"]')).nativeElement as HTMLElement;
			expect(datumBewilligungInput.hasAttribute('disabled')).toBeFalse();
			expect(sut.haveToEnterBewilligungsDatum).toBeTrue();
			expect(fixture.debugElement.query(By.css('input[name="chkConfirmOe2user"]'))).toBeNull();
		});

		it('when user is Ö3 and ApproveStatus is FreigegebenAusserhalbSchutzfrist no warn text shall appear'
			, () => {
				sut.selectedEntscheid = ApproveStatus.FreigegebenAusserhalbSchutzfrist;
				fixture.detectChanges();
				expect(sut.haveToEnterBewilligungsDatum).toBeFalse();
				expect(fixture.debugElement.query(By.css('input[name="chkConfirmOe2user"]'))).toBeNull();
				expect(fixture.debugElement.query(By.css('input[name="datumBewilligung"]'))).toBeNull();

			});
	});
});
