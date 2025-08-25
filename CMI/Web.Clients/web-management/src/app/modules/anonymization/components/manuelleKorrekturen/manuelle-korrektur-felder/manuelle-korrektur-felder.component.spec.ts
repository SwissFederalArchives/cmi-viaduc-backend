import {ComponentFixture, TestBed, waitForAsync} from '@angular/core/testing';
import {ManuelleKorrekturFelderComponent} from './manuelle-korrektur-felder.component';
import {FormBuilder} from '@angular/forms';
import {ManuelleKorrekturFeldDto} from '@cmi/viaduc-web-core';
import {NO_ERRORS_SCHEMA} from '@angular/core';

describe('ManuelleKorrekturFelderComponent', () => {
	let sut: ManuelleKorrekturFelderComponent;
	let fixture: ComponentFixture<ManuelleKorrekturFelderComponent>;

	beforeEach(async () => {
		await TestBed.configureTestingModule({
			declarations: [ ManuelleKorrekturFelderComponent ],
			schemas: [NO_ERRORS_SCHEMA],
			providers: [
				{provide: FormBuilder, useValue: new FormBuilder()}
			]
		}).compileComponents();
	});

	beforeEach(waitForAsync ( async () => {
		fixture = TestBed.createComponent(ManuelleKorrekturFelderComponent);
		sut = fixture.componentInstance;
		sut.feld = ManuelleKorrekturFeldDto.fromJS({
			manuelleKorrekturFelderId: 1254,
			manuelleKorrekturId: 1245,
			feldname: 'Titel',
			original: 'Haus am See',
			automatisch: 'Haus am See',
			manuell: 'Haus am See'
		});
		await sut.ngOnInit();
		await fixture.whenStable();
	}));

	it('should create', () => {
		expect(sut).toBeTruthy();
		expect(sut.myForm).toBeTruthy();
		expect(sut.myForm.controls['original'].value === 'Haus am See').toBeTruthy();
	});

});
