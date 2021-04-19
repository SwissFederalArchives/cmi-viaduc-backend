<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:bar="http://bar.admin.ch/gebrauchskopie/v1" version="3.0"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map" xmlns:array="http://www.w3.org/2005/xpath-functions/array" xpath-default-namespace="http://bar.admin.ch/gebrauchskopie/v1"
    exclude-result-prefixes="#all">
    <xsl:output method="html" html-version="5.0" indent="yes" encoding="UTF-8" escape-uri-attributes="no"/>

    <xsl:param name="packageId" />
 
    <xsl:variable name="orderedUnit" >
        <xsl:choose>
            <xsl:when test="$packageId != '' and //dossier/zusatzDaten/merkmal[@name = 'Identifikation digitales Magazin'][text() = $packageId]/parent::node()/parent::node()">
                <xsl:copy-of  select="//dossier/zusatzDaten/merkmal[@name = 'Identifikation digitales Magazin'][text() = $packageId]/parent::node()/parent::node()"/>
            </xsl:when>
            <xsl:when test="$packageId != '' and //dokument/zusatzDaten/merkmal[@name = 'Identifikation digitales Magazin'][text() = $packageId]/parent::node()/parent::node()">
                <xsl:copy-of  select="//dokument/zusatzDaten/merkmal[@name = 'Identifikation digitales Magazin'][text() = $packageId]/parent::node()/parent::node()"/>
            </xsl:when>            
            <xsl:otherwise>
                <xsl:copy-of select="//ordnungssystemposition/dossier"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:variable>
    <xsl:variable name="allFiles" select="//datei"/>
    <xsl:variable name="dossierIcon">
        <xsl:value-of>{"icon":"glyphicon glyphicon-folder-open"}</xsl:value-of>
    </xsl:variable>
    <xsl:variable name="documentIcon">
        <xsl:value-of>{"icon":"glyphicon glyphicon-file"}</xsl:value-of>
    </xsl:variable>


    <xsl:template match="paket">
        <html lang="de">
            <head>
                <meta charset="utf-8"/>
                <meta http-equiv="x-ua-compatible" content="ie=edge"/>
                <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no"/>
                <meta name="theme-color" content="#DC0018"/>

                <link rel="stylesheet" href="./design/css/vendors.css"/>
                <link rel="stylesheet" href="./design/css/admin.css"/>
                <link rel="stylesheet" href="./design/css/print.css"/>
                <link rel="stylesheet" href="./design/css/cmi.css"/>
                <link rel="stylesheet" href="./design/jsTreeTheme/style.min.css"/>
                <title>Offline Viewer</title>
            </head>
            <body>
                <div class="container container-main">

                    <xsl:call-template name="renderHeader">
                        <xsl:with-param name="orderedItem" select="$orderedUnit/node()"/>
                    </xsl:call-template>

                    <xsl:call-template name="renderNavigation"/>

                    <div class="row root-content-wrapper">
                        <div class="col-xs-12">
                            <div id="content" class="container-fluid">

                                <xsl:call-template name="renderPageDossier">
                                    <xsl:with-param name="orderedItem" select="$orderedUnit/node()"/>
                                </xsl:call-template>

                                <xsl:call-template name="renderPageOrdnungssystem">
                                    <xsl:with-param name="ordnungssystem" select="ablieferung/ordnungssystem"/>
                                </xsl:call-template>

                                <xsl:call-template name="renderPageAblieferung">
                                    <xsl:with-param name="paket" select="//paket"/>
                                </xsl:call-template>

                            </div>
                        </div>
                    </div>
                    <xsl:call-template name="renderFooter"/>
                </div>


                <script src="./design/js/vendors.min.js"/>
                <script src="./design/js/main.min.js"/>
                <script src="./design/js/jstree.min.js"/>
                <script src="./design/js/CLDRPluralRuleParser.js"/>
                <script src="./design/js/jquery.i18n.js"/>
                <script src="./design/js/jquery.i18n.messagestore.js"/>
                <script src="./design/js/jquery.i18n.fallbacks.js"/>
                <script src="./design/js/jquery.i18n.language.js"/>
                <script src="./design/js/jquery.i18n.parser.js"/>
                <script src="./design/js/jquery.i18n.emitter.js"/>
                <script src="./design/js/jquery.i18n.emitter.bidi.js"/>
                <script src="./design/js/i18n/de.js"/>
                <script src="./design/js/i18n/fr.js"/>
                <script src="./design/js/i18n/it.js"/>
                <script src="./design/js/i18n/en.js"/>

                <script>
                    $( function()  {
                        initi18n();
                        pageSetup();
                        showPage('Dokumente');
                    });
                    
                    function initi18n() {
                        $.i18n().load(localeDe, 'de').done(function() {
                            setLocale('de');
                            });
                        $.i18n().load(localeFr, 'fr');
                        $.i18n().load(localeIt, 'it');
                        $.i18n().load(localeEn, 'en');
                    }
                    
                    function setLocale(locale) {
                        if (locale) {
                            $.i18n().locale = locale;
                        } else {
                            $.i18n().locale = 'de';
                        }
                        
                        $('[data-i18n]').each(function() {
                            $(this).i18n();
                        });
                        
                        $('html').attr('lang', locale);
                    }
                    
                    function pageSetup(){
                        var dropdownToggle = $('.nav-mobile-menu, .dropdown-toggle');
                        $('.menuItemDokumente').click(function() {
                            showPage('Dokumente');
                            dropdownToggle.click();
                        });
                        $('.menuItemOrdnungssystem').click(function() {
                            showPage('Ordnungssystem');
                            dropdownToggle.click();
                        });
                        $('.menuItemPaketinformation').click(function() {
                            showPage('Paketinformation');
                            dropdownToggle.click();
                        });
                        
                        <![CDATA[
                        $('#contentTree').on('changed.jstree', function (e, data) {
                            showMetadata(data.node.id);
                        })
                        .jstree({
                            "core" : {"multiple": false,
                                "themes" : {"dots" : false, "ellipsis": true },
                                "keyboard" : {
                                    "ctrl-o" : function(e) {
                                        e.preventDefault();
                                        e.type = 'dblclick';
                                        $(e.currentTarget).trigger(e);
                                    },
                                    "ctrl-*" : function (e) {
                        				// aria defines * on numpad as open_all - not very common
                        				// so we use ctrl-* to close
                        				this.close_all();
                        			}
                                }
                            }
                        });
                      
                        
                        $('#contentTree').on('dblclick','.jstree-anchor', function (e) {
                            var instance = $.jstree.reference(this),
                            node = instance.get_node(this);
                            var href = node.a_attr.href;
                            if (href && href !== '#') {
                                window.open(href, '_blank');
                            }
                        });
                        
                        ]]>
                    }
                    
                    function showMetadata(id) {
                        $("div[id^='metadata_']").hide();
                        $("div[id^='metadata_" + id + "']").show();
                    }
                    
                    function showPage(pageName) {
                        $('.pageDokumente').hide();
                        $('.pageOrdnungssystem').hide();
                        $('.pagePaketinformation').hide();
                        
                        $('.menuItemDokumente').removeClass('active');
                        $('.menuItemOrdnungssystem').removeClass('active');
                        $('.menuItemPaketinformation').removeClass('active');
                        
                        $('.page' + pageName).show();
                        $('.menuItem' + pageName).addClass('active');
                    }
                </script>
            </body>
        </html>
    </xsl:template>

    <xsl:template match="inhaltsverzeichnis"/>

    <xsl:template name="paketInfo">
        <xsl:param name="paket" required="yes"/>
        <div class="row ./design/img/">
            <div class="col-md-12">
                <h2 data-i18n="viaduc-title-paketinformationen">Paketinformationen</h2>
                <xsl:call-template name="renderDateTimeField">
                    <xsl:with-param name="label">Generiert am</xsl:with-param>
                    <xsl:with-param name="field" select="generierungsdatum"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Bestellinformationen</xsl:with-param>
                    <xsl:with-param name="field" select="bestellinformation"/>
                </xsl:call-template>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="ablieferung">
        <div class="row ./design/img/">
            <div class="col-md-12">
                <h2 data-i18n="viaduc-title-ablieferungsdetails">Ablieferungsdetails</h2>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Ablieferungsnummer</xsl:with-param>
                    <xsl:with-param name="field" select="ablieferungsnummer"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Ablieferungstyp</xsl:with-param>
                    <xsl:with-param name="field" select="ablieferungstyp"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Abliefernde Stelle</xsl:with-param>
                    <xsl:with-param name="field" select="ablieferndeStelle"/>
                </xsl:call-template>
                <xsl:call-template name="renderHistorischerZeitraumField">
                    <xsl:with-param name="label">Entstehungszeitraum</xsl:with-param>
                    <xsl:with-param name="field" select="entstehungszeitraum"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Bemerkung</xsl:with-param>
                    <xsl:with-param name="field" select="bemerkung"/>
                </xsl:call-template>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="dossier">
        <xsl:choose>
            <xsl:when test=". = $orderedUnit">
                <xsl:apply-templates />
            </xsl:when>
            <xsl:otherwise>
                <ul>
                    <li data-jstree="{$dossierIcon}" id="{./@id}">
                        <xsl:value-of disable-output-escaping="true">&#160;</xsl:value-of>
                        <xsl:value-of select="titel"/>
                        <xsl:for-each select="./dossier | ./dokument | ./dateiRef">
                            <xsl:sort select="zusatzDaten/merkmal[@name='ReihenfolgeAnalogesDossier']" />
                            <xsl:apply-templates select="."/>
                        </xsl:for-each>
                    </li>
                </ul>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template match="dokument">
        <xsl:choose>
            <xsl:when test="count(dateiRef) = 1">
                <xsl:variable name="documentLink">
                    <xsl:call-template name="getDocumentLink">
                        <xsl:with-param name="dateiId" select="./dateiRef"/>
                    </xsl:call-template>
                </xsl:variable>
                <ul>
                    <li data-jstree="{$documentIcon}" id="{./@id}" class="jstree-document">
                        <a href="{$documentLink}">
                            <xsl:value-of select="titel"/> (<xsl:call-template name="getDocumentExtension">
                                <xsl:with-param name="dateiId" select="./dateiRef"/>
                            </xsl:call-template>) </a>
                    </li>
                </ul>
            </xsl:when>
            <xsl:otherwise>
                <ul>
                    <li data-jstree="{$dossierIcon}" id="{./@id}">
                        <xsl:value-of disable-output-escaping="true">&#160;</xsl:value-of>
                        <xsl:value-of select="titel"/>
                        <xsl:apply-templates/>
                    </li>
                </ul>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template match="dateiRef">
        <xsl:variable name="documentIcon">
            <xsl:value-of>{"icon":"glyphicon glyphicon-file"}</xsl:value-of>
        </xsl:variable>
        <xsl:variable name="documentLink">
            <xsl:call-template name="getDocumentLink">
                <xsl:with-param name="dateiId" select="."/>
            </xsl:call-template>
        </xsl:variable>
        <xsl:variable name="documentName">
            <xsl:call-template name="getDocumentName">
                <xsl:with-param name="dateiId" select="."/>
            </xsl:call-template>
        </xsl:variable>
        <xsl:choose>
            <xsl:when test="$documentLink != ''">
                <ul>
                    <li data-jstree="{$documentIcon}" id="{.}" class="jstree-document">
                        <a href="{$documentLink}">
                            <xsl:value-of select="$documentName"/> (<xsl:call-template name="getDocumentExtension">
                                <xsl:with-param name="dateiId" select="."/>
                            </xsl:call-template>) </a>
                    </li>
                </ul>
            </xsl:when>
        </xsl:choose>
    </xsl:template>

    <xsl:template match="provenienz">
        <div class="row ./design/img/">
            <div class="col-md-12">
                <h2 data-i18n="viaduc-title-provenienz">Provenienz</h2>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Aktenbildner Name</xsl:with-param>
                    <xsl:with-param name="field" select="aktenbildnerName"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Systemname</xsl:with-param>
                    <xsl:with-param name="field" select="systemName"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Systembeschreibung</xsl:with-param>
                    <xsl:with-param name="field" select="systemBeschreibung"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Verwandte Systeme</xsl:with-param>
                    <xsl:with-param name="field" select="verwandteSysteme"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Archivierungsmodus Löschvorschriften</xsl:with-param>
                    <xsl:with-param name="field" select="archivierungsmodusLoeschvorschriften"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Registratur</xsl:with-param>
                    <xsl:with-param name="field" select="registratur"/>
                </xsl:call-template>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="ordnungssystem">
        <div class="row ./design/img/">
            <div class="col-md-12">
                <h2 data-i18n="viaduc-title-ordnungssystem">Ordnungssystem</h2>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Name</xsl:with-param>
                    <xsl:with-param name="field" select="name"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Generation</xsl:with-param>
                    <xsl:with-param name="field" select="generation"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Mitbenutzung</xsl:with-param>
                    <xsl:with-param name="field" select="mitbenutzung"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Bemerkung</xsl:with-param>
                    <xsl:with-param name="field" select="bemerkung"/>
                </xsl:call-template>
            </div>
        </div>
        <div class="row mt-2">
            <div class="col-md-12">
                <h3 data-i18n="viaduc-title-ordnungssystempositionen">Positionen</h3>
                <xsl:apply-templates select="ordnungssystemposition"/>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="ordnungssystemposition">
        <xsl:call-template name="renderTextField">
            <xsl:with-param name="label">ID</xsl:with-param>
            <xsl:with-param name="field" select="@id"/>
        </xsl:call-template>
        <xsl:call-template name="renderTextField">
            <xsl:with-param name="label">Nummer</xsl:with-param>
            <xsl:with-param name="field" select="nummer"/>
        </xsl:call-template>
        <xsl:call-template name="renderTextField">
            <xsl:with-param name="label">Titel</xsl:with-param>
            <xsl:with-param name="field" select="titel"/>
        </xsl:call-template>
        <xsl:call-template name="renderTextField">
            <xsl:with-param name="label">Federführende Organisationseinheit</xsl:with-param>
            <xsl:with-param name="field" select="federfuehrendeOrganisationseinheit"/>
        </xsl:call-template>
        <xsl:call-template name="renderTextField">
            <xsl:with-param name="label">Klassifizierungskategorie</xsl:with-param>
            <xsl:with-param name="field" select="klassifizierungskategorie"/>
        </xsl:call-template>
        <xsl:call-template name="renderBooleanField">
            <xsl:with-param name="label">Datenschutz</xsl:with-param>
            <xsl:with-param name="field" select="datenschutz"/>
        </xsl:call-template>
        <xsl:call-template name="renderTextField">
            <xsl:with-param name="label">Öffentlichkeitsstatus</xsl:with-param>
            <xsl:with-param name="field" select="oeffentlichkeitsstatus"/>
        </xsl:call-template>
        <xsl:call-template name="renderTextField">
            <xsl:with-param name="label">Öffentlichkeitsstatus Begründung</xsl:with-param>
            <xsl:with-param name="field" select="oeffentlichkeitsstatusBegruendung"/>
        </xsl:call-template>
        <xsl:for-each select="./ordnungssystemposition">
            <hr/>
            <xsl:apply-templates select="."/>
        </xsl:for-each>

    </xsl:template>

    <xsl:template match="text()"/>

    <xsl:template name="renderOrderedItem">
        <xsl:param name="orderedItem" required="yes"/>
        <div class="row">
            <div class="col-md-12">
                <h2>
                    <xsl:value-of select="$orderedItem/zusatzDaten/merkmal[@name = 'Stufe']"/>
                </h2>
                <h3 data-i18n="viaduc-title-identifikation">Identifikation</h3>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Identifikation</xsl:with-param>
                    <xsl:with-param name="field" select="'Schweizerisches Bundesarchiv'"/>
                    <xsl:with-param name="i18n" select="'viaduc-meta-institution'"/>
                </xsl:call-template>                
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Signatur</xsl:with-param>
                    <xsl:with-param name="field" select="$orderedItem/zusatzDaten/merkmal[@name = 'Signatur']"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Titel</xsl:with-param>
                    <xsl:with-param name="field" select="$orderedItem/titel"/>
                </xsl:call-template>
                <xsl:call-template name="renderHistorischerZeitraumField">
                    <xsl:with-param name="label">Entstehungszeitraum</xsl:with-param>
                    <xsl:with-param name="field" select="$orderedItem/entstehungszeitraum"/>
                </xsl:call-template>
                <xsl:call-template name="renderTextField">
                    <xsl:with-param name="label">Aktenzeichen</xsl:with-param>
                    <xsl:with-param name="field" select="$orderedItem/aktenzeichen"/>
                </xsl:call-template>
            </div>
        </div>
        <div class="row form-group">
            <div class="col-md-12">
                <div class="row">
                    <div class="col-md-12">
                        <a data-toggle="collapse" href="#mehrMetadaten" aria-expanded="false" aria-controls="mehrMetadaten" data-i18n="viaduc-title-mehr_weniger_metadaten">Mehr/weniger
                            Metadaten</a>
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-12 collapse" id="mehrMetadaten">
                        <xsl:choose>
                            <xsl:when
                                test="
                                $orderedItem/zusatzmerkmal or
                                $orderedItem/zusatzDaten/merkmal[@name = 'Früheres Aktenzeichen'] or
                                $orderedItem/zusatzDaten/merkmal[@name = 'Land'] or
                                $orderedItem/federfuehrendeOrganisationseinheit or
                                ($orderedItem/eroeffnungsdatum and $orderedItem/eroeffnungsdatum/normalize-space())or
                                ($orderedItem/abschlussdatum  and $orderedItem/abschlussdatum/normalize-space()) or
                                $orderedItem/klassifizierungskategorie or
                                $orderedItem/datenschutz or
                                $orderedItem/oeffentlichkeitsstatus or
                                $orderedItem/oeffentlichkeitsstatusBegruendung or
                                $orderedItem/sonstigeBestimmungen or
                                $orderedItem/entstehungszeitraumAnmerkung or
                                $orderedItem/zusatzDaten/merkmal[@name = 'Identifikation digitales Magazin']
                                ">
                                <h3 data-i18n="viaduc-title-kontext">Kontext</h3>
                            </xsl:when>
                        </xsl:choose>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Entstehungszeitraum Anmerkung</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/entstehungszeitraumAnmerkung"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Zusatzkomponente</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/zusatzmerkmal"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Frühere Aktenzeichen</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/zusatzDaten/merkmal[@name = 'Früheres Aktenzeichen']"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Land</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/zusatzDaten/merkmal[@name = 'Land']"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Federführende Organisationseinheit</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/federfuehrendeOrganisationseinheit"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderHistorischerZeitpunktField">
                            <xsl:with-param name="label">Eröffnungsdatum</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/eroeffnungsdatum"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderHistorischerZeitpunktField">
                            <xsl:with-param name="label">Abschlussdatum</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/abschlussdatum"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Klassifizierungskategorie</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/klassifizierungskategorie"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderBooleanField">
                            <xsl:with-param name="label">Datenschutz</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/datenschutz"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Öffentlichkeitsstatus</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/oeffentlichkeitsstatus"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Öffentlichkeitsstatus Begründung</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/oeffentlichkeitsstatusBegruendung"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Sonstige Bestimmungen</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/sonstigeBestimmungen"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Identifikation digitales Magazin</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/zusatzDaten/merkmal[@name = 'Identifikation digitales Magazin']"/>
                        </xsl:call-template>
                       
                        <xsl:choose>
                            <xsl:when
                                test="
                                $orderedItem/erscheinungsform or
                                $orderedItem/inhalt or
                                $orderedItem/umfang
                                ">
                                <h3 data-i18n="viaduc-title-inhalt_und_innere_ordnung">Inhalt und innere Ordnung</h3>
                            </xsl:when>
                        </xsl:choose>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Form</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/erscheinungsform"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Darin</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/inhalt"/>
                        </xsl:call-template>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Umfang</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/umfang"/>
                        </xsl:call-template>
                        
                        <!-- TODO Vorgänge -->
                        <xsl:choose>
                            <xsl:when test="$orderedItem/zusatzDaten/merkmal[@name = 'Frühere Signaturen']">
                                <h3 data-i18n="viaduc-title-sachverwandte_unterlagen">Sachverwandte Unterlagen</h3>
                            </xsl:when>
                        </xsl:choose>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Frühere Signaturen</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/zusatzDaten/merkmal[@name = 'Frühere Signaturen']"/>
                        </xsl:call-template>
                        <xsl:choose>
                            <xsl:when test="$orderedItem/bemerkung">
                                <h3 data-i18n="viaduc-title-anmerkungen">Anmerkungen</h3>
                            </xsl:when>
                        </xsl:choose>
                        <xsl:call-template name="renderTextField">
                            <xsl:with-param name="label">Zusätzliche Informationen</xsl:with-param>
                            <xsl:with-param name="field" select="$orderedItem/bemerkung"/>
                        </xsl:call-template>
                    </div>
                </div>
            </div>
        </div>
        <div class="row form-grop">
            <div class="col-md-12">
                <xsl:call-template name="renderArchivplankontext">
                    <xsl:with-param name="label">Archivplankontext</xsl:with-param>
                    <xsl:with-param name="field" select="$orderedItem/zusatzDaten/merkmal[@name = 'Archivplankontext']"/>
                </xsl:call-template>
            </div>
        </div>
        <div class="row">
            <div class="col-md-6">
                <h3 data-i18n="[html]viaduc-title-inhaltsverzeichnis">Inhaltsverzeichnis<br/>
                    <small class="text-muted">Doppelklicken, oder Ctrl-O um Datei zu öffnen.</small></h3>
                <div id="contentTree">
                    <xsl:for-each select="$orderedItem/dossier | $orderedItem/dokument | $orderedItem/dateiRef">
                        <xsl:sort select="zusatzDaten/merkmal[@name='ReihenfolgeAnalogesDossier']" />
                        <xsl:apply-templates select="."/>
                    </xsl:for-each>
                </div>
            </div>
            <div class="col-md-6">
                <h3 data-i18n="viaduc-title-metadaten">Metadaten</h3>
                <xsl:call-template name="renderContentMetadata"/>
            </div>
        </div>
    </xsl:template>

    <xsl:template name="renderContentMetadata">
        <!-- All dossiers except the root dossier -->
        <xsl:for-each select="//dossier[(parent::dossier)]">
            <div id="metadata_{./@id}" style="display:none;">
                <xsl:call-template name="renderDossierMetadata">
                    <xsl:with-param name="dossier" select="."/>
                </xsl:call-template>
            </div>
        </xsl:for-each>
        <!-- All documents -->
        <xsl:for-each select="//dokument">
            <div id="metadata_{./@id}" style="display:none;">
                <xsl:call-template name="renderDokumentMetadata">
                    <xsl:with-param name="dokument" select="."/>
                </xsl:call-template>
            </div>
        </xsl:for-each>
        <!-- All file references for dossier that have one or more dateiRef -->
        <xsl:for-each select="//dossier[count(dateiRef) >= 1]/dateiRef">
            <xsl:variable name="dateiId" select="string()"/>
            <xsl:variable name="datei" select="//datei[@id = $dateiId]"/>
            <div id="metadata_{.}" style="display:none;">
                <xsl:call-template name="renderFileMetadata">
                    <xsl:with-param name="file" select="$datei"/>
                </xsl:call-template>
            </div>
        </xsl:for-each>
        <!-- All file references for documents that have more than one dateiRef
             (only those can be selected and show the metadata) -->
        <xsl:for-each select="//dokument[count(dateiRef) > 1]/dateiRef">
            <xsl:variable name="dateiId" select="string()"/>
            <xsl:variable name="datei" select="//datei[@id = $dateiId]"/>
            <div id="metadata_{.}" style="display:none;">
                <xsl:call-template name="renderFileMetadata">
                    <xsl:with-param name="file" select="$datei"/>
                </xsl:call-template>
            </div>
        </xsl:for-each>
    </xsl:template>

    <xsl:template name="renderFileMetadata">
        <xsl:param name="file" required="yes"/>
        <table class="table">
            <xsl:call-template name="renderMetadataHead"/>
            <tbody>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Name'"/>
                    <xsl:with-param name="value" select="$file/name"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Originalname'"/>
                    <xsl:with-param name="value" select="$file/originalName"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Prüfalgorithmus'"/>
                    <xsl:with-param name="value" select="$file/pruefalgorithmus"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Prüfsumme'"/>
                    <xsl:with-param name="value" select="$file/pruefsumme"/>
                </xsl:call-template>
            </tbody>
        </table>
    </xsl:template>

    <xsl:template name="renderDokumentMetadata">
        <xsl:param name="dokument" required="yes"/>
        <table class="table">
            <xsl:call-template name="renderMetadataHead"/>
            <tbody>
                <xsl:call-template name="renderMetadataTitleRow">
                    <xsl:with-param name="label" select="'Identifikation'"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Signatur'"/>
                    <xsl:with-param name="value" select="$dokument/zusatzDaten/merkmal[@name = 'Signatur']"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Titel'"/>
                    <xsl:with-param name="value" select="$dokument/titel"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Format'"/>
                    <xsl:with-param name="value" select="$dokument/zusatzDaten/merkmal[@name = 'Format']"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Entstehungszeitraum'"/>
                    <xsl:with-param name="value">
                        <xsl:call-template name="historischerZeitraum">
                            <xsl:with-param name="zeitraum" select="$dokument/entstehungszeitraum"/>
                        </xsl:call-template>
                    </xsl:with-param>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Identifikation digitales Magazin'"/>
                    <xsl:with-param name="value" select="$dokument/zusatzDaten/merkmal[@name = 'Identifikation digitales Magazin']"/>
                    <xsl:with-param name="valueCellClass" select="'textWrapAll'"/>
                </xsl:call-template>

                <xsl:choose>
                    <xsl:when
                        test="
                            $dokument/zusatzDaten/merkmal[@name = 'Urheber'] or
                            $dokument/zusatzDaten/merkmal[@name = 'Verleger'] or
                            $dokument/zusatzDaten/merkmal[@name = 'Abdeckung'] or
                            ($dokument/registrierdatum and $dokument/registrierdatum/normalize-space()) or
                            $dokument/klassifizierungskategorie or
                            $dokument/datenschutz or
                            $dokument/oeffentlichkeitsstatus or
                            $dokument/oeffentlichkeitsstatusBegruendung or
                            $dokument/sonstigeBestimmungen
                            ">
                        <xsl:call-template name="renderMetadataTitleRow">
                            <xsl:with-param name="label" select="'Kontext'"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Urheber'"/>
                    <xsl:with-param name="value" select="$dokument/zusatzDaten/merkmal[@name = 'Urheber']"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Verleger'"/>
                    <xsl:with-param name="value" select="$dokument/zusatzDaten/merkmal[@name = 'Verleger']"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Abdeckung'"/>
                    <xsl:with-param name="value" select="$dokument/zusatzDaten/merkmal[@name = 'Abdeckung']"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Registrierdatum'"/>
                    <xsl:with-param name="value">
                        <xsl:call-template name="historischerZeitpunkt">
                            <xsl:with-param name="zeitpunkt" select="$dokument/registrierdatum"/>
                        </xsl:call-template>
                    </xsl:with-param>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Klassifizierungskategorie'"/>
                    <xsl:with-param name="value" select="$dokument/klassifizierungskategorie"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Datenschutz'"/>
                    <xsl:with-param name="value" select="$dokument/datenschutz"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Öffentlichkeitsstatus'"/>
                    <xsl:with-param name="value" select="$dokument/oeffentlichkeitsstatus"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Öffentlichkeitsstatus Begründung'"/>
                    <xsl:with-param name="value" select="$dokument/oeffentlichkeitsstatusBegruendung"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Sonstige Bestimmungen'"/>
                    <xsl:with-param name="value" select="$dokument/sonstigeBestimmungen"/>
                </xsl:call-template>

                <xsl:choose>
                    <xsl:when
                        test="
                            $dokument/zusatzDaten/merkmal[@name = 'Form'] or
                            $dokument/erscheinungsform or
                            $dokument/zusatzDaten/merkmal[@name = 'Thema'] or
                            $dokument/inhalt or
                            $dokument/zusatzDaten/merkmal[@name = 'Darin']
                            ">
                        <xsl:call-template name="renderMetadataTitleRow">
                            <xsl:with-param name="label" select="'Inhalt und innere Ordnung'"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Form'"/>
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test="$dokument/zusatzDaten/merkmal[@name = 'Form']">
                                <xsl:value-of select="$dokument/zusatzDaten/merkmal[@name = 'Form']"/>
                            </xsl:when>
                            <xsl:otherwise>
                                <xsl:value-of select="$dokument/erscheinungsform"/>
                            </xsl:otherwise>
                        </xsl:choose>
                    </xsl:with-param>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Thema'"/>
                    <xsl:with-param name="value" select="$dokument/zusatzDaten/merkmal[@name = 'Thema']"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Inhalt'"/>
                    <xsl:with-param name="value" select="$dokument/inhalt"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Darin'"/>
                    <xsl:with-param name="value" select="$dokument/zusatzDaten/merkmal[@name = 'Darin']"/>
                </xsl:call-template>

                <xsl:choose>
                    <xsl:when test="
                            $dokument/zusatzDaten/merkmal[@name = 'Frühere Signaturen']
                            ">
                        <xsl:call-template name="renderMetadataTitleRow">
                            <xsl:with-param name="label" select="'Sachverwandte Unterlagen'"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Frühere Signaturen'"/>
                    <xsl:with-param name="value" select="$dokument/zusatzDaten/merkmal[@name = 'Frühere Signaturen']"/>
                </xsl:call-template>


                <xsl:choose>
                    <xsl:when test="
                            $dokument/bemerkung
                            ">
                        <xsl:call-template name="renderMetadataTitleRow">
                            <xsl:with-param name="label" select="'Anmerkungen'"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Zusätzliche Informationen'"/>
                    <xsl:with-param name="value" select="$dokument/bemerkung"/>
                </xsl:call-template>
            </tbody>
        </table>
    </xsl:template>

    <xsl:template name="renderDossierMetadata">
        <xsl:param name="dossier" required="yes"/>
        <table class="table">
            <xsl:call-template name="renderMetadataHead"/>
            <tbody>
                <xsl:call-template name="renderMetadataTitleRow">
                    <xsl:with-param name="label" select="'Identifikation'"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Signatur'"/>
                    <xsl:with-param name="value" select="$dossier/zusatzDaten/merkmal[@name = 'Signatur']"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Titel'"/>
                    <xsl:with-param name="value" select="$dossier/titel"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Entstehungszeitraum'"/>
                    <xsl:with-param name="value">
                        <xsl:call-template name="historischerZeitraum">
                            <xsl:with-param name="zeitraum" select="$dossier/entstehungszeitraum"/>
                        </xsl:call-template>
                    </xsl:with-param>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Entstehungszeitraum Anmerkung'"/>
                    <xsl:with-param name="value" select="$dossier/entstehungszeitraumAnmerkung"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Identifikation digitales Magazin'"/>
                    <xsl:with-param name="value" select="$dossier/zusatzDaten/merkmal[@name = 'Identifikation digitales Magazin']"/>
                </xsl:call-template>

                <xsl:choose>
                    <xsl:when
                        test="
                            $dossier/aktenzeichen or
                            $dossier/zusatzmerkmal or
                            $dossier/zusatzDaten/merkmal[@name = 'Früheres Aktenzeichen'] or
                            $dossier/zusatzDaten/merkmal[@name = 'Land'] or
                            $dossier/federfuehrendeOrganisationseinheit or
                            ($dossier/eroeffnungsdatum and $dossier/eroeffnungsdatum/normalize-space()) or
                            ($dossier/abschlussdatum and $dossier/abschlussdatum/normalize-space()) or
                            $dossier/klassifizierungskategorie or
                            $dossier/datenschutz or
                            $dossier/oeffentlichkeitsstatus or
                            $dossier/oeffentlichkeitsstatusBegruendung or
                            $dossier/sonstigeBestimmungen
                            ">
                        <xsl:call-template name="renderMetadataTitleRow">
                            <xsl:with-param name="label" select="'Kontext'"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Aktenzeichen'"/>
                    <xsl:with-param name="value" select="$dossier/aktenzeichen"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Zusatzkomponente'"/>
                    <xsl:with-param name="value" select="$dossier/zusatzmerkmal"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Früheres Aktenzeichen'"/>
                    <xsl:with-param name="value" select="$dossier/zusatzDaten/merkmal[@name = 'Früheres Aktenzeichen']"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Land'"/>
                    <xsl:with-param name="value" select="$dossier/zusatzDaten/merkmal[@name = 'Land']"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Federführende Organisationseinheit'"/>
                    <xsl:with-param name="value" select="$dossier/federfuehrendeOrganisationseinheit"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Eröffnungsdatum'"/>
                    <xsl:with-param name="value">
                        <xsl:call-template name="historischerZeitpunkt">
                            <xsl:with-param name="zeitpunkt" select="$dossier/eroeffnungsdatum"/>
                        </xsl:call-template>
                    </xsl:with-param>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Abschlussdatum'"/>
                    <xsl:with-param name="value">
                        <xsl:call-template name="historischerZeitpunkt">
                            <xsl:with-param name="zeitpunkt" select="$dossier/abschlussdatum"/>
                        </xsl:call-template>
                    </xsl:with-param>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Klassifizierungskategorie'"/>
                    <xsl:with-param name="value" select="$dossier/klassifizierungskategorie"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Datenschutz'"/>
                    <xsl:with-param name="value" select="$dossier/datenschutz"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Öffentlichkeitsstatus'"/>
                    <xsl:with-param name="value" select="$dossier/oeffentlichkeitsstatus"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Öffentlichkeitsstatus Begründung'"/>
                    <xsl:with-param name="value" select="$dossier/oeffentlichkeitsstatusBegruendung"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Sonstige Bestimmungen'"/>
                    <xsl:with-param name="value" select="$dossier/sonstigeBestimmungen"/>
                </xsl:call-template>

                <xsl:choose>
                    <xsl:when
                        test="
                            $dossier/zusatzDaten/merkmal[@name = 'Form'] or
                            $dossier/erscheinungsform or
                            $dossier/inhalt or
                            $dossier/umfang
                            ">
                        <xsl:call-template name="renderMetadataTitleRow">
                            <xsl:with-param name="label" select="'Inhalt und innere Ordnung'"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Form'"/>
                    <xsl:with-param name="value">
                        <xsl:choose>
                            <xsl:when test="$dossier/zusatzDaten/merkmal[@name = 'Form']">
                                <xsl:value-of select="$dossier/zusatzDaten/merkmal[@name = 'Form']"/>
                            </xsl:when>
                            <xsl:otherwise>
                                <xsl:value-of select="$dossier/erscheinungsform"/>
                            </xsl:otherwise>
                        </xsl:choose>
                    </xsl:with-param>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Darin'"/>
                    <xsl:with-param name="value" select="$dossier/inhalt"/>
                </xsl:call-template>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Umfang'"/>
                    <xsl:with-param name="value" select="$dossier/umfang"/>
                </xsl:call-template>

                <xsl:choose>
                    <xsl:when test="
                            $dossier/zusatzDaten/merkmal[@name = 'Frühere Signaturen']
                            ">
                        <xsl:call-template name="renderMetadataTitleRow">
                            <xsl:with-param name="label" select="'Sachverwandte Unterlagen'"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Frühere Signaturen'"/>
                    <xsl:with-param name="value" select="$dossier/zusatzDaten/merkmal[@name = 'Frühere Signaturen']"/>
                </xsl:call-template>

                <xsl:choose>
                    <xsl:when test="
                            $dossier/bemerkung
                            ">
                        <xsl:call-template name="renderMetadataTitleRow">
                            <xsl:with-param name="label" select="'Anmerkungen'"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
                <xsl:call-template name="renderMetadataRow">
                    <xsl:with-param name="label" select="'Zusätzliche Informationen'"/>
                    <xsl:with-param name="value" select="$dossier/bemerkung"/>
                </xsl:call-template>
            </tbody>
        </table>
    </xsl:template>

    <xsl:template name="renderMetadataHead">
        <thead>
            <tr>
                <th style="width:35%;"/>
                <th/>
            </tr>
        </thead>
    </xsl:template>

    <xsl:template name="renderMetadataRow">
        <xsl:param name="label"/>
        <xsl:param name="value"/>
        <xsl:param name="valueCellClass" required="false" />
        <xsl:choose>
            <xsl:when test="$value and $value/normalize-space()">
                <tr>
                    <td>
                        <span data-i18n="viaduc-label-{replace(lower-case($label), ' ', '_')}">
                            <xsl:value-of select="$label"/>
                        </span>
                    </td>
                    <xsl:choose>
                        <xsl:when test="$valueCellClass">
                            <td class="{$valueCellClass}">
                                <xsl:value-of select="$value"/>
                            </td>
                        </xsl:when>
                        <xsl:otherwise>
                            <td>
                                <xsl:value-of select="$value"/>
                            </td>
                        </xsl:otherwise>
                    </xsl:choose>
                </tr>
            </xsl:when>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="renderMetadataTitleRow">
        <xsl:param name="label"/>
        <tr>
            <td colspan="2">
                <h3 data-i18n="viaduc-title-{replace(lower-case($label), ' ', '_')}">
                    <xsl:value-of select="$label"/>
                </h3>
            </td>
        </tr>
    </xsl:template>

    <xsl:template name="getDocumentLink">
        <xsl:param name="dateiId" required="yes"/>
        <xsl:variable name="datei" select="$allFiles[@id = $dateiId]"/>
        <xsl:value-of select="string-join((string-join($datei/ancestor-or-self::ordner/name, '/'), $datei/name), '/')"/>
    </xsl:template>

    <xsl:template name="getDocumentExtension">
        <xsl:param name="dateiId" required="yes"/>
        <xsl:variable name="datei" select="$allFiles[@id = $dateiId]"/>
        <xsl:call-template name="getFileExtension">
            <xsl:with-param name="path" select="$datei/name"/>
        </xsl:call-template>
    </xsl:template>

    <xsl:template name="getFileExtension">
        <xsl:param name="path"/>
        <xsl:choose>
            <xsl:when test="contains($path, '/')">
                <xsl:call-template name="getFileExtension">
                    <xsl:with-param name="path" select="substring-after($path, '/')"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:when test="contains($path, '.')">
                <xsl:call-template name="getFileExtension">
                    <xsl:with-param name="path" select="substring-after($path, '.')"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$path"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="getDocumentName">
        <xsl:param name="dateiId" required="yes"/>
        <xsl:variable name="datei" select="$allFiles[@id = $dateiId]"/>
        <xsl:choose>
            <xsl:when test="$datei/originalName">
                <xsl:value-of select="$datei/originalName"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$datei/name"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="renderHeader">
        <xsl:param name="orderedItem" required="yes"/>
        <header>
            <div class="clearfix">
                <section class="nav-services clearfix">
                    <h2 class="sr-only">Sprachwahl</h2>
                    <nav class="nav-lang">
                        <ul>
                            <li>
                                <a href="javascript:setLocale('de')" lang="de" title="Deutsch" aria-label="Deutsch">DE</a>
                            </li>
                            <li>
                                <a href="javascript:setLocale('fr')" lang="fr" title="Français" aria-label="Français">FR</a>
                            </li>
                            <li>
                                <a href="javascript:setLocale('it')" lang="it" title="Italiano" aria-label="Italiano">IT</a>
                            </li>
                            <li>
                                <a href="javascript:setLocale('en')" lang="en" title="English" aria-label="English">EN</a>
                            </li>
                        </ul>
                    </nav>
                </section>
            </div>

            <div class="clearfix">
                <a href="/" class="brand hidden-xs">
                    <img src="./design/img/logo-CH.svg" onerror="this.onerror = null; this.src = './design/img/logo-CH.png'" alt="Logo der Schweizerischen Eidgenossenschaft – zur Startseite"/>
                    <div class="brand-header">
                        <h1 data-i18n="viaduc-header-title">Schweizerisches Bundesarchiv BAR</h1>
                        <xsl:choose>
                            <xsl:when test="not(parent::dossier)">
                                <br/>
                                <p>
                                    <xsl:value-of select="$orderedItem/zusatzDaten/merkmal[@name = 'Signatur']"/>
                                    <xsl:text>&#160;</xsl:text>
                                    <xsl:value-of select="$orderedItem/titel"/>
                                    <xsl:choose>
                                        <xsl:when test="$orderedItem/entstehungszeitraum">
                                            <xsl:text>&#160;(</xsl:text>
                                            <xsl:call-template name="historischerZeitraum">
                                                <xsl:with-param name="zeitraum" select="$orderedItem/entstehungszeitraum"/>
                                            </xsl:call-template>
                                            <xsl:text>)</xsl:text>
                                        </xsl:when>
                                    </xsl:choose>
                                </p>
                            </xsl:when>
                        </xsl:choose>
                    </div>
                </a>
            </div>
        </header>
    </xsl:template>

    <xsl:template name="renderNavigation">
        <nav class="nav-main navbar">
            <h2 class="sr-only">Navigation</h2>
            <section class="nav-mobile yamm nav-open">
                <div class="table-row nav-open">
                    <div class="nav-mobile-header">
                        <div class="table-row">
                            <span class="nav-mobile-logo">
                                <img src="./design/img/swiss.svg" onerror="this.onerror=null; this.src='./design/img/swiss.png'" alt="Confederatio Helvetica"/>
                            </span>
                            <h1>
                                <a href="#" data-i18n="viaduc-nav-mobile-title">Offline-Viewer</a>
                            </h1>
                        </div>
                    </div>
                    <div class="table-cell dropdown">
                        <a class="nav-mobile-menu dropdown-toggle" aria-expanded="true" aria-haspopup="true" href="#" data-toggle="dropdown">
                            <span class="icon icon--menu"/>
                        </a>
                        <div class="drilldown dropdown-menu" role="menu">
                            <div class="drilldown-container">
                                <nav class="nav-page-list">
                                    <ul role="menu">
                                        <li role="presentation">
                                            <a role="menuitem" class="menuItemDokumente" href="#" data-i18n="viaduc-nav-documents">Dokumente</a>
                                        </li>
                                        <li role="presentation">
                                            <a role="menuitem" class="menuItemOrdnungssystem" href="#" data-i18n="viaduc-nav-ordnungssystem">Ordnungssystem</a>
                                        </li>
                                        <li role="presentation">
                                            <a role="menuitem" class="menuItemPaketinformation" href="#" data-i18n="viaduc-nav-paket-info">Paketinformationen</a>
                                        </li>
                                    </ul>
                                    <button class="yamm-close-bottom" aria-label="Schliessen">
                                        <span class="icon icon--top" aria-hidden="true"/>
                                    </button>
                                </nav>

                            </div>
                        </div>
                    </div>
                </div>
            </section>

            <!-- The tab navigation -->
            <ul class="nav navbar-nav">
                <li class="dropdown">
                    <a class="menuItemDokumente" href="#" data-i18n="viaduc-nav-documents">Dokumente</a>
                </li>
                <li class="dropdown">
                    <a class="menuItemOrdnungssystem" href="#" data-i18n="viaduc-nav-ordnungssystem">Ordnungssystem</a>
                </li>
                <li class="dropdown">
                    <a class="menuItemPaketinformation" href="#" data-i18n="viaduc-nav-paket-info">Paketinformationen</a>
                </li>
            </ul>
        </nav>
    </xsl:template>

    <xsl:template name="renderFooter">
        <footer>
            <div class="container-fluid">
                <hr class="footer-line visible-xs"/>
                <img class="visible-xs" src="./design/img/logo-CH.svg" onerror="this.onerror = null; this.src = './design/img/logo-CH.png'" alt="back to home"/>
            </div>

            <div class="footer-address">
                <span class="hidden-xs" data-i18n="viaduc-footer-bar">Schweizerisches Bundesarchiv BAR</span>
                <nav class="pull-right">
                    <ul>
                        <li>
                            <span data-i18n="[html]viaduc-footer-rechtliches">Rechtliche Grundlagen</span>
                        </li>
                    </ul>
                </nav>
            </div>
        </footer>
    </xsl:template>

    <xsl:template name="renderPageAblieferung">
        <xsl:param name="paket" required="yes"/>
        <div class="row pagePaketinformation" style="display: none;">
            <div class="col-md-12">
                <xsl:call-template name="paketInfo">
                    <xsl:with-param name="paket" select="$paket"/>
                </xsl:call-template>
                <xsl:apply-templates select="ablieferung/provenienz"/>
                <xsl:apply-templates select="ablieferung"/>
            </div>
        </div>
    </xsl:template>

    <xsl:template name="renderPageOrdnungssystem">
        <xsl:param name="ordnungssystem" required="yes"/>
        <div class="row pageOrdnungssystem" style="display: none;">
            <div class="col-md-12">
                <xsl:apply-templates select="$ordnungssystem"/>
            </div>
        </div>
    </xsl:template>

    <xsl:template name="renderPageDossier">
        <xsl:param name="orderedItem" required="yes"/>
        <div class="row pageDokumente" style="display: none;">
            <div class="col-md-12">
                <xsl:call-template name="renderOrderedItem">
                    <xsl:with-param name="orderedItem" select="$orderedItem"/>
                </xsl:call-template>
            </div>
        </div>
    </xsl:template>

    <xsl:template name="renderTextField">
        <xsl:param name="field" required="yes"/>
        <xsl:param name="label" required="yes"/>
        <xsl:param name='i18n' required="no"/>
        <xsl:choose>
            <xsl:when test="$field">
                <div class="form-group row">
                    <span class="col-sm-2 col-form-label" data-i18n="viaduc-label-{replace(lower-case($label), ' ', '_')}">
                        <xsl:value-of select="$label"/>
                    </span>
                    <div class="col-sm-10">
                        <xsl:choose>
                            <xsl:when test="$i18n">
                                <span class="form-control border-0" data-i18n="{$i18n}">
                                    <xsl:value-of select="$field"/>
                                </span>
                            </xsl:when>
                            <xsl:otherwise>
                                <span class="form-control border-0">
                                    <xsl:value-of select="$field"/>
                                </span>
                            </xsl:otherwise>
                        </xsl:choose>
                    </div>
                </div>
            </xsl:when>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="renderDateTimeField">
        <xsl:param name="field" required="yes"/>
        <xsl:param name="label" required="yes"/>
        <xsl:choose>
            <xsl:when test="$field">
                <div class="form-group row">
                    <span class="col-sm-2  col-form-label" data-i18n="viaduc-label-{replace(lower-case($label), ' ', '_')}">
                        <xsl:value-of select="$label"/>
                    </span>
                    <div class="col-sm-10">
                        <span class="form-control border-0">
                            <xsl:choose>
                                <xsl:when test="string(.) castable as xs:date">
                                    <xsl:value-of select="format-date($field, '[D01].[M01].[Y0001]')"/>
                                </xsl:when>
                                <xsl:otherwise>
                                    <xsl:value-of select="$field"/>
                                </xsl:otherwise>
                            </xsl:choose>
                        </span>
                    </div>
                </div>
            </xsl:when>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="renderBooleanField">
        <xsl:param name="field" required="yes"/>
        <xsl:param name="label" required="yes"/>
        <xsl:choose>
            <xsl:when test="$field">
                <div class="form-group row">
                    <span class="col-sm-2 col-form-label" data-i18n="viaduc-label-{replace(lower-case($label), ' ', '_')}">
                        <xsl:value-of select="$label"/>
                    </span>
                    <div class="col-sm-10">
                        <span class="form-control border-0">
                            <xsl:choose>
                                <xsl:when test="$field = true()">Ja</xsl:when>
                                <xsl:otherwise>Nein</xsl:otherwise>
                            </xsl:choose>
                        </span>
                    </div>
                </div>
            </xsl:when>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="renderHistorischerZeitraumField">
        <xsl:param name="field" required="yes"/>
        <xsl:param name="label" required="yes"/>
        <xsl:choose>
            <xsl:when test="$field">
                <div class="form-group row">
                    <span class="col-sm-2 col-form-label" data-i18n="viaduc-label-{replace(lower-case($label), ' ', '_')}">
                        <xsl:value-of select="$label"/>
                    </span>
                    <div class="col-sm-10">
                        <span class="form-control border-0">
                            <xsl:call-template name="historischerZeitraum">
                                <xsl:with-param name="zeitraum" select="$field"/>
                            </xsl:call-template>
                        </span>
                    </div>
                </div>
            </xsl:when>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="renderHistorischerZeitpunktField">
        <xsl:param name="field" required="yes"/>
        <xsl:param name="label" required="yes"/>
        <xsl:choose>
            <xsl:when test="$field">
                <div class="form-group row">
                    <span class="col-sm-2 col-form-label" data-i18n="viaduc-label-{replace(lower-case($label), ' ', '_')}">
                        <xsl:value-of select="$label"/>
                    </span>
                    <div class="col-sm-10">
                        <span class="form-control border-0">
                            <xsl:call-template name="historischerZeitpunkt">
                                <xsl:with-param name="zeitpunkt" select="$field"/>
                            </xsl:call-template>
                        </span>
                    </div>
                </div>
            </xsl:when>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="renderArchivplankontext">
        <xsl:param name="field" required="yes"/>
        <xsl:param name="label" required="yes"/>
        <xsl:choose>
            <xsl:when test="$field">
                <xsl:variable name="jsonArray" select="json-to-xml(normalize-space($field/text()))"/>
                <div class="row">
                    <div class="col-md-12">
                        <a data-toggle="collapse" href="#archivplanContext" aria-expanded="false" aria-controls="mehrMetadaten" data-i18n="viaduc-title-{replace(lower-case($label), ' ', '_')}">
                            <xsl:value-of select="$label"/>
                        </a>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-12 collapse" id="archivplanContext">
                        <xsl:for-each select="$jsonArray/node()/node()">
                            <xsl:variable name="paddingStyle" select="concat('margin-left: ', position() - 1, 'em;')"/>
                            <div class="apItem" style="{$paddingStyle}">
                                <span>
                                    <xsl:value-of select="./node()[@key = 'RefCode']"/>
                                </span>
                                <xsl:value-of disable-output-escaping="true">&#160;</xsl:value-of>
                                <span>
                                    <xsl:value-of select="./node()[@key = 'Title']"/>
                                </span>
                                <xsl:choose>
                                    <xsl:when test="string-length(./node()[@key = 'DateRangeText']) > 0">
                                        <xsl:value-of disable-output-escaping="true">&#160;(</xsl:value-of>
                                        <span>
                                            <xsl:value-of select="./node()[@key = 'DateRangeText']"/>
                                        </span>
                                        <xsl:value-of disable-output-escaping="true">)</xsl:value-of>
                                    </xsl:when>
                                </xsl:choose>
                            </div>
                        </xsl:for-each>
                    </div>
                </div>
            </xsl:when>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="historischerZeitraum">
        <xsl:param name="zeitraum" required="yes"/>
        <span>
            <xsl:call-template name="historischerZeitpunkt">
                <xsl:with-param name="zeitpunkt" select="$zeitraum/von"/>
            </xsl:call-template>
            <xsl:text> - </xsl:text>
            <xsl:call-template name="historischerZeitpunkt">
                <xsl:with-param name="zeitpunkt" select="$zeitraum/bis"/>
            </xsl:call-template>
        </span>
    </xsl:template>

    <xsl:template name="historischerZeitpunkt">
        <xsl:param name="zeitpunkt" required="yes"/>
        <xsl:choose>
            <xsl:when test="$zeitpunkt/ca = true()">
                <xsl:text/>ca. </xsl:when>
        </xsl:choose>
        <xsl:choose>
            <xsl:when test="$zeitpunkt/datum castable as xs:date">
                <xsl:value-of select="format-date($zeitpunkt/datum, '[D01].[M01].[Y0001]')"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$zeitpunkt/datum"/>
            </xsl:otherwise>
        </xsl:choose>

    </xsl:template>

</xsl:stylesheet>
