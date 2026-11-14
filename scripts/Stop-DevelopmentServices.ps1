#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"
foreach ($name in @('admin-frontend', 'provider-frontend', 'frontend', 'gateway', 'payment', 'ordering', 'cart', 'discovery', 'location', 'customer', 'catalog', 'provider', 'experience', 'organization', 'identity')) { Stop-DevelopmentProcess $name }
'Stopped the FindYourFood development processes tracked by this workspace.'
