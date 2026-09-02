import {Component, OnInit, OnDestroy, Renderer2, Inject} from '@angular/core';
import {DOCUMENT} from '@angular/common';

/*
 * ============================================================
 *  bubble-chat.ch Widget — Konfigurationsübersicht
 *  (aus bundle.js extrahiert; kann sich bei Vendor-Updates ändern)
 * ============================================================
 *
 *  --- Script-Tag Attribute -----------------------------------
 *
 *  id="chatbot"           PFLICHT. Das Bundle sucht sich selbst
 *                         über `script#chatbot`. Ohne diese id
 *                         startet nichts.
 *
 *  src="...bundle.js"     PFLICHT. URL zum Bundle.
 *
 *  data-server            Voll qualifizierte URL zum Chatbot-Server,
 *                         z.B. https://ch-bar.bubble-chat.ch/chatbot
 *                         Fällt auf `src` ohne "/embed/bundle.js"
 *                         zurück, wenn weggelassen.
 *
 *  defer                  Empfohlen (Standard-HTML-Attribut).
 *
 *  data-expand-at-start   Öffnet das Fenster automatisch beim Laden.
 *                         Werte: "all" | "desktop" | "mobile"
 *                           all      -> immer
 *                           desktop  -> nur ab Breite >= 768px
 *                           mobile   -> nur bei Breite < 768px
 *                         Weglassen  -> kein Auto-Öffnen.
 *                         Respektiert das Cookie: hat der Nutzer
 *                         vorher geschlossen, bleibt es zu.
 *
 *  data-link-handling     Verhalten bei Links im Chat.
 *                         Werte: "default" | "new-tab" | "existing-tab"
 *                           default      -> gleicher Tab bei gleicher
 *                                           Domain, sonst neuer Tab
 *                           new-tab      -> immer neuer Tab
 *                           existing-tab -> immer gleicher Tab
 *                         (Auf Mobile < 768px schliesst sich das
 *                          Chatfenster beim Klick automatisch.)
 *
 *  data-fab-visible       "false" blendet den Floating-Button (FAB)
 *                         aus. Default: sichtbar.
 *                         ACHTUNG: Ohne FAB lässt sich der Chat NUR
 *                         noch per window.chatbot.openChatWindow()
 *                         öffnen.
 *
 *
 *  --- CSS-Variablen (Breakpoint: 768px) ----------------------
 *  Auf einem umschliessenden Element bzw. :root setzen.
 *
 *  Fenster:
 *    --window-width            (Default 30em)
 *    --window-height           (Default 50em)
 *    --window-max-height       (Default calc(100vh - 2em))
 *    --window-border-radius    (Default 8px)
 *    --window-top / -right / -bottom / -left
 *                              (Default auto / 1em / 1em / auto)
 *    --body-background-color   (Default white)
 *
 *  Floating-Button (FAB):
 *    --fab-size                (Default 6em Desktop / 5em Mobile)
 *    --fab-width / --fab-height (überschreiben --fab-size einzeln)
 *    --fab-top / -right / -bottom / -left
 *                              (Default auto / 1.5em / 1.5em / auto)
 *    Mobile-Varianten: jeweils --fab-*-mobile
 *                              (z.B. --fab-right-mobile)
 *
 *
 *  --- Weiteres -----------------------------------------------
 *  Das Widget lädt zusätzlich {data-server}/custom.css für
 *  serverseitiges Custom-Styling.
 *
 *  Öffentliche API auf window.chatbot:
 *    openChatWindow()  closeChatWindow()  toggleChatWindow()
 *    restartChat()     enterGuestMessage(text)  triggerStory(story)
 *    isChatbotLoaded() subscribe('ON_CHAT_WINDOW_STATE_CHANGE', cb)
 * ============================================================
 */

@Component({
	selector: 'cmi-chatbot',
	template: '', // Bundle injiziert seinen eigenen Container
	styleUrl: './chatbot.component.less',
	standalone: false
})
export class ChatbotComponent implements OnInit, OnDestroy {
	private script?: HTMLScriptElement;

	constructor(
		private renderer: Renderer2,
		@Inject(DOCUMENT) private document: Document
	) {
	}

	ngOnInit(): void {
		if (this.document.getElementById('chatbot')) {
			return; // schon geladen, kein Duplikat
		}

		const script = this.renderer.createElement('script') as HTMLScriptElement;
		script.id = 'chatbot';
		script.setAttribute('data-server', 'https://ch-bar.bubble-chat.ch/chatbot');
		script.src = 'https://ch-bar.bubble-chat.ch/chatbot/embed/bundle.js';
		script.defer = true;

		this.renderer.appendChild(this.document.body, script);
		this.script = script;
	}

	ngOnDestroy(): void {
		if (this.script) {
			this.renderer.removeChild(this.document.body, this.script);
		}
	}
}
