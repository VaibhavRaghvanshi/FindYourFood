#requires -Version 7.0
. "$PSScriptRoot/Development.ps1"
foreach ($name in @('admin-frontend', 'frontend', 'gateway', 'organization', 'identity')) { Stop-DevelopmentProcess $name }
'Stopped the FindYourFood development processes tracked by this workspace.'
