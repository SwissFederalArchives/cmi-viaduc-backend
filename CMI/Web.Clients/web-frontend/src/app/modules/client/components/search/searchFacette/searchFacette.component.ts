import {
	ChangeDetectionStrategy,
	Component,
	ElementRef,
	EventEmitter,
	Input,
	OnInit,
	Output
} from '@angular/core';
import {AggregationEntry, Facet, FacetteAction, FacetteFilterItem, Utilities as _util} from '@cmi/viaduc-web-core';
import {animate, state, style, transition, trigger} from '@angular/animations';

@Component({
    selector: 'cmi-viaduc-facette',
    templateUrl: 'searchFacette.component.html',
    styleUrls: ['./searchFacette.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
	animations: [
		trigger('collapsedState', [
			state(
				'collapsed',
				style({
					height: '0px',
					overflow: 'hidden'
				})
			),
			state(
				'expanded',
				style({
					height: 'var(--facette-height)',
					overflow: 'auto'
				})
			),
			transition('collapsed => expanded', [
				animate('250ms ease-in-out')
			]),
			transition('expanded => collapsed', [
				animate('250ms ease-in-out')
			])
		])
	],
    standalone: false
})
export class SearchFacetteComponent implements OnInit {
	@Input()
	public facette: Facet;

	@Input()
	public key = '';

	@Input()
	public facetteTitle = '';

	private _collapsed = true;

	@Input()
	public set collapsed(value: boolean) {
		this._collapsed = value;
	}

	public get collapsed(): boolean {
		return this._collapsed;
	}

	@Output()
	public onFilter = new EventEmitter<FacetteFilterItem>();

	@Output()
	public onFacetteShowAll = new EventEmitter<string>();

	@Input()
	public activeFilterStrings: string[] = [];

	public buttonCss: string;
	public ulCss: string;
	public facetteHeight: number = 0;

	constructor(private _elemRef: ElementRef) {
	}

	public ngOnInit(): void {
		this._setCssClasses();
	}

	public isActive(filterString: string): boolean {
		return this.activeFilterStrings.filter((value) => value === filterString)[0] != null;
	}

	public applyFilter(agg: AggregationEntry): void {
		const ff = new FacetteFilterItem();
		ff.key = this.key;
		ff.chosenFilter = agg;
		ff.facette = this.facette;

		const filter = ff.chosenFilter.filter;
		const i = this.activeFilterStrings.findIndex((value) => value === filter);
		if (i !== -1) {
			ff.action = FacetteAction.Remove;
		} else {
			ff.action = FacetteAction.Add;
		}

		this.onFilter.emit(ff);
	}

	public showAll(facetteKey: string) {
		this.onFacetteShowAll.emit(facetteKey);
	}

	public toggle() {
		this._collapsed = !this._collapsed;
		this._setCssClasses();
		if (!this._collapsed) {
			requestAnimationFrame(() => {
				const ul = this._elemRef.nativeElement.querySelector('ul') as HTMLElement;
				if (ul) {
					this.facetteHeight = ul.scrollHeight;
				}
			});
		}
	}

	private _setCssClasses(): void {
		this.buttonCss = this.getButtonCss();
		this.ulCss = this.getUlCss();
	}

	public getButtonCss(): string {
		return this.collapsed ? 'icon icon--before icon--root' : 'icon icon--before icon--greater active';
	}

	public getUlCss(): string {
		return this.collapsed ? 'limited' : 'in limited';
	}

	public get normalizedId(): string {
		return _util.toIdentifier(this.facetteTitle, true);
	}
}
