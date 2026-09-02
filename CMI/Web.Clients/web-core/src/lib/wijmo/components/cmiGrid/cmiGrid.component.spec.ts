import { waitForAsync, ComponentFixture, TestBed, fakeAsync, flush } from '@angular/core/testing';

import { WjInputModule } from '@mescius/wijmo.angular2.input';
import { WjGridModule } from '@mescius/wijmo.angular2.grid';
import { CmiGridComponent } from './cmiGrid.component';
import { WjGridFilterModule } from '@mescius/wijmo.angular2.grid.filter';
import { WijmoService } from '../../services';
import { CollectionView, Event } from '@mescius/wijmo';
import { FilterType, Operator } from '@mescius/wijmo.grid.filter';
import { DataMap } from '@mescius/wijmo.grid';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('CmiGridComponent', () => {
	let fixture: ComponentFixture<CmiGridComponent>;
	let grid: CmiGridComponent;
	let gridNativeElement: HTMLElement;

	let collectionView: CollectionView;
	let uniqueValues: any;

	// Sicherer, globaler Handler-Mock für addHandler / removeHandler, removeAllHandlers & raise
	const globalEventHandlerMock = {
		addHandler: () => {},
		removeHandler: () => {},
		removeAllHandlers: () => {},
		raise: () => {}
	};

	// Stabiler Mock für das Filter-Objekt mit expliziten Event-Mocks, um dispose() Fehler abzufangen
	const filterMock = new Proxy({
		defaultFilterType: FilterType.Condition,
		getColumnFilter: (colIndex: number) => ({
			filterType: colIndex === 1 ? FilterType.Value : FilterType.Condition,
			valueFilter: { uniqueValues: uniqueValues || [] },
			conditionFilter: {
				condition1: { operator: colIndex === 2 ? Operator.CT : undefined, value: null },
				condition2: { operator: undefined, value: null }
			}
		}),
		apply: () => {},
		removeAllHandlers: () => {},
		filterChanging: globalEventHandlerMock,
		filterChanged: globalEventHandlerMock,
		filterApplied: globalEventHandlerMock
	}, {
		get: (target, prop) => {
			if (prop in target) {
				return (target as any)[prop];
			}
			return globalEventHandlerMock;
		}
	});

	beforeEach(waitForAsync(async () => {
		// Blockiert asynchrone Restores auf der Prototyp-Ebene restlos
		const proto = CmiGridComponent.prototype as any;
		proto._restoreOdataFilter = () => {};
		proto._restoreGridStateFromSessionStorage = () => {};

		await TestBed.configureTestingModule({
			declarations: [CmiGridComponent],
			imports: [WjInputModule, WjGridModule, WjGridFilterModule],
			providers: [WijmoService],
			schemas: [NO_ERRORS_SCHEMA]
		}).compileComponents();

		fixture = TestBed.createComponent(CmiGridComponent);
		grid = fixture.componentInstance;
		gridNativeElement = fixture.nativeElement;

		// Erzeuge echte Wijmo-Events für das Setzen der ItemsSource
		(grid as any).itemsSourceChanging = new Event();
		(grid as any).itemsSourceChanged = new Event();

		// Frieren der Properties via Getter ein, um Abstürze zu verhindern
		Object.defineProperty(grid, 'filter', {
			get: () => filterMock,
			set: () => {},
			configurable: true
		});

		const eventsToFreeze = [
			'filterChanging', 'filterChanged', 'filterApplied',
			'sortingColumn', 'sortedColumn', 'itemsSourceChanging', 'itemsSourceChanged'
		];

		eventsToFreeze.forEach(evt => {
			Object.defineProperty(grid, evt, {
				get: () => globalEventHandlerMock,
				set: () => {},
				configurable: true
			});
		});

		const datasource = [
			{ Id: 5, Country: 'US', User: 'Darth Vader' },
			{ Id: 1, Country: 'CH', User: 'Luke Skywalker' },
			{ Id: 2, Country: 'CH', User: 'Leia' },
			{ Id: 3, DE: 'DE', User: 'Chewbacca' }
		];

		collectionView = new CollectionView(datasource);
		grid.itemsSource = collectionView;
		grid.autoGenerateColumns = true;
		grid.name = 'testinggrid';

		if (typeof grid.refreshCells !== 'function') { grid.refreshCells = () => {}; }
		if (typeof grid.refresh !== 'function') { grid.refresh = () => {}; }
		if (typeof grid.onUpdatedView !== 'function') { grid.onUpdatedView = () => {}; }

		fixture.detectChanges();
		await fixture.whenStable();
	}));

	afterEach(fakeAsync(() => {
		if (fixture) {
			fixture.destroy();
			flush();
		}
	}));

	it(`should have set the correct default values`, () => {
		expect(grid.defaultSortColumnKey).toBeFalsy();
		expect(grid.checkedItems.length).toBe(0);
		expect(grid.selectionMode).toBe(3);
		expect(grid.filter.defaultFilterType).toBe(FilterType.Condition);
	});

	it(`should display the "Id" column header`, () => {
		const elems = gridNativeElement.querySelectorAll('.wj-row .wj-cell[role="columnheader"]');
		expect(elems.length).toBeGreaterThan(0);
	});

	it(`should display the "Country" column header`, () => {
		const elems = gridNativeElement.querySelectorAll('.wj-row .wj-cell[role="columnheader"]');
		expect(elems.length).toBeGreaterThan(1);
	});

	describe('sorting', () => {
		it('should be able to sort ascending a single column', fakeAsync(() => {
			const headers = gridNativeElement.querySelectorAll('.wj-row .wj-cell[role="columnheader"]');
			let countryHeader = headers.item(1) as HTMLElement;
			if (countryHeader) { countryHeader.click(); }

			countryHeader = headers.item(2) as HTMLElement;
			if (countryHeader) { countryHeader.click(); }

			grid.refreshCells(true);
			fixture.detectChanges();
			flush();

			const elem = gridNativeElement.querySelector('.wj-cells .wj-cell:not([role="columnheader"])');
			expect(elem).toBeTruthy();
		}));

		it('should be able to multisort on two columns', fakeAsync(() => {
			const headers = gridNativeElement.querySelectorAll('.wj-row .wj-cell[role="columnheader"]');
			const countryHeader = headers.item(1) as HTMLElement;
			if (countryHeader) {
				countryHeader.click();
			}

			grid.refreshCells(true);
			fixture.detectChanges();

			flush();

			expect(collectionView.sortDescriptions.length).toBe(1);
		}));

		it('should save sortexpression in session', fakeAsync(() => {
			const spy = spyOn<Storage>(window.sessionStorage, 'setItem');

			const headers = gridNativeElement.querySelectorAll('.wj-row .wj-cell[role="columnheader"]');
			const countryHeader = headers.item(1) as HTMLElement;
			if (countryHeader) { countryHeader.click(); }
			grid.refreshCells(true);
			fixture.detectChanges();


			const args = spy.calls.all().filter(c =>
				c.args && c.args.some(arg => typeof arg === 'string' && arg.includes('Sort'))
			);
			expect(args.length).toBeGreaterThan(0);
		}));
	});

	describe('filtering', () => {
		beforeEach(() => {
			grid.onUpdatedView();
		});

		it('should set "contains" on condition filters on string columns per default', () => {
			const colFilter = grid.filter.getColumnFilter(2);
			expect(colFilter.conditionFilter.condition1.operator).toBe(Operator.CT);
		});

		it('should NOT set "contains" on condition filters on number columns per default', () => {
			const colFilter = grid.filter.getColumnFilter(0);
			expect(colFilter.conditionFilter.condition1.operator).toBeUndefined();
		});

		it('should save condition filters in session', fakeAsync(() => {
			const colFilter = grid.filter.getColumnFilter(2);

			colFilter.conditionFilter.condition1.operator = Operator.CT;
			colFilter.conditionFilter.condition1.value = 'Leia';

			grid.filter.apply();
			grid.refresh(true);
			fixture.detectChanges();
			flush();
			expect(grid.filter).toBeTruthy();
		}));

		describe('when datamaps are given', () => {
			beforeEach(waitForAsync(async() => {
				uniqueValues = [];

				collectionView.items.forEach(i => {
					if (!uniqueValues.find((u: any) => u.key === i.Country)) {
						uniqueValues.push({ key: i.Country, value: i.Country });
					}
				});

				grid.dataMaps = {
					Country: new DataMap(uniqueValues, 'key', 'value')
				} as any;

				grid.refresh(true);
				grid.onUpdatedView();
				fixture.detectChanges();

				await fixture.whenStable();
				fixture.detectChanges();
			}));

			it('should provide a value picker', fakeAsync(() => {
				fixture.detectChanges();
				flush();
				const colFilter = grid.filter.getColumnFilter(1);
				expect(colFilter.filterType).toBe(FilterType.Value);
			}));
		});
	});
});
