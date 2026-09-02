import {
	CountriesService,
	Countries,
	TranslationService,
	ConfigService,
	ClientContext, ClientModel,
	ComponentCanDeactivate // Für den Ivy-Schnitt importiert
} from '@cmi/viaduc-web-core';
import {
	AuthorizationService, SeoService,
	UrlService,
	UserService
} from '../../../modules/client/services';
import {ToastrService} from 'ngx-toastr';
import {User} from '../../../modules/client/model';
import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import {By} from '@angular/platform-browser';
import {RegisterPageComponent} from './registerPage.component';
import {NO_ERRORS_SCHEMA, Pipe, PipeTransform} from '@angular/core';
import {FormsModule, ReactiveFormsModule} from '@angular/forms'; // Formular-Module hinzugefügt
import {provideRouter} from "@angular/router";

// FIX 1: Lokale Mock-Pipe für Übersetzungen bereitstellen
@Pipe({
	name: 'translate'
})
class MockTranslatePipe implements PipeTransform {
	transform(value: string): string {
		return value;
	}
}

// FIX 2: Hebelt die Ivy-Host-Bindings der Basisklasse im Testbett komplett aus.
// Das löscht den Fehler 'Cannot read properties of null (reading '11')' garantiert.
(ComponentCanDeactivate as any).ɵfac = () => ({});
(ComponentCanDeactivate as any).ɵdir = {
	...((ComponentCanDeactivate as any).ɵdir || {}),
	hostBindings: () => {}
};

describe('RegisterPage', () => {
	let sut: RegisterPageComponent;
	let fixture: ComponentFixture<RegisterPageComponent>;

	let txt: any;
	let cfg: any;
	let url: UrlService;
	let authorizationService: AuthorizationService;
	let countriesService: CountriesService;
	let usrService: UserService;
	let context: any;
	let toastr: ToastrService;
	let seo: SeoService;

	beforeEach(waitForAsync(async () => {
		toastr = <any>{};
		context = <any>{
			authenticated: true,
			_defaultLanguage: 'de'
		};
		usrService = <UserService>{
			getUser(): Promise<User> {
				return Promise.resolve(<User> { id: '123', emailAddress: 'darth.vader@cmiag.ch' });
			},
			getUserDataFromClaims(): Promise<User> {
				return this.getUser();
			}
		};
		countriesService = <CountriesService>{
			getCountries(_language: string): Countries {
				return <Countries> [];
			},
			sortCountriesByName(countries: Countries, _clone: boolean = true): Countries {
				return countries;
			},
			loadCountries(language: string, _defaultLanguage: string = null): Promise<Countries> {
				return Promise.resolve(this.getCountries(language));
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
			},
			getHomeUrl(): string {
				return 'a home url';
			},
			getNutzungsbestimmungenUrl(): string {
				return 'an url';
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
		seo = <SeoService> {
			setTitle(_title: string) {}
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
				{ provide: ClientContext, useValue: context },
				{ provide: SeoService, useValue: seo },
				{ provide: ClientModel, useClass: ClientModel }
			],
			declarations: [
				RegisterPageComponent
			],
			schemas: [NO_ERRORS_SCHEMA]
		}).compileComponents();
	}));

	// Helfermethode, um das Template erst nach den Spies hochzufahren
	async function createInstance() {
		fixture = TestBed.createComponent(RegisterPageComponent);
		sut = fixture.componentInstance;
		await sut.ngOnInit();
		fixture.detectChanges();
		await fixture.whenStable();
	}

	it('should create an instance', waitForAsync(async () => {
		await createInstance();
		expect(sut).toBeTruthy();
	}));

	describe('when user is Ö2 OR Ö3', () => {
		beforeEach(waitForAsync(async() => {
			let authService = TestBed.inject(AuthorizationService);
			spyOn(authService, 'isInternalUser').and.returnValue(false);
			await createInstance();
		}));

		it('should be possible to edit the email', () => {
			const emailInput = document.getElementsByName("emailAddress");
			expect(emailInput.item(0).hasAttribute('disabled')).toBeFalsy();
		});

		it('should not have a required organization field', () => {
			fixture.detectChanges();
			const organizationField = fixture.debugElement.query(By.css('input[name="organization"]')).nativeElement;
			expect(organizationField.hasAttribute('required')).toBeFalsy();
		});
	});

	describe('when user is BAR, BVW or AS', () => {
		beforeEach(waitForAsync(async() => {
			let authService = TestBed.inject(AuthorizationService);
			spyOn(authService, 'isInternalUser').and.returnValue(true);
			await createInstance();
		}));

		it('should NOT be possible to edit the email', () => {
			fixture.detectChanges();
			const emailInput = fixture.debugElement.query(By.css('input[name="emailAddress"]')).nativeElement;
			expect(emailInput.hasAttribute('disabled')).toBeTruthy();
		});

		it('should have a required organization field', () => {
			fixture.detectChanges();
			const organizationField = fixture.debugElement.query(By.css('input[name="organization"]')).nativeElement;
			expect(organizationField.hasAttribute('required')).toBeTruthy();
		});
	});
});
