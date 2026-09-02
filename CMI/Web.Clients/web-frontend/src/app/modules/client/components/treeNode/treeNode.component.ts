import {
	AfterViewInit,
	Component,
	EventEmitter,
	Input,
	OnInit,
	Output,
	ViewEncapsulation
} from '@angular/core';
import {EntityService} from '../../services';
import {TranslationService} from '@cmi/viaduc-web-core';
import {Router} from '@angular/router';
import {UrlService} from '../../services/url.service';

@Component({
    selector: 'cmi-viaduc-tree-node',
    templateUrl: 'treeNode.component.html',
    styleUrls: ['./treeNode.component.less'],
    encapsulation: ViewEncapsulation.None,
    standalone: false
})
export class TreeNodeComponent implements OnInit, AfterViewInit {
	@Input()
	public nodesToLoad: string[];
	@Output()
	public loadingChange: EventEmitter<boolean> = new EventEmitter<boolean>();
	public rootNode = '';
	public isExpanded: boolean;

	private get _toExpand(): boolean {
		return this.nodesToLoad.length > 1;
	}

	constructor(private _entityService: EntityService,
				private _router: Router,
				private _url: UrlService,
				private _txt: TranslationService) {
	}

	public async getRootNode(id: string) {
		if (id && id !== undefined && id !== '') {
			this.rootNode = await this._entityService.getArchivplanHtml(id);
		}
	}

	public async getNodesAsync() {
		for (const id of this.nodesToLoad) {
			await this.loadOrCollapseNode(id);
		}
		if (this._toExpand === true) {
			const nodes = this.nodesToLoad;
			const elem = (document.getElementById(nodes.reverse()[0]) || <any>{});
			const parent = elem.parentElement || {};
			const grandparent = parent.parentElement;

			if (grandparent) {
				grandparent.className += ' highlighted';
				elem.scrollIntoView();
			}
		}
	}

	public async loadOrCollapseNode(id: string) {
		
		if (!id || id.trim().length === 0) {
		return;
		}

		// Try fast path
		let expandElem: HTMLElement | null = document.getElementById(id);
		let childElem: HTMLElement | null = document.getElementById('children' + id);
		const notOnlineRecherchableDossiersElem =
			document.getElementById('notOnlineRecherchableVe' + id);

		// Fallback: use findTreeNode (handles mixed IDs, scopeKey, docKey)
		if (!expandElem || !childElem) {
			const { expand, child } = this.findTreeNode(id);
			expandElem = expand;
			childElem = child;
		}

		if (expandElem && childElem) {
			if (expandElem.classList.contains('icon--greater')) {
			// Collapsed → expand
			await this._expandNode(expandElem, childElem, notOnlineRecherchableDossiersElem, id);
			} else if (expandElem.classList.contains('icon--root')) {
			// Expanded → collapse
			await this._closeNode(expandElem, childElem, notOnlineRecherchableDossiersElem);
			}
		} else {
			console.warn('Node not found for id:', id);
		}
	}


	private async _expandNode(expandElem: HTMLElement, childElem: HTMLElement, notOnlineRecherchableDossiersElem: HTMLElement, id: string) {
		this.loadingChange.emit(true);
		expandElem.className = 'tree-collapse icon icon--before icon--root';
		expandElem.setAttribute('aria-label', this._txt.translate('einklappen', 'archivplan.treeNode.collapse'));
		childElem.innerHTML =  await this._entityService.getChildHtml(id);
		if (notOnlineRecherchableDossiersElem) {
			notOnlineRecherchableDossiersElem.style.cssText = '';
		}
		this.loadingChange.emit(false);
		this.isExpanded = true;
	}

	private findTreeNode(value: string, className = 'tree-node-children'): { expand: HTMLElement | null, child: HTMLElement | null } {
		const v = value.trim();

		// Select by scopeKey or docKey
		const selector = `.${className}[data-scopekey="${v}"], .${className}[data-dockey="${v}"]`;
		const candidates = Array.from(document.querySelectorAll<HTMLElement>(selector));

		let child: HTMLElement | null = null;

		// Prefer scopeKey
		for (const c of candidates) {
			if (c.getAttribute('data-scopekey') === v) {
				child = c;
				break;
			}
		}

		// Fallback: first candidate
		if (!child && candidates.length > 0) {
			child = candidates[0];
		}

		// Extra fallback: children{v} ID
		if (!child) {
			child = document.getElementById(`children${v}`);
		}

		// Find opener <a.tree-collapse>
		let expand: HTMLElement | null = null;
		if (child) {
			const parentLi = child.previousElementSibling as HTMLElement | null;
			if (parentLi) {
				const innerUl = parentLi.querySelector('ul[id^="Node"]');
				if (innerUl) {
					expand =
						innerUl.querySelector(`a[id="${v}"]`) as HTMLElement | null ||
						innerUl.querySelector('a.tree-collapse') as HTMLElement | null;
				}
			}
		}

		if (!expand) {
			expand = document.getElementById(v);
		}

		return { expand, child };
	}
		
	private async _closeNode(expandElem: HTMLElement, childElem: HTMLElement, notOnlineRecherchableDossiersElem: HTMLElement) {
		expandElem.className = 'tree-collapse icon icon--before icon--greater';
		expandElem.setAttribute('aria-label', this._txt.translate('aufklappen', 'archivplan.treeNode.expand'));
		childElem.innerHTML = '';
		if (notOnlineRecherchableDossiersElem) {
			notOnlineRecherchableDossiersElem.style.cssText = 'display:none';
		}
		this.isExpanded = false;
	}

	public ngOnInit(): void {
		if (!(this.nodesToLoad && this.nodesToLoad.length > 0)) {
			this._entityService.getArchivplanRootNodes().then(r => {
				this.nodesToLoad = r;
				// Even though there could be more than one root node, we load only the first one returned
				this.getRootNode(this.nodesToLoad[0]).then(() => {
					if (this.nodesToLoad.length > 0) {
						setTimeout(() => this.getNodesAsync(), 1);
					}
				});
			});
		}
	}

	public ngAfterViewInit(): void {
		this.getRootNode(this.nodesToLoad[0]).then(() => {
			if (this.nodesToLoad.length > 0) {
				setTimeout(() => this.getNodesAsync(), 1);
			}
		});
	}

	public async innerHtmlClicked($event: any) {
		let id = $event.target.id.toString();
		if (id.startsWith('Node')) {
			id = id.slice(4);
		}
		this.loadOrCollapseNode(id);
	}

	public goToDetailPage($event: any) {
		let id = $event.target.id.toString();
		if (id.startsWith('Node')) {
			id = id.slice(4);
		}
		const url = this._url.getDetailUrl(id);
		this._router.navigateByUrl(url);
	}
}
