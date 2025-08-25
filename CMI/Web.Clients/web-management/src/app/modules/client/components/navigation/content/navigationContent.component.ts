import {AfterViewInit, Component, ElementRef, EventEmitter, Output} from '@angular/core';
import {NavigationStart, Router} from '@angular/router';
import {AuthenticationService} from '../../../services/index';
import {ClientContext, Utilities as _util} from '@cmi/viaduc-web-core';

@Component({
	selector: 'cmi-viaduc-nav-content',
	templateUrl: 'navigationContent.component.html',
	styleUrls: ['./navigationContent.component.less']
})
export class NavigationContentComponent implements AfterViewInit {
	private _elem: any;
	public mobileMainNavOpen = false;
	public mobileUserNavOpen = false;

	constructor(private _context: ClientContext,
				private _elemRef: ElementRef,
				private _authentication: AuthenticationService,
				private _router: Router) {
		this._elem = this._elemRef.nativeElement;

		this._router.events.subscribe(event => {
			if (event instanceof NavigationStart) {

				if (this.mobileMainNavOpen) {
					this.toggleMainMobileNav();
				}

				if (this.mobileUserNavOpen) {
					this.toggleUserMobileNav();
				}

			}
		});
	}

	@Output()
	public onMobileNavOpen: EventEmitter<boolean> = new EventEmitter<boolean>();

	public ngAfterViewInit(): void {
		_util.initJQForElement(this._elem);
	}

	public get language(): string {
		return this._context.language;
	}

	public toggleMainMobileNav(): void {
		this.mobileMainNavOpen = !this.mobileMainNavOpen;
		if (this.mobileUserNavOpen) {
			this.mobileUserNavOpen = false;
			this.onMobileNavOpen.emit(this.mobileUserNavOpen);
		}

		this.onMobileNavOpen.emit(this.mobileMainNavOpen);
	}

	public getMainMobileNavCss(): string {
		return (this.mobileMainNavOpen || this.mobileUserNavOpen) ? 'nav-mobile nav-open' : 'nav-mobile nav';
	}

	public getMainTableNavCss(): string {
		return this.mobileMainNavOpen ? 'table-row nav-open' : 'table-row nav';
	}

	public getMainTableCellNavCss(): string {
		return this.mobileMainNavOpen ? 'table-cell dropdown open' : 'table-cell dropdown';
	}

	public toggleUserMobileNav(): void {
		this.mobileUserNavOpen = !this.mobileUserNavOpen;
		if (this.mobileMainNavOpen) {
			this.mobileMainNavOpen = false;
			this.onMobileNavOpen.emit(this.mobileMainNavOpen);
		}

		this.onMobileNavOpen.emit(this.mobileUserNavOpen);
	}

	public getUserTableCellNavCss(): string {
		return this.mobileUserNavOpen ? 'table-cell dropdown open' : 'table-cell dropdown';
	}

	public get drillDownCointainerHeight(): string {
		const dropdownHeight = (window.screen.height) - 91;
		return (this.mobileUserNavOpen || this.mobileMainNavOpen) ? dropdownHeight + 'px' : 'auto';
	}

	public get authenticated(): boolean {
		return this._context.authenticated;
	}

	public login(): void {
		this._authentication.login();
	}

	public logout(): void {
		this._authentication.logout();
	}

	public nullifyClick(event: any): void {
		const senderElementName = event.target.tagName.toLowerCase();
		if (senderElementName !== 'a') {
			event.stopPropagation();
		}
	}
}
