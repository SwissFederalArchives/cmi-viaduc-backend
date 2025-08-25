import {PaginationComponent} from './pagination.component';
import {EventEmitter} from '@angular/core';
import {ConfigService, Paging, UserUiSettings} from '@cmi/viaduc-web-core';

describe('PaginationComponent', () => {

	let pagination: PaginationComponent;
	let configService: ConfigService;

	beforeEach(() => {
		let settings: UserUiSettings = <UserUiSettings>{pagingSize: 10};

		configService = <ConfigService> {
			getUserSettings: () => {
				return settings;
			},
			getValidPagingSize: () => {
				return settings.pagingSize;
			},
			getSettings: () => {
				return {};
			},
			getSetting(key: string) {
				return {};
			}
		};

		pagination = new PaginationComponent(configService);
		pagination.paging = <Paging> {skip: 0, take: 10, total: 19};

		pagination.onPaged = <EventEmitter<Paging> > {
			emit: p => {
				console.log('emitted!', p);
			}
		};

		pagination.ngOnInit();
	});

	it('onPaged emitter to have been called on setPageIndex', () => {
		pagination.paging = <Paging> {skip: 0, take: 10, total: 19};
		pagination.ngOnChanges(null);
		spyOn(pagination.onPaged, 'emit');

		pagination.setPageIndex(1);

		expect(pagination.onPaged.emit).toHaveBeenCalled();
	});

	it('Page index 1 should be navigable', () => {
		const result = pagination.pageIndexIsNavigable(1);
		expect(result).toBe(true);
	});

	it('Page index 2 must not be navigable', () => {
		const result = pagination.pageIndexIsNavigable(2);
		expect(result).not.toBe(true);
	});

	it('Page index 3 must not be navigable', () => {
		const result = pagination.pageIndexIsNavigable(3);
		expect(result).not.toBe(true);
	});

	it('Page index less than 0 must not be navigable', () => {
		const result = pagination.pageIndexIsNavigable(-1);
		expect(result).not.toBe(true);
	});

});
