import {ChangeDetectionStrategy, Component, Input, OnInit, Output, EventEmitter} from '@angular/core';
import {ManuelleKorrekturFeldDto} from '@cmi/viaduc-web-core/lib/core/model/entityFramework-models';
import {FormBuilder, FormGroup} from '@angular/forms';
@Component({
	selector: 'cmi-manuelle-korrektur-felder',
	templateUrl: './manuelle-korrektur-felder.component.html',
	styleUrls: ['./manuelle-korrektur-felder.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush,
})

export class ManuelleKorrekturFelderComponent implements OnInit {
	@Input()
	public feld: ManuelleKorrekturFeldDto;
	@Input()
	public parentForm: FormGroup;

	@Output()
	public manuellTextChanged = new EventEmitter<string>();

	public myForm: FormGroup;
	public contextmenuShow = false;
	public contextmenuX = 0;
	public contextmenuY = 0;
	public selectionStart = 0;
	public selectionEnd = 0;

	private isEditMode = false;
	constructor(private formbuilder: FormBuilder) {
	}

	public ngOnInit(): void {
		this.initForm();
	}

	private initForm() {
		this.myForm = this.formbuilder.group({
			original: [this.feld.original],
			automatisch: [this.feld.automatisch],
			manuell: [this.feld.manuell]
		});
		this.editMode = this.isEditMode;
	}

	@Input()
	set editMode(value: boolean) {
		this.isEditMode = value;
		if (this.myForm) {
			if (value) {
				this.myForm.controls['manuell'].enable();
			} else {
				this.myForm.controls['manuell'].disable();
				this.myForm.patchValue({
					manuell: [this.feld.manuell]
				});
			}
		}
	}

	public deleteText() {
		this.myForm.controls['manuell'].setValue('');
		this.changedText();
		this.onManuellChanged();
	}

	public changedText() {
		this.manuellTextChanged.emit(this.myForm.controls['manuell'].value);
		this.contextmenuShow = false;
	}

	public displayContextMenu(event) {
		if (this.isEditMode && this.selectionEnd - this.selectionStart > 0) {
			this.contextmenuShow = true;
			this.contextmenuY = event.offsetY;
			this.contextmenuX = event.offsetX;
		}
		return false;
	}

	public anonymisieren() {
		let text = this.myForm.controls['manuell'].value.toString();
		const selecteted =  text.substr(this.selectionStart, this.selectionEnd - this.selectionStart);
		text = text.replace(selecteted, '███');
		this.selectionEnd = this.selectionStart = 0;
		this.myForm.controls['manuell'].setValue(text);
		this.changedText();
		this.onManuellChanged();
	}

	public selectionchange($event: any) {
		this.selectionStart = $event.target.selectionStart;
		this.selectionEnd = $event.target.selectionEnd;
	}

	public copyAutomatischerText() {
		if (this.isEditMode) {
			this.myForm.controls['manuell'].setValue(this.myForm.controls['automatisch'].value);
			this.changedText();
			this.onManuellChanged();
		}
	}

	public localTranslate(feldname: string) {
		switch (feldname) {
			case 'Titel':
			case 'Darin':
				return feldname;
				break;
			case 'VerwandteVe':
				return 'Verwandte VE';
				break;
			case 'ZusatzkomponenteZac1':
				return 'Zusatzkomponente';
				break;
			case 'BemerkungZurVe':
				return 'Zusätzliche Informationen';
				break;
			default:
				return 'fieldname not found';
		}
	}

	public onManuellChanged() {
		// only via html this.form.Dirty(); is true
		this.parentForm.markAsDirty();
	}
}
