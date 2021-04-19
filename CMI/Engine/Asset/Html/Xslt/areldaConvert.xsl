<?xml version="1.0" encoding="UTF-8"?>

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema" version="2.0" xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array" xmlns:nsSource="http://bar.admin.ch/arelda/v4" xpath-default-namespace="http://bar.admin.ch/arelda/v4" exclude-result-prefixes="#all">
    <xsl:output method="xml" indent="yes" encoding="UTF-8" escape-uri-attributes="no"/>

    <!-- copy all nodes and attributes -->
    <xsl:template match="node() | @*">
        <xsl:copy>
            <xsl:apply-templates select="node() | @*"/>
        </xsl:copy>
    </xsl:template>

    <!-- template to change the namespace 
         of the elements  
         from "http://bar.admin.ch/arelda/v4" 
         to "http://bar.admin.ch/gebrauchskopie/v1" -->
    <xsl:template match="nsSource:*">
        <xsl:element name="{local-name()}" namespace="http://bar.admin.ch/gebrauchskopie/v1">
            <xsl:apply-templates select="@*, node()"/>
        </xsl:element>
    </xsl:template>

    <xsl:template match="paketTyp"/>

    <xsl:template match="paket">
        <paket xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xsi:type="paketDIP" schemaVersion="4.1"
            xsi:schemaLocation="http://bar.admin.ch/gebrauchskopie/v1 gebrauchskopie.xsd" xmlns="http://bar.admin.ch/gebrauchskopie/v1">
            <generierungsdatum>
                <xsl:value-of select="current-dateTime()"/>
            </generierungsdatum>
            <xsl:apply-templates/>
        </paket>
    </xsl:template>

    <!-- Template to remove xsi:type from ablieferung -->
    <xsl:template match="ablieferung">
        <xsl:element name="{local-name()}" namespace="http://bar.admin.ch/gebrauchskopie/v1">
            <xsl:apply-templates/>
        </xsl:element>
    </xsl:template>

    <xsl:template match="ordner">
        <xsl:element name="ordner" namespace="http://bar.admin.ch/gebrauchskopie/v1">
            <xsl:attribute name="id" select="generate-id(.)"/>
            <xsl:apply-templates/>
        </xsl:element>
    </xsl:template>

</xsl:stylesheet>
