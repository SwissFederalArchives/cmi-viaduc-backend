import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CituroFloatingButtonComponent } from './cituro-floating-button.component';
import { ConfigService, TranslationService } from '@cmi/viaduc-web-core'; // CoreModule entfernt
import { LocalizeLinkPipe } from '../../pipes';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter } from "@angular/router";
import { Pipe, PipeTransform } from '@angular/core';

// FIX 1: Lokale Standalone Mock-Pipe für Template-Übersetzungen bereitstellen
@Pipe({
	name: 'translate'
})
class MockTranslatePipe implements PipeTransform {
	transform(value: string): string {
		return value;
	}
}

describe('CituroFloatingButtonComponent', () => {
	let component: CituroFloatingButtonComponent;
	let fixture: ComponentFixture<CituroFloatingButtonComponent>;
	let txt: TranslationService;
	let cfg: ConfigService;

	beforeEach(async () => {
		// FIX 2: Mocks sauber innerhalb des beforeEach-Zyklus initialisieren
		cfg = <ConfigService>{
			getSetting(_key: string, defaultValue: any): any {
				return defaultValue;
			}
		};
		txt = <TranslationService>{
			translate(text: string, _key?: string, ..._args): string {
				return text;
			}
		};

		await TestBed.configureTestingModule({
			imports: [
				ReactiveFormsModule,
				MockTranslatePipe
			],
			providers: [
				provideRouter([]),
				{ provide: TranslationService, useValue: txt },
				{ provide: ConfigService, useValue: cfg },
				{ provide: LocalizeLinkPipe },
			],
			declarations: [ CituroFloatingButtonComponent, LocalizeLinkPipe ]
		}).compileComponents();

		fixture = TestBed.createComponent(CituroFloatingButtonComponent);
		component = fixture.componentInstance;
		fixture.detectChanges();
	});

	it('should create', () => {
		expect(component).toBeTruthy();
	});
});
