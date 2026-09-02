import { ViewContainerRef, Injectable, ComponentRef, Injector } from '@angular/core';
import {Observable, Subject, ReplaySubject, throwError, of} from 'rxjs';
import {ConfirmationModalComponent} from '../dialogs/confirmation.modal.component';
import {BasicModalComponent} from '../dialogs/basic.modal.component';
import {CanDeactivateData} from '../../../../model';

@Injectable({
	providedIn: 'root'
})
export class ModalService {
	private viewContainerRef: ViewContainerRef;
	public activeInstances: number;
	public activeInstances$: Subject<number> = new Subject();
	public modalRef: ComponentRef<any>[] = [];

	constructor(protected injector: Injector) {
	}

	public RegisterContainerRef(vcRef: ViewContainerRef) {
		this.viewContainerRef = vcRef;
	}

	public openDialog(parameters?: CanDeactivateData): Observable<ComponentRef<ConfirmationModalComponent>> {
		if (!this.viewContainerRef) {
			alert('CanDeactivateGuard benötigt cmi-viaduc-modal-service-container Control');
			return throwError(() => new Error('Missing ViewContainerRef'));
		}

		const componentRef =
			this.viewContainerRef.createComponent(ConfirmationModalComponent, {
				injector: this.injector
			});

		const instance = componentRef.instance;

		instance.candeactive = parameters as CanDeactivateData;
		instance.content = parameters?.content ?? '';
		instance.noButtonText = parameters?.noButtonText ?? '';
		instance.yesButtonText = parameters?.yesButtonText ?? '';

		this.open(componentRef);
		return of(componentRef);
	}

	public openMessage( parameters?: CanDeactivateData) : Observable<ComponentRef<BasicModalComponent>> {
		if (!this.viewContainerRef) {
			alert('CanDeactivateGuard benötigt cmi-viaduc-modal-service-container Control');
			return throwError(() => new Error('Missing ViewContainerRef'));
		}

		const componentRef = this.viewContainerRef.createComponent(BasicModalComponent, {injector: this.injector});
		componentRef.instance.candeactive = parameters  as CanDeactivateData;
		componentRef.instance.content =  (parameters  as CanDeactivateData).content;
		componentRef.instance.closeButtonText = (parameters  as CanDeactivateData).closeButtonText;

		this.open(componentRef);
		return of(componentRef);
	}

	private open<T>(componentRef: ComponentRef<T>)  {
		this.viewContainerRef.insert(componentRef.hostView);
		const componentRef$ = new ReplaySubject();
		this.viewContainerRef.insert(componentRef.hostView);
		this.activeInstances++;
		this.activeInstances$.next(this.activeInstances);
		(componentRef.instance as any)['componentIndex'] = this.activeInstances;
		(componentRef.instance as any)['destroy'] = () => {
			this.activeInstances--;
			this.activeInstances = Math.max(this.activeInstances, 0);

			const idx = this.modalRef.indexOf(componentRef);
			if (idx > -1) {
				this.modalRef.splice(idx, 1);
			}
			this.activeInstances$.next(this.activeInstances);
			componentRef.destroy();

			this.modalRef.push(componentRef);
			componentRef$.next(componentRef);
			componentRef$.complete();
		};
	}
}
