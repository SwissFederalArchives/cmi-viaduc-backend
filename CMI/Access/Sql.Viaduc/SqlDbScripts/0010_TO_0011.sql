
-- *** Tabelle Reason übersetzen ***

UPDATE [dbo].[Reason] SET 
[Name_de] = 'als Beweismittel',
[Name_fr] = 'comme moyens de preuve',
[Name_it] = 'come mezzi di prova',
[Name_en] = 'as evidence' WHERE ID = 1

UPDATE [dbo].[Reason] SET 
[Name_de] = 'für Gesetzgebung oder Rechtsprechung',
[Name_fr] = 'à des fins législatives ou jurisprudentielles',
[Name_it] = 'a fini legislativi o giurisprudenziali',
[Name_en] = 'for legislative purposes or for the administration of justice' WHERE ID = 2

UPDATE [dbo].[Reason] SET 
[Name_de] = 'für die Auswertung zu statistischen Zwecken',
[Name_fr] = 'pour des évaluations à buts statistiques',
[Name_it] = 'per la valutazione a fini statistici',
[Name_en] = 'for statistical analysis' WHERE ID = 3

UPDATE [dbo].[Reason] SET 
[Name_de] = 'für einen Entscheid über die Gewährung, Beschränkung oder Verweigerung des Einsichts- oder Auskunftsrechtes der betroffenen Person',
[Name_fr] = 'pour prendre une décision visant à autoriser, à restreindre ou à refuser le droit de la personne concernée de consulter les documents ou d''obtenir des renseignements',
[Name_it] = 'per una decisione in merito alla concessione, alla limitazione o al rifiuto del diritto alla consultazione o all''informazione della persona interessata',
[Name_en] = 'to decide on the granting, restriction or refusal of the right of the person concerned to consult documents or to obtain information' WHERE ID = 4
GO

-- *** Tabelle Art der Arbeit übersetzen ***

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Recherche privée',
[Name_it] = 'Ricerca privata',
[Name_en] = 'Private Research' WHERE ID = 1

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Recherche journalistique',
[Name_it] = 'Ricerca giornalistica',
[Name_en] = 'Journalistic Research' WHERE ID = 2

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Travail de séminaire / bachelor',
[Name_it] = 'Lavoro di seminario / lavoro di bachelor',
[Name_en] = 'Seminar / Bachelor Thesis' WHERE ID = 3

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Travail de licence / master',
[Name_it] = 'Lavoro di licenza / lavoro di master',
[Name_en] = 'Licentiate / Master Thesis' WHERE ID = 4

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Travail de diplôme',
[Name_it] = 'Lavoro di diploma',
[Name_en] = 'Degree Thesis' WHERE ID = 5

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Dissertation',
[Name_it] = 'Tesi di dottorato',
[Name_en] = 'Dissertation' WHERE ID = 6

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Habilitation',
[Name_it] = 'Abilitazione',
[Name_en] = 'Habilitation' WHERE ID = 7

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Projet de recherche',
[Name_it] = 'Progetto di ricerca',
[Name_en] = 'Research Project' WHERE ID = 8

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Mandat administratif',
[Name_it] = 'Mandato ufficiale',
[Name_en] = 'Official Assignment' WHERE ID = 9

UPDATE [dbo].[ArtDerArbeit] SET 
[Name_fr] = 'Autres',
[Name_it] = 'Altro',
[Name_en] = 'Others' WHERE ID = 10
GO
