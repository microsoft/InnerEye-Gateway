<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">

  <xsl:output method="xml" indent="yes" />

  <!-- identity template (copy everything by default) -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Remove .exe file entries (handled manually in Product.wxs) 
  This means end-with: substring(wix:File/@Source, string-length(wix:File/@Source) - string-length('.exe') +1)='.exe'
  -->
  <xsl:key name="exe-search" match="wix:Component[substring(wix:File/@Source, string-length(wix:File/@Source) - string-length('.exe') +1)='.exe']" use="@Id"/>
  <xsl:template match="wix:Component[key('exe-search', @Id)]"/>
  <xsl:template match="wix:ComponentRef[key('exe-search', @Id)]"/>

</xsl:stylesheet>