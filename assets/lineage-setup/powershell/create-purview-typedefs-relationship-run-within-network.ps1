# This needs to run from within network that is connected to Synapse Managed Vnet

param (
    
    [String] $tenantId,
    [String] $clientId,
    [String] $clientSecret,
    [String] $purviewname


)
echo "This utility creates type definitions and entities required for Synapse lineage."



if ([string]::IsNullOrEmpty($tenantId)) {
    $tenantId = Read-Host "Azure Active Directory Tenant Id"
} 


if ([string]::IsNullOrEmpty($clientId)) {
    $clientId = Read-Host "Service Principal Client Id"
} 

if ([string]::IsNullOrEmpty($clientSecret)) {
    $clientSecret = Read-Host "Service Principal Client Secret"
} 

if ([string]::IsNullOrEmpty($purviewname)) {
    $purviewname = Read-Host "Purview Account Name"
} 


$tenantId="72f988bf-86f1-41af-91ab-2d7cd011db47"
$clientId="52257128-5989-4b8d-a3fb-7cc123d48f7a"
$clientSecret="vgb8Q~rQ29VV6xP2lg5u4xp3StsGxoC6y6E7Nc01"
$purviewname="anildwapurview-vnet"

$scope="https%3A%2F%2Fpurview.azure.net%2F.default" #do not modify this



# Acquire AAD token for the purview resource

$url = "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token"
$data = "grant_type=client_credentials&client_id=$clientId&client_secret=$clientSecret&scope=$scope"


$headers = @{
    'Content-Type' = 'application/x-www-form-urlencoded'
}

$authorization = Invoke-WebRequest -Uri $url -Method POST -Headers $headers -Body $data

if ($authorization.StatusCode -eq 200) {
    # request was successful
    $tokenResponse = $authorization.Content | ConvertFrom-Json
} else {
    # request failed
    $authorization.StatusCode
}

$token = $tokenResponse.access_token
# Create a hashtable with the headers for the request
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$url = "https://$purviewname.purview.azure.com/catalog/api/atlas/v2/types/typedefs"

# Create typedef for purview_custom_connector_generic_entity_with_columns
# Set the endpoint URL and JSON data file path
$jsonDataFile = (Split-Path $PSScriptRoot) + "\synapse-purview-types\purview_custom_connector_generic_entity_with_columns.json"

# Read the JSON data from the file
$jsonData = Get-Content -Raw -Path $jsonDataFile
# Send the POST request
$response = Invoke-RestMethod -Method POST -Uri $url -Headers $headers -Body $jsonData
if ($response.StatusCode -eq 200) {
    # request was successful
    $response.Content | ConvertFrom-Json
} else {
    # request failed
    $response.StatusCode
}



# Create typedef for Azure Synapse Notebook
# Set the endpoint URL and JSON data file path
$jsonDataFile = (Split-Path $PSScriptRoot) + "\synapse-purview-types\purview_synapse_notebook_typedef.json"
# Read the JSON data from the file
$jsonData = Get-Content -Raw -Path $jsonDataFile
# Send the POST request
$response = Invoke-RestMethod -Method POST -Uri $url -Headers $headers -Body $jsonData
if ($response.StatusCode -eq 200) {
    # request was successful
    $response.Content | ConvertFrom-Json
} else {
    # request failed
    $response.StatusCode
}


# Create relationship between Synapse Workspace and Synapse Notebook
# Set the endpoint URL and JSON data file path
$jsonDataFile = (Split-Path $PSScriptRoot) + "\synapse-purview-types\purview_synapse_workspace_notebook_relationship.json"
# Read the JSON data from the file
$jsonData = Get-Content -Raw -Path $jsonDataFile
# Send the POST request
$response = Invoke-RestMethod -Method POST -Uri $url -Headers $headers -Body $jsonData
if ($response.StatusCode -eq 200) {
    # request was successful
    $response.Content | ConvertFrom-Json
} else {
    # request failed
    $response.StatusCode
}


# Create relationship between Synapse Notebook and Process typedef
# Set the endpoint URL and JSON data file path
$jsonDataFile = (Split-Path $PSScriptRoot) + "\synapse-purview-types\purview_synapse_notebook_process_basic_relationship.json"
# Read the JSON data from the file
$jsonData = Get-Content -Raw -Path $jsonDataFile
# Send the POST request
$response = Invoke-RestMethod -Method POST -Uri $url -Headers $headers -Body $jsonData
if ($response.StatusCode -eq 200) {
    # request was successful
    $response.Content | ConvertFrom-Json
} else {
    # request failed
    $response.StatusCode
}
