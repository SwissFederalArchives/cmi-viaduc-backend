import {Injectable} from '@angular/core';
import {Abbruchgrund, ApproveStatus, CoreOptions, EntscheidGesuchStatus, HttpService} from '@cmi/viaduc-web-core';
import {Observable} from 'rxjs';
import {EinsichtsGesuchEmailVorlage, Bestellhistorie, OrderingFlatDetailItem, PagingRequestParameter} from '../model';
import {OrderingFlatItem} from '../model/orderingFlatItem';
import {ResultSet} from '../model/resultSet';
import {DigipoolEntry} from '../model/digipoolEntry';

@Injectable()
export class OrderService {
	private _orderApiUrl: string;

	constructor(private _http: HttpService, private _options: CoreOptions) {
		this._orderApiUrl = this._options.serverUrl + this._options.publicPort + '/api/Order';
	}

	public getAllOrders(queryParams: PagingRequestParameter): Observable<ResultSet<OrderingFlatItem>> {
		return this._http.post<ResultSet<OrderingFlatItem>>(this._orderApiUrl + '/getOrderings', queryParams);
	}

	public getUnHiddenOrderingDetail(id: number): Observable<OrderingFlatDetailItem> {
		return this._http.get<OrderingFlatDetailItem>(this._orderApiUrl + '/NichtSichtbarEinsehen?id=' + id);
	}

	public getAuftragOrderingDetailFields(): Observable<any[]> {
		return this._http.get<any[]>(this._orderApiUrl + '/GetAuftragOrderingDetailFields');
	}

	public getEinsichtsgesuchOrderingDetailFields(): Observable<any[]> {
		return this._http.get<any[]>(this._orderApiUrl + '/GetEinsichtsgesuchOrderingDetailFields');
	}

	public getOrderingDetail(id: number): Observable<OrderingFlatDetailItem> {
		return this._http.get<OrderingFlatDetailItem>(this._orderApiUrl + '/GetOrderingDetailItem?id=' + id);
	}

	public auftragUpdateOrderingDetail(item: OrderingFlatItem) {
		return this._http.post(this._orderApiUrl + '/AuftragUpdateOrderItem', item);
	}

	public einsichtsgesuchUpdateOrderingDetail(item: OrderingFlatItem) {
		return this._http.post(this._orderApiUrl + '/EinsichtsgesuchUpdateOrderItem', item);
	}

	public getAllEinsichtsgesuche(): any[] {
		return [];
	}

	public getOrderingHistoryForVe(id: number): Observable<Bestellhistorie[]> {
		return this._http.get<Bestellhistorie[]>(this._orderApiUrl + '/GetOrderingHistoryForVe?id=' + id);
	}

	public getAushebungsAuftragHtml(ids: number[]): Observable<string> {
		const queryParams = ids.map(i => i).join('&orderItemIds=');
		return this._http.get<string>(this._orderApiUrl + '/GetAushebungsauftraegeHtml?orderItemIds=' + queryParams);
	}

	public getVersandkontrolleHtml(ids: number[]): Observable<string> {
		const queryParams = ids.map(i => i).join('&orderItemIds=');
		return this._http.get<string>(this._orderApiUrl + '/GetVersandkontrolleHtml?orderItemIds=' + queryParams);
	}

	public getDigipool(): Observable<DigipoolEntry[]> {
		return this._http.get<DigipoolEntry[]>(this._orderApiUrl + '/GetDigiPool');
	}

	public auftraegeEntscheidFreigabeHinterlegen(ids: number[], status: ApproveStatus, bewilligung?: Date, interneBemerkung?: string) {
		const postbody = {
			orderItemIds: ids,
			entscheid: status,
			datumBewilligung: bewilligung,
			interneBemerkung: interneBemerkung
		};
		return this._http.post<void>(this._orderApiUrl + '/AuftraegeEntscheidFreigabeHinterlegen', postbody);
	}

	public zuruecksetzen(ids: number[]) {
		const postbody = {
			orderItemIds: ids,
		};
		return this._http.post<void>(this._orderApiUrl + '/Zuruecksetzen', postbody);
	}

	public auftraegeReponieren(ids: number[]) {
		const postbody = {
			orderItemIds: ids,
		};
		return this._http.post<void>(this._orderApiUrl + '/ZumReponierenBereit', postbody);
	}

	public auftraegeAbschliessen(ids: number[]) {
		const postbody = {
			orderItemIds: ids,
		};
		return this._http.post<void>(this._orderApiUrl + '/AuftraegeAbschliessen', postbody);
	}

	public digitalisierungAusloesen(ids: number[]) {
		const postbody = {
			orderItemIds: ids,
		};
		return this._http.post<void>(this._orderApiUrl + '/DigitalisierungAusloesen', postbody);
	}

	public abbrechen(ids: number[], abbruchgrund: Abbruchgrund, bemerkungZumDossier: string, interneBemerkung: string) {
		const postbody = {
			orderItemIds: ids,
			abbruchgrund: abbruchgrund,
			bemerkungZumDossier: bemerkungZumDossier,
			interneBemerkung: interneBemerkung
		};
		return this._http.post<void>(this._orderApiUrl + '/Abbrechen', postbody);
	}

	public inVorlageExportieren(ids: number[], vorlage: EinsichtsGesuchEmailVorlage, sprache: string) {
		const postbody = {
			orderItemIds: ids,
			vorlage: vorlage,
			sprache: sprache
		};
		return this._http.post<void>(this._orderApiUrl + '/EinsichtsgesucheInVorlageExportieren', postbody);
	}

	public auftraegeAusleihen(ids: number[]) {
		const postbody = {
			orderItemIds: ids,
		};
		return this._http.post<void>(this._orderApiUrl + '/AuftraegeAusleihen', postbody);
	}

	public einsichtsgesucheEntscheidFuerGesucheHinterlegen(ids: number[], status: EntscheidGesuchStatus, bewilligung?: Date, interneBemerkung?: string) {
		const postbody = {
			orderItemIds: ids,
			entscheid: status,
			datumEntscheid: bewilligung,
			interneBemerkung: interneBemerkung
		};
		return this._http.post<void>(this._orderApiUrl + '/EinsichtsgesucheEntscheidGesuchHinterlegen', postbody);
	}

	public updateDigiPool(ids: number[], digitalisierungskategorie: number, terminDigitalisierungDatum: string, terminDigitalisierungZeit: string): Observable<any> {
		return this._http.post(this._orderApiUrl + '/UpdateDigiPool',
			{
				OrderItemIds: ids,
				DigitalisierungsKategorie: digitalisierungskategorie,
				TerminDigitalisierungDatum: terminDigitalisierungDatum,
				TerminDigitalisierungZeit: terminDigitalisierungZeit
				});
	}

	public resetAufbereitungsfehler(ids: number[]): Observable<any> {
		return this._http.post(this._orderApiUrl + '/ResetAufbereitungsfehler',
			{
				OrderItemIds: ids,
			});
	}

	public downloadGebrauchskopie(orderItemId: number): Observable<any> {
		return this._http.download(this._orderApiUrl + `/DownloadFile?orderItemId=${orderItemId}`);
	}

	public auftraegeMahnungVersenden(ids: number[], gewaehlteMahnungAnzahl: number, language: string): Observable<any> {
		const postbody = {
			orderItemIds: ids,
			gewaehlteMahnungAnzahl: gewaehlteMahnungAnzahl,
			language: language
		};
		return this._http.post<void>(this._orderApiUrl + '/MahnungVersenden', postbody);
	}

	public auftraegeErinnerungVersenden(ids: number[]): Observable<any> {
		const postbody = {
			orderItemIds: ids
		};
		return this._http.post<void>(this._orderApiUrl + '/ErinnerungVersenden', postbody);
	}
}
