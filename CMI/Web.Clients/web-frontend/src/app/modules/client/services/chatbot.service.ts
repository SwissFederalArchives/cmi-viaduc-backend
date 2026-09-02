import { Injectable, NgZone } from '@angular/core';
import { ChatbotApi } from '../model';

@Injectable({ providedIn: 'root' })
export class ChatbotService {
	constructor(private zone: NgZone) {}

	open(): void {
		this.whenReady(bot => bot.openChatWindow());
	}

	close(): void {
		this.whenReady(bot => bot.closeChatWindow());
	}

	toggle(): void {
		this.whenReady(bot => bot.toggleChatWindow());
	}

	private whenReady(
		action: (bot: ChatbotApi) => void,
		timeoutMs = 5000,
	): void {
		const bot = window.chatbot;
		if (bot?.isChatbotLoaded()) {
			action(bot);
			return;
		}

		// Ausserhalb der Angular-Zone, damit das Polling keine
		// dauernde Change Detection auslöst.
		this.zone.runOutsideAngular(() => {
			const start = Date.now();
			const timer = setInterval(() => {
				const b = window.chatbot;
				if (b?.isChatbotLoaded()) {
					clearInterval(timer);
					action(b);
				} else if (Date.now() - start > timeoutMs) {
					clearInterval(timer);
					console.warn('Chatbot wurde nicht rechtzeitig geladen.');
				}
			}, 200);
		});
	}
}
