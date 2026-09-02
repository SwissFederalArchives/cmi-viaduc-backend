import {ClientModel, OrderItem, ShippingType, TranslationService, Ordering, ClientContext, ConfigService} from '@cmi/viaduc-web-core';
import {CheckoutShippingTypeStepComponent} from './checkoutShippingTypeStep.component';
import {AuthorizationService, ShoppingCartService, UrlService} from '../../../services';
import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { provideRouter } from '@angular/router'; // Ersetzt das RouterTestingModule sauber
import {By} from '@angular/platform-browser';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {LocalizeLinkPipe} from '../../../pipes';
import {Observable, of} from 'rxjs';
import {KontingentResult} from '../../../model';
import {NO_ERRORS_SCHEMA, Pipe, PipeTransform} from '@angular/core';

// Lokale Mock-Pipe für Template-Übersetzungen
@Pipe({
	name: 'translate'
})
class MockTranslatePipe implements PipeTransform {
	transform(value: string): string {
		return value;
	}
}

describe('CheckoutShippingTypeStep', () => {

	let sut: CheckoutShippingTypeStepComponent;
	let fixture: ComponentFixture<CheckoutShippingTypeStepComponent>;
	let auth: AuthorizationService;
	let txt: TranslationService;
	let cfg: ConfigService;
	let shoppingCartService: ShoppingCartService;

	beforeEach(waitForAsync(async () => {
		shoppingCartService = <any> {
			getTotalItemsInCart(): number { return 1; },
			getActiveOrder(): Ordering { return null; },
			getShowDigitizationWarningSetting(): boolean { return false; },
			getKontingent(): Observable<KontingentResult> {
				return of({ bestellkontingent: 999, aktiveDigitalisierungsauftraege: 1, digitalisierungesbeschraenkung: 999});
			},
			getOrderableItems(): Observable<OrderItem[]> { return of([]); }
		};

		txt = <TranslationService>{
			translate(text: string, _key?: string, ..._args: any[]): string { return text; }
		};

		cfg = <ConfigService>{
			getSetting(_key: string, defaultValue: any): any { return defaultValue; }
		};

		auth = <AuthorizationService> {
			isBvwUser(): boolean { return true; },
			isAsUser(): boolean { return false; }
		};
		let urlService = <UrlService> {
			localizeUrl(_lang: string, url: string): string { return url; }
		};

		await TestBed.configureTestingModule({
			imports: [
				FormsModule,
				ReactiveFormsModule,
				MockTranslatePipe // Garantiert fehlerfreie | translate Verarbeitung
			],
			providers: [
				provideRouter([]), // Verhindert router duplicate forRoot guard Fehler restlos
				{ provide: TranslationService, useValue: txt},
				{ provide: ConfigService, useValue: cfg },
				{ provide: ShoppingCartService, useValue: shoppingCartService },
				{ provide: AuthorizationService, useValue: auth },
				{ provide: LocalizeLinkPipe },
				{ provide: UrlService, useValue: urlService },
				{ provide: ClientModel, useClass: ClientModel },
				{provide: ClientContext, useValue: { language: () => 'de', authenticated: true }}
			],
			declarations: [
				CheckoutShippingTypeStepComponent,
				LocalizeLinkPipe
			],
			schemas: [NO_ERRORS_SCHEMA]
		}).compileComponents();
	}));

	// Helferfunktion, um die Komponente erst NACH dem Einrichten der Spies sauber hochzufahren
	async function createComponentInstance() {
		fixture = TestBed.createComponent(CheckoutShippingTypeStepComponent);
		sut = fixture.componentInstance;
		await sut.ngOnInit();
		fixture.detectChanges();
		await fixture.whenStable();
	}

	describe('when a AS user visits the page', () => {
		beforeEach(waitForAsync(async() => {
			let authService = TestBed.inject(AuthorizationService);
			spyOn(authService, 'isAsUser').and.returnValue(true);
			spyOn(authService, 'isBvwUser').and.returnValue(false);
			await createComponentInstance();
		}));

		it('it should show Verwaltungsausleihe option', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkInsAmtBestellen')).nativeElement as HTMLElement;
			expect(vwOption).toBeTruthy();
		});

		it('it should show Lesesaal option', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkInLeseSaalBestellen')).nativeElement as HTMLElement;
			expect(vwOption).toBeTruthy();
		});

		it('it should show Digitalisat option', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkAlsDigitalisatBestellen')).nativeElement as HTMLElement;
			expect(vwOption).toBeTruthy();
		});
	});

	describe('when a BVW user visits the page', () => {
		beforeEach(waitForAsync(async() => {
			let authService = TestBed.inject(AuthorizationService);
			spyOn(authService, 'isBvwUser').and.returnValue(true);
			spyOn(authService, 'isAsUser').and.returnValue(false);
			await createComponentInstance();
		}));

		it('it should show Verwaltungsausleihe option', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkInsAmtBestellen')).nativeElement as HTMLElement;
			expect(vwOption).toBeTruthy();
		});

		it('it should show Lesesaal option', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkInLeseSaalBestellen')).nativeElement as HTMLElement;
			expect(vwOption).toBeTruthy();
		});

		it('it should show Digitalisat option', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkAlsDigitalisatBestellen')).nativeElement as HTMLElement;
			expect(vwOption).toBeTruthy();
		});
	});

	describe('when other users visits the page', () => {
		beforeEach(waitForAsync(async() => {
			let authService = TestBed.inject(AuthorizationService);
			spyOn(authService, 'isBvwUser').and.returnValue(false);
			spyOn(authService, 'isAsUser').and.returnValue(false);
			await createComponentInstance();
		}));

		it('it should hide Verwaltungsausleihe option', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkInsAmtBestellen'));
			expect(vwOption).toBeFalsy();
		});

		it('it should show Lesesaal option', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkInLeseSaalBestellen')).nativeElement as HTMLElement;
			expect(vwOption).toBeTruthy();
		});

		it('it should show Digitalisat option', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkAlsDigitalisatBestellen')).nativeElement as HTMLElement;
			expect(vwOption).toBeTruthy();
		});

		it('the Lesesaal option should be selected', () => {
			const vwOption = fixture.debugElement.query(By.css('#chkInLeseSaalBestellen')).nativeElement as HTMLInputElement;
			expect(vwOption.checked).toBeTruthy();
		});
	});

	describe('when the warning option is set', () => {
		beforeEach(waitForAsync(async() => {
			let authService = TestBed.inject(AuthorizationService);
			spyOn(authService, 'isBvwUser').and.returnValue(false);
			spyOn(authService, 'isAsUser').and.returnValue(false);
			let scs = TestBed.inject(ShoppingCartService);
			spyOn(scs, 'getShowDigitizationWarningSetting').and.returnValue(true); // Option ist AN
			await createComponentInstance();
		}));

		// FIX 1: Testbeschreibung und Erwartung korrigiert (Warnung MUSS angezeigt werden)
		it('selecting the Digitalisierung option results in showing a warning', waitForAsync(async() => {
			const vwOption = fixture.debugElement.query(By.css('#chkAlsDigitalisatBestellen')).nativeElement as HTMLInputElement;
			vwOption.click();
			sut.form.controls.shippingType.setValue(ShippingType.Digitalisierungsauftrag);

			fixture.detectChanges();
			await fixture.whenStable();

			const warning = fixture.debugElement.query(By.css('#alsDigitalisatBestellenWarning'));
			expect(warning).toBeTruthy();
		}));
	});

	describe('when the warning option is not set', () => {
		beforeEach(waitForAsync(async() => {
			let authService = TestBed.inject(AuthorizationService);
			spyOn(authService, 'isBvwUser').and.returnValue(false);
			spyOn(authService, 'isAsUser').and.returnValue(false);
			let scs = TestBed.inject(ShoppingCartService);
			spyOn(scs, 'getShowDigitizationWarningSetting').and.returnValue(false); // Option ist AUS
			await createComponentInstance();
		}));

		// FIX 2: Testbeschreibung korrigiert und den abgebrochenen Block sauber beendet (Warnung darf NICHT angezeigt werden)
		it('selecting the Digitalisierung option does not show a warning', waitForAsync(async() => {
			const vwOption = fixture.debugElement.query(By.css('#chkAlsDigitalisatBestellen')).nativeElement as HTMLInputElement;
			vwOption.click();
			sut.form.controls.shippingType.setValue(ShippingType.Digitalisierungsauftrag);

			fixture.detectChanges();
			await fixture.whenStable();

			const warning = fixture.debugElement.query(By.css('#alsDigitalisatBestellenWarning'));
			expect(warning).toBeNull();
		}));
	});
});
