# This needs to run from within network that is connected to Synapse Managed Vnet

param (
    
    [String] $tenantId,
    [String] $clientId,
    [String] $synapseworkspacename


)

if ([string]::IsNullOrEmpty($tenantId)) {
    $tenantId = Read-Host "Azure Active Directory Tenant Id"
} 


if ([string]::IsNullOrEmpty($clientId)) {
    $clientId = Read-Host "Service Principal Client Id"
} 

if ([string]::IsNullOrEmpty($synapseworkspacename)) {
    $synapseworkspacename = Read-Host "Synapse Workspace Name"
} 

echo "This utility assigns synapse role definitions so that the lineage API can retrieve additional details on Spark jobs to populate Purview."

$resource="https://dev.azuresynapse.net/"


# Acqiure Access token using az cli. This does not require client secret.
$token=(az account get-access-token --resource $resource --tenant $tenantId | ConvertFrom-Json).accessToken
$objectid=(az ad sp show --id $clientId | ConvertFrom-Json).id




# Assign synapse monitoring operator role definition to service principal
echo "Assigning synapse monitoring operator role definition to service principal..."
$roleassignmentId=[guid]::NewGuid().ToString()
$url = "https://${synapseworkspacename}.dev.azuresynapse.net/roleAssignments/${roleassignmentId}?api-version=2020-12-01"
$data = @{
    'roleId' = '8f9b2195-5b12-4a7c-af30-8f1f46197650'
    'principalId' = $objectid
    'scope' = 'workspaces/' + ${synapseworkspacename}
    'principalType' = 'ServicePrincipal'
}

$json = $data | ConvertTo-Json

$headers = @{
    'Authorization' = "Bearer $token"
    'Content-Type' = 'application/json'
}

$response = Invoke-WebRequest -Uri $url -Method Put -Headers $headers -Body $json

if ($response.StatusCode -eq 200) {
    # request was successful
    $response.Content | ConvertFrom-Json
} else {
    # request failed
    $response.StatusCode
}


# Assign synapse apache spark administrator role definition to service principal
$roleassignmentId=[guid]::NewGuid().ToString()
$url = "https://${synapseworkspacename}.dev.azuresynapse.net/roleAssignments/${roleassignmentId}?api-version=2020-12-01"
$data = @{
    'roleId' = 'c3a6d2f1-a26f-4810-9b0f-591308d5cbf1'
    'principalId' = $objectid
    'scope' = 'workspaces/' + ${synapseworkspacename}
    'principalType' = 'ServicePrincipal'
}

$json = $data | ConvertTo-Json

$headers = @{
    'Authorization' = "Bearer $token"
    'Content-Type' = 'application/json'
}

$response = Invoke-WebRequest -Uri $url -Method Put -Headers $headers -Body $json

if ($response.StatusCode -eq 200) {
    # request was successful
    $response.Content | ConvertFrom-Json
} else {
    # request failed
    $response.StatusCode
}








