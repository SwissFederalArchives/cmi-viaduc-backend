/*
Erstens, subscribe hat kein Gegenstück zum Abmelden. [Certain] Im Bundle macht die Funktion nur Vr.push(callback) —
es gibt keine unsubscribe-Methode. Registriere den State-Change-Listener also genau einmal (z.B. im APP_INITIALIZER
oder in einem Root-Service-Konstruktor), nicht pro Komponente, sonst sammelst du Leaks an.

Zweitens, wenn du den Fensterzustand ins Template bindest, kommt der Wert aus einem Fremd-Callback ausserhalb
der Zone — Angular bekommt die Änderung dann nicht mit. Zurück in die Zone holen:

this.zone.runOutsideAngular(() => {
  window.chatbot?.subscribe('ON_CHAT_WINDOW_STATE_CHANGE', (open) => {
    this.zone.run(() => {
      this.isOpen = open; // jetzt sieht die Change Detection es
    });
  });
});

Für reines Öffnen/Schliessen per Button brauchst du das subscribe nicht —
nur wenn du selbst auf den Zustand reagieren willst.
 */

// chatbot.types.ts
export interface ChatbotApi {
	openChatWindow(): void;
	closeChatWindow(): void;
	toggleChatWindow(): void;
	restartChat(): void;
	enterGuestMessage(text: string): void;
	triggerStory(story: string): void;
	isChatbotLoaded(): boolean;
	subscribe(
		event: 'ON_CHAT_WINDOW_STATE_CHANGE',
		callback: (open: boolean) => void,
	): void;
}

declare global {
	interface Window {
		chatbot?: ChatbotApi;
	}
}

