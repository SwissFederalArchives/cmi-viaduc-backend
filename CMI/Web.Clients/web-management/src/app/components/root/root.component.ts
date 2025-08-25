import { AfterViewInit, Component, OnInit, Renderer2 } from '@angular/core';
import {ClientContext, ConfigService, PreloadService} from '@cmi/viaduc-web-core';
import {ContextService} from '../../modules/client/services/context.service';

@Component({
	selector: 'cmi-viaduc-root',
	templateUrl: 'root.component.html',
	styleUrls: ['./root.component.less']
})
export class RootComponent implements OnInit, AfterViewInit {

	public preloading = true;

	constructor(private _context: ClientContext,
				private _contextService: ContextService,
				private _config: ConfigService,
				private _preloadService: PreloadService,
				private _renderer: Renderer2) {
	}

	public ngOnInit(): void {

		// eslint-disable-next-line
		this._contextService.context.subscribe((ctx) => {});
		// eslint-disable-next-line
		this._preloadService.preloaded.subscribe((state) => {});

		this._preloadService.preload(this._context.language, false).then(() => {
			const version = this._config.getSetting('service.version');
			this._context.client.setVersion(version);
			this.preloading = false;
		});
	}

	public ngAfterViewInit(): void {
		this._renderer.removeClass(document.documentElement, 'cmi-boot');
	}
}
