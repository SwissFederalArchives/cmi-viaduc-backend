# Removes all binding redirects from all app.config oder web.config
# This can be used to create new minimal redirects by applying
# Get-Project -All | Add-BindingRedirect -Force
# in the package manager console.

gci -Recurse -Include app.config,web.config |
 %{
   $xml = [xml](Get-Content $_.FullName)
   $ns = new-object System.Xml.XmlNamespaceManager($xml.NameTable)
   $ns.AddNamespace("asm","urn:schemas-microsoft-com:asm.v1")
   $node = $xml.SelectSingleNode("//asm:assemblyBinding",$ns)
   if ($node) { $null = $node.RemoveAll() ; $xml.Save($_.FullName) }
}
