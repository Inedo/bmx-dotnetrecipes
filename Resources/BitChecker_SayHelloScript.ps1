<#
.SYNOPSIS
A sample script that pings inedo.com. This script may be deleted from the Script Repository (under Admin) as needed.

.PARAMETER License Key
Your license key; so we know who's greeting us.

.PARAMETER Your Name
Your first name.
#>
param([string]${License Key}, [string]${Your Name})

Write-Output "Saying hello to inedo.com..."

$webClient = New-Object System.Net.WebClient
$data = New-Object System.Collections.Specialized.NameValueCollection

$data.Add('licenseKey', ${License Key})

If(${Your Name}) {
    $data.Add('name', ${Your Name})
}

Try {
    $response = $webClient.UploadValues('http://inedo.com/bm/hello', $data)
    $responseText = [System.Text.Encoding]::UTF8.GetString($response)
    Write-Output "Responded with: $responseText"
} Catch [System.Net.WebException] {
    Write-Warning "Couldn't connect to inedo.com to say hello."
}