import {UserAccountComponent} from '../..';
import {
	CountriesService,
	Countries,
	TranslationService,
	ConfigService,
	ClientContext
} from '@cmi/viaduc-web-core';
import {
	AuthorizationService,
	UrlService,
	UserService
} from '../../../services';
import {ToastrService} from 'ngx-toastr';
import {User} from '../../../model';
import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import {By} from '@angular/platform-browser';
import {provideRouter} from "@angular/router";
import {NO_ERRORS_SCHEMA, Pipe, PipeTransform} from '@angular/core';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';


@Pipe({
	name: 'translate'
})
class MockTranslatePipe implements PipeTransform {
	transform(value: string): string {
		return value;
	}
}

describe('UserAccount', () => {
	let sut: UserAccountComponent;
	let fixture: ComponentFixture<UserAccountComponent>;

	let txt: any;
	let cfg: any;
	let url: UrlService;
	let authorizationService: AuthorizationService;
	let countriesService: CountriesService;
	let usrService: UserService;
	let context: any;
	let toastr: ToastrService;

	beforeEach(waitForAsync(async () => {
		toastr = <any>{};
		context = <any>{
			authenticated: true,
			_defaultLanguage: 'de'
		};
		usrService = <UserService>{
			getUser(): Promise<User> {
				return Promise.resolve(<User> { id: '123', emailAddress: 'darth.vader@cmiag.ch' });
			}
		};
		countriesService = <CountriesService>{
			getCountries(_language: string): Countries {
				return <Countries> [];
			},
			sortCountriesByName(countries: Countries, _clone: boolean = true): Countries {
				return countries;
			}
		};
		authorizationService = <AuthorizationService>{
			isInternalUser(): boolean {
				return false;
			},
			isExternalUser(): boolean {
				return !this.isInternalUser();
			},
			hasMoreThenOe2Rights(): boolean {
				return this.isInternalUser();
			},
			hasRole(_r: string): boolean {
				return true;
			},
			roles: {
				Oe1: 'Ö1',
				Oe2: 'Ö2',
				Oe3: 'Ö3',
				BVW: 'BVW',
				AS: 'AS',
				BAR: 'BAR'
			}
		};
		url = <UrlService> {
			getExternalHostUrl(): string {
				return 'an external url';
			}
		};
		cfg = <ConfigService>{
			getSetting(_key: string, defaultValue?: any): any {
				return defaultValue;
			}
		};
		txt = <TranslationService>{
			translate: (text, _key) => {
				return text;
			},
			get(_key: string, defaultValue?: string, ..._args): string {
				return defaultValue;
			}
		};

		await TestBed.configureTestingModule({
			imports: [
				FormsModule,
				ReactiveFormsModule,
				MockTranslatePipe
			],
			providers: [
				provideRouter([]),
				{ provide: TranslationService, useValue: txt },
				{ provide: ConfigService, useValue: cfg },
				{ provide: ToastrService, useValue: toastr },
				{ provide: UserService, useValue: usrService },
				{ provide: CountriesService, useValue: countriesService },
				{ provide: AuthorizationService, useValue: authorizationService },
				{ provide: UrlService, useValue: url },
				{ provide: ClientContext, useValue: context }
			],
			declarations: [
				UserAccountComponent
			],
			schemas: [NO_ERRORS_SCHEMA]
		}).compileComponents();
	}));

	// FIX 3: Helferfunktion entkoppelt die Instanziierung.
	// Dadurch greifen Spies in den inneren Blöcken, BEVOR ngOnInit evaluiert wird.
	async function createComponentInstance() {
		fixture = TestBed.createComponent(UserAccountComponent);
		sut = fixture.componentInstance;
		sut.ngOnInit();
		fixture.detectChanges();
		await fixture.whenStable();
	}

	it('should create an instance', waitForAsync(async () => {
		await createComponentInstance();
		expect(sut).toBeTruthy();
	}));

	describe('when user is Ö2 OR Ö3', () => {
		beforeEach(waitForAsync(async () => {
			let authService = TestBed.inject(AuthorizationService);
			spyOn(authService, 'isInternalUser').and.returnValue(false);
			await createComponentInstance();
		}));

		it('should show a hint to also edit the email in eiam (PVW-258, AK-1, AK-2)', waitForAsync(async () => {
			fixture.detectChanges();
			await fixture.whenStable();

			const emailHint = fixture.debugElement.query(By.css('.email-hint'));
			expect(emailHint).toBeTruthy();
			expect(emailHint.nativeElement.innerText).toContain('Mobiltelefon-Nummer und E-Mail-Adresse dienen zur Kontaktaufnahme');
		}));

		describe('when user is editing his data', () => {
			beforeEach(waitForAsync(async () => {
				sut.onChangeSettingsClicked();
				fixture.detectChanges();
				await fixture.whenStable();
			}));

			it('should show a hint to also edit the email in eiam', () => {
				const emailHint = fixture.debugElement.query(By.css('.email-hint'));
				expect(emailHint).toBeTruthy();
				expect(emailHint.nativeElement.innerText).toContain('Mobiltelefon-Nummer und E-Mail-Adresse dienen zur Kontaktaufnahme');
			});

			it('should be possible to edit the email', () => {
				const emailInput = fixture.debugElement.query(By.css('#E-Mail')).nativeElement as HTMLElement;
				expect(emailInput.hasAttribute('readonly')).toBeFalsy();
			});
		});
	});

	describe('when user is BAR, BVW or AS', () => {
		beforeEach(waitForAsync(async () => {
			let authService = TestBed.inject(AuthorizationService);
			spyOn(authService, 'isInternalUser').and.returnValue(true);
			await createComponentInstance();
		}));

		it('should NOT show a hint to edit the email in eiam', waitForAsync(async () => {
			fixture.detectChanges();
			await fixture.whenStable();

			const emailHint = fixture.debugElement.query(By.css('.email-hint'));
			expect(emailHint).toBeFalsy(); // Muss null/falsy sein, da ausgeblendet
		}));

		describe('when user is editing his data', () => {
			beforeEach(waitForAsync(async () => {
				sut.onChangeSettingsClicked();
				fixture.detectChanges();
				await fixture.whenStable();
			}));

			it('should NOT show a hint to edit the email in eiam', () => {
				const emailHint = fixture.debugElement.query(By.css('.email-hint'));
				expect(emailHint).toBeFalsy();
			});

			it('should NOT be possible to edit the email', () => {
				const emailInput = fixture.debugElement.query(By.css('#E-Mail')).nativeElement as HTMLElement;
				expect(emailInput.hasAttribute('readonly')).toBeTruthy();
			});
		});
	});
});
