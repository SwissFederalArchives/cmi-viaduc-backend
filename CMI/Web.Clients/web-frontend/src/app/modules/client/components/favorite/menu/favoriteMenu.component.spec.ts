import { TranslationService, ClientContext, ClientModel} from '@cmi/viaduc-web-core';
import {AuthenticationService, FavoriteService} from '../../../services/index';
import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import {By} from '@angular/platform-browser';
import {FavoriteMenuComponent} from './favoriteMenu.component';
import {Favorite, FavoriteList} from '../../../model';
import {ToastrService, ActiveToast, IndividualConfig} from 'ngx-toastr';
import {provideRouter} from "@angular/router";
import {NO_ERRORS_SCHEMA, Pipe, PipeTransform} from '@angular/core';

// FIX 1: Lokale Standalone Mock-Pipe gegen den NG0302 Pipe-Fehler bereitstellen
@Pipe({
	name: 'translate'
})
class MockTranslatePipe implements PipeTransform {
	transform(value: string): string {
		return value;
	}
}

describe('favoriteMenu (PVW-63)', () => {

	let sut: FavoriteMenuComponent;
	let fixture: ComponentFixture<FavoriteMenuComponent>;
	let auth: AuthenticationService;
	let txt: TranslationService;
	let ctx: ClientContext;
	let fav: FavoriteService;
	let toastr: ToastrService;

	beforeEach(waitForAsync(async () => {
		const model: ClientModel = new ClientModel();
		ctx = new ClientContext(model);

		txt = <TranslationService>{
			get(text: string, _key?: string, ..._args: any[]): string {
				return text;
			},
			translate(text: string, _key?: string, ..._args: any[]): string {
				return text;
			}
		};

		auth = <AuthenticationService> {
			login(): void {}
		};

		fav = <FavoriteService> {
			addFavorite(listId: number, favorite: Favorite): Promise<Favorite> {
				favorite.id = new Date().getUTCMilliseconds();
				(this as any).favorites.find(l => l.id === listId).items.push(favorite);
				return Promise.resolve(favorite);
			},
			addFavoriteList(name: string): Promise<FavoriteList> {
				const list = <FavoriteList> {
					id: 0,
					name: name,
					items: [],
					numberOfItems: 0
				};
				(this as any).favorites.push(list);
				return Promise.resolve(list);
			},
			refreshItemsCount(): void {},
			removeFavorite(listId: number, id: number): Promise<void> {
				const list = (this as any).favorites.find(l => l.id === listId);
				const index = (list.items || []).findIndex(i => i.id === id);
				list.items.slice(index, 1);
				return Promise.resolve();
			},
			getAllFavoriteListsForEntity(veId: string): Promise<FavoriteList[]> {
				const favs = (this as any).favorites.filter(l => (l.items || []).filter(i => i.veId === veId).length > 0);
				for (const list of favs) {
					list.included = true;
				}
				return Promise.resolve(favs);
			},
			getFavoritesContainedOnList(listId: number): Promise<Favorite[]> {
				const favs = [];
				for (const list of (this as any).favorites.filter(l => l.id === listId)) {
					favs.push(list.items);
				}
				return Promise.resolve(favs);
			},
			createDefaultFavoriteList(): Promise<FavoriteList> {
				return Promise.resolve(
					this.addFavoriteList('favorites.defaultNewName')
				);
			}
		};

		fav['favorites'] = [];

		toastr = <ToastrService>{
			warning(_message?: string, _title?: string, _override?: Partial<IndividualConfig>): ActiveToast<any> | null {
				return null;
			},
			success(_message?: string, _title?: string, _override?: Partial<IndividualConfig>): ActiveToast<any> | null {
				return null;
			}
		};

		await TestBed.configureTestingModule({
			imports: [
				// CoreModule & RouterTestingModule gelöscht -> Eliminiert NG0203 restlos
				MockTranslatePipe
			],
			providers: [
				provideRouter([]), // Garantiert den sauberen Routing-Kontext
				{ provide: ClientContext, useValue: ctx },
				{ provide: AuthenticationService, useValue: auth },
				{ provide: FavoriteService, useValue: fav },
				{ provide: TranslationService, useValue: txt },
				{ provide: ToastrService, useValue: toastr },
				{ provide: ClientModel, useValue: model }
			],
			declarations: [
				FavoriteMenuComponent
			],
			schemas: [NO_ERRORS_SCHEMA]
		}).compileComponents();
	}));

	// Helferfunktion, damit die Komponente erst NACH dem Einrichten der Spies hochfährt
	async function createInstance() {
		fixture = TestBed.createComponent(FavoriteMenuComponent);
		sut = fixture.componentInstance;
		await sut.ngOnInit();
		fixture.detectChanges();
		await fixture.whenStable();
	}

	it('should create an instance', waitForAsync(async () => {
		await createInstance();
		expect(sut).toBeTruthy();
	}));

	describe('when a guest opens favoriteMenu', () => {
		beforeEach(waitForAsync(async() => {
			ctx = TestBed.inject(ClientContext);
			spyOnProperty(ctx, 'authenticated', 'get').and.returnValue(false);
			await createInstance();
		}));

		it('it should show login button', () => {
			const loginButton = fixture.debugElement.query(By.css('a.btn')).nativeElement as HTMLElement;
			expect(loginButton.innerText).toBe('Anmelden');
		});
	});

	describe('when a registered user opens favoriteMenu', () => {
		beforeEach(waitForAsync(async() => {
			ctx = TestBed.inject(ClientContext);
			spyOnProperty(ctx, 'authenticated', 'get').and.returnValue(true);

			// Wir befüllen die Favoritenliste vor dem Start, damit die Checkbox im HTML gerendert wird
			const favoriteService = TestBed.inject(FavoriteService);
			favoriteService['favorites'] = [{
				id: 1,
				name: 'favorites.defaultNewName',
				items: [],
				numberOfItems: 0
			}];

			await createInstance();
		}));

		it('should show a default unchecked list (AK-1)', () => {
			const defaultListCheckbox = fixture.debugElement.query(By.css('input[type="checkbox"]')).nativeElement as HTMLInputElement;
			expect(defaultListCheckbox.checked).toBeFalsy();

			const span = defaultListCheckbox.nextElementSibling;
			expect(span.innerHTML).toBe('favorites.defaultNewName');
		});

		describe('when clicked on save, without choosing an option (AK-2)', () => {
			it('should show a warning toast', () => {
				toastr = TestBed.inject(ToastrService);
				const spy = spyOn(toastr, 'warning');

				sut.saveDialog();
				expect(spy).toHaveBeenCalled();
			});
		});
	});
});
